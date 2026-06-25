using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct NetworkPlayerSlot : INetworkSerializable, IEquatable<NetworkPlayerSlot>
{
    public ulong ClientId;
    public FixedString32Bytes PlayerName;
    public bool IsReady;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref IsReady);
    }

    public bool Equals(NetworkPlayerSlot other)
    {
        return ClientId == other.ClientId &&
               PlayerName.Equals(other.PlayerName) &&
               IsReady == other.IsReady;
    }
}

public class NetworkRoomManager : NetworkBehaviour
{
    public static NetworkRoomManager Instance;

    private NetworkList<NetworkPlayerSlot> connectedPlayers;
    private NetworkVariable<int> mapSeed = new NetworkVariable<int>(0);
    private NetworkVariable<bool> gameStarted = new NetworkVariable<bool>(false);

    public event Action OnLobbyUpdated;
    public event Action<int> OnNetworkGameStarted;

    void Awake()
    {
        connectedPlayers = new NetworkList<NetworkPlayerSlot>();
    }

    public override void OnNetworkSpawn()
    {
        Instance = this;
        connectedPlayers.OnListChanged += HandleLobbyChanged;
        gameStarted.OnValueChanged += HandleGameStartedChanged;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;

        connectedPlayers.OnListChanged -= HandleLobbyChanged;
        gameStarted.OnValueChanged -= HandleGameStartedChanged;
    }

    void HandleClientDisconnect(ulong clientId)
    {
        if (!IsServer)
            return;

        for (int i = connectedPlayers.Count - 1; i >= 0; i--)
        {
            if (connectedPlayers[i].ClientId == clientId)
                connectedPlayers.RemoveAt(i);
        }

        if (RoomManager.Instance != null)
            RoomManager.Instance.RemovePlayer(clientId);
    }

    void HandleLobbyChanged(NetworkListEvent<NetworkPlayerSlot> changeEvent)
    {
        SyncLobbyToRoomManager();
        OnLobbyUpdated?.Invoke();
    }

    void HandleGameStartedChanged(bool previous, bool current)
    {
        if (current)
            OnNetworkGameStarted?.Invoke(mapSeed.Value);
    }

    void SyncLobbyToRoomManager()
    {
        if (RoomManager.Instance == null)
            return;

        RoomManager.Instance.ClearLobby();
        foreach (var slot in connectedPlayers)
        {
            RoomManager.Instance.TryAddPlayer(slot.ClientId, slot.PlayerName.ToString());
            RoomManager.Instance.SetPlayerReady(slot.ClientId, slot.IsReady);
        }
    }

    public IReadOnlyList<NetworkPlayerSlot> GetConnectedPlayers()
    {
        var list = new List<NetworkPlayerSlot>();
        foreach (var slot in connectedPlayers)
            list.Add(slot);
        return list;
    }

    public void RegisterLocalHostPlayer(string playerName)
    {
        if (!IsServer)
            return;

        RegisterPlayerServer(NetworkManager.Singleton.LocalClientId, playerName);
    }

    [ServerRpc(RequireOwnership = false)]
    public void JoinRoomServerRpc(FixedString32Bytes playerName, ServerRpcParams rpcParams = default)
    {
        RegisterPlayerServer(rpcParams.Receive.SenderClientId, playerName.ToString());
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetReadyServerRpc(bool ready, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        for (int i = 0; i < connectedPlayers.Count; i++)
        {
            if (connectedPlayers[i].ClientId != clientId)
                continue;

            var slot = connectedPlayers[i];
            slot.IsReady = ready;
            connectedPlayers[i] = slot;
            if (RoomManager.Instance != null)
                RoomManager.Instance.SetPlayerReady(clientId, ready);
            return;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer || gameStarted.Value)
            return;

        if (rpcParams.Receive.SenderClientId != NetworkManager.Singleton.LocalClientId)
            return;

        if (RoomManager.Instance == null || GameManager.Instance == null)
            return;

        if (!RoomManager.Instance.CanStartGame(requireAllReady: false))
            return;

        mapSeed.Value = UnityEngine.Random.Range(1, int.MaxValue);
        var roster = RoomManager.Instance.AssignRoles();
        gameStarted.Value = true;
        NetworkGameController.Instance.BeginNetworkGameClientRpc(mapSeed.Value, BuildRosterPayload(roster));
    }

    void RegisterPlayerServer(ulong clientId, string playerName)
    {
        if (RoomManager.Instance == null)
            return;

        if (!RoomManager.Instance.TryAddPlayer(clientId, playerName))
            return;

        for (int i = 0; i < connectedPlayers.Count; i++)
        {
            if (connectedPlayers[i].ClientId == clientId)
                return;
        }

        connectedPlayers.Add(new NetworkPlayerSlot
        {
            ClientId = clientId,
            PlayerName = playerName,
            IsReady = false
        });
    }

    NetworkRosterEntry[] BuildRosterPayload(List<PlayerData> roster)
    {
        var payload = new NetworkRosterEntry[roster.Count];
        for (int i = 0; i < roster.Count; i++)
        {
            payload[i] = new NetworkRosterEntry
            {
                ClientId = roster[i].clientId,
                PlayerName = roster[i].playerName,
                Role = (int)roster[i].role,
                TeamIndex = roster[i].teamIndex
            };
        }
        return payload;
    }
}
