using System.Collections.Generic;
using UnityEngine;

public enum GamePhase
{
    Placement,
    Playing
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int maxRounds = 15;
    public int currentRound = 1;
    public int stepsPerTurn = 1;

    public GamePhase CurrentPhase { get; private set; } = GamePhase.Placement;

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

    private List<List<PlayerData>> policeTeams = new List<List<PlayerData>>();
    private List<PlayerData> thiefPlayers = new List<PlayerData>();
    private List<PlayerData> allPlayers = new List<PlayerData>();

    public int CurrentRound => currentRound;

    void Awake()
    {
        Instance = this;
        InitializeTeams();
        if (PlayerInputController.Instance != null)
            BeginPlacement();
    }

    void InitializeTeams()
    {
        policeTeams = new List<List<PlayerData>>();
        for (int i = 0; i < policeTeamCount; i++)
            policeTeams.Add(new List<PlayerData>());

        // Create regular police players and distribute them across teams
        for (int i = 0; i < regularPoliceCount; i++)
        {
            int teamIndex = i % policeTeamCount;
            AddPlayer(new PlayerData($"p{i}", PlayerRole.Police, teamIndex));
        }

        // Create corrupt police players
        for (int i = 0; i < corruptPoliceCount; i++)
        {
            int teamIndex = i % policeTeamCount;
            AddPlayer(new PlayerData($"c{i}", PlayerRole.CorruptPolice, teamIndex));
        }

        // Create thief players
        for (int i = 0; i < thiefCount; i++)
        {
            AddThief(new PlayerData($"t{i}", PlayerRole.Thief, -1));
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
    // --- Testing Utilities ---
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
        currentTeamTurnIndex = (currentRound - 1) % policeTeamCount;
        currentPlayerIndex = 0;
        NextPlayerTurn();
    }

    void BeginPlacement()
    {
        CurrentPhase = GamePhase.Placement;
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
            BeginTurn();
            return;
        }

        var player = placementOrder[placementIndex];
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
        currentTeamTurnIndex = (currentRound - 1) % policeTeamCount;
        currentPlayerIndex = 0;
        NextPlayerTurn();
    }

    public void NextPlayerTurn()
    {
        if (currentPlayerIndex < policeTeams[currentTeamTurnIndex].Count)
        {
            var player = policeTeams[currentTeamTurnIndex][currentPlayerIndex];
            player.remainingSteps = stepsPerTurn;
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

    void EndRound()
    {
        Debug.Log($"Round {currentRound} end");

        currentRound++;
        if (currentRound > maxRounds)
        {
            Debug.Log("Game over");
            return;
        }
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

}
