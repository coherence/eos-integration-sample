using Coherence.Toolkit;
using UnityEngine;

public class PlayerFactory : MonoBehaviour
{
    [SerializeField] private CoherenceBridge bridge;
    [SerializeField] private GameObject playerPrefab;
    
    void Start()
    {
        bridge.onConnected.AddListener(_ =>
        {
            Debug.Log("Connected to Coherence, spawning player.");
            Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        });
    }
}
