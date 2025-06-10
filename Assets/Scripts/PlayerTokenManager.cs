using System.Collections.Generic;
using UnityEngine;

public class PlayerTokenManager : MonoBehaviour
{
    public static PlayerTokenManager Instance;

    public GameObject tokenPrefab;
    public Transform tokenParent;

    private Dictionary<PlayerData, GameObject> tokens = new Dictionary<PlayerData, GameObject>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void CreateTokens(IEnumerable<PlayerData> players)
    {
        foreach (var p in players)
            CreateToken(p);
    }

    public void CreateToken(PlayerData player)
    {
        if (tokenPrefab == null || tokens.ContainsKey(player))
            return;

        GameObject token = Instantiate(tokenPrefab, tokenParent);
        tokens[player] = token;
        var tokenComponent = token.GetComponent<PlayerToken>();
        if (tokenComponent == null)
            tokenComponent = token.AddComponent<PlayerToken>();
        tokenComponent.player = player;
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
            tokenRect.SetParent(tokenParent, false);
            tokenRect.position = nodeRect.position;
        }
    }
}
