using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int maxRounds = 15;
    public int currentRound = 1;
    public int stepsPerTurn = 1;

    private int policeTeamCount = 3;
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
        policeTeams = new List<List<PlayerData>>()
        {
            new List<PlayerData>(),
            new List<PlayerData>(),
            new List<PlayerData>()
        };

        for (int i = 0; i < 10; i++) AddPlayer(new PlayerData($"p{i}", PlayerRole.Police, 0));
        AddPlayer(new PlayerData("g1", PlayerRole.CorruptPolice, 1));
        AddPlayer(new PlayerData("g2", PlayerRole.CorruptPolice, 2));
        AddThief(new PlayerData("t1", PlayerRole.Thief, -1));
        AddThief(new PlayerData("t2", PlayerRole.Thief, -1));

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
