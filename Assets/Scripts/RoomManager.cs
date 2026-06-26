using System;
using System.Collections.Generic;
using UnityEngine;

public enum RoomState
{
    Lobby,
    InGame,
    Finished
}

/// <summary>
/// �ж��y�{�]���a���^�G�إߩж��B�]�w�B�[�J�B�}�l�C���C
/// �s�u�P�B�w�d������X�R�C
/// </summary>
public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;

    public GameConfig config = GameConfig.Default;
    public string roomCode;
    public RoomState State { get; private set; } = RoomState.Lobby;
    public bool IsHost { get; private set; } = true;

    public event Action OnGameStarted;

    private readonly List<string> joinedPlayers = new List<string>();
    private readonly List<string> spectators = new List<string>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        roomCode = GenerateRoomCode();
    }

    string GenerateRoomCode()
    {
        return UnityEngine.Random.Range(100000, 999999).ToString();
    }

    public void UpdateConfig(GameConfig newConfig)
    {
        config = newConfig;
    }

    public bool TryJoin(string playerName, int currentPlayerCount)
    {
        if (State != RoomState.Lobby) return false;

        if (currentPlayerCount < config.maxPlayers)
        {
            if (!joinedPlayers.Contains(playerName))
                joinedPlayers.Add(playerName);
            return true;
        }

        if (!spectators.Contains(playerName))
            spectators.Add(playerName);
        return false;
    }

    public bool CanStartGame(int currentPlayerCount)
    {
        return currentPlayerCount >= config.minPlayers;
    }

    public void StartGame()
    {
        if (State != RoomState.Lobby) return;
        State = RoomState.InGame;
        OnGameStarted?.Invoke();

        if (UIController.Instance != null)
            UIController.Instance.ShowMessage($"�ж� {roomCode} �C���}�l");
    }

    public void EndGame()
    {
        State = RoomState.Finished;
    }

    public IReadOnlyList<string> GetSpectators() => spectators;
}
