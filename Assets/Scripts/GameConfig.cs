using System;
using UnityEngine;

/// <summary>
/// 房間與對局設定，對應企劃書第二章可調參數。
/// </summary>
[Serializable]
public class GameConfig
{
    public int minPlayers = 5;
    public int maxPlayers = 12;

    public int maxRounds = 15;
    public int treasureGoal = 12;
    public int mapTreasureCount = 28;

    public int thiefStepsPerTurn = 2;
    public int policeStepsPerTurn = 1;

    public float thiefActionTimeSeconds = 60f;
    public float policeTeamTimeSeconds = 300f;

    public int policeTeamCount = 3;
    public int regularPoliceCount = 8;
    public int corruptPoliceCount = 2;
    public int thiefCount = 2;

    public int[] publicTreasureRevealRounds = { 5, 10 };

    public static GameConfig Default => new GameConfig();
}
