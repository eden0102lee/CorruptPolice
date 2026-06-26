using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 結算時寶物分配與警察得分計算。
/// </summary>
public class TreasureDistributionSystem : MonoBehaviour
{
    public static TreasureDistributionSystem Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public SettlementResult CalculateSettlement(GameResult gameResult, VotingResult votingResult)
    {
        var result = new SettlementResult();
        if (GameManager.Instance == null) return result;

        var thieves = GameManager.Instance.GetThieves();
        var allPlayers = GameManager.Instance.GetAllPlayers();
        int effectiveTreasures = GameManager.Instance.GetEffectiveTreasureCount();

        var survivingThieves = thieves.Where(t => !t.isArrested).ToList();
        var corruptPolice = allPlayers.Where(p => p.role == PlayerRole.CorruptPolice).ToList();
        var identifiedSet = new HashSet<PlayerData>(votingResult.identifiedCorrupt);
        var survivingUnidentifiedCorrupt = corruptPolice
            .Where(c => !c.isArrested && !identifiedSet.Contains(c))
            .ToList();

        int thiefPool = effectiveTreasures;
        int corruptShare = 0;

        if (survivingUnidentifiedCorrupt.Count > 0 && corruptPolice.Count > identifiedSet.Count)
        {
            corruptShare = thiefPool / 2;
            thiefPool -= corruptShare;
            int perCorrupt = corruptShare / survivingUnidentifiedCorrupt.Count;
            foreach (var c in survivingUnidentifiedCorrupt)
                result.playerTreasureShare[c.playerName] = perCorrupt;
        }
        else if (survivingThieves.Count > 0)
        {
            foreach (var t in survivingThieves)
                result.playerTreasureShare[t.playerName] = t.treasureCount;
        }

        if (survivingThieves.Count > 0 && thiefPool > 0)
        {
            int perThief = thiefPool / survivingThieves.Count;
            foreach (var t in survivingThieves)
            {
                if (!result.playerTreasureShare.ContainsKey(t.playerName))
                    result.playerTreasureShare[t.playerName] = 0;
                result.playerTreasureShare[t.playerName] += perThief;
            }
        }

        foreach (var p in allPlayers)
        {
            if (p.role != PlayerRole.Police) continue;

            int score = 0;
            if (gameResult == GameResult.Police)
                score += 1;

            if (votingResult.identifiedCorrupt.Exists(c => c.teamIndex == p.teamIndex &&
                votingResult.teamResults.Exists(tr =>
                    tr.teamIndex == p.teamIndex && tr.accusedPlayerName == c.playerName)))
            {
                score += 1;
            }

            if (votingResult.teamResults.Exists(tr =>
                tr.teamIndex == p.teamIndex && tr.accusedPlayerName == p.playerName))
                score = 0;

            result.policeScores[p.playerName] = score;
        }

        return result;
    }
}

[System.Serializable]
public class SettlementResult
{
    public Dictionary<string, int> playerTreasureShare = new Dictionary<string, int>();
    public Dictionary<string, int> policeScores = new Dictionary<string, int>();
}
