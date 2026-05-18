using UnityEngine;
using System.Collections.Generic;
using BehaviorTree;

// 【核心修改 1】实现 ITeamMember 接口，作为最高效的身份标识
public class NPCController : BehaviorTree.Tree, ITeamMember
{
    [Header("身份配置")]
    public Team team;

    [Header("环境引用")]
    public Transform ballTransform;
    public Transform myGoalTransform;
    public Transform opponentGoalTransform;

    [Header("战术参数")]
    public float chaseRadius = 10f; // 抢球半径

    // === 隐藏变量，由 RoleGroup 赋值 ===
    [HideInInspector] public Vector3 homePositionOffset;
    [HideInInspector] public float pushUpDistance;
    [HideInInspector] public float shootDistance;
    [HideInInspector] public float shootForce;

    private Vector3 _homePosition;
    public Vector3 HomePosition => _homePosition;

    [HideInInspector] public PlayerMovement movement;
    [HideInInspector] public PlayerDribble dribble;

    private BoxCollider _activityZone;
    private List<NPCController> teammates = new List<NPCController>();

    // 【核心修改 2】实现接口方法，供 BallTouchTracker (黑匣子) 调用
    public Team GetTeam() => team;

    public void OnFootstep() { }

    protected override void Start()
    {
        movement = GetComponent<PlayerMovement>();
        dribble = GetComponent<PlayerDribble>();
        if (movement != null) movement.isAIControlled = true;

        InitializeSettings();
        FindGoals(); // 将球门查找逻辑封装，让Start更干净
        FindTeammates();

        base.Start();
    }

    private void InitializeSettings()
    {
        RoleGroup group = GetComponentInParent<RoleGroup>();
        if (group != null)
        {
            homePositionOffset = group.homePositionOffset;
            pushUpDistance = group.pushUpDistance;
            shootDistance = group.shootDistance;
            shootForce = group.shootForce;
            _activityZone = group.activityZone;
        }
        else
        {
            homePositionOffset = Vector3.forward * 5f;
            pushUpDistance = 5f;
            shootDistance = 25f;
            shootForce = 20f;
        }
    }

    private void FindGoals()
    {
        if (myGoalTransform != null && opponentGoalTransform != null) return;

        GameObject goalA = GameObject.Find("Goal_A");
        GameObject goalB = GameObject.Find("Goal_B");

        if (goalA == null || goalB == null)
        {
            var allColliders = GameObject.FindObjectsOfType<Collider>();
            foreach (var c in allColliders)
            {
                if (c.name.Contains("Goal") || c.name.Contains("Door"))
                {
                    if (c.name.Contains("A")) goalA = c.gameObject;
                    if (c.name.Contains("B")) goalB = c.gameObject;
                }
            }
        }

        if (team == Team.TeamA)
        {
            if (goalA != null) myGoalTransform = goalA.transform;
            if (goalB != null) opponentGoalTransform = goalB.transform;
        }
        else
        {
            if (goalB != null) myGoalTransform = goalB.transform;
            if (goalA != null) opponentGoalTransform = goalA.transform;
        }

        if (opponentGoalTransform == null) Debug.LogError($"{gameObject.name} 找不到对方球门！");

        Vector3 goalOffset = myGoalTransform != null ? myGoalTransform.position : Vector3.zero;
        _homePosition = goalOffset + homePositionOffset;
    }

    private void FindTeammates()
    {
        var allPlayers = FindObjectsOfType<NPCController>();
        foreach (var p in allPlayers)
        {
            if (p != this && p.team == this.team) teammates.Add(p);
        }
    }

    protected override Node SetUpTree()
    {
        Node root = new Selector(new List<Node>
        {
            // ==========================================
            // 【最高优先级】万能拦截：只要不是“比赛中”，全部停止！
            // 这一行完美涵盖了 PreMatch(赛前)、DeadBall(死球)、GoalScored(进球)、Ended(结束)
            // ==========================================
            new Sequence(new List<Node>
            {
                new Inverter(new ConditionMatchPlaying(this)), // 如果当前状态 != Playing
                new ActionStopMovement(this)                  // 就立刻停止移动
            }),

            // 以下全部是 MatchState.Playing 时才会执行的逻辑
            // 2. 进攻逻辑 (我方有球)
            new Sequence(new List<Node>
            {
                new ConditionTeamHasBall(this),
                new Selector(new List<Node>
                {
                    // A. 核心球员 (自己带球)
                    new Sequence(new List<Node>
                    {
                        new ConditionSelfHasBall(this),
                        new Selector(new List<Node>
                        {
                            new Sequence(new List<Node> { new ConditionCanShoot(this), new ActionShoot(this) }),
                            new Sequence(new List<Node> { new ConditionShouldPass(this), new ActionPass(this) }),
                            new ActionDribble(this)
                        })
                    }),
                    // B. 无球跑位 (队友带球)
                    new ActionOffensivePositioning(this)
                })
            }),

            // 3. 防守/抢球逻辑 (我方无球)
            new Sequence(new List<Node>
            {
                new Inverter(new ConditionTeamHasBall(this)),
                new Selector(new List<Node>
                {
                    new Sequence(new List<Node>
                    {
                        new ConditionBallNearby(this, chaseRadius),
                        new ActionTackle(this)
                    }),
                    new ActionReturnToDefensePosition(this)
                })
            })
        });

        return root;
    }


    // --- 移动与物理辅助方法 (保持不变) ---

    public void MoveToTarget(Vector3 targetPosition)
    {
        float speedMult = 1f;
        if (RuleManager.Instance != null) speedMult = RuleManager.Instance.globalSpeedMultiplier;

        if (_activityZone != null) targetPosition = ClampToBoxZone(targetPosition);

        TeamManager tm = TeamManager.GetTeamManager(this.team);
        if (tm != null && tm.fieldBoundary != null) targetPosition = tm.fieldBoundary.ClosestPoint(targetPosition);

        Vector3 dir = (targetPosition - transform.position);
        dir.y = 0;
        float dist = dir.magnitude;

        if (dist < 0.5f) { StopMoving(); return; }

        dir.Normalize();
        bool shouldSprint = dist > 8f && speedMult > 0.1f;
        movement.SetMoveInput(dir * speedMult, shouldSprint);

        if (!HasBall()) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f * speedMult);
    }

    Vector3 ClampToBoxZone(Vector3 target)
    {
        if (_activityZone == null) return target;
        Vector3 localPos = _activityZone.transform.InverseTransformPoint(target);
        Vector3 center = _activityZone.center;
        Vector3 halfSize = _activityZone.size * 0.5f;
        localPos.x = Mathf.Clamp(localPos.x, center.x - halfSize.x, center.x + halfSize.x);
        localPos.z = Mathf.Clamp(localPos.z, center.z - halfSize.z, center.z + halfSize.z);
        return _activityZone.transform.TransformPoint(localPos);
    }

    public void StopMoving()
    {
        movement.SetMoveInput(Vector3.zero, false);
    }

    public bool HasBall()
    {
        return dribble != null && dribble.IsDribbling;
    }

    // --- 战术分析辅助方法 (保持不变) ---

    public int GetEnemyCountInRange(float radius)
    {
        int count = 0;
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var h in hits)
        {
            NPCController npc = h.GetComponent<NPCController>();
            if (npc != null && npc.team != this.team) count++;
        }
        return count;
    }

    public Transform FindBetterPositionedTeammate()
    {
        if (opponentGoalTransform == null) return null;
        float myDistToGoal = Vector3.Distance(transform.position, opponentGoalTransform.position);
        foreach (var mate in teammates)
        {
            if (mate == null) continue;
            if (Vector3.Distance(mate.transform.position, opponentGoalTransform.position) < myDistToGoal - 2f) return mate.transform;
        }
        return null;
    }

    public Transform FindBestPassTarget()
    {
        if (opponentGoalTransform == null || teammates.Count == 0) return null;
        Transform bestTarget = null;
        float bestScore = float.MinValue;
        foreach (var mate in teammates)
        {
            if (mate == null) continue;
            float distToGoal = Vector3.Distance(mate.transform.position, opponentGoalTransform.position);
            float score = 100f - distToGoal;
            if (Vector3.Distance(transform.position, mate.transform.position) < 3f) score -= 50f; // 别传太近的
            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = mate.transform;
            }
        }
        return bestTarget;
    }

    public void LookAtTarget(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
    }

    private void LateUpdate()
    {
        // 【核心修改 4】删除了原本在这里每帧强制设置 RuleManager.SetPossession 的代码
        // 因为在行为树架构下，控球权的交接应该由底层的 PlayerDribble 触发，而不是由大脑每帧轮询

        if (HasBall() && opponentGoalTransform != null)
        {
            SmoothLookAt(opponentGoalTransform.position, 15f);
        }
    }

    private void SmoothLookAt(Vector3 target, float speed)
    {
        Vector3 direction = (target - transform.position).normalized;
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * speed);
    }
}
