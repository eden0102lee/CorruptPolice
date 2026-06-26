public enum PlayerRole
{
    Police,
    CorruptPolice,
    Thief
}

public class PlayerData
{
    public string playerName;
    public PlayerRole role;
    public int teamIndex;
    public int currentNodeId;
    public int remainingSteps;
    public bool isArrested;
    public bool isSpectator;
    public int treasureCount;
    public bool hasActedThisTeamTurn;

    public PlayerData(string name, PlayerRole role, int teamIndex)
    {
        this.playerName = name;
        this.role = role;
        this.teamIndex = teamIndex;
        this.currentNodeId = -1;
        this.remainingSteps = 0;
        this.isArrested = false;
        this.isSpectator = false;
        this.treasureCount = 0;
        this.hasActedThisTeamTurn = false;
    }

    public void MoveTo(int nodeId)
    {
        currentNodeId = nodeId;
    }

    public void ResetTeamTurnFlag()
    {
        hasActedThisTeamTurn = false;
    }
}
