using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum ActionType
{
    Move,
    Investigate,
    Arrest
}

public class ActionRecord
{
    public string playerName;
    public string role;
    public int round;
    public int nodeId;
    public ActionType action;
    public string result;
    public bool isShared;
    public ShareScope shareScope;
}

[System.Serializable]
public class ActionLogExport
{
    public List<ActionRecord> records = new List<ActionRecord>();
}

public class ActionLogger : MonoBehaviour
{
    public static ActionLogger Instance;

    private List<ActionRecord> records = new List<ActionRecord>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void Log(PlayerData player, int round, int nodeId, string actionName, string result, bool isShared,
        ShareScope scope = ShareScope.Secret)
    {
        ActionType action = ActionType.Move;
        if (actionName == "Investigate") action = ActionType.Investigate;
        else if (actionName == "Arrest") action = ActionType.Arrest;

        ActionRecord record = new ActionRecord
        {
            playerName = player.playerName,
            role = player.role.ToString(),
            round = round,
            nodeId = nodeId,
            action = action,
            result = result,
            isShared = isShared,
            shareScope = scope
        };

        records.Add(record);
        Debug.Log($"[Log] {record.playerName} ({record.role}) - Round {record.round} - Node {record.nodeId} - {record.action} - Result: {record.result} - Share: {record.shareScope}");
    }

    public List<ActionRecord> GetRecords() => records;

    public void ClearRecords() => records.Clear();

    public void ExportToFile(string fileName = "action_replay.json")
    {
        var export = new ActionLogExport { records = new List<ActionRecord>(records) };
        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllText(path, JsonUtility.ToJson(export, true));
        Debug.Log($"[Log] Replay exported to {path}");
    }
}
