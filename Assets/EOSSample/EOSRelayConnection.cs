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
    public class EOSRelayConnection : IRelayConnection
    {
        private EOSConnection connection;
        private P2PInterface P2PHandle;
        
        private readonly Queue<ArraySegment<byte>> messagesFromSteamToServer = new Queue<ArraySegment<byte>>();

        public EOSRelayConnection(EOSConnection connection, P2PInterface P2PHandle)
        {
            this.connection = connection;
            this.P2PHandle = P2PHandle;
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
        ~EOSRelayConnection()
        {
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }
#endif
        
        public void OnConnectionOpened()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif
            var options = new AcceptConnectionOptions()
            {
                LocalUserId = EOSManager.Instance.GetProductUserId(),
                RemoteUserId = connection.User,
                SocketId = connection.SocketId,
            };

            var result = P2PHandle.AcceptConnection(ref options);

            if (result != Result.Success)
            {
                Debug.LogError($"{nameof(EOSRelayConnection)} connection failed to be accepted with {connection.User} failed with result: {result}");
            }
        }

        public void OnConnectionClosed()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
#endif
            var options = new CloseConnectionOptions()
            {
                LocalUserId = EOSManager.Instance.GetProductUserId(),
                RemoteUserId = connection.User,
                SocketId = connection.SocketId,
            };

            var result = P2PHandle.CloseConnection(ref options);
            
            if (result != Result.Success)
            {
                Debug.LogError($"{nameof(EOSRelayConnection)} connection failed to be closed with {connection.User} failed with result: {result}");
            }
            
            messagesFromSteamToServer.Clear();
        }

        public void ReceiveMessagesFromClient(List<ArraySegment<byte>> packetBuffer)
        {
            // Transfer packets to the coherence replication server
            while (messagesFromSteamToServer.Count > 0)
            {
                var packetData = messagesFromSteamToServer.Dequeue();
                packetBuffer.Add(packetData);
            }
        }

        public void SendMessageToClient(ReadOnlySpan<byte> packetData)
        {
            var segmentData = new ArraySegment<byte>(packetData.ToArray());
            
            var sendPacketOptions = new SendPacketOptions()
            {
                LocalUserId = EOSManager.Instance.GetProductUserId(),
                RemoteUserId = connection.User,
                SocketId = connection.SocketId,
                Data = segmentData,
                Reliability = PacketReliability.UnreliableUnordered,
                Channel = 0
            };

            var result = P2PHandle.SendPacket(ref sendPacketOptions);
            
            if (result != Result.Success)
            {
                Debug.LogError($"{nameof(EOSRelayConnection)} sending message to {connection.User} failed with result: {result}");
            }
        }

        public void EnqueueMessageFromEos(ArraySegment<byte> packetData)
        {
            messagesFromSteamToServer.Enqueue(packetData);
        }
    }
}
#endif