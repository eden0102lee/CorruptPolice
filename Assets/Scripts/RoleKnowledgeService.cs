using System.Collections.Generic;

/// <summary>
/// 角色資訊可見性：誰知道誰、誰能看到寶物與小偷位置。
/// </summary>
public static class RoleKnowledgeService
{
    public static bool CanSeeTreasures(PlayerData viewer)
    {
        return viewer != null && viewer.role == PlayerRole.Thief;
    }

    public static bool CanSeeCorruptPoliceIdentity(PlayerData viewer)
    {
        return viewer != null && viewer.role == PlayerRole.Thief;
    }

    public static bool CanSeeThiefPosition(PlayerData viewer, PlayerData thief)
    {
        if (viewer == null || thief == null || thief.role != PlayerRole.Thief)
            return false;

        if (thief.isArrested)
            return false;

        if (viewer.role == PlayerRole.Thief)
            return true;

        if (viewer.role == PlayerRole.CorruptPolice)
            return true;

        return false;
    }

    public static bool CanSeePolicePosition(PlayerData viewer, PlayerData police)
    {
        if (viewer == null || police == null || police.role == PlayerRole.Thief)
            return false;

        return police.role == PlayerRole.Police || police.role == PlayerRole.CorruptPolice;
    }

    public static bool CanSeePlayerToken(PlayerData viewer, PlayerData target)
    {
        if (viewer == null || target == null || target.isArrested)
            return false;

        if (target.role == PlayerRole.Thief)
            return CanSeeThiefPosition(viewer, target);

        return CanSeePolicePosition(viewer, target);
    }

    public static List<PlayerData> GetKnownThieves(PlayerData viewer, IEnumerable<PlayerData> allPlayers)
    {
        var list = new List<PlayerData>();
        if (viewer == null || viewer.role == PlayerRole.Thief)
            return list;

        foreach (var p in allPlayers)
        {
            if (p.role == PlayerRole.Thief)
                list.Add(p);
        }
        return list;
    }

    public static List<PlayerData> GetKnownCorruptPolice(PlayerData viewer, IEnumerable<PlayerData> allPlayers)
    {
        var list = new List<PlayerData>();
        if (viewer == null || !CanSeeCorruptPoliceIdentity(viewer))
            return list;

        foreach (var p in allPlayers)
        {
            if (p.role == PlayerRole.CorruptPolice)
                list.Add(p);
        }
        return list;
    }
}
