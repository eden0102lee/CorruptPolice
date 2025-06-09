using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int maxRounds = 15;
    public int currentRound = 1;

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
        // �o�̬����ո�ơA��ڱ��p���� Lobby �γ]�w�ǤJ
        policeTeams = new List<List<PlayerData>>()
        {
            new List<PlayerData>(),
            new List<PlayerData>(),
            new List<PlayerData>()
        };

        // �̷ӳ]�w�[�J���ժ��a
        for (int i = 0; i < 10; i++) AddPlayer(new PlayerData("ĵ��" + i, PlayerRole.Police, 0));
        AddPlayer(new PlayerData("�g��1", PlayerRole.CorruptPolice, 1));
        AddPlayer(new PlayerData("�g��2", PlayerRole.CorruptPolice, 2));
        AddThief(new PlayerData("�p��1", PlayerRole.Thief, -1));
        AddThief(new PlayerData("�p��2", PlayerRole.Thief, -1));

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
        Debug.Log($"�� {currentRound} �^�X�}�l");
        currentTeamTurnIndex = (currentRound - 1) % policeTeamCount;
        currentPlayerIndex = 0;
        NextPlayerTurn();
    }

    public void NextPlayerTurn()
    {
        if (currentPlayerIndex < policeTeams[currentTeamTurnIndex].Count)
        {
            PlayerInputController.Instance.SetCurrentPlayer(policeTeams[currentTeamTurnIndex][currentPlayerIndex]);
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
        Debug.Log($"�� {currentRound} �^�X����");

        currentRound++;
        if (currentRound > maxRounds)
        {
            Debug.Log("�C������");
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
