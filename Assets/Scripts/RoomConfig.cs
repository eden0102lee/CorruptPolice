using System;
using UnityEngine;

[Serializable]
public class RoomConfig
{
    public int maxPlayers = 12;
    public int maxRounds = 12;
    public int treasureGoal = 3;
    public int stepsPerTurn = 1;
    public int policeTeamCount = 3;
    public int regularPoliceCount = 8;
    public int corruptPoliceCount = 2;
    public int thiefCount = 2;
    public int mapSeed = 0;

    public void ApplyTo(GameManager gameManager)
    {
        if (gameManager == null)
            return;

        gameManager.maxRounds = maxRounds;
        gameManager.treasureGoal = treasureGoal;
        gameManager.stepsPerTurn = stepsPerTurn;
        gameManager.policeTeamCount = policeTeamCount;
        gameManager.regularPoliceCount = regularPoliceCount;
        gameManager.corruptPoliceCount = corruptPoliceCount;
        gameManager.thiefCount = thiefCount;
    }

    public static RoomConfig FromGameManager(GameManager gameManager)
    {
        return new RoomConfig
        {
            maxPlayers = gameManager.regularPoliceCount + gameManager.corruptPoliceCount + gameManager.thiefCount,
            maxRounds = gameManager.maxRounds,
            treasureGoal = gameManager.treasureGoal,
            stepsPerTurn = gameManager.stepsPerTurn,
            policeTeamCount = gameManager.policeTeamCount,
            regularPoliceCount = gameManager.regularPoliceCount,
            corruptPoliceCount = gameManager.corruptPoliceCount,
            thiefCount = gameManager.thiefCount
        };
    }
}
