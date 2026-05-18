using UnityEngine;

// 【修复】将 Team 枚举定义在这里，解决 CS0246 错误
// public enum Team { None, TeamA, TeamB }

public class TeamManager : MonoBehaviour
{
    [Header("队伍身份")]
    public Team team;

    [Header("全队共享设置")]
    [Tooltip("整个球场的边界")]
    public Collider fieldBoundary;

    // 提供静态方法，方便 NPC 查找
    public static TeamManager GetTeamManager(Team team)
    {
        var allManagers = FindObjectsOfType<TeamManager>();
        foreach (var m in allManagers)
        {
            if (m.team == team) return m;
        }
        return null;
    }
}
