using System.Collections.Generic;
using System.IO;
using UnityEngine;
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
    private Texture2D lineTex;
    private bool isDrawing = false;

    void Start()
    {
        nextId = startIndex;
        lineTex = new Texture2D(1, 1);
        lineTex.SetPixel(0, 0, Color.cyan);
        lineTex.Apply();
        LoadMapIfExists();
    }

    void Update()
    {
        if (isDrawing)
        {
            HandleDrawingInput();
        }
        else
        {
            HandleEditInput();
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

        GUILayout.BeginArea(new Rect(5, 5, 300, 160), GUI.skin.box);
        GUILayout.Label("Map Name");
        mapName = GUILayout.TextField(mapName);
        routeName = GUILayout.TextField(routeName);
        startIndex = int.TryParse(GUILayout.TextField(startIndex.ToString()), out var si) ? si : startIndex;
        mergeThreshold = float.TryParse(GUILayout.TextField(mergeThreshold.ToString()), out var mt) ? mt : mergeThreshold;
        if (!isDrawing && GUILayout.Button("Start Drawing"))
        {
            nodes.Clear();
            nextId = startIndex;
            lastNode = null;
            LoadMapIfExists();
            isDrawing = true;
        }
        else if (isDrawing && GUILayout.Button("Stop Drawing"))
        {
            isDrawing = false;
            ExportJson();
        }
        if (GUILayout.Button("Clear"))
        {
            nodes.Clear();
            nextId = startIndex;
            lastNode = null;
        }
        GUILayout.EndArea();
        if (selectedNode != null)
        {
            GUILayout.BeginArea(new Rect(5, 170, 300, 120), GUI.skin.box);
            GUILayout.Label($"Editing Node {selectedNode.id}");
            selectedNode.name = GUILayout.TextField(selectedNode.name);
            selectedNode.route = GUILayout.TextField(selectedNode.route);
            GUILayout.EndArea();
        }

        DrawConnections();
        DrawNodes();
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

    private Vector2 LocalToGUIPoint(Vector2 local)
    {
        if (mapRoot == null)
            return local;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, mapRoot.TransformPoint(local));
        screen.y = Screen.height - screen.y;
        return screen;
    }

    private void DrawNodes()
    {
        foreach (var node in nodes)
        {
            Vector2 guiPos = LocalToGUIPoint(new Vector2(node.x, node.y));
            Rect r = new Rect(guiPos.x - 10, guiPos.y - 10, 20, 20);
            GUI.color = node == selectedNode ? Color.yellow : Color.cyan;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(r, node.id.ToString());
        }
    }

    private void DrawConnections()
    {
        foreach (var node in nodes)
        {
            foreach (var nId in node.neighbors)
            {
                if (node.id < nId)
                {
                    var other = nodes.Find(n => n.id == nId);
                    if (other != null)
                        DrawLine(new Vector2(node.x, node.y), new Vector2(other.x, other.y));
                }
            }
        }
    }

    private void DrawLine(Vector2 a, Vector2 b)
    {
        Vector2 guiA = LocalToGUIPoint(a);
        Vector2 guiB = LocalToGUIPoint(b);

        Matrix4x4 matrix = GUI.matrix;
        float angle = Vector3.Angle(guiB - guiA, Vector2.right);
        if (guiA.y > guiB.y) angle = -angle;
        GUIUtility.RotateAroundPivot(angle, guiA);
        GUI.DrawTexture(new Rect(guiA.x, guiA.y, (guiB - guiA).magnitude, 2), lineTex);
        GUI.matrix = matrix;
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
}
