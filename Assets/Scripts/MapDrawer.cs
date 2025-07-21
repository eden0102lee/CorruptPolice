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

    private List<MapNodeData> nodes = new();
    private int nextId;
    private MapNodeData lastNode;
    private MapNodeData selectedNode;
    private bool draggingNode = false;
    public float lineWidth = 2f;

    public RouteColorConfig routeColorConfig;

    private readonly List<UILine> connectionLines = new();
    private bool isDrawing = false;
    private bool isEditing = false;
    private bool isPreviewing = false;
    private MapManager mapManager;

    void Start()
    {
        nextId = startIndex;
        mapManager = MapManager.Instance;
        LoadMapIfExists();
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
    }

    private void HandleDrawingInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (drawArea != null &&
                !RectTransformUtility.RectangleContainsScreenPoint(drawArea, Input.mousePosition))
                return;

            Vector2 localPos = ScreenToLocal(Input.mousePosition);

            MapNodeData current = FindNearbyNode(localPos, mergeThreshold);
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

            if (isPreviewing)
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
            draggingNode = selectedNode != null;
        }
        else if (Input.GetMouseButton(0) && draggingNode && selectedNode != null)
        {
            Vector2 localPos = ScreenToLocal(Input.mousePosition);
            selectedNode.x = localPos.x;
            selectedNode.y = localPos.y;
            if (mapManager != null && isPreviewing)
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

        GUILayout.BeginArea(new Rect(5, 5, 300, 300), GUI.skin.box);
        GUILayout.Label("Map Name");
        mapName = GUILayout.TextField(mapName);
        routeName = GUILayout.TextField(routeName);
        startIndex = int.TryParse(GUILayout.TextField(startIndex.ToString()), out var si) ? si : startIndex;
        mergeThreshold = float.TryParse(GUILayout.TextField(mergeThreshold.ToString()), out var mt) ? mt : mergeThreshold;

        if (!isDrawing && !isEditing && GUILayout.Button("Load Map"))
        {
            LoadMapIfExists();
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
            if (isPreviewing)
                BuildPreview();
        }
        GUILayout.EndArea();
        if (selectedNode != null)
        {
            GUILayout.BeginArea(new Rect(5, 210, 300, 180), GUI.skin.box);
            GUILayout.Label($"Editing Node {selectedNode.id}");
            int newId = int.TryParse(GUILayout.TextField(selectedNode.id.ToString()), out var nid) ? nid : selectedNode.id;
            float newX = float.TryParse(GUILayout.TextField(selectedNode.x.ToString()), out var nx) ? nx : selectedNode.x;
            float newY = float.TryParse(GUILayout.TextField(selectedNode.y.ToString()), out var ny) ? ny : selectedNode.y;
            selectedNode.name = GUILayout.TextField(selectedNode.name);
            selectedNode.route = GUILayout.TextField(selectedNode.route);
            if (newId != selectedNode.id)
            {
                UpdateNodeId(selectedNode, newId);
                if (isPreviewing)
                    BuildPreview();
            }
            selectedNode.x = newX;
            selectedNode.y = newY;
            if (mapManager != null && isPreviewing)
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
                if (isPreviewing)
                    BuildPreview();
            }
            GUILayout.EndArea();
        }

        // connections and nodes are displayed via UILine and NodeUI when previewing
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
        foreach (var line in connectionLines)
        {
            if (line != null)
                Destroy(line.gameObject);
        }
        connectionLines.Clear();
    }

    private void DrawConnections()
    {
        if (mapManager == null) return;
        ClearLines();
        foreach (var node in nodes)
        {
            foreach (var nId in node.neighbors)
            {
                if (node.id < nId)
                {
                    var other = nodes.Find(n => n.id == nId);
                    if (other != null)
                    {
                        GameObject startGo = mapManager.GetNodeObject(node.id);
                        GameObject endGo = mapManager.GetNodeObject(other.id);
                        if (startGo != null && endGo != null)
                        {
                            Transform parent = mapManager.nodeParent != null ? mapManager.nodeParent : mapRoot;
                            var line = UILine.CreateLine(parent);
                            line.transform.SetAsFirstSibling();
                            line.line.Clear();
                            line.line.Push();
                            line.line.Push();
                            Vector3 p0 = startGo.GetComponent<RectTransform>().position;
                            Vector3 p1 = endGo.GetComponent<RectTransform>().position;
                            line.line.EditPoint(0, p0, lineWidth);
                            line.line.EditPoint(1, p1, lineWidth);
                            Color c = GetColorForRoute(node.route);
                            var grad = new Gradient();
                            grad.SetKeys(
                                new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                                new[] { new GradientAlphaKey(c.a, 0f), new GradientAlphaKey(c.a, 1f) });
                            line.line.option.color = grad;
                            connectionLines.Add(line);
                        }
                    }
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
        if (isPreviewing)
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
        if (isPreviewing)
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
        isPreviewing = true;
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
        isPreviewing = false;
        ExportJson();
    }
}
