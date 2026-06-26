using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 遊戲結束後各警察隊投票指認腐敗警察。
/// </summary>
public class VotingSystem : MonoBehaviour
{
    public static VotingSystem Instance;

    public event Action<VotingResult> OnVotingCompleted;

    private readonly Dictionary<int, Dictionary<string, string>> teamVotes =
        new Dictionary<int, Dictionary<string, string>>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public VotingResult LastResult { get; private set; }

    public void BeginVoting(IEnumerable<PlayerData> allPlayers)
    {
        teamVotes.Clear();
        if (GameManager.Instance == null) return;

        foreach (var team in GameManager.Instance.GetPoliceTeams())
        {
            if (team.Count == 0) continue;
            int teamIndex = team[0].teamIndex;
            teamVotes[teamIndex] = new Dictionary<string, string>();

            var voters = team.FindAll(p => p.role == PlayerRole.Police && !p.isSpectator);
            foreach (var voter in voters)
            {
                string target = AutoVoteTarget(team, voter);
                CastVote(teamIndex, voter.playerName, target);
            }
        }

        LastResult = BuildResult(allPlayers);
        OnVotingCompleted?.Invoke(LastResult);

        if (UIController.Instance != null)
            UIController.Instance.ShowVotingResult(LastResult);
    }

    public void CastVote(int teamIndex, string voterName, string targetPlayerName)
    {
        if (!teamVotes.ContainsKey(teamIndex))
            teamVotes[teamIndex] = new Dictionary<string, string>();

        teamVotes[teamIndex][voterName] = targetPlayerName;
    }

    string AutoVoteTarget(List<PlayerData> team, PlayerData voter)
    {
        var corrupt = team.Where(p => p.role == PlayerRole.CorruptPolice).ToList();
        if (corrupt.Count == 0) return "無人";

        if (UnityEngine.Random.value < 0.5f)
            return corrupt[UnityEngine.Random.Range(0, corrupt.Count)].playerName;

        return "無人";
    }

    public VotingResult BuildResult(IEnumerable<PlayerData> allPlayers)
    {
        var result = new VotingResult();
        var playerLookup = allPlayers.ToDictionary(p => p.playerName);

        foreach (var teamPair in teamVotes)
        {
            var tally = new Dictionary<string, int>();
            foreach (var vote in teamPair.Value.Values)
            {
                if (!tally.ContainsKey(vote)) tally[vote] = 0;
                tally[vote]++;
            }

            string winner = "無人";
            int maxVotes = 0;
            foreach (var pair in tally)
            {
                if (pair.Value > maxVotes)
                {
                    maxVotes = pair.Value;
                    winner = pair.Key;
                }
            }

            var teamResult = new TeamVoteResult
            {
                teamIndex = teamPair.Key,
                accusedPlayerName = winner,
                voteCount = maxVotes
            };
            result.teamResults.Add(teamResult);

            if (winner != "無人" && playerLookup.TryGetValue(winner, out var accused) &&
                accused.role == PlayerRole.CorruptPolice)
            {
                result.identifiedCorrupt.Add(accused);
            }
        }

        return result;
    }
}

[Serializable]
public class VotingResult
{
    public List<TeamVoteResult> teamResults = new List<TeamVoteResult>();
    public List<PlayerData> identifiedCorrupt = new List<PlayerData>();
}

[Serializable]
public class TeamVoteResult
{
    public int teamIndex;
    public string accusedPlayerName;
    public int voteCount;
}
