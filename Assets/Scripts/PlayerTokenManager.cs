using System.Collections.Generic;
using UnityEngine;

public class PlayerTokenManager : MonoBehaviour
{
    public static PlayerTokenManager Instance;

    public GameObject tokenPrefab;
    public Transform tokenParent;

    private Dictionary<PlayerData, GameObject> tokens = new Dictionary<PlayerData, GameObject>();

    // Keep counters to assign numbers within each team and for thieves
    private Dictionary<int, int> teamCounters = new Dictionary<int, int>();
    private int thiefCounter = 0;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void CreateTokens(IEnumerable<PlayerData> players)
    {
        teamCounters.Clear();
        thiefCounter = 0;
        foreach (var p in players)
            CreateToken(p);
    }

    public void CreateToken(PlayerData player)
    {
        if (tokenPrefab == null || tokens.ContainsKey(player))
            return;

        GameObject token = Instantiate(tokenPrefab, tokenParent);
        tokens[player] = token;
        var tokenComponent = token.GetComponent<PlayerUI>();
        if (tokenComponent == null)
            tokenComponent = token.AddComponent<PlayerUI>();
        tokenComponent.player = player;

        var ui = token.GetComponent<PlayerUI>();
        if (ui != null)
        {
            if (player.role == PlayerRole.Thief)
            {
                thiefCounter++;
                Color color = thiefCounter == 1 ? Color.magenta : Color.yellow;
                ui.SetColor(color);
                ui.SetPlayerNumber(thiefCounter);
            }
            else
            {
                if (!teamCounters.ContainsKey(player.teamIndex))
                    teamCounters[player.teamIndex] = 0;
                teamCounters[player.teamIndex]++;
                int number = teamCounters[player.teamIndex];
                ui.SetPlayerNumber(number);

                Color color = Color.white;
                switch (player.teamIndex)
                {
                    case 0: color = Color.red; break;
                    case 1: color = Color.green; break;
                    case 2: color = Color.blue; break;
                }
                ui.SetColor(color);
            }
        }

        UpdateTokenPosition(player);
    }

    public void UpdateTokenPosition(PlayerData player)
    {
        if (!tokens.TryGetValue(player, out GameObject token))
            return;

        if (player.currentNodeId < 0)
            return;

        GameObject nodeObj = MapManager.Instance.GetNodeObject(player.currentNodeId);
        if (nodeObj == null) return;

        RectTransform tokenRect = token.GetComponent<RectTransform>();
        RectTransform nodeRect = nodeObj.GetComponent<RectTransform>();
        if (nodeRect != null)
        {
            tokenRect.position = nodeRect.position;
        }
    }
}