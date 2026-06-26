using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 遊戲 AI 智能體：依企劃規則自動完成放置與回合行動。
/// </summary>
public class GameAgentController : MonoBehaviour
{
    public static GameAgentController Instance;

    [Header("Agent Settings")]
    public bool enableAgent = true;
    public float thinkDelay = 0.6f;

    [Header("Control Scope")]
    public bool autoPlayThieves = true;
    public bool autoPlayPolice = true;
    public bool autoPlayPlacement = true;

    [Tooltip("留空表示全部交由 AI；填入玩家名稱則該玩家由人類操作")]
    public string humanPlayerName = "";

    private Coroutine activeRoutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool ShouldControl(PlayerData player)
    {
        if (!enableAgent || player == null || player.isArrested || player.isSpectator)
            return false;

        if (!string.IsNullOrEmpty(humanPlayerName) && player.playerName == humanPlayerName)
            return false;

        if (player.role == PlayerRole.Thief)
            return autoPlayThieves;

        return autoPlayPolice;
    }

    public void OnTurnStarted(PlayerData player, bool isPlacement)
    {
        if (!ShouldControl(player))
            return;

        if (isPlacement && !autoPlayPlacement)
            return;

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(isPlacement ? PlayPlacement(player) : PlayTurn(player));
    }

    IEnumerator PlayPlacement(PlayerData player)
    {
        yield return new WaitForSeconds(thinkDelay);

        int nodeId = ChoosePlacementNode(player);
        if (nodeId < 0)
        {
            Debug.LogWarning($"[Agent] {player.playerName} could not find a placement node.");
            yield break;
        }

        if (PlayerInputController.Instance != null)
            PlayerInputController.Instance.OnNodeClicked(nodeId);
    }

    IEnumerator PlayTurn(PlayerData player)
    {
        while (player != null && player.remainingSteps > 0 && ShouldControl(player))
        {
            yield return new WaitForSeconds(thinkDelay);

            if (!TryDecideAction(player, out int targetNode, out ActionType action))
            {
                Debug.LogWarning($"[Agent] {player.playerName} has no valid action.");
                break;
            }

            if (PlayerInputController.Instance == null)
                break;

            bool acted = PlayerInputController.Instance.PerformAction(action, targetNode);
            if (!acted)
            {
                Debug.LogWarning($"[Agent] {player.playerName} failed to perform {action} on node {targetNode}.");
                break;
            }

            if (player.remainingSteps <= 0)
                break;
        }

        activeRoutine = null;
    }

    int ChoosePlacementNode(PlayerData player)
    {
        if (MapManager.Instance == null) return -1;

        var candidates = new List<int>();
        foreach (var ui in MapManager.Instance.GetAllNodeUIs())
        {
            if (player.role == PlayerRole.Thief && MapManager.Instance.HasTreasure(ui.nodeId))
                continue;
            candidates.Add(ui.nodeId);
        }

        if (candidates.Count == 0) return -1;

        if (player.role == PlayerRole.Thief)
            return PickFarthestFromPolice(candidates);

        return candidates[Random.Range(0, candidates.Count)];
    }

    int PickFarthestFromPolice(List<int> candidates)
    {
        int bestNode = candidates[0];
        int bestDist = -1;

        foreach (var nodeId in candidates)
        {
            int minPoliceDist = int.MaxValue;
            foreach (var p in GameManager.Instance.GetAllPlayers())
            {
                if (p.role == PlayerRole.Thief || p.currentNodeId < 0)
                    continue;
                int d = MapManager.Instance.GetShortestDistance(nodeId, p.currentNodeId);
                if (d < minPoliceDist)
                    minPoliceDist = d;
            }

            if (minPoliceDist > bestDist)
            {
                bestDist = minPoliceDist;
                bestNode = nodeId;
            }
        }

        return bestNode;
    }

    bool TryDecideAction(PlayerData player, out int targetNode, out ActionType action)
    {
        targetNode = -1;
        action = ActionType.Move;

        if (MapManager.Instance == null || player.currentNodeId < 0)
            return false;

        var neighbors = MapManager.Instance.GetNeighborIds(player.currentNodeId);
        if (neighbors.Count == 0)
            return false;

        if (player.role == PlayerRole.Thief)
            return DecideThiefAction(player, neighbors, out targetNode, out action);

        return DecidePoliceAction(player, neighbors, out targetNode, out action);
    }

    bool DecideThiefAction(PlayerData player, List<int> neighbors, out int targetNode, out ActionType action)
    {
        action = ActionType.Move;
        targetNode = PickThiefMove(player, neighbors);
        return targetNode >= 0;
    }

    int PickThiefMove(PlayerData player, List<int> neighbors)
    {
        int bestNode = -1;
        int bestScore = int.MinValue;

        foreach (var nodeId in neighbors)
        {
            int score = 0;

            if (MapManager.Instance.HasTreasure(nodeId))
                score += 20;

            if (MapManager.Instance.HasFootprint(nodeId))
                score -= 2;

            foreach (var p in GameManager.Instance.GetAllPlayers())
            {
                if (p.role == PlayerRole.Thief || p.isArrested || p.currentNodeId < 0)
                    continue;

                int dist = MapManager.Instance.GetShortestDistance(nodeId, p.currentNodeId);
                if (dist <= 1) score -= 10;
                else if (dist == 2) score -= 3;
            }

            score += Random.Range(0, 3);

            if (score > bestScore)
            {
                bestScore = score;
                bestNode = nodeId;
            }
        }

        if (bestNode < 0 && neighbors.Count > 0)
            bestNode = neighbors[Random.Range(0, neighbors.Count)];

        return bestNode;
    }

    bool DecidePoliceAction(PlayerData player, List<int> neighbors, out int targetNode, out ActionType action)
    {
        if (player.role != PlayerRole.CorruptPolice)
        {
            foreach (var nodeId in neighbors)
            {
                if (GameManager.Instance.GetThiefAt(nodeId) != null)
                {
                    targetNode = nodeId;
                    action = ActionType.Arrest;
                    return true;
                }
            }
        }

        foreach (var nodeId in neighbors)
        {
            if (MapManager.Instance.HasFootprint(nodeId))
            {
                targetNode = nodeId;
                action = ActionType.Investigate;
                return true;
            }
        }

        int clueTarget = FindNearestFootprintNode(player.currentNodeId);
        if (clueTarget >= 0)
        {
            int step = MapManager.Instance.GetNextStepToward(player.currentNodeId, clueTarget);
            if (step >= 0 && neighbors.Contains(step))
            {
                targetNode = step;
                action = ActionType.Investigate;
                return true;
            }
        }

        targetNode = neighbors[Random.Range(0, neighbors.Count)];
        action = player.role == PlayerRole.CorruptPolice ? ActionType.Investigate : ActionType.Investigate;
        return true;
    }

    int FindNearestFootprintNode(int fromId)
    {
        var clueNodes = MapManager.Instance.GetNodesWithFootprints();
        if (clueNodes.Count == 0)
            return -1;

        int best = -1;
        int bestDist = int.MaxValue;
        foreach (var nodeId in clueNodes)
        {
            int d = MapManager.Instance.GetShortestDistance(fromId, nodeId);
            if (d < bestDist)
            {
                bestDist = d;
                best = nodeId;
            }
        }
        return best;
    }
}
