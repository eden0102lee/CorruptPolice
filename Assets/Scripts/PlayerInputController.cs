using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 控制玩家操作，包括移動、調查與逮捕行動的選擇與執行
/// </summary>
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
    private bool placementMode;
    private ActionType currentAction;

    void Awake()
    {
        Instance = this;
        if (actionPanel != null)
            actionPanel.SetActive(false);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmAction);
        if (moveButton != null)
            moveButton.onClick.AddListener(() => SetActionType(ActionType.Move));
        if (investigateButton != null)
            investigateButton.onClick.AddListener(() => SetActionType(ActionType.Investigate));
        if (arrestButton != null)
            arrestButton.onClick.AddListener(() => SetActionType(ActionType.Arrest));
    }

    public void CancelPlacement()
    {
        placementMode = false;
        selectedNodeId = -1;
        if (actionPanel != null)
            actionPanel.SetActive(false);
    }

    public void SetCurrentPlayer(PlayerData player)
    {
        currentPlayer = player;
        if (GameManager.Instance != null)
        {
            currentPlayer.remainingSteps = player.role == PlayerRole.Thief
                ? GameManager.Instance.Config.thiefStepsPerTurn
                : GameManager.Instance.Config.policeStepsPerTurn;
        }

        selectedNodeId = -1;
        HighlightCurrentNode();

        int round = GameManager.Instance != null ? GameManager.Instance.CurrentRound : -1;
        Debug.Log($"[Turn] Round {round} - {currentPlayer.playerName} ({currentPlayer.role}) begins with {currentPlayer.remainingSteps} steps");

        if (GameAgentController.Instance != null)
            GameAgentController.Instance.OnTurnStarted(player, false);
    }

    public void StartPlacement(PlayerData player)
    {
        placementMode = true;
        currentPlayer = player;
        selectedNodeId = -1;
        Debug.Log($"Select start position for {player.playerName}");

        if (GameAgentController.Instance != null)
            GameAgentController.Instance.OnTurnStarted(player, true);
    }

    void HighlightCurrentNode()
    {
        if (MapManager.Instance == null || currentPlayer == null)
            return;

        foreach (var ui in MapManager.Instance.GetAllNodeUIs())
            ui.Unhighlight();

        var currentUI = MapManager.Instance.GetNodeUI(currentPlayer.currentNodeId);
        if (currentUI != null) currentUI.Highlight();
    }

    public void OnNodeClicked(int nodeId)
    {
        if (MapManager.Instance == null)
            return;

        if (placementMode)
        {
            if (currentPlayer == null) return;

            if (GameStartManager.Instance != null)
            {
                GameStartManager.Instance.ConfirmPlacement(currentPlayer, nodeId);
                placementMode = false;
                MapManager.Instance.GetNodeUI(nodeId)?.Highlight();
                return;
            }

            currentPlayer.currentNodeId = nodeId;
            if (PlayerTokenManager.Instance != null)
                PlayerTokenManager.Instance.UpdateTokenPosition(currentPlayer);
            placementMode = false;
            if (GameManager.Instance != null)
                GameManager.Instance.OnPlacementComplete();
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

        if (currentPlayer.role == PlayerRole.Thief)
            PerformAction(ActionType.Move, nodeId);
        else
            ShowActionPanel();
    }

    void ShowActionPanel()
    {
        if (actionPanel == null || currentPlayer == null)
            return;

        actionPanel.SetActive(true);
        if (moveButton != null)
            moveButton.gameObject.SetActive(false);
        if (investigateButton != null)
            investigateButton.gameObject.SetActive(true);
        if (arrestButton != null)
            arrestButton.gameObject.SetActive(true);
    }

    void SetActionType(ActionType action)
    {
        currentAction = action;
    }

    void OnConfirmAction()
    {
        if (selectedNodeId == -1 || currentPlayer == null) return;
        PerformAction(currentAction, selectedNodeId);
    }

    public bool PerformAction(ActionType action, int targetNodeId)
    {
        if (currentPlayer == null || MapManager.Instance == null)
            return false;

        if (!MapManager.Instance.AreNodesConnected(currentPlayer.currentNodeId, targetNodeId))
            return false;

        currentAction = action;
        selectedNodeId = targetNodeId;

        currentPlayer.currentNodeId = targetNodeId;
        if (PlayerTokenManager.Instance != null)
            PlayerTokenManager.Instance.UpdateTokenPosition(currentPlayer);

        currentPlayer.remainingSteps--;

        int round = GameManager.Instance != null ? GameManager.Instance.CurrentRound : -1;
        Debug.Log($"[Action] Round {round} - {currentPlayer.playerName} performed {action} on node {targetNodeId}. Remaining steps: {currentPlayer.remainingSteps}");

        if (action == ActionType.Move && currentPlayer.role == PlayerRole.Thief)
            HandleThiefMove(targetNodeId);
        else if (action == ActionType.Investigate && currentPlayer.role != PlayerRole.Thief)
            HandleInvestigate(targetNodeId);
        else if (action == ActionType.Arrest && currentPlayer.role != PlayerRole.Thief)
            HandleArrest(targetNodeId);

        if (currentPlayer.remainingSteps <= 0 && GameManager.Instance != null)
            GameManager.Instance.OnPlayerTurnComplete(currentPlayer);

        if (actionPanel != null)
            actionPanel.SetActive(false);
        selectedNodeId = -1;
        return true;
    }

    void HandleThiefMove(int targetNodeId)
    {
        MapManager.Instance.AddFootprint(targetNodeId, currentPlayer.playerName);
        bool collected = MapManager.Instance.CollectTreasure(targetNodeId);
        string result = collected ? "Found treasure" : "No treasure";

        if (collected && GameManager.Instance != null)
            GameManager.Instance.RecordTreasureForThief(currentPlayer);

        if (ActionLogger.Instance != null && GameManager.Instance != null)
        {
            ActionLogger.Instance.Log(
                currentPlayer,
                GameManager.Instance.CurrentRound,
                targetNodeId,
                ActionType.Move.ToString(),
                result,
                true,
                ShareScope.Public);
        }
    }

    void HandleInvestigate(int targetNodeId)
    {
        if (InvestigationSystem.Instance == null) return;

        var result = InvestigationSystem.Instance.Investigate(currentPlayer, targetNodeId);
        if (UIController.Instance != null)
            UIController.Instance.PromptInvestigationShare(result);
        else
            InvestigationSystem.Instance.ShareResult(result, ShareScope.Team);
    }

    void HandleArrest(int targetNodeId)
    {
        var thief = GameManager.Instance.GetThiefAt(targetNodeId);
        string result;

        if (currentPlayer.role == PlayerRole.CorruptPolice)
        {
            result = "Arrest failed (corrupt police)";
        }
        else if (thief != null)
        {
            GameManager.Instance.ArrestThief(thief, currentPlayer);
            result = $"Arrested ({thief.playerName})";
        }
        else
        {
            result = "No thief";
        }

        if (ActionLogger.Instance != null)
        {
            ActionLogger.Instance.Log(
                currentPlayer,
                GameManager.Instance.CurrentRound,
                targetNodeId,
                ActionType.Arrest.ToString(),
                result,
                true,
                ShareScope.Public);
        }
    }
}
