using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


[System.Serializable]
public class MapData
{
    public List<MapNodeData> nodes;
}

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    public TextAsset mapJsonFile;
    public GameObject nodePrefab;
    public Transform nodeParent;

    private Dictionary<int, MapNodeData> nodeDataDict = new Dictionary<int, MapNodeData>();
    private Dictionary<int, GameObject> nodeGameObjects = new Dictionary<int, GameObject>();
    private Dictionary<int, NodeUI> nodeUIs = new Dictionary<int, NodeUI>();
    private HashSet<int> treasures = new HashSet<int>();
    private Dictionary<int, List<string>> footprints = new Dictionary<int, List<string>>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        LoadAndBuildMap();
    }

    public void LoadAndBuildMap()
    {
        if (mapJsonFile == null)
        {
            Debug.LogError("Map JSON file not assigned.");
            return;
        }

        MapData mapData = JsonUtility.FromJson<MapData>(mapJsonFile.text);
        // shift IDs so numbering starts at 1
        foreach (var node in mapData.nodes)
        {
            node.id += 1;
            for (int i = 0; i < node.neighbors.Count; i++)
                node.neighbors[i] += 1;
        }
        nodeDataDict.Clear();
        nodeGameObjects.Clear();
        nodeUIs.Clear();
        treasures.Clear();
        footprints.Clear();

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

    public void AddTreasure(int nodeId)
    {
        treasures.Add(nodeId);
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
