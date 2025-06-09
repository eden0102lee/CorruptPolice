using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MapNodeData
{
    public int id;
    public float x;
    public float y;
    public List<int> neighbors;
    public bool hasTreasure = false;
    public List<string> thiefFootprints = new List<string>();

    public bool HasFootprint(string thiefName)
    {
        return thiefFootprints.Contains(thiefName);
    }

    public void AddFootprint(string thiefName)
    {
        if (!thiefFootprints.Contains(thiefName))
        {
            thiefFootprints.Add(thiefName);
        }
    }
}
