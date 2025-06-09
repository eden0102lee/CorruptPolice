using System.Collections.Generic;
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

    public void Log(PlayerData player, int round, int nodeId, string actionName, string result, bool isShared)
    {
        ActionType action = ActionType.Move;
        if (actionName == "�լd") action = ActionType.Investigate;
        else if (actionName == "�e��") action = ActionType.Arrest;

        ActionRecord record = new ActionRecord
        {
            playerName = player.playerName,
            role = player.role.ToString(),
            round = round,
            nodeId = nodeId,
            action = action,
            result = result,
            isShared = isShared
        };

        records.Add(record);
        Debug.Log($"[����] {record.playerName} ({record.role}) - �^�X {record.round} - �`�I {record.nodeId} - {record.action} - ���G: {record.result} - ����: {record.isShared}");
    }

    public List<ActionRecord> GetRecords()
    {
        return records;
    }

    public void ClearRecords()
    {
        records.Clear();
    }
}
