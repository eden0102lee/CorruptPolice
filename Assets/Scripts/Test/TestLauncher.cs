using UnityEngine;

public class TestLauncher : MonoBehaviour
{
    public MapManager mapManager;
    public GameManager gameManager;

    void Start()
    {
        if (mapManager == null) mapManager = MapManager.Instance;
        if (gameManager == null) gameManager = GameManager.Instance;

        //mapManager.LoadAndBuildMap();

        //GenerateTestPlayers();
        

        int[] startNodes = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

        gameManager.SetPlayerPositions(startNodes);

        gameManager.ForceStartGame();

        //gameManager.BeginTurn();  // ݭnTO GameManager  BeginTurn O public

    }

    void GenerateTestPlayers()
    {
        // 10 警察（平均分3組），2 小偷
        for (int i = 0; i < 10; i++)
        {
            var player = new PlayerData("警察" + i, PlayerRole.Police, i % 3);
            player.currentNodeId = i % 4;  // 預設初始站點
            gameManager.RegisterPolice(player);
        }

        var thief1 = new PlayerData("小偷1", PlayerRole.Thief, -1);
        thief1.currentNodeId = 6;
        gameManager.RegisterThief(thief1);

        var thief2 = new PlayerData("小偷2", PlayerRole.Thief, -1);
        thief2.currentNodeId = 7;
        gameManager.RegisterThief(thief2);
    }
}
