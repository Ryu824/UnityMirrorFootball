using UnityEngine;
using System.Collections.Generic;
using BehaviorTree;

// --- 装饰器 ---
public class Inverter : Node
{
    private Node _node;
    public Inverter(Node node) { _node = node; }
    public override NodeState Evaluate()
    {
        switch (_node.Evaluate())
        {
            case NodeState.RUNNING: state = NodeState.RUNNING; break;
            case NodeState.SUCCESS: state = NodeState.FAILURE; break;
            case NodeState.FAILURE: state = NodeState.SUCCESS; break;
        }
        return state;
    }
}

// --- 比赛状态节点 ---
// 比赛结束判定
public class ConditionMatchEnded : Node
{
    private NPCController _npc;
    public ConditionMatchEnded(NPCController npc) { _npc = npc; }
    public override NodeState Evaluate()
    {
        if (RuleManager.Instance == null) return NodeState.FAILURE;
        // 对应新的 MatchState.Ended
        return RuleManager.Instance.CurrentState == MatchState.Ended ? NodeState.SUCCESS : NodeState.FAILURE;
    }
}

// 【新增核心节点】死球状态判定（出界、犯规时，NPC必须停止）
public class ConditionIsDeadBall : Node
{
    private NPCController _npc;
    public ConditionIsDeadBall(NPCController npc) { _npc = npc; }
    public override NodeState Evaluate()
    {
        if (RuleManager.Instance == null) return NodeState.FAILURE;
        return RuleManager.Instance.CurrentState == MatchState.DeadBall ? NodeState.SUCCESS : NodeState.FAILURE;
    }
}

// 比赛进行中判定 (替代旧的 WaitingForKickoff)
public class ConditionMatchPlaying : Node
{
    private NPCController _npc;
    public ConditionMatchPlaying(NPCController npc) { _npc = npc; }
    public override NodeState Evaluate()
    {
        if (RuleManager.Instance == null) return NodeState.FAILURE;
        return RuleManager.Instance.CurrentState == MatchState.Playing ? NodeState.SUCCESS : NodeState.FAILURE;
    }
}


public class ActionStopMovement : Node
{
    private NPCController _npc;
    public ActionStopMovement(NPCController npc) { _npc = npc; }
    public override NodeState Evaluate()
    {
        _npc.StopMoving();
        return NodeState.SUCCESS;
    }
}

public class ActionIdle : Node
{
    private NPCController _npc;
    public ActionIdle(NPCController npc) { _npc = npc; }
    public override NodeState Evaluate()
    {
        _npc.StopMoving();
        return NodeState.SUCCESS;
    }
}

// --- 条件节点 ---
public class ConditionTeamHasBall : Node
{
    private NPCController _npc;
    public ConditionTeamHasBall(NPCController npc) { _npc = npc; }
    public override NodeState Evaluate()
    {
        if (RuleManager.Instance == null) return NodeState.FAILURE;
        // 从新的 RuleManager 读取
        state = RuleManager.Instance.currentPossessionTeam == _npc.team ? NodeState.SUCCESS : NodeState.FAILURE;
        return state;
    }
}

public class ConditionBallLoose : Node
{
    private NPCController _npc;
    public ConditionBallLoose(NPCController npc) { _npc = npc; }
    public override NodeState Evaluate()
    {
        if (RuleManager.Instance == null) return NodeState.FAILURE;
        return RuleManager.Instance.currentPossessionTeam == Team.None ? NodeState.SUCCESS : NodeState.FAILURE;
    }
}


public class ConditionSelfHasBall : Node
{
    private NPCController _npc;
    public ConditionSelfHasBall(NPCController npc) { _npc = npc; }
    public override NodeState Evaluate()
    {
        state = _npc.HasBall() ? NodeState.SUCCESS : NodeState.FAILURE;
        return state;
    }
}

public class ConditionCanShoot : Node
{
    private NPCController _npc;
    public ConditionCanShoot(NPCController npc) { _npc = npc; }
    public override NodeState Evaluate()
    {
        if (_npc.opponentGoalTransform == null) { state = NodeState.FAILURE; return state; }
        float dist = Vector3.Distance(_npc.transform.position, _npc.opponentGoalTransform.position);
        state = (dist < _npc.shootDistance) ? NodeState.SUCCESS : NodeState.FAILURE;
        return state;
    }
}

public class ConditionShouldPass : Node
{
    private NPCController _npc;
    public ConditionShouldPass(NPCController npc) { _npc = npc; }

    public override NodeState Evaluate()
    {
        // 1. 是否有身位更好的队友
        Transform betterMate = _npc.FindBetterPositionedTeammate();
        if (betterMate != null)
        {
            _npc.SetData("PassTarget", betterMate);
            return NodeState.SUCCESS;
        }

        // 2. 是否被包围 (周围敌人 >= 3)
        int enemyCount = _npc.GetEnemyCountInRange(5f);
        if (enemyCount >= 3)
        {
            Transform safeMate = _npc.FindBestPassTarget();
            if (safeMate != null)
            {
                _npc.SetData("PassTarget", safeMate);
                return NodeState.SUCCESS;
            }
        }

        return NodeState.FAILURE;
    }
}

public class ConditionBallNearby : Node
{
    private NPCController _npc;
    private float _radius;
    public ConditionBallNearby(NPCController npc, float radius) { _npc = npc; _radius = radius; }
    public override NodeState Evaluate()
    {
        if (_npc.ballTransform == null) return NodeState.FAILURE;
        float dist = Vector3.Distance(_npc.transform.position, _npc.ballTransform.position);
        state = (dist < _radius) ? NodeState.SUCCESS : NodeState.FAILURE;
        return state;
    }
}

// --- 动作节点 ---
public class ActionShoot : Node
{
    private NPCController _npc;
    public ActionShoot(NPCController npc) { _npc = npc; }
    public override NodeState Evaluate()
    {
        if (!_npc.HasBall()) return NodeState.FAILURE;

        _npc.LookAtTarget(_npc.opponentGoalTransform.position);
        Vector3 dir = (_npc.opponentGoalTransform.position - _npc.transform.position).normalized;

        _npc.dribble.KickBall(dir, _npc.shootForce);

        return NodeState.SUCCESS;
    }
}

public class ActionPass : Node
{
    private NPCController _npc;
    public ActionPass(NPCController npc) { _npc = npc; }
    public override NodeState Evaluate()
    {
        if (!_npc.HasBall()) return NodeState.FAILURE;
        object targetObj = _npc.GetData("PassTarget");
        if (targetObj == null) return NodeState.FAILURE;

        Transform target = (Transform)targetObj;
        _npc.LookAtTarget(target.position);
        float dist = Vector3.Distance(_npc.transform.position, target.position);
        float force = Mathf.Clamp(dist * 1.2f, 8f, 25f);
        Vector3 dir = (target.position - _npc.transform.position).normalized;
        _npc.dribble.KickBall(dir, force);
        _npc.ClearData("PassTarget");
        return NodeState.SUCCESS;
    }
}

public class ActionDribble : Node
{
    private NPCController _npc;
    public ActionDribble(NPCController npc) { _npc = npc; }
    public override NodeState Evaluate()
    {
        if (!_npc.HasBall()) return NodeState.FAILURE;
        _npc.MoveToTarget(_npc.opponentGoalTransform.position);
        return NodeState.RUNNING;
    }
}

public class ActionOffensivePositioning : Node
{
    private NPCController _npc;
    public ActionOffensivePositioning(NPCController npc) { _npc = npc; }
    public override NodeState Evaluate()
    {
        Vector3 oppGoalPos = _npc.opponentGoalTransform.position;
        Vector3 myGoalPos = _npc.myGoalTransform.position;

        Vector3 mainDir = (oppGoalPos - myGoalPos).normalized;
        Vector3 homeOffset = _npc.HomePosition - myGoalPos;

        float forwardDist = Vector3.Dot(homeOffset, mainDir);
        Vector3 lateralOffset = homeOffset - mainDir * forwardDist;

        float ballDepth = Vector3.Dot(_npc.ballTransform.position - myGoalPos, mainDir);
        float targetDepth = ballDepth - _npc.pushUpDistance;

        Vector3 targetPos = myGoalPos + mainDir * targetDepth + lateralOffset;
        _npc.MoveToTarget(targetPos);
        return NodeState.RUNNING;
    }
}

public class ActionTackle : Node
{
    private NPCController _npc;
    public ActionTackle(NPCController npc) { _npc = npc; }
    public override NodeState Evaluate()
    {
        if (_npc.ballTransform == null) return NodeState.FAILURE;
        _npc.MoveToTarget(_npc.ballTransform.position);
        if (_npc.HasBall())
        {
            if (RuleManager.Instance != null) RuleManager.SetPossession(_npc.team);
            return NodeState.SUCCESS;
        }
        return NodeState.RUNNING;
    }
}

public class ActionReturnToDefensePosition : Node
{
    private NPCController _npc;
    public ActionReturnToDefensePosition(NPCController npc) { _npc = npc; }
    public override NodeState Evaluate()
    {
        _npc.MoveToTarget(_npc.HomePosition);
        return NodeState.RUNNING;
    }
}
