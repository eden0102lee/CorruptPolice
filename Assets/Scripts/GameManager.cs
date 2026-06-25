using System;
using System.Collections.Generic;
using UnityEngine;

public enum GamePhase
{
    Placement,
    Playing
}

public enum GameResult
{
    None,
    Police,
    Thieves
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int maxRounds = 12;
    public int currentRound = 1;
    public int stepsPerTurn = 1;

    [Header("Win Conditions")]
    [Tooltip("How many treasures thieves must collect to win")]
    public int treasureGoal = 3;

    [Header("Mode")]
    [Tooltip("When enabled, wait for network room setup instead of auto-starting locally.")]
    public bool useNetworkMode = false;

    private int treasuresCollected = 0;
    public GameResult Result { get; private set; } = GameResult.None;

    public GamePhase CurrentPhase { get; private set; } = GamePhase.Placement;
    public GameFlowState FlowState { get; private set; } = GameFlowState.Local;
    public bool IsNetworkMode => useNetworkMode;
    public PlayerData CurrentTurnPlayer { get; private set; }

    public void SetCurrentTurnPlayer(PlayerData player)
    {
        CurrentTurnPlayer = player;
        OnTurnChanged?.Invoke(player);
    }

    public event Action<GameFlowState> OnFlowStateChanged;
    public event Action<PlayerData> OnTurnChanged;
    public event Action<GameResult> OnGameOver;

    private List<PlayerData> placementOrder = new List<PlayerData>();
    private int placementIndex = 0;

    [Header("Team Configuration")]
    [Tooltip("How many police teams participate in the game")]
    public int policeTeamCount = 3;

    [Header("Player Counts")]
    [Tooltip("Number of regular police players")] public int regularPoliceCount = 8;
    [Tooltip("Number of corrupt police players")] public int corruptPoliceCount = 2;
    [Tooltip("Number of thief players")] public int thiefCount = 2;
    private int currentTeamTurnIndex = 0;
    private int currentPlayerIndex = 0;
    private bool thiefPhase = true;
    private int currentThiefIndex = 0;
    private bool gameOver = false;

    private List<List<PlayerData>> policeTeams = new List<List<PlayerData>>();
    private List<PlayerData> thiefPlayers = new List<PlayerData>();
    private List<PlayerData> allPlayers = new List<PlayerData>();

    public int CurrentRound => currentRound;
    public int TreasuresCollected => treasuresCollected;

    void Awake()
    {
        Instance = this;
        if (!useNetworkMode)
        {
            InitializeTeams();
            if (PlayerInputController.Instance != null)
                BeginPlacement();
        }
        else
        {
            SetFlowState(GameFlowState.Lobby);
        }
    }

    void Start()
    {
        if (MapManager.Instance != null && !MapManager.Instance.autoBuildOnStart && !useNetworkMode)
        {
            MapManager.Instance.BuildMap();
        }
    }

    public void SetFlowState(GameFlowState state)
    {
        FlowState = state;
        OnFlowStateChanged?.Invoke(state);
    }

    public void ResetGameState()
    {
        currentRound = 1;
        treasuresCollected = 0;
        Result = GameResult.None;
        CurrentPhase = GamePhase.Placement;
        gameOver = false;
        placementIndex = 0;
        currentTeamTurnIndex = 0;
        currentPlayerIndex = 0;
        thiefPhase = true;
        currentThiefIndex = 0;
        CurrentTurnPlayer = null;
        placementOrder.Clear();
        policeTeams.Clear();
        thiefPlayers.Clear();
        allPlayers.Clear();
    }

    public void InitializeTeams()
    {
        ResetGameState();

        policeTeams = new List<List<PlayerData>>();
        for (int i = 0; i < policeTeamCount; i++)
            policeTeams.Add(new List<PlayerData>());

        for (int i = 0; i < regularPoliceCount; i++)
        {
            int teamIndex = i % policeTeamCount;
            AddPlayer(new PlayerData($"p{i}", PlayerRole.Police, teamIndex));
        }

        for (int i = 0; i < corruptPoliceCount; i++)
        {
            int teamIndex = i % policeTeamCount;
            AddPlayer(new PlayerData($"c{i}", PlayerRole.CorruptPolice, teamIndex));
        }

        for (int i = 0; i < thiefCount; i++)
        {
            AddThief(new PlayerData($"t{i}", PlayerRole.Thief, -1));
        }
    }

    public void InitializeFromRoster(IReadOnlyList<PlayerData> roster, RoomConfig config)
    {
        ResetGameState();
        if (config != null)
            config.ApplyTo(this);

        policeTeams = new List<List<PlayerData>>();
        for (int i = 0; i < policeTeamCount; i++)
            policeTeams.Add(new List<PlayerData>());

        foreach (var player in roster)
        {
            if (player.role == PlayerRole.Thief)
                AddThief(player);
            else
                AddPlayer(player);
        }
    }

    void AddPlayer(PlayerData player)
    {
        policeTeams[player.teamIndex].Add(player);
        allPlayers.Add(player);
    }

    void AddThief(PlayerData thief)
    {
        thiefPlayers.Add(thief);
        allPlayers.Add(thief);
    }

    public void SetPlayerPositions(int[] nodeIds)
    {
        for (int i = 0; i < nodeIds.Length && i < allPlayers.Count; i++)
        {
            allPlayers[i].MoveTo(nodeIds[i]);
        }
    }

    public void ForceStartGame()
    {
        CurrentPhase = GamePhase.Playing;
        SetFlowState(GameFlowState.Playing);
        thiefPhase = true;
        currentThiefIndex = 0;
        currentTeamTurnIndex = (currentRound - 1) % policeTeamCount;
        currentPlayerIndex = 0;
        NextPlayerTurn();
    }

    public void BeginPlacement()
    {
        CurrentPhase = GamePhase.Placement;
        SetFlowState(GameFlowState.Placement);
        placementOrder.Clear();
        for (int i = 0; i < policeTeamCount; i++)
        {
            placementOrder.AddRange(policeTeams[i]);
        }
        placementOrder.AddRange(thiefPlayers);
        placementIndex = 0;
        PromptPlacement();
    }

    void PromptPlacement()
    {
        if (placementIndex >= placementOrder.Count)
        {
            CurrentPhase = GamePhase.Playing;
            SetFlowState(GameFlowState.Playing);
            BeginTurn();
            return;
        }

        var player = placementOrder[placementIndex];
        SetCurrentTurnPlayer(player);
        if (PlayerInputController.Instance != null)
            PlayerInputController.Instance.StartPlacement(player);
    }

    public void ConfirmPlacement()
    {
        placementIndex++;
        PromptPlacement();
    }

    public void BeginTurn()
    {
        Debug.Log($"Round {currentRound} start");
        thiefPhase = true;
        currentThiefIndex = 0;
        currentTeamTurnIndex = (currentRound - 1) % policeTeamCount;
        currentPlayerIndex = 0;
        NextPlayerTurn();
    }

    public void NextPlayerTurn()
    {
        if (gameOver)
            return;
        if (thiefPhase)
        {
            while (currentThiefIndex < thiefPlayers.Count && thiefPlayers[currentThiefIndex].isArrested)
                currentThiefIndex++;

            if (currentThiefIndex < thiefPlayers.Count)
            {
                var player = thiefPlayers[currentThiefIndex];
                player.remainingSteps = stepsPerTurn;
                SetCurrentTurnPlayer(player);
                if (PlayerInputController.Instance != null)
                    PlayerInputController.Instance.SetCurrentPlayer(player);
                currentThiefIndex++;
            }
            else
            {
                thiefPhase = false;
                currentTeamTurnIndex = (currentRound - 1) % policeTeamCount;
                currentPlayerIndex = 0;
                NextPlayerTurn();
            }
        }
        else
        {
            while (currentPlayerIndex < policeTeams[currentTeamTurnIndex].Count &&
                   policeTeams[currentTeamTurnIndex][currentPlayerIndex].isArrested)
            {
                currentPlayerIndex++;
            }

            if (currentPlayerIndex < policeTeams[currentTeamTurnIndex].Count)
            {
                var player = policeTeams[currentTeamTurnIndex][currentPlayerIndex];
                player.remainingSteps = stepsPerTurn;
                SetCurrentTurnPlayer(player);
                if (PlayerInputController.Instance != null)
                    PlayerInputController.Instance.SetCurrentPlayer(player);
                currentPlayerIndex++;
            }
            else
            {
                currentTeamTurnIndex = (currentTeamTurnIndex + 1) % policeTeamCount;
                if (currentTeamTurnIndex == (currentRound - 1) % policeTeamCount)
                {
                    EndRound();
                }
                else
                {
                    currentPlayerIndex = 0;
                    NextPlayerTurn();
                }
            }
        }
    }

    void EndRound()
    {
        Debug.Log($"Round {currentRound} end");

        currentRound++;
        CheckGameEnd();
        if (!gameOver)
            BeginTurn();
    }

    public bool IsThiefAt(int nodeId)
    {
        foreach (var thief in thiefPlayers)
        {
            if (thief.currentNodeId == nodeId)
                return true;
        }
        return false;
    }

    public PlayerData GetThiefAt(int nodeId)
    {
        foreach (var thief in thiefPlayers)
        {
            if (thief.currentNodeId == nodeId)
                return thief;
        }
        return null;
    }

    public PlayerData GetPlayerByClientId(ulong clientId)
    {
        foreach (var player in allPlayers)
        {
            if (player.clientId == clientId)
                return player;
        }
        return null;
    }

    public PlayerData GetPlayerByName(string playerName)
    {
        foreach (var player in allPlayers)
        {
            if (player.playerName == playerName)
                return player;
        }
        return null;
    }

    public void ArrestThief(PlayerData thief)
    {
        if (thief == null || thief.isArrested)
            return;

        thief.isArrested = true;
        thief.currentNodeId = -1;

        if (PlayerTokenManager.Instance != null)
            PlayerTokenManager.Instance.HideToken(thief);

        CheckGameEnd();
    }

    public void RecordTreasure()
    {
        treasuresCollected++;
        CheckGameEnd();
    }

    public List<PlayerData> GetAllPlayers() => allPlayers;
    public List<PlayerData> GetThieves() => thiefPlayers;
    public List<List<PlayerData>> GetPoliceTeams() => policeTeams;

    public void RegisterPolice(PlayerData player)
    {
        if (!policeTeams[player.teamIndex].Contains(player))
            policeTeams[player.teamIndex].Add(player);
        if (!allPlayers.Contains(player))
            allPlayers.Add(player);
    }

    public void RegisterThief(PlayerData player)
    {
        if (!thiefPlayers.Contains(player))
            thiefPlayers.Add(player);
        if (!allPlayers.Contains(player))
            allPlayers.Add(player);
    }

    bool AreAllThievesArrested()
    {
        foreach (var t in thiefPlayers)
            if (!t.isArrested)
                return false;
        return true;
    }

    void CheckGameEnd()
    {
        if (gameOver)
            return;
        if (AreAllThievesArrested() || currentRound > maxRounds)
        {
            Result = GameResult.Police;
            GameOver();
        }
        else if (treasuresCollected >= treasureGoal)
        {
            Result = GameResult.Thieves;
            GameOver();
        }
    }

    void GameOver()
    {
        gameOver = true;
        SetFlowState(GameFlowState.GameOver);
        OnGameOver?.Invoke(Result);
        Debug.Log($"Game over - {Result} win");
    }

    public bool TryExecuteAction(PlayerData player, int targetNodeId, ActionType action, out string result, out bool isShared, bool advanceTurn = true)
    {
        result = string.Empty;
        isShared = true;

        if (player == null || player.remainingSteps <= 0)
            return false;

        if (CurrentPhase == GamePhase.Placement)
        {
            player.currentNodeId = targetNodeId;
            if (PlayerTokenManager.Instance != null)
                PlayerTokenManager.Instance.UpdateTokenPosition(player);
            return true;
        }

        if (!MapManager.Instance.AreNodesConnected(player.currentNodeId, targetNodeId))
            return false;

        player.currentNodeId = targetNodeId;
        if (PlayerTokenManager.Instance != null)
            PlayerTokenManager.Instance.UpdateTokenPosition(player);

        player.remainingSteps--;

        if (action == ActionType.Move && player.role == PlayerRole.Thief)
        {
            MapManager.Instance.AddFootprint(targetNodeId, player.playerName);
            bool collected = MapManager.Instance.CollectTreasure(targetNodeId);
            if (collected)
            {
                result = "Found treasure";
                RecordTreasure();
            }
            else
            {
                result = "No treasure";
            }
        }
        else if (action == ActionType.Investigate && player.role != PlayerRole.Thief)
        {
            var hasClue = MapManager.Instance.HasFootprint(targetNodeId);
            result = hasClue ? "Found clue" : "No clue";
            isShared = false;
        }
        else if (action == ActionType.Arrest && player.role != PlayerRole.Thief)
        {
            var thief = GetThiefAt(targetNodeId);
            if (thief != null)
            {
                ArrestThief(thief);
                result = $"Arrested ({thief.playerName})";
            }
            else
            {
                result = "No thief";
            }
            isShared = true;
        }

        if (ActionLogger.Instance != null)
        {
            ActionLogger.Instance.Log(
                player,
                CurrentRound,
                targetNodeId,
                action.ToString(),
                result,
                isShared
            );
        }

        if (advanceTurn && player.remainingSteps <= 0)
            NextPlayerTurn();

        return true;
    }
}
