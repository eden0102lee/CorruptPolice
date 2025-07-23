using UnityEngine;
using geniikw.DataRenderer2D;

public partial class MapDrawer
{
    private Color GetColorForRoute(string route)
    {
        if (routeColorConfig != null)
            return routeColorConfig.GetColor(route);
        return Color.white;
    }

    private void ClearLines()
    {
        foreach (var info in connectionLines)
        {
            if (info.line != null)
                Destroy(info.line.gameObject);
        }
        connectionLines.Clear();
    }

    private void DrawConnections()
    {
        if (mapManager == null) return;

        foreach (var info in connectionLines)
        {
            if (info.line != null)
                Destroy(info.line.gameObject);
        }
        connectionLines.Clear();

        foreach (var node in nodes)
        {
            foreach (var neighborId in node.neighbors)
            {
                MapNodeData neighbor = nodes.Find(n => n.id == neighborId);
                if (neighbor == null) continue;

                if (node.route == neighbor.route)
                {
                    if (node.id < neighbor.id)
                        CreateConnection(node.id, neighbor.id, node.route);
                }
                else if (node.id < neighbor.id)
                {
                    CreateConnection(node.id, neighbor.id, neighbor.route);
                }
            }
        }
    }

    private void CreateConnection(int fromId, int toId, string route)
    {
        Transform parent = mapManager.roadParent;
        UILine line = UILine.CreateLine(parent);
        line.transform.SetAsFirstSibling();

        Color c = GetColorForRoute(route);
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
            new[] { new GradientAlphaKey(c.a, 0f), new GradientAlphaKey(c.a, 1f) });
        line.line.option.color = grad;

        var info = new ConnectionInfo
        {
            fromId = fromId,
            toId = toId,
            route = route,
            line = line
        };
        connectionLines.Add(info);

        GameObject fromObj = mapManager.GetNodeObject(fromId);
        GameObject toObj = mapManager.GetNodeObject(toId);
        if (fromObj != null && toObj != null)
        {
            while (line.line.Count < 2)
                line.line.Push();
            Vector3 fromPos = fromObj.GetComponent<RectTransform>().position;
            Vector3 toPos = toObj.GetComponent<RectTransform>().position;
            line.line.EditPoint(0, fromPos, lineWidth);
            line.line.EditPoint(1, toPos, lineWidth);
        }
    }

    private void UpdateLinePositions()
    {
        if (mapManager == null) return;

        foreach (var info in connectionLines)
        {
            if (info.line == null) continue;

            GameObject fromObj = mapManager.GetNodeObject(info.fromId);
            GameObject toObj = mapManager.GetNodeObject(info.toId);
            if (fromObj == null || toObj == null) continue;

            while (info.line.line.Count > 2)
                info.line.line.Pop();
            while (info.line.line.Count < 2)
                info.line.line.Push();

            Vector3 fromPos = fromObj.GetComponent<RectTransform>().position;
            Vector3 toPos = toObj.GetComponent<RectTransform>().position;
            info.line.line.EditPoint(0, fromPos, lineWidth);
            info.line.line.EditPoint(1, toPos, lineWidth);
        }
    }
}
