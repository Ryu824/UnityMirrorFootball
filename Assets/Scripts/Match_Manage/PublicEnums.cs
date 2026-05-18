using UnityEngine;



public enum Team { None, TeamA, TeamB }
public enum PlayerRole { Goalkeeper, Defender, Midfielder, Forward }

public enum MatchState
{
    PreMatch,
    Playing,
    DeadBall,
    GoalScored,
    Ended
}

public enum BoundaryType
{
    TouchLine_Top,
    TouchLine_Bottom,
    GoalLine_Left_Goal,
    GoalLine_Left_Out,
    GoalLine_Right_Goal,
    GoalLine_Right_Out
}
public class PlayerAttributes : MonoBehaviour
{
    [Header("球员的基础属性")]
    public Team team;
    public PlayerRole role;
    public int playerNumber;
    public bool IsControllingBall { get; set; } = false;

}
// 区分球员属于哪支队伍
public interface ITeamMember
{
    Team GetTeam();
}