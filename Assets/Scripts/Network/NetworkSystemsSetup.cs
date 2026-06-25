using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class NetworkSystemsSetup : MonoBehaviour
{
    public static NetworkSystemsSetup Instance;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        if (GetComponent<NetworkObject>() == null)
            gameObject.AddComponent<NetworkObject>();
        if (GetComponent<NetworkRoomManager>() == null)
            gameObject.AddComponent<NetworkRoomManager>();
        if (GetComponent<NetworkGameController>() == null)
            gameObject.AddComponent<NetworkGameController>();
    }

    public void EnsureSpawned()
    {
        var networkObject = GetComponent<NetworkObject>();
        if (networkObject != null && !networkObject.IsSpawned && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            networkObject.Spawn();
    }
}
