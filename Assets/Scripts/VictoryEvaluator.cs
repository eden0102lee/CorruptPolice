using UnityEngine;

/// <summary>
/// 勝負條件判定。
/// </summary>
public class VictoryEvaluator : MonoBehaviour
{
    public static VictoryEvaluator Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public GameResult Evaluate(int currentRound, int maxRounds, int effectiveTreasures, int treasureGoal,
        bool allThievesArrested)
    {
        if (effectiveTreasures >= treasureGoal)
            return GameResult.Thieves;

        if (allThievesArrested)
            return GameResult.Police;

        if (currentRound > maxRounds)
            return effectiveTreasures >= treasureGoal ? GameResult.Thieves : GameResult.Police;

        return GameResult.None;
    }
}
