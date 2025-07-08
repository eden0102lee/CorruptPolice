using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MapDrawer : MonoBehaviour
{
    public Texture2D background;
    public string routeName = "A";
    public int startIndex = 1;
    public float mergeThreshold = 10f;

    private List<MapNodeData> nodes = new();
    private int nextId;
    private MapNodeData lastNode;
    private Texture2D lineTex;

    void Start()
    {
        nextId = startIndex;
        lineTex = new Texture2D(1, 1);
        lineTex.SetPixel(0, 0, Color.cyan);
        lineTex.Apply();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 pos = Input.mousePosition;
            pos.y = Screen.height - pos.y;
            MapNodeData current = FindNearbyNode(pos, mergeThreshold);
            if (current == null)
            {
                current = new MapNodeData
                {
                    id = nextId,
                    x = pos.x,
                    y = pos.y,
                    route = routeName,
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

    void OnGUI()
    {
        Rect drawRect = new Rect(0, 0, Screen.width, Screen.height);
        if (background != null)
            GUI.DrawTexture(drawRect, background, ScaleMode.StretchToFill);

        GUILayout.BeginArea(new Rect(5, 5, 200, 140), GUI.skin.box);
        routeName = GUILayout.TextField(routeName);
        startIndex = int.TryParse(GUILayout.TextField(startIndex.ToString()), out var si) ? si : startIndex;
        mergeThreshold = float.TryParse(GUILayout.TextField(mergeThreshold.ToString()), out var mt) ? mt : mergeThreshold;
        if (GUILayout.Button("Start Route"))
        {
            nextId = startIndex;
            lastNode = null;
        }
        if (GUILayout.Button("Clear"))
        {
            nodes.Clear();
            nextId = startIndex;
            lastNode = null;
        }
        if (GUILayout.Button("Export JSON"))
        {
            ExportJson();
        }
        GUILayout.EndArea();

        DrawConnections();
        DrawNodes();
    }

    private MapNodeData FindNearbyNode(Vector2 pos, float threshold)
    {
        foreach (var node in nodes)
        {
            if (Vector2.Distance(new Vector2(node.x, node.y), pos) <= threshold)
                return node;
        }
        return null;
    }

    private void AddNeighbor(MapNodeData a, MapNodeData b)
    {
        if (!a.neighbors.Contains(b.id)) a.neighbors.Add(b.id);
        if (!b.neighbors.Contains(a.id)) b.neighbors.Add(a.id);
    }

    private void DrawNodes()
    {
        foreach (var node in nodes)
        {
            Rect r = new Rect(node.x - 10, node.y - 10, 20, 20);
            GUI.color = Color.cyan;
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
        Matrix4x4 matrix = GUI.matrix;
        float angle = Vector3.Angle(b - a, Vector2.right);
        if (a.y > b.y) angle = -angle;
        GUIUtility.RotateAroundPivot(angle, a);
        GUI.DrawTexture(new Rect(a.x, a.y, (b - a).magnitude, 2), lineTex);
        GUI.matrix = matrix;
    }

    private void ExportJson()
    {
        var data = new MapData { nodes = new List<MapNodeData>(nodes) };
        string path = Path.Combine(Application.persistentDataPath, "map.json");
        File.WriteAllText(path, JsonUtility.ToJson(data, true));
        Debug.Log("Map saved to " + path);
    }
}
