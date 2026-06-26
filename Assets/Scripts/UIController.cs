using System.Text;
using UnityEngine;
using TMPro;

/// <summary>
/// 遊戲 HUD：回合、計時、公告、投票與結算顯示。
/// </summary>
public class UIController : MonoBehaviour
{
    public static UIController Instance;

    public TMP_Text statusText;
    public TMP_Text timerText;
    public TMP_Text roomText;

    public GameObject sharePanel;
    public UnityEngine.UI.Button shareTeamButton;
    public UnityEngine.UI.Button sharePublicButton;
    public UnityEngine.UI.Button shareSecretButton;

    private InvestigationResult pendingInvestigation;
    private readonly StringBuilder logBuffer = new StringBuilder();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (sharePanel != null) sharePanel.SetActive(false);
        if (shareTeamButton != null) shareTeamButton.onClick.AddListener(() => ConfirmShare(ShareScope.Team));
        if (sharePublicButton != null) sharePublicButton.onClick.AddListener(() => ConfirmShare(ShareScope.Public));
        if (shareSecretButton != null) shareSecretButton.onClick.AddListener(() => ConfirmShare(ShareScope.Secret));
    }

    private void Start()
    {
        if (RoomManager.Instance != null && roomText != null)
            roomText.text = $"房號：{RoomManager.Instance.roomCode}";
    }

    public void ShowMessage(string message)
    {
        Debug.Log($"[UI] {message}");
        logBuffer.AppendLine(message);
        if (statusText != null)
            statusText.text = logBuffer.ToString();
    }

    public void UpdateTeamTimer(int teamIndex, float secondsRemaining)
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt(secondsRemaining / 60f);
        int seconds = Mathf.FloorToInt(secondsRemaining % 60f);
        timerText.text = $"隊伍 {teamIndex} 剩餘 {minutes:00}:{seconds:00}";
    }

    public void PromptInvestigationShare(InvestigationResult result)
    {
        pendingInvestigation = result;
        if (sharePanel != null)
        {
            sharePanel.SetActive(true);
            ShowMessage($"{result.investigator.playerName} 調查結果：{(result.hasClue ? "有線索" : "無線索")}，請選擇分享方式");
            return;
        }

        if (InvestigationSystem.Instance != null)
            InvestigationSystem.Instance.ShareResult(result, ShareScope.Team);
    }

    void ConfirmShare(ShareScope scope)
    {
        if (pendingInvestigation == null || InvestigationSystem.Instance == null) return;
        InvestigationSystem.Instance.ShareResult(pendingInvestigation, scope);
        pendingInvestigation = null;
        if (sharePanel != null) sharePanel.SetActive(false);
    }

    public void ShowVotingResult(VotingResult result)
    {
        foreach (var team in result.teamResults)
        {
            ShowMessage($"隊伍 {team.teamIndex} 投票結果：{team.accusedPlayerName}（{team.voteCount} 票）");
        }
    }

    public void ShowSettlement(SettlementResult settlement, GameResult gameResult)
    {
        ShowMessage($"對局結束：{(gameResult == GameResult.Thieves ? "小偷陣營" : "警察陣營")} 獲勝");

        foreach (var pair in settlement.playerTreasureShare)
            ShowMessage($"寶物分配 {pair.Key}：{pair.Value}");

        foreach (var pair in settlement.policeScores)
            ShowMessage($"警察得分 {pair.Key}：{pair.Value}");
    }

    public void SetLocalViewer(PlayerData viewer)
    {
        if (PlayerTokenManager.Instance != null)
            PlayerTokenManager.Instance.RefreshAllVisibility(viewer);
    }
}
