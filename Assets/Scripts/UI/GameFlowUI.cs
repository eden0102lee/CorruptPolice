using UnityEngine;
using TMPro;

public class GameFlowUI : MonoBehaviour
{
    public TMP_Text phaseLabel;
    public TMP_Text turnLabel;
    public TMP_Text roundLabel;
    public TMP_Text resultLabel;
    public GameObject resultPanel;

    void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFlowStateChanged += HandleFlowStateChanged;
            GameManager.Instance.OnTurnChanged += HandleTurnChanged;
            GameManager.Instance.OnGameOver += HandleGameOver;
            RefreshAll();
        }
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFlowStateChanged -= HandleFlowStateChanged;
            GameManager.Instance.OnTurnChanged -= HandleTurnChanged;
            GameManager.Instance.OnGameOver -= HandleGameOver;
        }
    }

    void HandleFlowStateChanged(GameFlowState state)
    {
        if (phaseLabel != null)
        {
            phaseLabel.text = state switch
            {
                GameFlowState.Lobby => "大廳",
                GameFlowState.Placement => "初始放置",
                GameFlowState.Playing => "進行中",
                GameFlowState.GameOver => "遊戲結束",
                _ => state.ToString()
            };
        }

        if (resultPanel != null)
            resultPanel.SetActive(state == GameFlowState.GameOver);
    }

    void HandleTurnChanged(PlayerData player)
    {
        if (turnLabel == null)
            return;

        if (player == null)
        {
            turnLabel.text = "等待中...";
            return;
        }

        turnLabel.text = $"{player.playerName} ({player.role})";
    }

    void HandleGameOver(GameResult result)
    {
        if (resultLabel == null)
            return;

        resultLabel.text = result switch
        {
            GameResult.Police => "警察陣營獲勝",
            GameResult.Thieves => "小偷陣營獲勝",
            _ => "平手"
        };

        if (resultPanel != null)
            resultPanel.SetActive(true);
    }

    void RefreshAll()
    {
        if (GameManager.Instance == null)
            return;

        HandleFlowStateChanged(GameManager.Instance.FlowState);
        HandleTurnChanged(GameManager.Instance.CurrentTurnPlayer);

        if (roundLabel != null)
            roundLabel.text = $"第 {GameManager.Instance.CurrentRound} 回合";

        if (GameManager.Instance.Result != GameResult.None)
            HandleGameOver(GameManager.Instance.Result);
    }

    void Update()
    {
        if (roundLabel != null && GameManager.Instance != null)
            roundLabel.text = $"第 {GameManager.Instance.CurrentRound} 回合";
    }
}
