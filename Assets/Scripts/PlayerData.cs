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
    public ulong clientId;
    public int slotIndex;

    public PlayerData(string name, PlayerRole role, int teamIndex)
    {
        this.playerName = name;
        this.role = role;
        this.teamIndex = teamIndex;
        this.currentNodeId = -1;
        this.remainingSteps = 0;
        this.isArrested = false;
        this.clientId = ulong.MaxValue;
        this.slotIndex = -1;
    }

    public bool IsLocalPlayer(ulong localClientId)
    {
        return clientId != ulong.MaxValue && clientId == localClientId;
    }

    public void MoveTo(int nodeId)
    {
        currentNodeId = nodeId;
    }
}
