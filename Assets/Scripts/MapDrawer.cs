using System.Collections.Generic;
using System.IO;
using UnityEngine;
using geniikw.DataRenderer2D;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MapDrawer : MonoBehaviour
{
    public Texture2D background;
    public RectTransform drawArea;
    public RectTransform mapRoot;

    public string mapName = "test_map";
    public string routeName = "A";
    public int startIndex = 1;
    public float mergeThreshold = 10f;
    public bool autoMerge = true;

    private List<MapNodeData> nodes = new();
    private int nextId;
    private MapNodeData lastNode;
    private MapNodeData selectedNode;
    private bool draggingNode = false;
    public float lineWidth = 2f;

    private string neighborEditString = string.Empty;

    public RouteColorConfig routeColorConfig;

    private readonly Dictionary<string, UILine> routeLines = new();

    private static readonly string[] ROUTES =
    {
        "A","B","C","D","E","F","G","H","I","J"
    };

    private bool isDrawing = false;
    private bool isEditing = false;
    private MapManager mapManager;

    void Start()
    {
        nextId = startIndex;
        mapManager = MapManager.Instance;
        LoadMapIfExists();
    }

    void OnDisable()
    {
        ClearLines();
    }

    void Update()
    {
        if (isDrawing)
        {
            HandleDrawingInput();
        }
        else if (isEditing)
        {
            HandleEditInput();
            if (selectedNode != null && Input.GetKeyDown(KeyCode.Delete))
            {
                RemoveNode(selectedNode);
            }
        }

        // Keep connection lines in sync with node positions only during edit or draw modes
        if (isDrawing || isEditing)
            UpdateLinePositions();
    }

    private void HandleDrawingInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (drawArea != null &&
                !RectTransformUtility.RectangleContainsScreenPoint(drawArea, Input.mousePosition))
                return;

            Vector2 localPos = ScreenToLocal(Input.mousePosition);

            MapNodeData current = autoMerge ? FindNearbyNode(localPos, mergeThreshold) : null;
            if (current == null)
            {
                current = new MapNodeData
                {
                    id = nextId,
                    x = localPos.x,
                    y = localPos.y,
                    route = routeName,
                    name = "Node " + nextId,
                    neighbors = new List<int>()
                };
                nodes.Add(current);
                nextId++;
            }

            if (lastNode != null && lastNode.id != current.id)
            {
                AddNeighbor(lastNode, current);
            }

            lastNode = current;

            BuildPreview();
        }
    }

    private void HandleEditInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (drawArea != null &&
                !RectTransformUtility.RectangleContainsScreenPoint(drawArea, Input.mousePosition))
                return;

            Vector2 localPos = ScreenToLocal(Input.mousePosition);
            selectedNode = FindNearbyNode(localPos, mergeThreshold);
            if (selectedNode != null)
                neighborEditString = string.Join(",", selectedNode.neighbors);
            draggingNode = selectedNode != null;
        }
        else if (Input.GetMouseButton(0) && draggingNode && selectedNode != null)
        {
            Vector2 localPos = ScreenToLocal(Input.mousePosition);
            selectedNode.x = localPos.x;
            selectedNode.y = localPos.y;
            if (mapManager != null)
            {
                GameObject go = mapManager.GetNodeObject(selectedNode.id);
                if (go != null)
                {
                    RectTransform r = go.GetComponent<RectTransform>();
                    if (r != null)
                        r.anchoredPosition = localPos;
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            draggingNode = false;
        }
    }

    private Vector2 ScreenToLocal(Vector2 screenPos)
    {
        Vector2 localPos = Vector2.zero;
        if (mapRoot != null)
            RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRoot, screenPos, null, out localPos);
        else
            RectTransformUtility.ScreenPointToLocalPointInRectangle(drawArea, screenPos, null, out localPos);
        return localPos;
    }

    void OnGUI()
    {
        Rect drawRect = new Rect(0, 0, Screen.width, Screen.height);
        if (background != null)
            GUI.DrawTexture(drawRect, background, ScaleMode.StretchToFill);

        GUILayout.BeginArea(new Rect(5, 5, 300, 500), GUI.skin.box);
        GUILayout.Label("Map Name");
        mapName = GUILayout.TextField(mapName);
        GUILayout.Label("Route Name");
        routeName = GUILayout.TextField(routeName);
        GUILayout.Label("Start Index");
        startIndex = int.TryParse(GUILayout.TextField(startIndex.ToString()), out var si) ? si : startIndex;
        GUILayout.Label("Merge Threshold");
        mergeThreshold = float.TryParse(GUILayout.TextField(mergeThreshold.ToString()), out var mt) ? mt : mergeThreshold;
        autoMerge = GUILayout.Toggle(autoMerge, "Auto Merge Nodes");

        if (!isDrawing && !isEditing && GUILayout.Button("Load Map"))
        {
            LoadMapIfExists();
            EnterPreview();
        }
        if (!isDrawing && !isEditing && GUILayout.Button("Start Drawing"))
        {
            nodes.Clear();
            nextId = startIndex;
            lastNode = null;
            LoadMapIfExists();
            isDrawing = true;
            EnterPreview();
        }
        else if (isDrawing && GUILayout.Button("Stop Drawing"))
        {
            isDrawing = false;
            ExitPreview();
        }
        if (!isDrawing && !isEditing && GUILayout.Button("Edit Nodes"))
        {
            isEditing = true;
            selectedNode = null;
            EnterPreview();
        }
        else if (isEditing && GUILayout.Button("Save"))
        {
            isEditing = false;
            selectedNode = null;
            ExitPreview();
        }
        if (GUILayout.Button("Clear"))
        {
            nodes.Clear();
            nextId = startIndex;
            lastNode = null;
            selectedNode = null;
            BuildPreview();
        }
        GUILayout.EndArea();
        if (selectedNode != null)
        {
            GUILayout.BeginArea(new Rect(5, 210, 300, 500), GUI.skin.box);
            GUILayout.Label($"Editing Node {selectedNode.id}");
            GUILayout.Label("ID");
            int newId = int.TryParse(GUILayout.TextField(selectedNode.id.ToString()), out var nid) ? nid : selectedNode.id;
            GUILayout.Label("X");
            float newX = float.TryParse(GUILayout.TextField(selectedNode.x.ToString()), out var nx) ? nx : selectedNode.x;
            GUILayout.Label("Y");
            float newY = float.TryParse(GUILayout.TextField(selectedNode.y.ToString()), out var ny) ? ny : selectedNode.y;
            GUILayout.Label("Name");
            selectedNode.name = GUILayout.TextField(selectedNode.name);
            GUILayout.Label("Route");
            selectedNode.route = GUILayout.TextField(selectedNode.route);
            GUILayout.Label("Neighbors (comma separated)");
            string newNeighborStr = GUILayout.TextField(neighborEditString);
            if (newNeighborStr != neighborEditString)
            {
                neighborEditString = newNeighborStr;
                UpdateNeighborsFromString(selectedNode, neighborEditString);
            }
            if (newId != selectedNode.id)
            {
                UpdateNodeId(selectedNode, newId);
                BuildPreview();
            }
            selectedNode.x = newX;
            selectedNode.y = newY;
            if (mapManager != null)
            {
                GameObject go = mapManager.GetNodeObject(selectedNode.id);
                if (go != null)
                {
                    RectTransform r = go.GetComponent<RectTransform>();
                    if (r != null)
                        r.anchoredPosition = new Vector2(selectedNode.x, selectedNode.y);
                }
            }
            if (GUILayout.Button("Delete Node"))
            {
                RemoveNode(selectedNode);
                BuildPreview();
            }
            GUILayout.EndArea();
        }

    }

    private MapNodeData FindNearbyNode(Vector2 localPos, float threshold)
    {
        foreach (var node in nodes)
        {
            if (Vector2.Distance(new Vector2(node.x, node.y), localPos) <= threshold)
                return node;
        }
        return null;
    }

    private void AddNeighbor(MapNodeData a, MapNodeData b)
    {
        if (!a.neighbors.Contains(b.id)) a.neighbors.Add(b.id);
        if (!b.neighbors.Contains(a.id)) b.neighbors.Add(a.id);
    }

    private Color GetColorForRoute(string route)
    {
        if (routeColorConfig != null)
            return routeColorConfig.GetColor(route);
        return Color.white;
    }

    private void ClearLines()
    {
        foreach (var pair in routeLines)
        {
            if (pair.Value != null)
                Destroy(pair.Value.gameObject);
        }
        routeLines.Clear();
    }

    private void DrawConnections()
    {
        if (mapManager == null) return;

        // Group nodes by route
        var routeGroups = new Dictionary<string, List<MapNodeData>>();
        foreach (var node in nodes)
        {
            if (!routeGroups.TryGetValue(node.route, out var list))
            {
                list = new List<MapNodeData>();
                routeGroups[node.route] = list;
            }
            list.Add(node);
        }

        // Remove unused lines
        var toRemove = new List<string>();
        foreach (var pair in routeLines)
        {
            if (!routeGroups.ContainsKey(pair.Key) || routeGroups[pair.Key].Count < 2)
            {
                if (pair.Value != null)
                    Destroy(pair.Value.gameObject);
                toRemove.Add(pair.Key);
            }
        }
        foreach (var r in toRemove)
            routeLines.Remove(r);

        // Create/update lines for each route in order
        foreach (var route in ROUTES)
        {
            if (!routeGroups.TryGetValue(route, out var list))
                continue;
            list.Sort((a, b) => a.id.CompareTo(b.id));
            if (list.Count < 2)
                continue;

            if (!routeLines.TryGetValue(route, out var line) || line == null)
            {
                Transform parent = mapManager.nodeParent != null ? mapManager.nodeParent : mapRoot;
                line = UILine.CreateLine(parent);
                line.name = $"UILine_{route}";
                line.transform.SetAsFirstSibling();
                routeLines[route] = line;
            }

            while (line.line.Count > list.Count)
                line.line.Pop();
            while (line.line.Count < list.Count)
                line.line.Push();

            for (int i = 0; i < list.Count; i++)
            {
                GameObject go = mapManager.GetNodeObject(list[i].id);
                if (go != null)
                {
                    Vector3 pos = go.GetComponent<RectTransform>().position;
                    line.line.EditPoint(i, pos, lineWidth);
                }
            }

            Color c = GetColorForRoute(route);
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                new[] { new GradientAlphaKey(c.a, 0f), new GradientAlphaKey(c.a, 1f) });
            line.line.option.color = grad;
        }
    }


    private void UpdateLinePositions()
    {
        if (mapManager == null) return;

        foreach (var pair in routeLines)
        {
            string route = pair.Key;
            UILine line = pair.Value;
            if (line == null) continue;

            var list = nodes.FindAll(n => n.route == route);
            list.Sort((a, b) => a.id.CompareTo(b.id));

            while (line.line.Count > list.Count)
                line.line.Pop();
            while (line.line.Count < list.Count)
                line.line.Push();

            for (int i = 0; i < list.Count; i++)
            {
                GameObject go = mapManager.GetNodeObject(list[i].id);
                if (go != null)
                {
                    Vector3 pos = go.GetComponent<RectTransform>().position;
                    line.line.EditPoint(i, pos, lineWidth);
                }
            }
        }
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
