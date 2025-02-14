using UnityEngine;
using Epic.OnlineServices;
using PlayEveryWare.EpicOnlineServices;

namespace EosSample
{
    [RequireComponent(typeof(CoherenceEOSManager))]
    public class EOSSampleUI : MonoBehaviour
    {
        private CoherenceEOSManager coherenceEosManager;

        private string productUserIdToJoin;
        private string devAuthToolName;

        void Awake()
        {
            coherenceEosManager = GetComponent<CoherenceEOSManager>();
        }

        void OnGUI()
        {
            if (coherenceEosManager.EosUserId == null)
            {
                if (coherenceEosManager.IsLoggingInWithEpic)
                {
                    GUILayout.Label("Logging in with Epic..");
                    return;
                }
                
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"Dev Auth Tool Name:");
                    devAuthToolName = GUILayout.TextArea(devAuthToolName, GUILayout.MinWidth(100));
                }
                GUILayout.EndHorizontal();
                     
                if (GUILayout.Button("Login With Epic"))
                {
                    coherenceEosManager.LoginWithEpic(devAuthToolName);
                }
         
                return;
            }
            
            if (coherenceEosManager.IsConnected)
            {
                GUILayout.Label("Connected");
                
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"My ProductUserId: {coherenceEosManager.EosUserId.ToString()}");
                    
                    if (GUILayout.Button("Copy To Clipboard"))
                    {
                        GUIUtility.systemCopyBuffer = coherenceEosManager.EosUserId.ToString();
                    }
                }
                
                GUILayout.EndHorizontal();
                
                if (GUILayout.Button("Disconnect"))
                {
                    coherenceEosManager.Disconnect();
                }
                return;
            }
            
            DrawMenu();
        }

        void DrawMenu()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("ProductUserId to join:");
                productUserIdToJoin = GUILayout.TextArea(productUserIdToJoin, GUILayout.MinWidth(100));
            }
            GUILayout.EndHorizontal();
            
            if (GUILayout.Button("Join") && !string.IsNullOrEmpty(productUserIdToJoin))
            {
                coherenceEosManager.JoinGame(ProductUserId.FromString(productUserIdToJoin));
            }

            if (GUILayout.Button("Host"))
            {
                coherenceEosManager.HostGame();
            }
        }
    }
}
