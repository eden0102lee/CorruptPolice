using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class MapDrawer
{
    private void AddNeighbor(MapNodeData a, MapNodeData b)
    {
        if (!a.neighbors.Contains(b.id)) a.neighbors.Add(b.id);
        if (!b.neighbors.Contains(a.id)) b.neighbors.Add(a.id);
    }

    private void UpdateNodeId(MapNodeData node, int newId)
    {
        int oldId = node.id;
        if (nodes.Exists(n => n.id == newId && n != node))
            return;

        foreach (var n in nodes)
        {
            for (int i = 0; i < n.neighbors.Count; i++)
            {
                if (n.neighbors[i] == oldId)
                    n.neighbors[i] = newId;
            }
        }

        node.id = newId;
        if (newId >= nextId) nextId = newId + 1;
        BuildPreview();
    }

    private void RemoveNode(MapNodeData node)
    {
        if (node == null) return;
        nodes.Remove(node);
        foreach (var n in nodes)
            n.neighbors.Remove(node.id);
        if (lastNode == node) lastNode = null;
        if (selectedNode == node) selectedNode = null;

        if (selectedNode == node)
        {
            selectedNode = null;
            neighborEditString = string.Empty;
        }
        BuildPreview();
    }

    private void UpdateNeighborsFromString(MapNodeData node, string str)
    {
        if (node == null) return;
        var newList = new List<int>();
        if (!string.IsNullOrEmpty(str))
        {
            var parts = str.Split(',');
            foreach (var p in parts)
            {
                if (int.TryParse(p.Trim(), out var id) && id != node.id && !newList.Contains(id))
                {
                    newList.Add(id);
                }
            }
        }

        foreach (var n in nodes)
        {
            if (n == node) continue;
            if (newList.Contains(n.id))
            {
                if (!n.neighbors.Contains(node.id)) n.neighbors.Add(node.id);
            }
            else
            {
                n.neighbors.Remove(node.id);
            }
        }

        node.neighbors = newList;

        BuildPreview();
    }

    private void ExportJson()
    {
        var data = new MapData { nodes = new List<MapNodeData>(nodes) };
#if UNITY_EDITOR
        string dir = Path.Combine(Application.dataPath, "Resources/Map");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, mapName + ".json");
        if (File.Exists(path))
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Overwrite?",
                $"Map '{mapName}' already exists. Overwrite?", "Yes", "No"))
            {
                path = UnityEditor.EditorUtility.SaveFilePanel("Save Map As", dir, mapName, "json");
            }
        }
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log("Map saved to " + path);
        }
#else
        string path = Path.Combine(Application.persistentDataPath, mapName + ".json");
        File.WriteAllText(path, JsonUtility.ToJson(data, true));
        Debug.Log("Map saved to " + path);
#endif
    }

    private void LoadMapIfExists()
    {
#if UNITY_EDITOR
        TextAsset asset = Resources.Load<TextAsset>("Map/" + mapName);
        if (asset != null)
        {
            MapData data = JsonUtility.FromJson<MapData>(asset.text);
            nodes = new List<MapNodeData>(data.nodes);
            nextId = nodes.Count > 0 ? nodes[nodes.Count - 1].id + 1 : startIndex;
            lastNode = null;
        }
#endif
    }

    private void BuildPreview()
    {
        if (mapManager == null)
            mapManager = MapManager.Instance != null ? MapManager.Instance : FindObjectOfType<MapManager>();
        if (mapManager == null) return;
        ClearLines();
        mapManager.ClearLoadedMap();
        string json = JsonUtility.ToJson(new MapData { nodes = new List<MapNodeData>(nodes) }, true);
        mapManager.mapJsonFile = new TextAsset(json);
        mapManager.LoadAndBuildMap();
        DrawConnections();
    }

    private void EnterPreview()
    {
        BuildPreview();
    }

    private void ExitPreview()
    {
        if (mapManager == null)
            mapManager = MapManager.Instance != null ? MapManager.Instance : FindObjectOfType<MapManager>();
        if (mapManager != null)
        {
            foreach (var node in nodes)
            {
                GameObject go = mapManager.GetNodeObject(node.id);
                if (go != null)
                {
                    RectTransform r = go.GetComponent<RectTransform>();
                    if (r != null)
                    {
                        node.x = r.anchoredPosition.x;
                        node.y = r.anchoredPosition.y;
                    }
                }
            }
            mapManager.ClearLoadedMap();
        }
        ClearLines();
        ExportJson();
    }
}
