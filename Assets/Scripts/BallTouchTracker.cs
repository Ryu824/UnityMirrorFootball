using UnityEngine;

public class BallTouchTracker : MonoBehaviour
{
    // 记录最后碰球的队伍
    public static Team LastTouchTeam = Team.None;

    // 【核心方法】供 PlayerDribble 踢球时手动调用登记
    public static void ManualSetLastTouchTeam(Team team)
    {
        LastTouchTeam = team;
    }

    // 裁判用来清空记录的（开球、进球后调用）
    public static void ClearTouchData()
    {
        LastTouchTeam = Team.None;
    }
}
