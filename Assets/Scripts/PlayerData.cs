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

    public PlayerData(string name, PlayerRole role, int teamIndex)
    {
        this.playerName = name;
        this.role = role;
        this.teamIndex = teamIndex;
        this.currentNodeId = -1;
        this.remainingSteps = 0;
    }

    public void MoveTo(int nodeId)
    {
        currentNodeId = nodeId;
    }
}

