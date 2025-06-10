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

    private bool placementMode = false;

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
        if (GameManager.Instance != null)
            currentPlayer.remainingSteps = GameManager.Instance.stepsPerTurn;
        originalNodeId = player.currentNodeId;
        selectedNodeId = -1;
        HighlightCurrentNode();
    }

    public void StartPlacement(PlayerData player)
    {
        placementMode = true;
        currentPlayer = player;
        selectedNodeId = -1;
        originalNodeId = -1;
        Debug.Log($"Select start position for {player.playerName}");
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
        if (PlayerTokenManager.Instance != null)
            PlayerTokenManager.Instance.UpdateTokenPosition(currentPlayer);
        currentPlayer.remainingSteps--;

        string result = string.Empty;
        bool isShared = true;

        if (currentAction == ActionType.Move && currentPlayer.role == PlayerRole.Thief)
        {
            MapManager.Instance.AddFootprint(selectedNodeId, currentPlayer.playerName);
            result = MapManager.Instance.HasTreasure(selectedNodeId) ? "Found treasure" : "No treasure";
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
            result = thief != null ? $"Arrested ({thief.playerName})" : "No thief";
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
