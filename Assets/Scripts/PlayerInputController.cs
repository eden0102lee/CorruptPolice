using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// 控制玩家操作，包括移動、調查與逮捕行動的選擇與執行
/// </summary>
public class PlayerInputController : MonoBehaviour
{
    public static PlayerInputController Instance;

    public GameObject actionPanel;         // 行動選單面板
    public Button moveButton;              // 小偷移動按鈕
    public Button investigateButton;       // 警察調查按鈕
    public Button arrestButton;            // 警察逮捕按鈕
    public Button confirmButton;           // 確認行動按鈕

    private PlayerData currentPlayer;      // 當前輪到的玩家
    private int selectedNodeId = -1;       // 玩家選擇的目標節點
    private int originalNodeId = -1;       // 玩家起始節點

    private bool placementMode = false;    // 是否處於初始放置階段
    private ActionType currentAction;      // 當前選擇的行動類型

    void Awake()
    {
        Instance = this;
        actionPanel.SetActive(false);

        // 為各個按鈕綁定對應的操作方法
        confirmButton.onClick.AddListener(OnConfirmAction);
        moveButton.onClick.AddListener(() => SetActionType(ActionType.Move));
        investigateButton.onClick.AddListener(() => SetActionType(ActionType.Investigate));
        arrestButton.onClick.AddListener(() => SetActionType(ActionType.Arrest));
    }

    /// <summary>
    /// 設定當前控制的玩家並初始化其狀態
    /// </summary>
    public void SetCurrentPlayer(PlayerData player)
    {
        currentPlayer = player;
        if (GameManager.Instance != null)
            currentPlayer.remainingSteps = GameManager.Instance.stepsPerTurn;

        originalNodeId = player.currentNodeId;
        selectedNodeId = -1;

        HighlightCurrentNode();

        int round = GameManager.Instance != null ? GameManager.Instance.CurrentRound : -1;
        Debug.Log($"[Turn] Round {round} - {currentPlayer.playerName} ({currentPlayer.role}) begins with {currentPlayer.remainingSteps} steps");
    }

    /// <summary>
    /// 啟動初始放置模式，等待玩家選擇起始節點
    /// </summary>
    public void StartPlacement(PlayerData player)
    {
        placementMode = true;
        currentPlayer = player;
        selectedNodeId = -1;
        originalNodeId = -1;
        Debug.Log($"Select start position for {player.playerName}");
    }

    /// <summary>
    /// 高亮目前所在的節點
    /// </summary>
    void HighlightCurrentNode()
    {
        foreach (var ui in MapManager.Instance.GetAllNodeUIs())
            ui.Unhighlight();

        var currentUI = MapManager.Instance.GetNodeUI(currentPlayer.currentNodeId);
        if (currentUI != null) currentUI.Highlight();
    }

    /// <summary>
    /// 處理節點點擊事件，包含移動與初始放置
    /// </summary>
    public void OnNodeClicked(int nodeId)
    {
        if (placementMode)
        {
            if (currentPlayer == null) return;

            currentPlayer.currentNodeId = nodeId;
            if (PlayerTokenManager.Instance != null)
                PlayerTokenManager.Instance.UpdateTokenPosition(currentPlayer);

            MapManager.Instance.GetNodeUI(nodeId)?.Highlight();
            placementMode = false;
            GameManager.Instance.ConfirmPlacement();
            return;
        }

        if (currentPlayer == null || currentPlayer.remainingSteps <= 0) return;

        if (!MapManager.Instance.AreNodesConnected(currentPlayer.currentNodeId, nodeId))
        {
            Debug.Log("Nodes are not connected.");
            return;
        }

        if (nodeId == selectedNodeId) return;

        if (selectedNodeId != -1)
            MapManager.Instance.GetNodeUI(selectedNodeId)?.Unhighlight();

        selectedNodeId = nodeId;
        MapManager.Instance.GetNodeUI(nodeId)?.Highlight();

        ShowActionPanel();
    }

    /// <summary>
    /// 顯示行動選單，依照角色開啟對應選項
    /// </summary>
    void ShowActionPanel()
    {
        actionPanel.SetActive(true);
        moveButton.gameObject.SetActive(currentPlayer.role == PlayerRole.Thief);
        investigateButton.gameObject.SetActive(currentPlayer.role != PlayerRole.Thief);
        arrestButton.gameObject.SetActive(currentPlayer.role != PlayerRole.Thief);
    }

    /// <summary>
    /// 設定當前選擇的行動
    /// </summary>
    void SetActionType(ActionType action)
    {
        currentAction = action;
    }

    /// <summary>
    /// 執行目前選擇的行動邏輯，並紀錄結果
    /// </summary>
    void OnConfirmAction()
    {
        if (selectedNodeId == -1 || currentPlayer == null) return;

        currentPlayer.currentNodeId = selectedNodeId;
        if (PlayerTokenManager.Instance != null)
            PlayerTokenManager.Instance.UpdateTokenPosition(currentPlayer);

        currentPlayer.remainingSteps--;

        int round = GameManager.Instance != null ? GameManager.Instance.CurrentRound : -1;
        Debug.Log($"[Action] Round {round} - {currentPlayer.playerName} performed {currentAction} on node {selectedNodeId}. Remaining steps: {currentPlayer.remainingSteps}");

        string result = string.Empty;
        bool isShared = true;
        if (currentAction == ActionType.Move && currentPlayer.role == PlayerRole.Thief)
        {
            MapManager.Instance.AddFootprint(selectedNodeId, currentPlayer.playerName);
            if (MapManager.Instance.HasTreasure(selectedNodeId))
            {
                MapManager.Instance.CollectTreasure(selectedNodeId);
                result = "Found treasure";
            }
            else
            {
                result = "No treasure";
            }
        }
        else if (currentAction == ActionType.Investigate && currentPlayer.role != PlayerRole.Thief)
        {
            var hasClue = MapManager.Instance.HasFootprint(selectedNodeId);
            result = hasClue ? "Found clue" : "No clue";
            isShared = false;
        }
        else if (currentAction == ActionType.Arrest && currentPlayer.role != PlayerRole.Thief)
        {
            var thief = GameManager.Instance.GetThiefAt(selectedNodeId);
            if (thief != null)
            {
                GameManager.Instance.ArrestThief(thief);
                result = $"Arrested ({thief.playerName})";
            }
            else
            {
                result = "No thief";
            }
            isShared = true;
        }
        if (ActionLogger.Instance != null)
        {
            ActionLogger.Instance.Log(
                currentPlayer,
                GameManager.Instance.CurrentRound,
                selectedNodeId,
                currentAction.ToString(),
                result,
                isShared
            );
        }
        else
        {
            Debug.LogWarning("ActionLogger instance missing - skipping log entry.");
        }

        if (currentPlayer.remainingSteps <= 0)
        {
            GameManager.Instance.NextPlayerTurn();
        }

        actionPanel.SetActive(false);
        selectedNodeId = -1;
    }
}
