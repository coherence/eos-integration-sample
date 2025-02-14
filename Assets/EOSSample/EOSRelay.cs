#if !EOS_DISABLE
using System;
using System.Collections.Generic;
using Coherence.Toolkit.Relay;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;

namespace EosSample
{
    public class EOSRelay : IRelay
    {
        private readonly Dictionary<string, EOSRelayConnection> connectionMap = new();

        public CoherenceRelayManager RelayManager { get; set; }

        private P2PInterface P2PHandle;
        private ulong addId;
        private ulong closeId;
        
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
        ~EOSRelay()
        {
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }
#endif
        
        public void Open()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif
            P2PHandle = EOSManager.Instance.GetEOSP2PInterface();
            EOSManager.Instance.AddApplicationCloseListener(Close);
          
            var addNotifyPeerConnectionRequestOptions = new AddNotifyPeerConnectionRequestOptions()
            {
                LocalUserId = EOSManager.Instance.GetProductUserId(),
                SocketId = null, // Notify us about all connection requests to our local user regardless of socket name.
            };
          
            addId = P2PHandle.AddNotifyPeerConnectionRequest(ref addNotifyPeerConnectionRequestOptions, null, OnConnectionRequestNotification);
            
            var addNotifyPeerConnectionClosedOptions = new AddNotifyPeerConnectionClosedOptions()
            {
                LocalUserId = EOSManager.Instance.GetProductUserId(),
                SocketId = null, // Notify us about all connection requests to our local user regardless of socket name.
            };
            
            closeId = P2PHandle.AddNotifyPeerConnectionClosed(ref addNotifyPeerConnectionClosedOptions, null, OnConnectionClosedNotification);
            Debug.Log("EOS Relay Opened");
        }

        public void Close()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
#endif
            P2PHandle?.RemoveNotifyPeerConnectionRequest(addId);
            P2PHandle?.RemoveNotifyPeerConnectionClosed(closeId);
        }

        public void Update()
        {
            if (P2PHandle == null)
            {
                return;
            }
            
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
                    
                P2PHandle.GetNextReceivedPacketSize(ref getNextReceivedPacketSizeOptions, out uint nextPacketSizeBytes);

                if (nextPacketSizeBytes == 0)
                {
                    return;
                }
            
                var packet = new byte[nextPacketSizeBytes];
                var dataSegment = new ArraySegment<byte>(packet);
                
                ProductUserId remoteUserId = null;
                SocketId socketId = default;
                //TODO: verify that this still works
                Result result = P2PHandle.ReceivePacket(ref receivePacketOptions, ref remoteUserId, ref socketId, out var channel, dataSegment, out uint bytesWritten);
       
                // No packets to be received?
                if (result == Result.NotFound)
                {
                    return;
                }

                if (!connectionMap.TryGetValue(remoteUserId.ToString(), out var relayConnection))
                {
                    Debug.LogError($"{nameof(EOSRelay)} Failed to find client for connection with Id: {remoteUserId.ToString()}");
                    return;
                }
            
                relayConnection.EnqueueMessageFromEos(dataSegment);
            }
        }
        
        private void OnConnectionRequestNotification(ref OnIncomingConnectionRequestInfo data)
        {
            var socketName = data.SocketId?.SocketName;
            var remoteUserId = data.RemoteUserId;

            var connection = new EOSConnection()
            {
                SocketId = data.SocketId,
                User = remoteUserId
            };

            var relayConnection = new EOSRelayConnection(connection, P2PHandle);
            RelayManager.OpenRelayConnection(relayConnection);

            connectionMap.Add(remoteUserId.ToString(), relayConnection);
            
            Debug.Log($"EOSRelay.OnConnectionRequestNotification: Attempting to remotely open (incoming) socket connection named '{socketName}' with remote peer '{remoteUserId}'...");
        }
        
        private void OnConnectionClosedNotification(ref OnRemoteConnectionClosedInfo data)
        {
            if (!connectionMap.TryGetValue(data.RemoteUserId.ToString(), out var relayConnection))
            {
                Debug.LogError($"{nameof(EOSRelay)} Failed to find client for connection with Id: {data.RemoteUserId.ToString()}");
                return;
            }
            
            RelayManager.CloseAndRemoveRelayConnection(relayConnection);
            connectionMap.Remove(data.RemoteUserId.ToString());
        }
    }
}
#endif