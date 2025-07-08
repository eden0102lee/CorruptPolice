#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class MapEditorWindow : EditorWindow
{
    private List<MapNodeData> nodes = new();
    private Texture2D background;

    private string routeName = "A";
    private int startIndex = 1;
    private int nextId = 1;
    private MapNodeData lastNode = null;
    private float mergeThreshold = 10f;

    [MenuItem("Tools/Map Editor")]
    public static void Open()
    {
        GetWindow<MapEditorWindow>("Map Editor");
    }

    private void OnGUI()
    {
        Rect drawRect = new Rect(0, 0, position.width, position.height);
        if (background != null)
        {
            GUI.DrawTexture(drawRect, background, ScaleMode.StretchToFill);
        }

        GUILayout.BeginArea(new Rect(5,5,200,120));
        routeName = EditorGUILayout.TextField("Route", routeName);
        startIndex = EditorGUILayout.IntField("Start ID", startIndex);
        mergeThreshold = EditorGUILayout.FloatField("Merge Dist", mergeThreshold);
        if (GUILayout.Button("Start Route"))
        {
            nextId = startIndex;
            lastNode = null;
        }
        GUILayout.EndArea();

        HandleInput();
        DrawConnections();
        DrawNodes();

        GUILayout.BeginHorizontal();
        background = (Texture2D)EditorGUILayout.ObjectField(background, typeof(Texture2D), false, GUILayout.Width(60), GUILayout.Height(60));
        if (GUILayout.Button("Clear"))
        {
            nodes.Clear();
            nextId = startIndex;
            lastNode = null;
            Repaint();
        }
        if (GUILayout.Button("Export JSON"))
        {
            ExportJson();
        }
        GUILayout.EndHorizontal();
    }

    private void HandleInput()
    {
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Vector2 pos = e.mousePosition;
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
            e.Use();
            Repaint();
        }
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

    private void DrawConnections()
    {
        Handles.BeginGUI();
        foreach (var node in nodes)
        {
            foreach (var nId in node.neighbors)
            {
                if (node.id < nId)
                {
                    var other = nodes.Find(n => n.id == nId);
                    if (other != null)
                        Handles.DrawLine(new Vector3(node.x, node.y), new Vector3(other.x, other.y));
                }
            }
        }
        Handles.EndGUI();
    }

    private void DrawNodes()
    {
        foreach (var node in nodes)
        {
            Rect r = new Rect(node.x - 10, node.y - 10, 20, 20);
            EditorGUI.DrawRect(r, Color.cyan);
            GUI.Label(r, node.id.ToString());
        }
    }

    private void ExportJson()
    {
        string path = EditorUtility.SaveFilePanel("Save Map", Application.dataPath, "map", "json");
        if (string.IsNullOrEmpty(path)) return;

        MapData data = new MapData
        {
            nodes = new List<MapNodeData>(nodes)
        };
        File.WriteAllText(path, JsonUtility.ToJson(data, true));
        AssetDatabase.Refresh();
    }
}
#endif
