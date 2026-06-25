using System;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;

    [Header("Defaults")]
    public RoomConfig defaultConfig = new RoomConfig();

    public RoomConfig ActiveConfig { get; private set; }
    public IReadOnlyList<LobbyPlayer> LobbyPlayers => lobbyPlayers;

    public event Action OnLobbyChanged;
    public event Action OnGameStarting;

    private readonly List<LobbyPlayer> lobbyPlayers = new List<LobbyPlayer>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        ActiveConfig = CloneConfig(defaultConfig);
    }

    public void CreateRoom(int playerCount, int roundLimit, int treasureTarget)
    {
        ActiveConfig = CloneConfig(defaultConfig);
        ActiveConfig.maxPlayers = Mathf.Clamp(playerCount, 2, 12);
        ActiveConfig.maxRounds = Mathf.Max(1, roundLimit);
        ActiveConfig.treasureGoal = Mathf.Max(1, treasureTarget);
        lobbyPlayers.Clear();
        OnLobbyChanged?.Invoke();
    }

    public void UpdateConfig(RoomConfig config)
    {
        ActiveConfig = CloneConfig(config);
        OnLobbyChanged?.Invoke();
    }

    public bool TryAddPlayer(ulong clientId, string playerName)
    {
        if (FindLobbyPlayer(clientId) != null)
            return true;

        if (lobbyPlayers.Count >= ActiveConfig.maxPlayers)
            return false;

        lobbyPlayers.Add(new LobbyPlayer
        {
            clientId = clientId,
            playerName = playerName,
            isReady = false
        });
        OnLobbyChanged?.Invoke();
        return true;
    }

    public void RemovePlayer(ulong clientId)
    {
        lobbyPlayers.RemoveAll(p => p.clientId == clientId);
        OnLobbyChanged?.Invoke();
    }

    public void SetPlayerReady(ulong clientId, bool ready)
    {
        var player = FindLobbyPlayer(clientId);
        if (player == null)
            return;

        player.isReady = ready;
        OnLobbyChanged?.Invoke();
    }

    public bool CanStartGame(bool requireAllReady)
    {
        if (lobbyPlayers.Count < 2)
            return false;

        if (!requireAllReady)
            return true;

        foreach (var player in lobbyPlayers)
        {
            if (!player.isReady)
                return false;
        }

        return true;
    }

    public List<PlayerData> AssignRoles()
    {
        var roster = new List<PlayerData>();
        int playerCount = lobbyPlayers.Count;
        int thiefTotal = Mathf.Clamp(ActiveConfig.thiefCount, 1, Mathf.Max(1, playerCount / 4));
        int corruptTotal = Mathf.Clamp(ActiveConfig.corruptPoliceCount, 0, playerCount - thiefTotal);
        int policeTotal = playerCount - thiefTotal - corruptTotal;

        var shuffled = new List<LobbyPlayer>(lobbyPlayers);
        Shuffle(shuffled);

        int index = 0;
        for (int i = 0; i < policeTotal; i++, index++)
        {
            var lobbyPlayer = shuffled[index];
            var player = new PlayerData(lobbyPlayer.playerName, PlayerRole.Police, i % ActiveConfig.policeTeamCount)
            {
                clientId = lobbyPlayer.clientId,
                slotIndex = index
            };
            roster.Add(player);
        }

        for (int i = 0; i < corruptTotal; i++, index++)
        {
            var lobbyPlayer = shuffled[index];
            var player = new PlayerData(lobbyPlayer.playerName, PlayerRole.CorruptPolice, i % ActiveConfig.policeTeamCount)
            {
                clientId = lobbyPlayer.clientId,
                slotIndex = index
            };
            roster.Add(player);
        }

        for (int i = 0; i < thiefTotal; i++, index++)
        {
            var lobbyPlayer = shuffled[index];
            var player = new PlayerData(lobbyPlayer.playerName, PlayerRole.Thief, -1)
            {
                clientId = lobbyPlayer.clientId,
                slotIndex = index
            };
            roster.Add(player);
        }

        return roster;
    }

    public void StartGame()
    {
        OnGameStarting?.Invoke();
    }

    public LobbyPlayer FindLobbyPlayer(ulong clientId)
    {
        return lobbyPlayers.Find(p => p.clientId == clientId);
    }

    public void ClearLobby()
    {
        lobbyPlayers.Clear();
        OnLobbyChanged?.Invoke();
    }

    static RoomConfig CloneConfig(RoomConfig source)
    {
        return new RoomConfig
        {
            maxPlayers = source.maxPlayers,
            maxRounds = source.maxRounds,
            treasureGoal = source.treasureGoal,
            stepsPerTurn = source.stepsPerTurn,
            policeTeamCount = source.policeTeamCount,
            regularPoliceCount = source.regularPoliceCount,
            corruptPoliceCount = source.corruptPoliceCount,
            thiefCount = source.thiefCount,
            mapSeed = source.mapSeed
        };
    }

    static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

[Serializable]
public class LobbyPlayer
{
    public ulong clientId;
    public string playerName;
    public bool isReady;
}
