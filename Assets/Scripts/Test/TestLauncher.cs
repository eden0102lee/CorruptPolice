using UnityEngine;

public class TestLauncher : MonoBehaviour
{
    public MapManager mapManager;
    public GameManager gameManager;

    void Start()
    {
        if (mapManager == null) mapManager = MapManager.Instance;
        if (gameManager == null) gameManager = GameManager.Instance;

        mapManager.LoadAndBuildMap();

        GenerateTestPlayers();
        gameManager.BeginTurn();  // �ݭn�T�O GameManager �� BeginTurn �O public
    }

    void GenerateTestPlayers()
    {
        // 10 ĵ��]������3�ա^�A2 �p��
        for (int i = 0; i < 10; i++)
        {
            var player = new PlayerData("ĵ��" + i, PlayerRole.Police, i % 3);
            player.currentNodeId = i % 4;  // �w�]��l���I
            gameManager.RegisterPolice(player);
        }

        var thief1 = new PlayerData("�p��1", PlayerRole.Thief, -1);
        thief1.currentNodeId = 6;
        gameManager.RegisterThief(thief1);

        var thief2 = new PlayerData("�p��2", PlayerRole.Thief, -1);
        thief2.currentNodeId = 7;
        gameManager.RegisterThief(thief2);
    }
}
