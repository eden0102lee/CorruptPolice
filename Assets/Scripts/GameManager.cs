using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int maxRounds = 15;
    public int currentRound = 1;
    public int stepsPerTurn = 1;

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

        BeginTurn();
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

    void BeginTurn()
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
}
