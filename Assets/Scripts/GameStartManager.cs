using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 遊戲開始：放置順序、起始位置規則、角色資訊揭露。
/// </summary>
public class GameStartManager : MonoBehaviour
{
    public static GameStartManager Instance;

    private readonly List<PlayerData> placementOrder = new List<PlayerData>();
    private int placementIndex;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void BeginPlacement()
    {
        placementOrder.Clear();
        placementIndex = 0;

        if (GameManager.Instance == null) return;

        foreach (var team in GameManager.Instance.GetPoliceTeams())
            placementOrder.AddRange(team);

        placementOrder.AddRange(GameManager.Instance.GetThieves());

        PromptNextPlacement();
    }

    public bool IsThiefPlacementPhase()
    {
        if (placementIndex >= placementOrder.Count) return false;
        return placementOrder[placementIndex].role == PlayerRole.Thief;
    }

    public PlayerData GetCurrentPlacementPlayer()
    {
        if (placementIndex >= placementOrder.Count) return null;
        return placementOrder[placementIndex];
    }

    void PromptNextPlacement()
    {
        if (placementIndex >= placementOrder.Count)
        {
            GameManager.Instance.OnPlacementComplete();
            return;
        }

        var player = placementOrder[placementIndex];
        if (PlayerInputController.Instance != null)
            PlayerInputController.Instance.StartPlacement(player);
    }

    public bool CanPlaceAt(PlayerData player, int nodeId)
    {
        if (MapManager.Instance == null) return false;

        if (player.role == PlayerRole.Thief && MapManager.Instance.HasTreasure(nodeId))
            return false;

        return MapManager.Instance.GetNodeData(nodeId) != null;
    }

    public void ConfirmPlacement(PlayerData player, int nodeId)
    {
        if (!CanPlaceAt(player, nodeId))
        {
            Debug.LogWarning($"無法在節點 {nodeId} 放置 {player.playerName}");
            return;
        }

        player.currentNodeId = nodeId;
        if (PlayerTokenManager.Instance != null)
        {
            PlayerTokenManager.Instance.UpdateTokenPosition(player);
            PlayerTokenManager.Instance.RefreshAllVisibility();
        }

        bool isThief = player.role == PlayerRole.Thief;
        if (UIController.Instance != null)
        {
            string visibility = isThief ? "（位置僅小偷與腐敗警察可見）" : "（公開）";
            UIController.Instance.ShowMessage($"{player.playerName} 完成放置 {visibility}");
        }

        placementIndex++;
        PromptNextPlacement();
    }

    public void RevealRoleKnowledge(PlayerData localViewer)
    {
        if (GameManager.Instance == null || localViewer == null) return;

        var thieves = RoleKnowledgeService.GetKnownThieves(localViewer, GameManager.Instance.GetAllPlayers());
        var corrupt = RoleKnowledgeService.GetKnownCorruptPolice(localViewer, GameManager.Instance.GetAllPlayers());

        if (thieves.Count > 0 && UIController.Instance != null)
            UIController.Instance.ShowMessage($"已知小偷：{string.Join(", ", thieves.ConvertAll(t => t.playerName))}");

        if (corrupt.Count > 0 && UIController.Instance != null)
            UIController.Instance.ShowMessage($"已知腐敗警察：{string.Join(", ", corrupt.ConvertAll(c => c.playerName))}");
    }
}
