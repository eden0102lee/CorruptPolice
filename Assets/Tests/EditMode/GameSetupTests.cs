using NUnit.Framework;
using UnityEngine;

public class GameSetupTests
{
    [Test]
    public void GameManagerCreatesTwelvePlayers()
    {
        var go = new GameObject("GameManagerTest");
        var gm = go.AddComponent<GameManager>();
        gm.policeTeamCount = 3;
        gm.regularPoliceCount = 8;
        gm.corruptPoliceCount = 2;
        gm.thiefCount = 2;

        // Awake is called automatically when the component is added
        Assert.AreEqual(12, gm.GetAllPlayers().Count);
        Assert.AreEqual(3, gm.GetPoliceTeams().Count);

        Object.DestroyImmediate(go);
    }
}
