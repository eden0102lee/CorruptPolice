using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 回合流程：小偷階段 → 警察隊伍階段（含共享計時）。
/// </summary>
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public event Action<int> OnRoundStarted;
    public event Action<int> OnRoundEnded;
    public event Action<PlayerData> OnThiefTurnStarted;
    public event Action<PlayerData, int> OnThiefTurnEnded;
    public event Action<int, float> OnPoliceTeamTurnStarted;
    public event Action<int> OnPoliceTeamTurnEnded;

    private bool thiefPhase = true;
    private int currentThiefIndex;
    private int currentTeamTurnIndex;
    private int currentPlayerIndex;
    private float teamTimeRemaining;
    private Coroutine teamTimerRoutine;
    private bool teamTimedOut;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void BeginRound(int round, int startingTeamIndex)
    {
        thiefPhase = true;
        currentThiefIndex = 0;
        currentTeamTurnIndex = startingTeamIndex;
        currentPlayerIndex = 0;
        OnRoundStarted?.Invoke(round);
        AdvanceTurn();
    }

    public void AdvanceTurn()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver)
            return;

        if (thiefPhase)
            AdvanceThiefTurn();
        else
            AdvancePoliceTurn();
    }

    void AdvanceThiefTurn()
    {
        var thieves = GameManager.Instance.GetThieves();
        while (currentThiefIndex < thieves.Count && thieves[currentThiefIndex].isArrested)
            currentThiefIndex++;

        if (currentThiefIndex < thieves.Count)
        {
            var player = thieves[currentThiefIndex];
            player.remainingSteps = GameManager.Instance.Config.thiefStepsPerTurn;
            OnThiefTurnStarted?.Invoke(player);
            if (PlayerInputController.Instance != null)
                PlayerInputController.Instance.SetCurrentPlayer(player);
            currentThiefIndex++;
            return;
        }

        thiefPhase = false;
        currentTeamTurnIndex = (GameManager.Instance.CurrentRound - 1) % GameManager.Instance.Config.policeTeamCount;
        currentPlayerIndex = 0;
        BeginPoliceTeamTurn(currentTeamTurnIndex);
        AdvancePoliceTurn();
    }

    public void CompleteThiefTurn(PlayerData thief)
    {
        OnThiefTurnEnded?.Invoke(thief, thief.currentNodeId);
        GameManager.Instance.NotifyCorruptPoliceOfThiefPosition(thief);
        AdvanceTurn();
    }

    void BeginPoliceTeamTurn(int teamIndex)
    {
        teamTimedOut = false;
        teamTimeRemaining = GameManager.Instance.Config.policeTeamTimeSeconds;

        var team = GameManager.Instance.GetPoliceTeams()[teamIndex];
        foreach (var p in team)
            p.ResetTeamTurnFlag();

        if (teamTimerRoutine != null)
            StopCoroutine(teamTimerRoutine);
        teamTimerRoutine = StartCoroutine(PoliceTeamTimer(teamIndex));

        OnPoliceTeamTurnStarted?.Invoke(teamIndex, teamTimeRemaining);
    }

    IEnumerator PoliceTeamTimer(int teamIndex)
    {
        while (teamTimeRemaining > 0f && !teamTimedOut)
        {
            teamTimeRemaining -= Time.deltaTime;
            if (UIController.Instance != null)
                UIController.Instance.UpdateTeamTimer(teamIndex, teamTimeRemaining);
            yield return null;
        }

        teamTimedOut = true;
        if (UIController.Instance != null)
            UIController.Instance.ShowMessage($"隊伍 {teamIndex} 行動時間結束，剩餘玩家失去本回合行動。");

        SkipRemainingPoliceInTeam(teamIndex);
        AdvancePoliceTurn();
    }

    void SkipRemainingPoliceInTeam(int teamIndex)
    {
        var team = GameManager.Instance.GetPoliceTeams()[teamIndex];
        foreach (var p in team)
        {
            if (!p.hasActedThisTeamTurn && !p.isArrested)
                p.hasActedThisTeamTurn = true;
        }
    }

    void AdvancePoliceTurn()
    {
        if (GameManager.Instance.IsGameOver)
            return;

        int policeTeamCount = GameManager.Instance.Config.policeTeamCount;
        int roundStartTeam = (GameManager.Instance.CurrentRound - 1) % policeTeamCount;
        var team = GameManager.Instance.GetPoliceTeams()[currentTeamTurnIndex];

        if (teamTimedOut)
        {
            EndPoliceTeamAndMaybeRound(roundStartTeam, policeTeamCount);
            return;
        }

        while (currentPlayerIndex < team.Count &&
               (team[currentPlayerIndex].isArrested || team[currentPlayerIndex].hasActedThisTeamTurn))
            currentPlayerIndex++;

        if (currentPlayerIndex < team.Count)
        {
            var player = team[currentPlayerIndex];
            player.remainingSteps = GameManager.Instance.Config.policeStepsPerTurn;
            if (PlayerInputController.Instance != null)
                PlayerInputController.Instance.SetCurrentPlayer(player);
            currentPlayerIndex++;
            return;
        }

        EndPoliceTeamAndMaybeRound(roundStartTeam, policeTeamCount);
    }

    void EndPoliceTeamAndMaybeRound(int roundStartTeam, int policeTeamCount)
    {
        if (teamTimerRoutine != null)
        {
            StopCoroutine(teamTimerRoutine);
            teamTimerRoutine = null;
        }

        OnPoliceTeamTurnEnded?.Invoke(currentTeamTurnIndex);

        currentTeamTurnIndex = (currentTeamTurnIndex + 1) % policeTeamCount;
        if (currentTeamTurnIndex == roundStartTeam)
        {
            OnRoundEnded?.Invoke(GameManager.Instance.CurrentRound);
            GameManager.Instance.EndRound();
            return;
        }

        currentPlayerIndex = 0;
        BeginPoliceTeamTurn(currentTeamTurnIndex);
        AdvancePoliceTurn();
    }

    public void CompletePoliceAction(PlayerData police)
    {
        police.hasActedThisTeamTurn = true;
        AdvanceTurn();
    }
}
