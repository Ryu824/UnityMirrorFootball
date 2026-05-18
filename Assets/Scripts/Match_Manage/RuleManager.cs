using UnityEngine;
using UnityEngine.Events;

public class RuleManager : MonoBehaviour
{
    public static RuleManager Instance { get; private set; }
    [HideInInspector] public Team currentPossessionTeam = Team.None;
    [HideInInspector] public float globalSpeedMultiplier = 1f;

    [Header("场地引用")]
    public Transform ballTransform;

    [Header("发球预设点 (根据你的实际场地大小在Scene视图拖拽调整)")]
    public Vector3 leftCornerPos;
    public Vector3 rightCornerPos;
    public Vector3 leftGoalKickPos;
    public Vector3 rightGoalKickPos;
    public Vector3 centerCirclePos;

    [Header("事件广播 (供UI或未来的AI监听)")]
    public UnityEvent<string> OnMatchEvent; // 传递出界/进球的文本提示
    public UnityEvent OnMatchStart;
    public UnityEvent OnMatchEnd;

    // 当前状态
    public MatchState CurrentState { get; private set; } = MatchState.PreMatch;

    // 时间控制
    private float matchDuration;
    private float currentMatchTime;

    private Rigidbody ballRigidbody;

    [Header("UI data")]

    public int leftTeamScore = 0;
    public int rightTeamScore = 0;

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        Instance = this;

        if (ballTransform != null)
        {
            ballRigidbody = ballTransform.GetComponent<Rigidbody>();
            ballRigidbody.isKinematic = true;
        }

    }

    void Update()
    {
        if (CurrentState == MatchState.Playing)
        {
            currentMatchTime -= Time.deltaTime;
            if (currentMatchTime <= 0)
            {
                EndMatch();
            }
        }
    }

    // ==========================================
    // 对外接口：由赛前面板调用，开始比赛
    // ==========================================
    public void StartMatch(float duration)
    {
        matchDuration = duration;
        currentMatchTime = duration;
        CurrentState = MatchState.Playing;
        BallTouchTracker.ClearTouchData();

        ResetBallToPosition(centerCirclePos);
        UnlockBall();

        OnMatchStart?.Invoke();
        Debug.Log($"比赛开始！时长: {duration}秒");
    }

    // ==========================================
    // 核心判定：接收边界报告并查表
    // ==========================================
    public void OnBoundaryTriggered(BoundaryType type)
    {
        Team lastTeam = BallTouchTracker.LastTouchTeam;
        Debug.Log($"触发边界！裁判查到的最后触球队伍为: {lastTeam}");
        // 1. 瞬间切入死球，锁死物理
        SetDeadBall();

        // 2. 裁判查表推演
        switch (type)
        {
            case BoundaryType.TouchLine_Top:
            case BoundaryType.TouchLine_Bottom:
                HandleThrowIn(lastTeam);
                break;

            case BoundaryType.GoalLine_Left_Out:
                HandleGoalLineOut(lastTeam, isLeftSide: true);
                break;

            case BoundaryType.GoalLine_Right_Out:
                HandleGoalLineOut(lastTeam, isLeftSide: false);
                break;

            case BoundaryType.GoalLine_Left_Goal:
                HandleGoal(isLeftGoal: true);
                break;

            case BoundaryType.GoalLine_Right_Goal:
                HandleGoal(isLeftGoal: false);
                break;
        }
    }

    // --- 具体规则处理逻辑 ---

    private void HandleThrowIn(Team lastTeam)
    {
        Debug.Log("界外球！");
        OnMatchEvent?.Invoke("界外球");
        // TODO: 未来在这里通知 AI 去发球。目前只做死球拦截测试。
    }

    private void HandleGoalLineOut(Team lastTeam, bool isLeftSide)
    {
        if (lastTeam == Team.None)
        {
            Debug.LogWarning("不知道谁碰出去的，默认中圈开球");
            ResetBallToPosition(centerCirclePos);
            return;
        }

        if (isLeftSide)
        {
            // 球从左侧出底线
            if (lastTeam == Team.TeamA)
            {
                // A队碰出 -> B队角球
                Debug.Log("左侧角球 - TeamB发球");
                OnMatchEvent?.Invoke("TeamB 角球");
                ResetBallToPosition(leftCornerPos);
            }
            else
            {
                // B队碰出 -> A队门球
                Debug.Log("左侧门球 - TeamA发球");
                OnMatchEvent?.Invoke("TeamA 门球");
                ResetBallToPosition(leftGoalKickPos);
            }
        }
        else
        {
            // 球从右侧出底线 (镜像逻辑)
            if (lastTeam == Team.TeamB)
            {
                Debug.Log("右侧角球 - TeamA发球");
                OnMatchEvent?.Invoke("TeamA 角球");
                ResetBallToPosition(rightCornerPos);
            }
            else
            {
                Debug.Log("右侧门球 - TeamB发球");
                OnMatchEvent?.Invoke("TeamB 门球");
                ResetBallToPosition(rightGoalKickPos);
            }
        }
    }

    private void HandleGoal(bool isLeftGoal)
    {
        CurrentState = MatchState.GoalScored;

        if (isLeftGoal) rightTeamScore++;
        else leftTeamScore++;


        string scoringTeam = isLeftGoal ? "TeamB" : "TeamA"; // 进左球门一般是B队得分（假设A队进攻左边）
        Debug.Log($"进球！{scoringTeam} 得分！");
        OnMatchEvent?.Invoke($"{scoringTeam} 进球！");

        // 简单的倒计时恢复（替代协程，更安全）
        Invoke(nameof(ResetAfterGoal), 2f);
    }

    private void ResetAfterGoal()
    {
        ResetBallToPosition(centerCirclePos);
        BallTouchTracker.ClearTouchData();
        CurrentState = MatchState.Playing;
        UnlockBall();
    }

    private void EndMatch()
    {
        CurrentState = MatchState.Ended;
        SetDeadBall();
        Debug.Log("全场结束！");
        OnMatchEvent?.Invoke("比赛结束");
        OnMatchEnd?.Invoke();
    }

    // --- 底层物理控制方法 ---

    private void SetDeadBall()
    {
        CurrentState = MatchState.DeadBall;
        if (ballRigidbody != null)
        {
            ballRigidbody.velocity = Vector3.zero;
            ballRigidbody.angularVelocity = Vector3.zero;
            ballRigidbody.isKinematic = true; // 冻结物理
        }
    }

    private void ResetBallToPosition(Vector3 targetPos)
    {
        if (ballTransform != null)
        {
            ballTransform.position = targetPos;
        }
    }

    private void UnlockBall()
    {
        if (ballRigidbody != null)
        {
            ballRigidbody.isKinematic = false;
        }
    }

    public static void SetPossession(Team team)
    {
        if (Instance != null) Instance.currentPossessionTeam = team;
    }

    public static void ClearPossession()
    {
        if (Instance != null) Instance.currentPossessionTeam = Team.None;
    }
    // UI数据更新
    public string DisplayTime
    {
        get
        {
            if (currentMatchTime <= 0) return "00:00";
            int minutes = Mathf.FloorToInt(currentMatchTime / 60f);
            int seconds = Mathf.FloorToInt(currentMatchTime % 60f);
            return $"{minutes:00}:{seconds:00}";
        }
    }
}
