using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.UI;


[System.Serializable]
public class MapData
{
    public List<MapNodeData> nodes;
}

[System.Serializable]
public class TreasureRecord
{
    public int nodeId;
    public bool collected;
}

[System.Serializable]
public class TreasureLog
{
    public List<TreasureRecord> treasures = new List<TreasureRecord>();
}

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    public TextAsset mapJsonFile;
    public GameObject nodePrefab;
    public Transform nodeParent;
    public Transform pieceUIParent;
    public GameObject pieceUIPrefab;

    [Tooltip("Automatically build map on Start")] public bool autoBuildOnStart = true;

    [Header("Treasure Settings")]
    [Tooltip("Number of treasures placed in each game")] public int treasureCount = 20;

    private TreasureLog treasureLog = new TreasureLog();
    private string treasureLogPath;

    private void SaveTreasureLog()
    {
        if (!string.IsNullOrEmpty(treasureLogPath))
            File.WriteAllText(treasureLogPath, JsonUtility.ToJson(treasureLog, true));
    }

    private Dictionary<int, MapNodeData> nodeDataDict = new Dictionary<int, MapNodeData>();
    private Dictionary<int, GameObject> nodeGameObjects = new Dictionary<int, GameObject>();
    private Dictionary<int, NodeUI> nodeUIs = new Dictionary<int, NodeUI>();
    private HashSet<int> treasures = new HashSet<int>();
    private Dictionary<int, List<string>> footprints = new Dictionary<int, List<string>>();
    private Dictionary<int, GameObject> treasurePieces = new Dictionary<int, GameObject>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        treasureLogPath = Path.Combine(Application.persistentDataPath, "treasure_log.json");
        if (autoBuildOnStart)
        {
            BuildMap();
        }
    }

    public void BuildMap()
    {
        LoadAndBuildMap();
        if (PlayerTokenManager.Instance != null && GameManager.Instance != null)
        {
            PlayerTokenManager.Instance.CreateTokens(GameManager.Instance.GetAllPlayers());
        }
    }

    public void LoadAndBuildMap()
    {
        if (mapJsonFile == null)
        {
            Debug.LogError("Map JSON file not assigned.");
            return;
        }

        MapData mapData = JsonUtility.FromJson<MapData>(mapJsonFile.text);

        // Place treasures so that they are at least two nodes apart
        int treasureTotal = Mathf.Min(treasureCount, mapData.nodes.Count);
        foreach (var node in mapData.nodes)
            node.hasTreasure = false;

        var tempDict = mapData.nodes.ToDictionary(n => n.id);
        var available = new List<MapNodeData>(mapData.nodes);
        var chosen = new List<MapNodeData>();

        void RemoveNearby(MapNodeData center)
        {
            var remove = new List<MapNodeData>();
            foreach (var cand in available)
            {
                if (GetGraphDistance(tempDict, center.id, cand.id) <= 2)
                    remove.Add(cand);
            }
            foreach (var r in remove)
                available.Remove(r);
        }

        if (treasureTotal > 0 && available.Count > 0)
        {
            int idx = Random.Range(0, available.Count);
            var first = available[idx];
            first.hasTreasure = true;
            chosen.Add(first);
            available.RemoveAt(idx);
            RemoveNearby(first);
        }

        while (chosen.Count < treasureTotal && available.Count > 0)
        {
            MapNodeData best = null;
            int bestDist = -1;
            foreach (var cand in available)
            {
                int minDist = int.MaxValue;
                foreach (var c in chosen)
                {
                    int d = GetGraphDistance(tempDict, cand.id, c.id);
                    if (d < minDist) minDist = d;
                }
                if (minDist > bestDist)
                {
                    bestDist = minDist;
                    best = cand;
                }
            }

            if (best == null || bestDist <= 2)
                break;

            best.hasTreasure = true;
            chosen.Add(best);
            available.Remove(best);
            RemoveNearby(best);
        }

        treasureLog.treasures.Clear();
        foreach (var node in mapData.nodes)
        {
            if (node.hasTreasure)
                treasureLog.treasures.Add(new TreasureRecord { nodeId = node.id, collected = false });
        }
        File.WriteAllText(treasureLogPath, JsonUtility.ToJson(treasureLog, true));
        nodeDataDict.Clear();
        nodeGameObjects.Clear();
        nodeUIs.Clear();
        treasures.Clear();
        footprints.Clear();
        foreach (var piece in treasurePieces.Values)
            Destroy(piece);
        treasurePieces.Clear();

        foreach (var node in mapData.nodes)
        {
            nodeDataDict[node.id] = node;
        }

        foreach (var node in mapData.nodes)
        {
            GameObject go = Instantiate(nodePrefab, nodeParent);
            go.name = $"Node_{node.id}";
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(node.x, node.y);

            NodeUI ui = go.GetComponent<NodeUI>();
            if (ui != null)
                ui.SetId(node.id);

            nodeGameObjects[node.id] = go;
            nodeUIs[node.id] = ui;

            if (node.hasTreasure)
            {
                AddTreasure(node.id);
                if (pieceUIPrefab != null)
                {
                    Transform parent = pieceUIParent == null ? nodeParent : pieceUIParent;
                    GameObject piece = Instantiate(pieceUIPrefab, parent);
                    piece.name = $"Treasure_{node.id}";
                    RectTransform pieceRect = piece.GetComponent<RectTransform>();
                    if (pieceRect != null)
                        pieceRect.anchoredPosition = rect.anchoredPosition;
                    else
                        piece.transform.position = rect.position;
                    treasurePieces[node.id] = piece;
                }
            }
        }
    }

    public bool AreNodesConnected(int fromId, int toId)
    {
        return nodeDataDict.ContainsKey(fromId) && nodeDataDict[fromId].neighbors.Contains(toId);
    }

    public MapNodeData GetNodeData(int id)
    {
        nodeDataDict.TryGetValue(id, out MapNodeData data);
        return data;
    }

    public GameObject GetNodeObject(int id)
    {
        nodeGameObjects.TryGetValue(id, out GameObject go);
        return go;
    }

    public NodeUI GetNodeUI(int id)
    {
        nodeUIs.TryGetValue(id, out NodeUI ui);
        return ui;
    }

    public List<NodeUI> GetAllNodeUIs()
    {
        return new List<NodeUI>(nodeUIs.Values);
    }

    public void ClearLoadedMap()
    {
        foreach (var go in nodeGameObjects.Values)
            Destroy(go);
        nodeGameObjects.Clear();
        nodeUIs.Clear();
        foreach (var piece in treasurePieces.Values)
            Destroy(piece);
        treasurePieces.Clear();
        nodeDataDict.Clear();
        treasures.Clear();
        footprints.Clear();
    }

    public void AddTreasure(int nodeId)
    {
        treasures.Add(nodeId);
    }

    public void SetTreasure(int nodeId, bool present)
    {
        if (!nodeDataDict.ContainsKey(nodeId)) return;

        var node = nodeDataDict[nodeId];

        if (present)
        {
            if (treasures.Add(nodeId))
            {
                node.hasTreasure = true;

                if (!treasurePieces.ContainsKey(nodeId) && pieceUIPrefab != null && nodeGameObjects.ContainsKey(nodeId))
                {
                    Transform parent = pieceUIParent == null ? nodeParent : pieceUIParent;
                    GameObject piece = Instantiate(pieceUIPrefab, parent);
                    piece.name = $"Treasure_{nodeId}";
                    RectTransform nodeRect = nodeGameObjects[nodeId].GetComponent<RectTransform>();
                    RectTransform pieceRect = piece.GetComponent<RectTransform>();
                    if (nodeRect != null && pieceRect != null)
                        pieceRect.anchoredPosition = nodeRect.anchoredPosition;
                    else if (nodeRect != null)
                        piece.transform.position = nodeRect.position;
                    treasurePieces[nodeId] = piece;
                }

                if (treasureLog.treasures.Find(t => t.nodeId == nodeId) == null)
                    treasureLog.treasures.Add(new TreasureRecord { nodeId = nodeId, collected = false });
                SaveTreasureLog();
            }
        }
        else
        {
            if (treasures.Remove(nodeId))
            {
                node.hasTreasure = false;

                if (treasurePieces.ContainsKey(nodeId))
                {
                    Destroy(treasurePieces[nodeId]);
                    treasurePieces.Remove(nodeId);
                }

                var rec = treasureLog.treasures.Find(t => t.nodeId == nodeId && !t.collected);
                if (rec != null)
                {
                    treasureLog.treasures.Remove(rec);
                    SaveTreasureLog();
                }
            }
        }
    }

    public bool CollectTreasure(int nodeId)
    {
        if (!treasures.Remove(nodeId))
            return false;

        if (treasurePieces.ContainsKey(nodeId))
        {
            Destroy(treasurePieces[nodeId]);
            treasurePieces.Remove(nodeId);
        }

        var record = treasureLog.treasures.Find(t => t.nodeId == nodeId);
        if (record != null)
        {
            record.collected = true;
            File.WriteAllText(treasureLogPath, JsonUtility.ToJson(treasureLog, true));
        }
        return true;
    }

    public bool HasTreasure(int nodeId)
    {
        return treasures.Contains(nodeId);
    }

    public void AddFootprint(int nodeId, string thiefName)
    {
        if (!footprints.ContainsKey(nodeId)) footprints[nodeId] = new List<string>();
        if (!footprints[nodeId].Contains(thiefName)) footprints[nodeId].Add(thiefName);
    }

    public bool HasFootprint(int nodeId)
    {
        return footprints.ContainsKey(nodeId) && footprints[nodeId].Count > 0;
    }

    private int GetGraphDistance(Dictionary<int, MapNodeData> dict, int startId, int endId)
    {
        if (startId == endId) return 0;
        var visited = new HashSet<int> { startId };
        var q = new Queue<(int id, int dist)>();
        q.Enqueue((startId, 0));
        while (q.Count > 0)
        {
            var (id, dist) = q.Dequeue();
            foreach (var n in dict[id].neighbors)
            {
                if (!visited.Add(n)) continue;
                if (n == endId) return dist + 1;
                q.Enqueue((n, dist + 1));
            }
        }
        return int.MaxValue;
    }

    void OnDrawGizmos()
    {
        if (nodeDataDict == null || nodeDataDict.Count == 0) return;

        Gizmos.color = Color.cyan;
        foreach (var node in nodeDataDict.Values)
        {
            if (!nodeGameObjects.ContainsKey(node.id)) continue;
            Vector3 a = nodeGameObjects[node.id].GetComponent<RectTransform>().position;
            foreach (var neigh in node.neighbors)
            {
                if (neigh <= node.id) continue;
                if (!nodeGameObjects.ContainsKey(neigh)) continue;
                Vector3 b = nodeGameObjects[neigh].GetComponent<RectTransform>().position;
                Gizmos.DrawLine(a, b);
            }
        }
    }
}
