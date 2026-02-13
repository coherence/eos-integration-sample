using System;
using System.IO;
using Coherence;
using Coherence.Connection;
using Coherence.Toolkit;
using Coherence.Toolkit.ReplicationServer;
using UnityEngine;
using Coherence.Log;
using Coherence.Transport;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Connect;
using PlayEveryWare.EpicOnlineServices;
using CopyIdTokenOptions = Epic.OnlineServices.Auth.CopyIdTokenOptions;
using IdToken = Epic.OnlineServices.Auth.IdToken;
using Logger = Coherence.Log.Logger;

namespace EosSample
{
    public class CoherenceEOSManager : MonoBehaviour
    {
        [Tooltip("Enter the port where you are hosting your Dev Auth Tool")]
        public uint devAuthToolPort = 8888;
        
        public ProductUserId EosUserId { get; private set; }
        
        public bool IsConnected => bridge.IsConnected;
        public bool IsLoggingInWithEpic { get; private set; }

        CoherenceBridge bridge;
        EndpointData endpointData;
        IReplicationServer replicationServer;

        private static readonly Logger rsLogger = Log.GetLogger<ReplicationServer>();
        
        private IdToken? AuthToken = null;

        void Start()
        {
            // Make sure the scene contains a CoherenceBridge
            if (!CoherenceBridgeStore.TryGetBridge(gameObject.scene, out bridge))
            {
                throw new Exception("Could not find a CoherenceBridge in the scene.");
            }

            rsLogger.UseWatermark = false;

            // Listen for connection events
            bridge.onDisconnected.AddListener(OnDisconnected);
            bridge.onConnectionError.AddListener(OnConnectionError);

            // Create an endpoint for the replication server (default is 127.0.0.1:32001)
            // You can configure the replication server endpoint from the coherence Project Settings
            InitEndpoint();
        }

        void InitEndpoint()
        {
            endpointData = new EndpointData
            {
                host = RuntimeSettings.Instance.LocalHost,
                port = RuntimeSettings.Instance.LocalWorldUDPPort,
                region = EndpointData.LocalRegion,
                schemaId = RuntimeSettings.Instance.SchemaID,
            };

            // Validate the endpoint
            var result = endpointData.ValidateLocalAddress();
            var error = endpointData.GetErrorMessage(result);
            if ((result & EndpointData.ValidationResult.ValidLocalEndpoint) != EndpointData.ValidationResult.ValidLocalEndpoint)
            {
                throw new Exception($"Invalid {nameof(EndpointData)}: {error}");
            }
        }

        public void LoginWithEpic(string devAuthName)
        {
            // Logging in with Epic requires a Dev Auth Tool to be running
            if (string.IsNullOrEmpty(devAuthName))
            {
                throw new Exception("You need to enter your Dev Auth Tool credentials name in the inspector.");
            }

            IsLoggingInWithEpic = true;
            
            EOSManager.Instance.StartLoginWithLoginTypeAndToken(LoginCredentialType.Developer, $"localhost:{devAuthToolPort}", devAuthName, callbackInfo =>
            {
                Debug.Log($"LoginWithDevAuth Complete: {callbackInfo.ResultCode}");
                if (callbackInfo.ResultCode == Result.Success)
                {
                    StartConnectLoginWithEpicAccount(callbackInfo.LocalUserId);
                }
                else if (callbackInfo.ResultCode == Result.InvalidUser)
                {
                    EOSManager.Instance.StartLoginWithLoginTypeAndToken(LoginCredentialType.AccountPortal,
                        string.Empty,
                        string.Empty,
                        (info) =>
                        {
                            if (info.ResultCode == Result.Success)
                            {
                                StartConnectLoginWithEpicAccount(callbackInfo.LocalUserId);
                                Debug.Log($"Connect log in was successful: {info.ResultCode}");
                            }
                            else
                            {
                                Debug.LogError($"Failed to log in with Epic: {info.ResultCode}");
                            }
                        } );
                }
                else
                {
                    IsLoggingInWithEpic = false;
                    Debug.LogError($"Failed to log in with Epic: {callbackInfo.ResultCode}");
                }
            });
        }
        
        private void StartConnectLoginWithEpicAccount(EpicAccountId epicAccountId)
        {
            EOSManager.Instance.StartConnectLoginWithEpicAccount(epicAccountId, info =>
            {
                IsLoggingInWithEpic = false;

                if (info.ResultCode == Result.Success)
                {
                    EosUserId = info.LocalUserId;
                    Debug.Log($"Log in was successful: {info.ResultCode}");
                    GetAuthToken();
                }
                else
                {
                    if (info.ResultCode == Result.InvalidUser && info.ContinuanceToken != null)
                    {
                        EOSManager.Instance.CreateConnectUserWithContinuanceToken(info.ContinuanceToken, OnCreateUser);    
                    }
                    else
                    {
                        Debug.LogError($"Failed to log in to Epic. {info.ResultCode}");    
                    }
                }
            });
        }

        private void OnCreateUser(CreateUserCallbackInfo info)
        {
            if (info.ResultCode == Result.Success)
            {
                EosUserId = info.LocalUserId;
                Debug.Log($"Log in using continuance token was successful: {info.ResultCode}");
                GetAuthToken();
            }
            else
            {
                Debug.LogError($"Failed to log in to Epic using continuation token. {info.ResultCode}");
            }
        }
        
        private void GetAuthToken()
        {
            var accountId = EOSManager.Instance.GetLocalUserId();
            AuthInterface authHandle = EOSManager.Instance.GetEOSPlatformInterface().GetAuthInterface();
            Debug.Log($"Fetching auth token for account (player) ID: {accountId}");
            var options = new CopyIdTokenOptions() 
            {
                AccountId = accountId,
            };
            var result= authHandle.CopyIdToken(ref options, out AuthToken);
            
            if (result != Result.Success)
            {
                Debug.LogError("(GetAuthToken): failed to copy local user id token");
                return;
            }
            
            Debug.Log(AuthToken.Value.JsonWebToken);
        }

        void OnDisable()
        {
            // Cleanup
            if (bridge)
            {
                bridge.Disconnect();
            }
        }

        void OnDestroy()
        {
            if (bridge)
            {
                bridge.Disconnect();
            }
            
            if (replicationServer != null)
            {
                replicationServer.Stop();
            }
        }

        public void JoinGame(ProductUserId hostUserId)
        {
            Debug.Log($"Joining game with ProductUserId #{hostUserId}");

            // Make sure we are not already in a game or joining a game
            if (bridge.IsConnected || bridge.IsConnecting)
            {
                throw new Exception("Failed to join game, CoherenceBridge is already connected.");
            }

            // Connect to Replication Server via Epic relay
            bridge.SetTransportFactory(new EOSTransportFactory(hostUserId));
            bridge.Connect(endpointData);
        }

        [ContextMenu("Host Game")]
        public void HostGame()
        {
            if (EosUserId == null)
            {
                throw new Exception("Cannot host a game if you're not logged in with Epic");
            }
            
            StartReplicationServer();

            Debug.Log($"Hosting game with ProductUserId #{EosUserId}");

            // Make sure we are not already hosting or joining a game
            if (bridge.IsConnected || bridge.IsConnecting)
            {
                throw new Exception("Failed to host game, CoherenceBridge is already connected.");
            }

            // Init Eos Relay
            bridge.SetRelay(new EOSRelay());
            
            // Connect to Replication Server using the normal UDP transport
            bridge.SetTransportFactory(new DefaultTransportFactory());
            bridge.Connect(endpointData);
        }

        [ContextMenu("Disconnect")]
        public void Disconnect()
        {
            Debug.Log("Disconnecting");

            if (!bridge.IsConnected && !bridge.IsConnecting)
            {
                throw new Exception("Failed to disconnect, CoherenceBridge is not connected");
            }

            bridge.Disconnect();

            if (replicationServer != null)
            {
                StopReplicationServer();
            }
        }

        void OnDisconnected(CoherenceBridge _, ConnectionCloseReason reason)
        {
            Debug.Log($"CoherenceBridge OnDisconnected: {reason}");
        }

        void OnConnectionError(CoherenceBridge _, ConnectionException exception)
        {
            Debug.LogError($"CoherenceBridge OnConnectionError: {exception}");
        }

        void StartReplicationServer()
        {
            if (replicationServer != null)
            {
                Debug.LogWarning("The replication server is already running");
                return;
            }

            var config = new ReplicationServerConfig
            {
                Mode = Mode.World,
                APIPort = (ushort)RuntimeSettings.Instance.WorldsAPIPort,
                UDPPort = 32001,
                SignallingPort = 32002,
                SendFrequency = 20,
                ReceiveFrequency = 60,
                DisableThrottling = true,
            };

            var consoleLogDir = Path.GetDirectoryName(Application.consoleLogPath);
            var logFilePath = Path.Combine(consoleLogDir, "coherence-server");
            replicationServer = Launcher.Create(config, $"--log-file \"{logFilePath}\"");
            replicationServer.OnLog += ReplicationServer_OnLog;
            replicationServer.OnExit += ReplicationServer_OnExit;
            replicationServer.Start();
        }

        void StopReplicationServer()
        {
            replicationServer.Stop();
            replicationServer = null;
        }

        void ReplicationServer_OnLog(string log)
        {
            rsLogger.Info(log);
        }

        void ReplicationServer_OnExit(int code)
        {
            Debug.Log($"Replication server exited with code {code}.");
            replicationServer = null;
        }
    }
}
