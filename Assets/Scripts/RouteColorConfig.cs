using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RouteColors", menuName = "Map/Route Colors")]
public class RouteColorConfig : ScriptableObject
{
    [System.Serializable]
    public class RouteColorEntry
    {
        public string route;
        public Color color = Color.white;
    }

    public List<RouteColorEntry> colors = new();

    public Color GetColor(string route)
    {
        foreach (var entry in colors)
        {
            if (entry.route == route)
                return entry.color;
        }
        return Color.white;
    }
}
