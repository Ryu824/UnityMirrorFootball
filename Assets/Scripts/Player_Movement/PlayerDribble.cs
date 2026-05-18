using UnityEngine;

public class PlayerDribble : MonoBehaviour
{
    [Header("设置")]
    public Transform ball;
    public float detectRadius = 1.5f;
    public float dribbleOffset = 0.8f;

    [Header("力度设置")]
    public float maxChargeTime = 1.5f;

    [Header("传球设置 (左键)")]
    public float minPassForce = 5f;
    public float maxPassForce = 20f;

    [Header("射门设置 (右键)")]
    public float minShootForce = 10f;
    public float maxShootForce = 40f;
    public float shootElevationAngle = 0.6f;

    [Header("调试")]
    public bool showGizmos = true;

    private bool isDribbling = false;
    private Rigidbody ballRigidbody;
    private Collider playerCollider;
    private Collider ballCollider;
    private float leaveBallTime = 0f;
    public float kickCooldown = 0.5f;

    // 蓄力状态
    private float chargeStartTime = 0f;
    private bool isCharging = false;
    private bool isChargingShoot = false;

    // 缓存 PlayerMovement 组件用于判断是否是 AI
    private PlayerMovement playerMovement;

    public bool IsDribbling => isDribbling;
    public float ChargeProgress { get; private set; }

    void Start()
    {
        if (ball != null)
        {
            ballRigidbody = ball.GetComponent<Rigidbody>();
            ballCollider = ball.GetComponent<Collider>();
        }
        playerCollider = GetComponent<Collider>();
        playerMovement = GetComponent<PlayerMovement>(); // 获取引用
    }

    void Update()
    {
        if (ball == null) return;

        // 1. 冷却逻辑
        if (leaveBallTime > 0)
        {
            leaveBallTime -= Time.deltaTime;
            if (leaveBallTime <= 0)
            {
                ResetCollisionIgnore();
            }
            if (isDribbling) StopDribble();
            return;
        }

        // 2. 状态判断
        float distance = Vector3.Distance(transform.position, ball.position);

        if (distance <= detectRadius)
        {
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            bool ballIsSlow = (ballRb != null && ballRb.velocity.magnitude < 3f);
            Vector3 dirToPlayer = (transform.position - ball.position).normalized;
            bool ballApproaching = Vector3.Dot(ballRb.velocity.normalized, dirToPlayer) > 0.1f;

            if (ballIsSlow || ballApproaching)
            {
                if (!isDribbling) StartDribble();
            }
        }
        else
        {
            if (isDribbling) StopDribble();
        }

        // 3. 运球位置更新
        if (isDribbling)
        {
            PerformDribble();
            HandleInput();
        }

        // 蓄力进度
        if (isCharging)
        {
            float duration = Time.time - chargeStartTime;
            ChargeProgress = Mathf.Clamp01(duration / maxChargeTime);
        }
        else
        {
            ChargeProgress = 0f;
        }
    }

    void HandleInput()
    {
        // 只有非 AI 才处理输入
        if (playerMovement != null && playerMovement.isAIControlled) return;

        if (Input.GetMouseButtonDown(0)) StartCharge(false);
        else if (Input.GetMouseButtonDown(1)) StartCharge(true);

        if (Input.GetMouseButtonUp(0) && isCharging && !isChargingShoot) ExecutePass();
        else if (Input.GetMouseButtonUp(1) && isCharging && isChargingShoot) ExecuteShoot();
    }

    void StartCharge(bool isShoot)
    {
        isCharging = true;
        isChargingShoot = isShoot;
        chargeStartTime = Time.time;
    }

    void ExecutePass()
    {
        isCharging = false;
        float duration = Time.time - chargeStartTime;
        float ratio = Mathf.Clamp01(duration / maxChargeTime);
        float force = Mathf.Lerp(minPassForce, maxPassForce, ratio);
        Vector3 dir = transform.forward;
        RegisterKickTouch();
        StopDribble();
        leaveBallTime = kickCooldown;
        ballRigidbody.AddForce(dir * force, ForceMode.Impulse);
    }

    void ExecuteShoot()
    {
        isCharging = false;
        float duration = Time.time - chargeStartTime;
        float ratio = Mathf.Clamp01(duration / maxChargeTime);
        float force = Mathf.Lerp(minShootForce, maxShootForce, ratio);
        Vector3 dir = transform.forward + Vector3.up * shootElevationAngle;
        dir.Normalize();
        RegisterKickTouch();
        StopDribble();
        leaveBallTime = kickCooldown;
        ballRigidbody.AddForce(dir * force, ForceMode.Impulse);
    }

    void StartDribble()
    {
        isDribbling = true;
        // 上面的 Team myTeam = myTeam.None; 已经被删掉了

        if (ballRigidbody != null)
        {
            ballRigidbody.isKinematic = true;
            ballRigidbody.velocity = Vector3.zero;
            ballRigidbody.angularVelocity = Vector3.zero;
        }
        if (playerCollider != null && ballCollider != null)
        {
            Physics.IgnoreCollision(playerCollider, ballCollider, true);
        }

        // 【解开注释】通过接口获取队伍，并通知裁判系统“我拿到球了”
        if (RuleManager.Instance != null)
        {
            ITeamMember member = GetComponent<ITeamMember>();
            if (member != null)
                RuleManager.SetPossession(member.GetTeam());
        }
    }


    void StopDribble()
    {
        isDribbling = false;
        if (ballRigidbody != null)
        {
            ballRigidbody.isKinematic = false;
        }

        // 【核心修复 2】：球离脚，清除控球权，所有人变为“松散球”状态去争抢
        if (RuleManager.Instance != null)
        {
            RuleManager.ClearPossession();
        }
    }

    void ResetCollisionIgnore()
    {
        if (playerCollider != null && ballCollider != null)
        {
            Physics.IgnoreCollision(playerCollider, ballCollider, false);
        }
    }

    void PerformDribble()
    {
        Vector3 targetPosition = transform.position + transform.forward * dribbleOffset;
        ball.position = targetPosition;
    }

    // --- AI 接口 ---

    public void KickBall(Vector3 direction, float force)
    {
        if (!isDribbling) return;
        RegisterKickTouch();
        StopDribble(); // 这里的 StopDribble 会自动触发 ClearPossession
        leaveBallTime = kickCooldown;

        ballRigidbody.velocity = Vector3.zero;
        ballRigidbody.AddForce(direction.normalized * force, ForceMode.Impulse);
    }

    public void HeavyKickBall(Vector3 direction)
    {
        if (!isDribbling) return;
        RegisterKickTouch();
        StopDribble(); // 这里的 StopDribble 会自动触发 ClearPossession
        leaveBallTime = kickCooldown;

        Vector3 shootDir = direction + Vector3.up * shootElevationAngle;
        shootDir.Normalize();

        ballRigidbody.AddForce(shootDir * maxShootForce, ForceMode.Impulse);
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
        Gizmos.color = Color.green;
        Vector3 targetPos = transform.position + transform.forward * dribbleOffset;
        Gizmos.DrawWireSphere(targetPos, 0.2f);
    }

    private void RegisterKickTouch()
    {
        Team myTeam = Team.None;

        // 尝试1：通过 ITeamMember 接口获取（NPCController 实现了这个接口）
        ITeamMember member = GetComponent<ITeamMember>();
        if (member != null)
        {
            myTeam = member.GetTeam();
        }
        // 尝试2：兜底机制，通过 PlayerAttributes 获取（人类玩家可能只挂了这个）
        else
        {
            PlayerAttributes attr = GetComponent<PlayerAttributes>();
            if (attr != null)
            {
                myTeam = attr.team;
            }
            else
            {
                Debug.LogError($"找不到队伍信息！请确保 {gameObject.name} 挂载了 ITeamMember 或 PlayerAttributes！");
            }
        }

        // 只要不是空队伍，就登记！
        if (myTeam != Team.None)
        {
            BallTouchTracker.ManualSetLastTouchTeam(myTeam);
        }
    }

}
