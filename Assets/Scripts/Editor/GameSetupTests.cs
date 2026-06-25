#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;

public class GameSetupTests
{
    [Test]
    public void GameManager_InitializesTwelveParticipants()
    {
        var gameObject = new GameObject("GameManagerTest");
        var gameManager = gameObject.AddComponent<GameManager>();
        gameManager.useNetworkMode = false;
        gameManager.regularPoliceCount = 8;
        gameManager.corruptPoliceCount = 2;
        gameManager.thiefCount = 2;
        gameManager.InitializeTeams();

        Assert.AreEqual(12, gameManager.GetAllPlayers().Count);
        Assert.AreEqual(2, gameManager.GetThieves().Count);
        Assert.AreEqual(3, gameManager.GetPoliceTeams().Count);

        Object.DestroyImmediate(gameObject);
    }

    [Test]
    public void RoomManager_AssignRoles_MatchesConnectedPlayers()
    {
        var roomObject = new GameObject("RoomManagerTest");
        var roomManager = roomObject.AddComponent<RoomManager>();
        roomManager.CreateRoom(4, 6, 2);

        roomManager.TryAddPlayer(1, "Alice");
        roomManager.TryAddPlayer(2, "Bob");
        roomManager.TryAddPlayer(3, "Carol");
        roomManager.TryAddPlayer(4, "Dave");

        var roster = roomManager.AssignRoles();

        Assert.AreEqual(4, roster.Count);
        Assert.IsTrue(roomManager.CanStartGame(requireAllReady: false));

        Object.DestroyImmediate(roomObject);
    }
}
#endif
