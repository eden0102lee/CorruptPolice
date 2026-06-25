using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkBootstrap : MonoBehaviour
{
    public static NetworkBootstrap Instance;

    [Header("Transport")]
    public ushort port = 7777;
    public string listenAddress = "0.0.0.0";

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
        EnsureNetworkManager();
    }

    public bool IsConnected =>
        NetworkManager.Singleton != null &&
        (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost);

    public bool IsHost =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

    public ulong LocalClientId =>
        NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : ulong.MaxValue;

    void EnsureNetworkManager()
    {
        if (NetworkManager.Singleton != null)
            return;

        var networkObject = new GameObject("NetworkManager");
        networkObject.transform.SetParent(transform);

        var manager = networkObject.AddComponent<NetworkManager>();
        var transport = networkObject.AddComponent<UnityTransport>();
        manager.NetworkConfig = new NetworkConfig
        {
            NetworkTransport = transport,
            EnableSceneManagement = false
        };

        DontDestroyOnLoad(networkObject);
    }

    UnityTransport GetTransport()
    {
        EnsureNetworkManager();
        return NetworkManager.Singleton.GetComponent<UnityTransport>();
    }

    public bool StartHostGame()
    {
        EnsureNetworkManager();
        if (NetworkManager.Singleton.IsListening)
            return true;

        var transport = GetTransport();
        transport.SetConnectionData(listenAddress, port);

        bool started = NetworkManager.Singleton.StartHost();
        if (started)
        {
            EnsureNetworkSystems();
            Debug.Log($"Host started on port {port}");
        }
        else
            Debug.LogError("Failed to start host.");

        return started;
    }

    public bool StartClientGame(string address)
    {
        EnsureNetworkManager();
        if (NetworkManager.Singleton.IsListening)
            return true;

        if (string.IsNullOrWhiteSpace(address))
            address = "127.0.0.1";

        var transport = GetTransport();
        transport.SetConnectionData(address.Trim(), port);

        bool started = NetworkManager.Singleton.StartClient();
        if (started)
        {
            EnsureNetworkSystems();
            Debug.Log($"Client connecting to {address}:{port}");
        }
        else
            Debug.LogError("Failed to start client.");

        return started;
    }

    void EnsureNetworkSystems()
    {
        if (NetworkSystemsSetup.Instance == null)
        {
            var systemsObject = new GameObject("NetworkSystems");
            systemsObject.AddComponent<NetworkSystemsSetup>();
        }

        NetworkSystemsSetup.Instance.EnsureSpawned();
    }

    public void Shutdown()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();
    }
}
