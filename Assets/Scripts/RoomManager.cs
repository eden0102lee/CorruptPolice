// 房間與玩家配置管理
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public int maxPlayers;
    public int maxRounds;
    public int treasureGoal;
    //public List<PlayerData> players;
    public MapNodeData selectedMap;

    public void CreateRoom(int playerCount, int roundLimit, int treasureTarget) { }
    public void AssignRoles() { }
    public void StartGame() { }
}

