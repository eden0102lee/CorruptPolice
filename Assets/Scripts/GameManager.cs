using System.Collections.Generic;
using UnityEngine;

public enum GamePhase
{
    Placement,
    Playing,
    Voting,
    Settlement
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

    public GameConfig config = GameConfig.Default;

    public int currentRound = 1;
    public GameResult Result { get; private set; } = GameResult.None;
    public GamePhase CurrentPhase { get; private set; } = GamePhase.Placement;
    public bool IsGameOver { get; private set; }

    private List<List<PlayerData>> policeTeams = new List<List<PlayerData>>();
    private List<PlayerData> thiefPlayers = new List<PlayerData>();
    private List<PlayerData> allPlayers = new List<PlayerData>();

    public GameConfig Config => RoomManager.Instance != null ? RoomManager.Instance.config : config;
    public int CurrentRound => currentRound;

    void Awake()
    {
        Instance = this;
        EnsureSystems();
        InitializeTeams();
        EnsureGameAgent();

        if (RoomManager.Instance != null)
            RoomManager.Instance.StartGame();

        if (GameStartManager.Instance != null)
            GameStartManager.Instance.BeginPlacement();
    }

    void EnsureSystems()
    {
        EnsureComponent<RoomManager>();
        EnsureComponent<GameStartManager>();
        EnsureComponent<TurnManager>();
        EnsureComponent<InvestigationSystem>();
        EnsureComponent<VotingSystem>();
        EnsureComponent<TreasureDistributionSystem>();
        EnsureComponent<VictoryEvaluator>();
        EnsureComponent<UIController>();

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnRoundEnded += HandleRoundEnded;
            TurnManager.Instance.OnThiefTurnEnded += HandleThiefTurnEnded;
        }
    }

    void EnsureComponent<T>() where T : Component
    {
        if (FindObjectOfType<T>() == null)
            gameObject.AddComponent<T>();
    }

    void EnsureGameAgent()
    {
        if (FindObjectOfType<GameAgentController>() == null)
            gameObject.AddComponent<GameAgentController>();
    }

    void Start()
    {
        if (MapManager.Instance != null)
        {
            MapManager.Instance.treasureCount = Config.mapTreasureCount;
            if (!MapManager.Instance.autoBuildOnStart)
                MapManager.Instance.BuildMap();
        }
    }

    void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnRoundEnded -= HandleRoundEnded;
            TurnManager.Instance.OnThiefTurnEnded -= HandleThiefTurnEnded;
        }
    }

    void InitializeTeams()
    {
        var cfg = Config;
        policeTeams = new List<List<PlayerData>>();
        allPlayers.Clear();
        thiefPlayers.Clear();

        for (int i = 0; i < cfg.policeTeamCount; i++)
            policeTeams.Add(new List<PlayerData>());

        for (int i = 0; i < cfg.regularPoliceCount; i++)
        {
            int teamIndex = i % cfg.policeTeamCount;
            AddPlayer(new PlayerData($"p{i}", PlayerRole.Police, teamIndex));
        }

        for (int i = 0; i < cfg.corruptPoliceCount; i++)
        {
            int teamIndex = i % cfg.policeTeamCount;
            AddPlayer(new PlayerData($"c{i}", PlayerRole.CorruptPolice, teamIndex));
        }

        for (int i = 0; i < cfg.thiefCount; i++)
            AddThief(new PlayerData($"t{i}", PlayerRole.Thief, -1));
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

    public void OnPlacementComplete()
    {
        CurrentPhase = GamePhase.Playing;
        currentRound = 1;

        if (UIController.Instance != null)
        {
            UIController.Instance.ShowMessage("放置完成，遊戲開始！");
            if (allPlayers.Count > 0)
                GameStartManager.Instance.RevealRoleKnowledge(allPlayers[0]);
        }

        if (MapManager.Instance != null)
            MapManager.Instance.RefreshTreasureVisibility(allPlayers.Count > 0 ? allPlayers[0] : null);

        if (PlayerTokenManager.Instance != null)
            PlayerTokenManager.Instance.RefreshAllVisibility(allPlayers.Count > 0 ? allPlayers[0] : null);

        BeginTurn();
    }

    public void BeginTurn()
    {
        if (IsGameOver) return;

        if (UIController.Instance != null)
            UIController.Instance.ShowMessage($"第 {currentRound} 回合開始");

        int startingTeam = (currentRound - 1) % Config.policeTeamCount;
        if (TurnManager.Instance != null)
            TurnManager.Instance.BeginRound(currentRound, startingTeam);
    }

    void HandleRoundEnded(int round)
    {
        foreach (int revealRound in Config.publicTreasureRevealRounds)
        {
            if (round == revealRound && UIController.Instance != null)
            {
                UIController.Instance.ShowMessage(
                    $"第 {revealRound} 回合公告：存活小偷持有寶物總數 {GetEffectiveTreasureCount()}");
            }
        }
    }

    void HandleThiefTurnEnded(PlayerData thief, int nodeId) { }

    public void NotifyCorruptPoliceOfThiefPosition(PlayerData thief)
    {
        if (thief == null || thief.isArrested) return;

        foreach (var p in allPlayers)
        {
            if (p.role != PlayerRole.CorruptPolice) continue;
            if (UIController.Instance != null)
                UIController.Instance.ShowMessage($"[腐敗警察] {p.playerName} 得知 {thief.playerName} 位於節點 {thief.currentNodeId}");
        }
    }

    public void EndRound()
    {
        currentRound++;
        CheckGameEnd();
        if (!IsGameOver)
            BeginTurn();
    }

    public void OnPlayerTurnComplete(PlayerData player)
    {
        if (player == null || IsGameOver) return;

        if (player.role == PlayerRole.Thief)
        {
            if (player.remainingSteps <= 0 && TurnManager.Instance != null)
                TurnManager.Instance.CompleteThiefTurn(player);
            return;
        }

        player.hasActedThisTeamTurn = true;
        if (TurnManager.Instance != null)
            TurnManager.Instance.CompletePoliceAction(player);
    }

    public void RecordTreasureForThief(PlayerData thief)
    {
        if (thief == null) return;
        thief.treasureCount++;
        CheckGameEnd();
    }

    public int GetEffectiveTreasureCount()
    {
        int total = 0;
        foreach (var t in thiefPlayers)
        {
            if (!t.isArrested)
                total += t.treasureCount;
        }
        return total;
    }

    public bool IsThiefAt(int nodeId)
    {
        foreach (var thief in thiefPlayers)
        {
            if (!thief.isArrested && thief.currentNodeId == nodeId)
                return true;
        }
        return false;
    }

    public PlayerData GetThiefAt(int nodeId)
    {
        foreach (var thief in thiefPlayers)
        {
            if (!thief.isArrested && thief.currentNodeId == nodeId)
                return thief;
        }
        return null;
    }

    public void ArrestThief(PlayerData thief, PlayerData arrester)
    {
        if (thief == null || thief.isArrested) return;

        if (arrester != null && arrester.role == PlayerRole.CorruptPolice)
        {
            if (UIController.Instance != null)
                UIController.Instance.ShowMessage($"{arrester.playerName} 嘗試逮捕 {thief.playerName}：失敗（腐敗警察）");
            return;
        }

        int lostTreasures = thief.treasureCount;
        thief.treasureCount = 0;
        thief.isArrested = true;
        thief.currentNodeId = -1;

        if (lostTreasures > 0 && MapManager.Instance != null)
            MapManager.Instance.ReturnTreasuresToMap(lostTreasures, thief.playerName);

        if (PlayerTokenManager.Instance != null)
            PlayerTokenManager.Instance.HideToken(thief);

        if (UIController.Instance != null)
            UIController.Instance.ShowMessage($"{thief.playerName} 遭逮捕，失去 {lostTreasures} 個寶物");

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
        if (IsGameOver || VictoryEvaluator.Instance == null) return;

        Result = VictoryEvaluator.Instance.Evaluate(
            currentRound,
            Config.maxRounds,
            GetEffectiveTreasureCount(),
            Config.treasureGoal,
            AreAllThievesArrested());

        if (Result != GameResult.None)
            EnterPostGame();
    }

    void EnterPostGame()
    {
        IsGameOver = true;
        CurrentPhase = GamePhase.Voting;

        if (UIController.Instance != null)
            UIController.Instance.ShowMessage($"遊戲結束 — {(Result == GameResult.Thieves ? "小偷" : "警察")} 陣營勝利");

        if (VotingSystem.Instance != null)
            VotingSystem.Instance.BeginVoting(allPlayers);

        CurrentPhase = GamePhase.Settlement;
        if (VotingSystem.Instance != null && TreasureDistributionSystem.Instance != null)
        {
            var voteResult = VotingSystem.Instance.LastResult;
            var settlement = TreasureDistributionSystem.Instance.CalculateSettlement(Result, voteResult);
            if (UIController.Instance != null)
                UIController.Instance.ShowSettlement(settlement, Result);
        }

        if (ActionLogger.Instance != null)
            ActionLogger.Instance.ExportToFile();

        if (RoomManager.Instance != null)
            RoomManager.Instance.EndGame();
    }

    public void SetPlayerPositions(int[] nodeIds)
    {
        for (int i = 0; i < nodeIds.Length && i < allPlayers.Count; i++)
            allPlayers[i].MoveTo(nodeIds[i]);
    }

    public void ForceStartGame()
    {
        CurrentPhase = GamePhase.Playing;
        if (PlayerInputController.Instance != null)
            PlayerInputController.Instance.CancelPlacement();
        BeginTurn();
    }
}
