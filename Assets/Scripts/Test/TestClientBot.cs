using System.Collections.Generic;
using UnityEngine;

public static class TestClientBot
{
    public static bool TryGetPlacementNode(PlayerData player, out int nodeId)
    {
        nodeId = -1;
        if (MapManager.Instance == null)
            return false;

        var nodes = MapManager.Instance.GetAllNodeUIs();
        if (nodes == null || nodes.Count == 0)
            return false;

        int index = player != null && player.slotIndex >= 0
            ? player.slotIndex % nodes.Count
            : Random.Range(0, nodes.Count);

        nodeId = nodes[index].nodeId;
        return true;
    }

    public static bool TryGetTurnAction(PlayerData player, out int targetNodeId, out ActionType action)
    {
        targetNodeId = -1;
        action = ActionType.Move;

        if (player == null || MapManager.Instance == null || GameManager.Instance == null)
            return false;

        var neighbors = GetReachableNeighbors(player);
        if (neighbors.Count == 0)
            return false;

        if (player.role == PlayerRole.Thief)
        {
            targetNodeId = neighbors[Random.Range(0, neighbors.Count)];
            action = ActionType.Move;
            return true;
        }

        foreach (int node in neighbors)
        {
            if (GameManager.Instance.GetThiefAt(node) != null)
            {
                targetNodeId = node;
                action = ActionType.Arrest;
                return true;
            }
        }

        targetNodeId = neighbors[Random.Range(0, neighbors.Count)];
        action = ActionType.Investigate;
        return true;
    }

    static List<int> GetReachableNeighbors(PlayerData player)
    {
        var results = new List<int>();
        if (player.currentNodeId < 0)
            return results;

        var nodeData = MapManager.Instance.GetNodeData(player.currentNodeId);
        if (nodeData?.neighbors == null)
            return results;

        foreach (int neighbor in nodeData.neighbors)
        {
            if (MapManager.Instance.AreNodesConnected(player.currentNodeId, neighbor))
                results.Add(neighbor);
        }

        return results;
    }
}
