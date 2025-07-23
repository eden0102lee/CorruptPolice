using System.Collections.Generic;
using UnityEngine;
using geniikw.DataRenderer2D;
#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class MapDrawer : MonoBehaviour
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

    private class ConnectionInfo
    {
        public int fromId;
        public int toId;
        public string route;
        public UILine line;
    }

    private readonly List<ConnectionInfo> connectionLines = new();

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

}
