using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class PlayerInputController : MonoBehaviour
{
    public static PlayerInputController Instance;

    public GameObject actionPanel;
    public Button moveButton;
    public Button investigateButton;
    public Button arrestButton;
    public Button confirmButton;

    private PlayerData currentPlayer;
    private int selectedNodeId = -1;
    private int originalNodeId = -1;

    private ActionType currentAction;

    void Awake()
    {
        Instance = this;
        actionPanel.SetActive(false);

        confirmButton.onClick.AddListener(OnConfirmAction);
        moveButton.onClick.AddListener(() => SetActionType(ActionType.Move));
        investigateButton.onClick.AddListener(() => SetActionType(ActionType.Investigate));
        arrestButton.onClick.AddListener(() => SetActionType(ActionType.Arrest));
    }

    public void SetCurrentPlayer(PlayerData player)
    {
        currentPlayer = player;
        originalNodeId = player.currentNodeId;
        selectedNodeId = -1;
        HighlightCurrentNode();
    }

    void HighlightCurrentNode()
    {
        foreach (var ui in MapManager.Instance.GetAllNodeUIs())
            ui.Unhighlight();

        var currentUI = MapManager.Instance.GetNodeUI(currentPlayer.currentNodeId);
        if (currentUI != null) currentUI.Highlight();
    }

    public void OnNodeClicked(int nodeId)
    {
        if (currentPlayer == null || currentPlayer.remainingSteps <= 0) return;

        if (!MapManager.Instance.AreNodesConnected(currentPlayer.currentNodeId, nodeId))
        {
            Debug.Log("請選擇相鄰站點");
            return;
        }

        if (nodeId == selectedNodeId) return;

        if (selectedNodeId != -1)
            MapManager.Instance.GetNodeUI(selectedNodeId)?.Unhighlight();

        selectedNodeId = nodeId;
        MapManager.Instance.GetNodeUI(nodeId)?.Highlight();

        ShowActionPanel();
    }

    void ShowActionPanel()
    {
        actionPanel.SetActive(true);
        moveButton.gameObject.SetActive(currentPlayer.role == PlayerRole.Thief);
        investigateButton.gameObject.SetActive(currentPlayer.role != PlayerRole.Thief);
        arrestButton.gameObject.SetActive(currentPlayer.role != PlayerRole.Thief);
    }

    void SetActionType(ActionType action)
    {
        currentAction = action;
    }

    void OnConfirmAction()
    {
        if (selectedNodeId == -1 || currentPlayer == null) return;

        currentPlayer.currentNodeId = selectedNodeId;
        currentPlayer.remainingSteps--;

        string result = "";
        bool isShared = true;

        if (currentAction == ActionType.Move && currentPlayer.role == PlayerRole.Thief)
        {
            MapManager.Instance.AddFootprint(selectedNodeId, currentPlayer.playerName);
            result = MapManager.Instance.HasTreasure(selectedNodeId) ? "獲得寶物" : "移動成功";
        }
        else if (currentAction == ActionType.Investigate && currentPlayer.role != PlayerRole.Thief)
        {
            var hasClue = MapManager.Instance.HasFootprint(selectedNodeId);
            result = hasClue ? "有小偷線索" : "無線索";
            isShared = false;
        }
        else if (currentAction == ActionType.Arrest && currentPlayer.role != PlayerRole.Thief)
        {
            var thief = GameManager.Instance.GetThiefAt(selectedNodeId);
            result = thief != null ? $"逮捕成功 ({thief.playerName})" : "逮捕失敗";
            isShared = true;
        }

        ActionLogger.Instance.Log(
            currentPlayer,
            GameManager.Instance.CurrentRound,
            selectedNodeId,
            currentAction.ToString(),
            result,
            isShared
        );

        if (currentPlayer.remainingSteps <= 0)
        {
            GameManager.Instance.NextPlayerTurn();
        }

        actionPanel.SetActive(false);
        selectedNodeId = -1;
    }
}
