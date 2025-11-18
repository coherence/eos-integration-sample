#if !EOS_DISABLE
using System;
using System.Collections.Generic;
using System.Net;
using Coherence.Brook;
using Coherence.Brook.Octet;
using Coherence.Common;
using Coherence.Connection;
using Coherence.Stats;
using Coherence.Transport;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;
using Logger = Coherence.Log.Logger;

namespace EosSample
{
    public class EOSTransport : ITransport
    {
        public ProductUserId HostEosId;

        public event Action OnOpen;
        public event Action<ConnectionException> OnError;
        public TransportState State { get; private set; }
        public bool IsReliable => false;
        public bool CanSend => true;
        public int HeaderSize { get; }
        public string Description => "EOS Transport";

        private P2PInterface P2PHandle;
        private string P2PSocketName = "EOSP2PTransport";

        private SocketId socketId = new SocketId();
        private ulong establishedId;
        private ulong leftId;
        private bool isClosing;

        private IStats stats;
        
        private readonly Queue<byte[]> incomingPackets = new();

        public EOSTransport(IStats stats, ProductUserId hostId)
        {
            HostEosId = hostId;
            this.stats = stats;
        }
        
#if UNITY_EDITOR
        void OnPlayModeChanged(UnityEditor.PlayModeStateChange modeChange)
        {
            if (modeChange == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                //prevent attempts to call native EOS code while exiting play mode, which crashes the editor
                P2PHandle = null;
            }
        }
#endif

#if UNITY_EDITOR
        ~EOSTransport()
        {
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }
#endif
        
        public void Open(EndpointData endpoint, ConnectionSettings settings)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif
            socketId.SocketName = P2PSocketName;
            
            var options = new AcceptConnectionOptions()
            {
                LocalUserId = EOSManager.Instance.GetProductUserId(),
                RemoteUserId = HostEosId,
                SocketId = socketId,
            };
            
            P2PHandle = EOSManager.Instance.GetEOSP2PInterface();
            P2PHandle.AcceptConnection(ref options);
            State = TransportState.Opening;
            Debug.Log($"Opening Transport with {HostEosId.ToString()}");
            var addNotifyPeerConnectionEstablishedOptions = new AddNotifyPeerConnectionEstablishedOptions()
            {
                LocalUserId = EOSManager.Instance.GetProductUserId(),
                SocketId = null, // Notify us about all connection requests to our local user regardless of socket name.
            };

            establishedId = P2PHandle.AddNotifyPeerConnectionEstablished(ref addNotifyPeerConnectionEstablishedOptions, null, ConnectionEstablishedHandler);
            
            var addNotifyPeerConnectionClosedOptions = new AddNotifyPeerConnectionClosedOptions()
            {
                LocalUserId = EOSManager.Instance.GetProductUserId(),
                SocketId = null, // Notify us about all connection requests to our local user regardless of socket name.
            };
            
            leftId = P2PHandle.AddNotifyPeerConnectionClosed(ref addNotifyPeerConnectionClosedOptions, null, ConnectionClosedHandler);

            OnOpen?.Invoke();
        }

        private void ConnectionClosedHandler(ref OnRemoteConnectionClosedInfo data)
        {
            if (!data.RemoteUserId.ToString().Equals(HostEosId.ToString())) return;
            
            var msg = $"Host has left the game. Disconnecting";
            Debug.LogError(msg);
            OnError?.Invoke(new ConnectionException(msg));
        }

        private void ConnectionEstablishedHandler(ref OnPeerConnectionEstablishedInfo data)
        {
            State = TransportState.Open;
            P2PHandle.RemoveNotifyPeerConnectionEstablished(establishedId);
            Debug.Log($"CONFIRMING CONNECTION ESTABLISHED");
        }

        public void Close()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
#endif
            var options = new CloseConnectionOptions()
            {
                LocalUserId = EOSManager.Instance.GetProductUserId(),
                RemoteUserId = HostEosId,
                SocketId = socketId,
            };

            _ = P2PHandle?.CloseConnection(ref options);

            State = TransportState.Closed;
            P2PHandle?.RemoveNotifyPeerConnectionEstablished(establishedId);
            P2PHandle?.RemoveNotifyPeerConnectionClosed(leftId);
        }

        public void Send(IOutOctetStream stream)
        {
            var buffer = stream.Close();

            var sendPacketOptions = new SendPacketOptions()
            {
                LocalUserId = EOSManager.Instance.GetProductUserId(),
                RemoteUserId = HostEosId,
                SocketId = socketId,
                Data = buffer,
                Reliability = isClosing ? PacketReliability.ReliableUnordered : PacketReliability.UnreliableUnordered,
                Channel = 0
            };

            var result = P2PHandle.SendPacket(ref sendPacketOptions);
            
            if (result != Result.Success)
            {
                var msg = $"{nameof(EOSRelayConnection)} sending message to {HostEosId} failed with result: {result}";
                Debug.LogError(msg);

                if (!isClosing)
                {
                    OnError?.Invoke(new ConnectionException(msg));
                }
                return;
            }
            
            stats.TrackOutgoingPacket(stream.Position);
        }

        public void Receive(List<(IInOctetStream, IPEndPoint)> buffer)
        {
            GetNextPackets();
            
            while (incomingPackets.Count > 0)
            {
                var packet = incomingPackets.Dequeue();
                var stream = new InOctetStream(packet);
                buffer.Add((stream, default));
                stats.TrackIncomingPacket((uint)stream.RemainingOctetCount);
            }
        }

        private void GetNextPackets()
        {
            ReceivePacketOptions receivePacketOptions = new ReceivePacketOptions()
            {
                LocalUserId = EOSManager.Instance.GetProductUserId(),
                MaxDataSizeBytes = 4096,
                RequestedChannel = null
            };

            var getNextReceivedPacketSizeOptions = new GetNextReceivedPacketSizeOptions
            {
                LocalUserId = EOSManager.Instance.GetProductUserId(),
                RequestedChannel = null
            };

            var gotPacket = true;

            while (gotPacket)
            {
                gotPacket = false;
                P2PHandle.GetNextReceivedPacketSize(ref getNextReceivedPacketSizeOptions, out var nextPacketSizeBytes);

                if (nextPacketSizeBytes == 0)
                {
                    continue;
                }
            
                var packet = new byte[nextPacketSizeBytes];
                var dataSegment = new ArraySegment<byte>(packet);

                ProductUserId remoteUserId = null;
                SocketId socketId = default;

                //TODO: verify that this still works
                var result = P2PHandle.ReceivePacket(ref receivePacketOptions, ref remoteUserId, ref socketId, out var channel, dataSegment, out uint bytesWritten);

                // No packets to be received?
                if (result == Result.NotFound)
                {
                    continue;
                }
            
                gotPacket = true;
     
                incomingPackets.Enqueue(dataSegment.Array);
            }
        }

        public void PrepareDisconnect()
        {
            isClosing = true;
        }
    }

    public class EOSTransportFactory : ITransportFactory
    {
        private ProductUserId userId;
        
        public EOSTransportFactory(ProductUserId userIdToJoin)
        {
            userId = userIdToJoin;
        }

        public ITransport Create(ushort mtu, IStats stats, Logger logger)
        {
            return new EOSTransport(stats, userId);
        }
    }
}
#endif