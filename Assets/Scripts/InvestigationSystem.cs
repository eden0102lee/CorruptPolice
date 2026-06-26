using System;
using System.Collections.Generic;
using UnityEngine;

public enum ShareScope
{
    Secret,
    Team,
    Public
}

/// <summary>
/// 警察調查與線索分享（可組內分享或公開，玩家可說謊）。
/// </summary>
public class InvestigationSystem : MonoBehaviour
{
    public static InvestigationSystem Instance;

    public event Action<PlayerData, int, bool, ShareScope> OnInvestigationCompleted;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public InvestigationResult Investigate(PlayerData investigator, int nodeId)
    {
        bool hasClue = MapManager.Instance != null && MapManager.Instance.HasFootprint(nodeId);
        return new InvestigationResult
        {
            investigator = investigator,
            nodeId = nodeId,
            hasClue = hasClue,
            reportedHasClue = hasClue
        };
    }

    public void ShareResult(InvestigationResult result, ShareScope scope)
    {
        string message = result.reportedHasClue ? "有線索" : "無線索";
        string scopeText = scope == ShareScope.Public ? "公開" : scope == ShareScope.Team ? "組內" : "保密";

        if (UIController.Instance != null)
        {
            UIController.Instance.ShowMessage(
                $"{result.investigator.playerName} 調查節點 {result.nodeId}：{message}（{scopeText}）");
        }

        if (ActionLogger.Instance != null && GameManager.Instance != null)
        {
            ActionLogger.Instance.Log(
                result.investigator,
                GameManager.Instance.CurrentRound,
                result.nodeId,
                ActionType.Investigate.ToString(),
                message,
                scope != ShareScope.Secret,
                scope);
        }

        OnInvestigationCompleted?.Invoke(result.investigator, result.nodeId, result.reportedHasClue, scope);
    }
}

[Serializable]
public class InvestigationResult
{
    public PlayerData investigator;
    public int nodeId;
    public bool hasClue;
    public bool reportedHasClue;
}
