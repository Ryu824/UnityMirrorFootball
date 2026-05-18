using System.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if USE_MIRROR
using Mirror;

namespace MultiplePlayers
{

    public class MPGameSession : NetworkBehaviour
    {
        public static MPGameSession Instance { get; private set; }

        [Header("Match Rules")]
        [SerializeField] private double gameDurationSeconds = 120d * 60d;
        [SerializeField] private double shutdownDelaySeconds = 10d;

        [Header("Rule Pause")]
        [SerializeField] private double ruleRestartDelaySeconds = 5d;
        [SerializeField] private double ruleMessageDurationSeconds = 3d;

        [Tooltip("防止进球 / 出界在极短时间内重复触发。")]
        [SerializeField] private double ruleEventLockoutSeconds = 0.5d;

        private double nextRuleEventAllowedTime;

        [Header("Set Piece Restart")]
        [SerializeField] private Transform leftTopCornerPoint;
        [SerializeField] private Transform leftBottomCornerPoint;
        [SerializeField] private Transform rightTopCornerPoint;
        [SerializeField] private Transform rightBottomCornerPoint;
        [SerializeField] private Transform leftGoalKickPoint;
        [SerializeField] private Transform rightGoalKickPoint;

        [SerializeField] private float restartGroundBallY = 0.35f;
        [SerializeField] private float throwInBallHeight = 1.6f;
        [SerializeField] private float executorOffsetFromBall = 1.4f;
        [SerializeField] private float restartReControlCooldown = 0.45f;

        [Header("Set Piece Force - Throw In")]
        [SerializeField] private float throwInMinForce = 5f;
        [SerializeField] private float throwInMaxForce = 15f;
        [SerializeField] private float throwInUpRatio = 0.55f;

        [Header("Set Piece Force - Corner Kick")]
        [SerializeField] private float cornerMinForce = 10f;
        [SerializeField] private float cornerMaxForce = 32f;
        [SerializeField] private float cornerUpRatio = 0.38f;

        [Header("Set Piece Force - Goal Kick")]
        [SerializeField] private float goalKickMinForce = 14f;
        [SerializeField] private float goalKickMaxForce = 42f;
        [SerializeField] private float goalKickUpRatio = 0.42f;

        [Header("Current Set Piece State")]
        [SyncVar] private MPRuleEventType restartEventType = MPRuleEventType.None;
        [SyncVar] private MPTeam restartAwardTeam = MPTeam.None;
        [SyncVar] private MPRestartLocation restartLocation = MPRestartLocation.None;
        [SyncVar] private MPSetPieceMode restartMode = MPSetPieceMode.None;
        [SyncVar] private Vector3 restartPoint;
        [SyncVar] private uint restartBallNetId;
        [SyncVar] private uint restartExecutorNetId;
        [SyncVar] private float restartMinForce;
        [SyncVar] private float restartMaxForce;
        [SyncVar] private float restartUpRatio;

        [Tooltip("默认 false：只要求真正连接进来的 Client Ready，Host 不需要 Ready。")]

        [SerializeField] private bool includeHostInReadyCheck = false;

        [Header("Network Match State")]
        [SyncVar] private MPMatchState phase = MPMatchState.Lobby;
        [SyncVar] private double gameStartTime;
        [SyncVar] private double shutdownTime;

        [Header("Ready State")]
        [SyncVar] private int readyPlayers;
        [SyncVar] private int requiredReadyPlayers;
        [SyncVar] private bool allClientsReady;

        [Header("Score State")]
        [SyncVar] private int redLeftScore;
        [SyncVar] private int blueRightScore;

        [Header("Center Message State")]
        [SyncVar] private string centerMessage;
        [SyncVar] private double centerMessageExpireTime;

        [Header("Pause Time State")]
        [SyncVar] private double accumulatedPauseSeconds;
        [SyncVar] private double currentPauseStartTime;

        private Coroutine rulePauseCoroutine;

        public MPMatchState Phase => phase;
        public bool IsLobby => phase == MPMatchState.Lobby;
        public bool IsSetPiece => phase == MPMatchState.SetPiece;
        public MPRuleEventType RestartEventType => restartEventType;
        public MPTeam RestartAwardTeam => restartAwardTeam;
        public MPRestartLocation RestartLocation => restartLocation;
        public MPSetPieceMode RestartMode => restartMode;
        public Vector3 RestartPoint => restartPoint;
        public uint RestartExecutorNetId => restartExecutorNetId;
        public float RestartMinForce => restartMinForce;
        public float RestartMaxForce => restartMaxForce;
        public float RestartUpRatio => restartUpRatio;
        public bool AllClientsReady => allClientsReady;
        public string ReadyText => $"{readyPlayers}/{requiredReadyPlayers}";

        public int RedLeftScore => redLeftScore;
        public int BlueRightScore => blueRightScore;

        public string ScoreText => $"Red {redLeftScore} - {blueRightScore} Blue";

        public string CenterMessage => centerMessage;

        public bool ShouldShowCenterMessage =>
            !string.IsNullOrEmpty(centerMessage)
            && NetworkTime.time < centerMessageExpireTime;

        public static bool IsGamePlaying =>
            Instance != null && Instance.phase == MPMatchState.Playing;

        public static bool IsMatchControllable =>
            Instance != null && Instance.phase == MPMatchState.Playing;

        public double ElapsedSeconds
        {
            get
            {
                if (phase == MPMatchState.Lobby || gameStartTime <= 0d)
                {
                    return 0d;
                }

                double pauseSeconds = accumulatedPauseSeconds;

                if ((phase == MPMatchState.RulePause || phase == MPMatchState.SetPiece)
                    && currentPauseStartTime > 0d)
                {
                    pauseSeconds += NetworkTime.time - currentPauseStartTime;
                }

                double elapsed = NetworkTime.time - gameStartTime - pauseSeconds;

                return System.Math.Max(0d, System.Math.Min(gameDurationSeconds, elapsed));
            }
        }

        public double RemainingSeconds
        {
            get
            {
                if (phase == MPMatchState.Lobby)
                {
                    return gameDurationSeconds;
                }

                if (phase == MPMatchState.TimeUp || phase == MPMatchState.Closing)
                {
                    return 0d;
                }

                return System.Math.Max(0d, gameDurationSeconds - ElapsedSeconds);
            }
        }

        public double ShutdownRemainingSeconds
        {
            get
            {
                if (phase != MPMatchState.TimeUp && phase != MPMatchState.Closing)
                {
                    return 0d;
                }

                return System.Math.Max(0d, shutdownTime - NetworkTime.time);
            }
        }

        public string PhaseText
        {
            get
            {
                switch (phase)
                {
                    case MPMatchState.Lobby:
                        return "Lobby";
                    case MPMatchState.Playing:
                        return "Playing";
                    case MPMatchState.RulePause:
                        return "Rule Pause";
                    case MPMatchState.SetPiece:
                        return "Set Piece";
                    case MPMatchState.TimeUp:
                        return "Time Up";
                    case MPMatchState.Closing:
                        return "Closing";
                    default:
                        return phase.ToString();
                }
            }
        }

        public string ElapsedText => FormatSeconds(ElapsedSeconds);
        public string RemainingText => FormatSeconds(RemainingSeconds);
        public string ShutdownText => Mathf.CeilToInt((float)ShutdownRemainingSeconds).ToString();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            ServerResetLobby();
        }

        [Server]
        private void ServerResetLobby()
        {
            phase = MPMatchState.Lobby;

            gameStartTime = 0d;
            shutdownTime = 0d;

            redLeftScore = 0;
            blueRightScore = 0;

            centerMessage = string.Empty;
            centerMessageExpireTime = 0d;

            accumulatedPauseSeconds = 0d;
            currentPauseStartTime = 0d;

            nextRuleEventAllowedTime = 0d;
            ServerClearSetPieceState();

            RefreshReadyState();
        }

        [ServerCallback]
        private void Update()
        {
            switch (phase)
            {
                case MPMatchState.Lobby:
                    RefreshReadyState();
                    break;

                case MPMatchState.Playing:
                    if (RemainingSeconds <= 0d)
                    {
                        ServerEndGame();
                    }
                    break;

                case MPMatchState.RulePause:
                    // 暂停状态由协程推进，这里不扣比赛时间。
                    break;

                case MPMatchState.SetPiece:
                    // 等待指定球员发球，这里不扣比赛时间。
                    break;

                case MPMatchState.TimeUp:
                    if (NetworkTime.time >= shutdownTime)
                    {
                        ServerCloseGame();
                    }
                    break;
            }
        }

        [Server]
        public void ServerNotifyPlayerListOrReadyChanged()
        {
            if (phase == MPMatchState.Lobby)
            {
                RefreshReadyState();
            }
        }

        [Server]
        public bool ServerCanStartGame()
        {
            RefreshReadyState();

            return phase == MPMatchState.Lobby
                   && allClientsReady
                   && requiredReadyPlayers > 0;
        }

        [Server]
        public void ServerTryStartGame()
        {
            if (!ServerCanStartGame())
            {
                return;
            }

            phase = MPMatchState.Playing;

            gameStartTime = NetworkTime.time;
            shutdownTime = 0d;

            redLeftScore = 0;
            blueRightScore = 0;

            centerMessage = string.Empty;
            centerMessageExpireTime = 0d;

            accumulatedPauseSeconds = 0d;
            currentPauseStartTime = 0d;

            nextRuleEventAllowedTime = 0d;
            ServerClearSetPieceState();

            if (MPAIManager.Instance != null)
            {
                MPAIManager.Instance.ServerSpawnAIsForMatch();
            }
        }

        [Server]
        public void ServerReportBoundary(MPNetworkBall ball, MPBoundaryType boundaryType)
        {
            Vector3 boundaryPoint = ball != null ? ball.transform.position : Vector3.zero;
            ServerHandleBoundaryTriggered(boundaryType, ball, boundaryPoint);
        }

        [Server]
        public void ServerReportBoundary(
            MPNetworkBall ball,
            MPBoundaryType boundaryType,
            Vector3 boundaryPoint)
        {
            ServerHandleBoundaryTriggered(boundaryType, ball, boundaryPoint);
        }

        [Server]
        public void ServerHandleBoundaryTriggered(
            MPBoundaryType boundaryType,
            MPNetworkBall ball,
            Vector3 boundaryPoint)
        {
            if (phase != MPMatchState.Playing)
            {
                return;
            }

            if (ball == null)
            {
                return;
            }

            if (NetworkTime.time < nextRuleEventAllowedTime)
            {
                return;
            }

            MPRuleDecision decision = ServerEvaluateBoundary(boundaryType, ball);

            if (decision.eventType == MPRuleEventType.None)
            {
                return;
            }

            nextRuleEventAllowedTime = NetworkTime.time + ruleEventLockoutSeconds;

            if (decision.eventType == MPRuleEventType.Goal)
            {
                AddScore(decision.awardTeam);
            }

            ServerBeginRulePause(decision, ball, boundaryPoint);
        }

        [Server]
        private MPRuleDecision ServerEvaluateBoundary(
            MPBoundaryType boundaryType,
            MPNetworkBall ball)
        {
            MPTeam lastTouchTeam = ball.ServerGetEffectiveLastTouchTeam();

            switch (boundaryType)
            {
                case MPBoundaryType.GoalLine_Left_Goal:
                    return new MPRuleDecision(
                        MPRuleEventType.Goal,
                        MPTeam.BlueRight,
                        MPRestartLocation.Center,
                        "Goal for BlueRight");

                case MPBoundaryType.GoalLine_Right_Goal:
                    return new MPRuleDecision(
                        MPRuleEventType.Goal,
                        MPTeam.RedLeft,
                        MPRestartLocation.Center,
                        "Goal for RedLeft");

                case MPBoundaryType.TouchLine_Top:
                    return ServerMakeThrowInDecision(
                        lastTouchTeam,
                        MPRestartLocation.TouchLineTop);

                case MPBoundaryType.TouchLine_Bottom:
                    return ServerMakeThrowInDecision(
                        lastTouchTeam,
                        MPRestartLocation.TouchLineBottom);

                case MPBoundaryType.GoalLine_Left_Out_Top:
                    return ServerMakeGoalLineOutDecision(
                        lastTouchTeam,
                        defendingTeam: MPTeam.RedLeft,
                        attackingTeam: MPTeam.BlueRight,
                        cornerLocation: MPRestartLocation.LeftTopCorner,
                        goalKickLocation: MPRestartLocation.LeftGoalKick);

                case MPBoundaryType.GoalLine_Left_Out_Bottom:
                    return ServerMakeGoalLineOutDecision(
                        lastTouchTeam,
                        defendingTeam: MPTeam.RedLeft,
                        attackingTeam: MPTeam.BlueRight,
                        cornerLocation: MPRestartLocation.LeftBottomCorner,
                        goalKickLocation: MPRestartLocation.LeftGoalKick);

                case MPBoundaryType.GoalLine_Right_Out_Top:
                    return ServerMakeGoalLineOutDecision(
                        lastTouchTeam,
                        defendingTeam: MPTeam.BlueRight,
                        attackingTeam: MPTeam.RedLeft,
                        cornerLocation: MPRestartLocation.RightTopCorner,
                        goalKickLocation: MPRestartLocation.RightGoalKick);

                case MPBoundaryType.GoalLine_Right_Out_Bottom:
                    return ServerMakeGoalLineOutDecision(
                        lastTouchTeam,
                        defendingTeam: MPTeam.BlueRight,
                        attackingTeam: MPTeam.RedLeft,
                        cornerLocation: MPRestartLocation.RightBottomCorner,
                        goalKickLocation: MPRestartLocation.RightGoalKick);

                default:
                    return new MPRuleDecision(
                        MPRuleEventType.None,
                        MPTeam.None,
                        MPRestartLocation.None,
                        string.Empty);
            }
        }

        [Server]
        private MPRuleDecision ServerMakeThrowInDecision(
            MPTeam lastTouchTeam,
            MPRestartLocation restartLocation)
        {
            if (lastTouchTeam == MPTeam.None)
            {
                return new MPRuleDecision(
                    MPRuleEventType.UnknownOut,
                    MPTeam.None,
                    restartLocation,
                    "Out of bounds");
            }

            MPTeam awardTeam = GetOppositeTeam(lastTouchTeam);

            return new MPRuleDecision(
                MPRuleEventType.ThrowIn,
                awardTeam,
                restartLocation,
                $"Throw-in for {TeamName(awardTeam)}");
        }

        [Server]
        private MPRuleDecision ServerMakeGoalLineOutDecision(
            MPTeam lastTouchTeam,
            MPTeam defendingTeam,
            MPTeam attackingTeam,
            MPRestartLocation cornerLocation,
            MPRestartLocation goalKickLocation)
        {
            if (lastTouchTeam == MPTeam.None)
            {
                return new MPRuleDecision(
                    MPRuleEventType.UnknownOut,
                    MPTeam.None,
                    MPRestartLocation.Center,
                    "Out of bounds");
            }

            if (lastTouchTeam == defendingTeam)
            {
                return new MPRuleDecision(
                    MPRuleEventType.CornerKick,
                    attackingTeam,
                    cornerLocation,
                    $"Corner kick for {TeamName(attackingTeam)}");
            }

            if (lastTouchTeam == attackingTeam)
            {
                return new MPRuleDecision(
                    MPRuleEventType.GoalKick,
                    defendingTeam,
                    goalKickLocation,
                    $"Goal kick for {TeamName(defendingTeam)}");
            }

            return new MPRuleDecision(
                MPRuleEventType.UnknownOut,
                MPTeam.None,
                MPRestartLocation.Center,
                "Out of bounds");
        }

        private static MPTeam GetOppositeTeam(MPTeam team)
        {
            switch (team)
            {
                case MPTeam.RedLeft:
                    return MPTeam.BlueRight;

                case MPTeam.BlueRight:
                    return MPTeam.RedLeft;

                default:
                    return MPTeam.None;
            }
        }

        private static string TeamName(MPTeam team)
        {
            switch (team)
            {
                case MPTeam.RedLeft:
                    return "RedLeft";

                case MPTeam.BlueRight:
                    return "BlueRight";

                default:
                    return "Unknown";
            }
        }

        [Server]
        private void ServerBeginRulePause(
            MPRuleDecision decision,
            MPNetworkBall ball,
            Vector3 boundaryPoint)
        {
            if (phase != MPMatchState.Playing)
            {
                return;
            }

            phase = MPMatchState.RulePause;
            currentPauseStartTime = NetworkTime.time;

            centerMessage = decision.centerMessage;
            centerMessageExpireTime = NetworkTime.time + ruleMessageDurationSeconds;

            nextRuleEventAllowedTime = NetworkTime.time + ruleEventLockoutSeconds;

            if (ball != null)
            {
                ball.ServerStopAndClearControl();
            }

            if (rulePauseCoroutine != null)
            {
                StopCoroutine(rulePauseCoroutine);
            }

            rulePauseCoroutine = StartCoroutine(
                ServerRulePauseRoutine(decision, ball, boundaryPoint));
        }

        [Server]
        private IEnumerator ServerRulePauseRoutine(
            MPRuleDecision decision,
            MPNetworkBall ball,
            Vector3 boundaryPoint)
        {
            yield return new WaitForSeconds((float)ruleRestartDelaySeconds);

            if (phase != MPMatchState.RulePause)
            {
                yield break;
            }

            if (ball == null)
            {
                ServerFinishPausedRestartToPlaying(null);
                yield break;
            }

            if (ShouldUseSetPiece(decision.eventType))
            {
                ServerEnterSetPiece(decision, ball, boundaryPoint);
                rulePauseCoroutine = null;
                yield break;
            }

            ball.ResetToSpawn();
            ServerFinishPausedRestartToPlaying(ball);
            rulePauseCoroutine = null;
        }

        private static bool ShouldUseSetPiece(MPRuleEventType eventType)
        {
            return eventType == MPRuleEventType.ThrowIn
                   || eventType == MPRuleEventType.CornerKick
                   || eventType == MPRuleEventType.GoalKick
                   || eventType == MPRuleEventType.PenaltyKick;
        }

        [Server]
        private void ServerEnterSetPiece(
            MPRuleDecision decision,
            MPNetworkBall ball,
            Vector3 boundaryPoint)
        {
            Vector3 groundRestartPoint = ServerGetRestartGroundPoint(
                decision.restartLocation,
                boundaryPoint);

            MPSetPieceMode mode = ServerGetSetPieceMode(decision.eventType);

            Vector3 ballPosition = groundRestartPoint;
            ballPosition.y = restartGroundBallY;

            if (mode == MPSetPieceMode.ElevatedThrow)
            {
                ballPosition.y = restartGroundBallY + throwInBallHeight;
            }

            NetworkIdentity executor = ServerFindNearestPlayerInTeam(
                decision.awardTeam,
                groundRestartPoint);

            if (executor == null)
            {
                centerMessage = "No restart player found";
                centerMessageExpireTime = NetworkTime.time + ruleMessageDurationSeconds;
                ball.ResetToSpawn();
                ServerFinishPausedRestartToPlaying(ball);
                return;
            }

            ServerGetSetPieceForceSettings(
                decision.eventType,
                out float minForce,
                out float maxForce,
                out float upRatio);

            restartEventType = decision.eventType;
            restartAwardTeam = decision.awardTeam;
            restartLocation = decision.restartLocation;
            restartMode = mode;
            restartPoint = groundRestartPoint;
            restartBallNetId = ball.netId;
            restartExecutorNetId = executor.netId;
            restartMinForce = minForce;
            restartMaxForce = maxForce;
            restartUpRatio = upRatio;

            ball.ServerPlaceForRestart(ballPosition, freezeBall: true);

            MPNetworkPlayerController playerController =
                executor.GetComponent<MPNetworkPlayerController>();

            if (playerController != null)
            {
                Vector3 playerPosition = ServerGetExecutorPosition(
                    decision.restartLocation,
                    groundRestartPoint);

                Quaternion playerRotation = ServerGetExecutorRotation(
                    playerPosition,
                    groundRestartPoint);

                playerController.ServerTeleportForRestart(playerPosition, playerRotation);
            }

            phase = MPMatchState.SetPiece;

            centerMessage =
                $"{decision.centerMessage}\nHold Left Mouse to charge, release to play";
            centerMessageExpireTime = NetworkTime.time + 999999d;
        }

        [Server]
        public bool ServerCanPlayerUseSetPiece(NetworkIdentity playerIdentity)
        {
            if (phase != MPMatchState.SetPiece)
            {
                return false;
            }

            if (playerIdentity == null)
            {
                return false;
            }

            if (playerIdentity.netId != restartExecutorNetId)
            {
                return false;
            }

            MPNetworkPlayerController player =
                playerIdentity.GetComponent<MPNetworkPlayerController>();

            if (player == null || player.Team != restartAwardTeam)
            {
                return false;
            }

            return true;
        }

        [Server]
        public void ServerExecuteSetPiece(
            NetworkIdentity executor,
            Vector3 aimDirection,
            float charge01)
        {
            if (!ServerCanPlayerUseSetPiece(executor))
            {
                return;
            }

            if (!NetworkServer.spawned.TryGetValue(
                    restartBallNetId,
                    out NetworkIdentity ballIdentity))
            {
                return;
            }

            MPNetworkBall ball = ballIdentity.GetComponent<MPNetworkBall>();

            if (ball == null)
            {
                return;
            }

            Vector3 horizontalDirection = aimDirection;
            horizontalDirection.y = 0f;

            if (horizontalDirection.sqrMagnitude < 0.01f)
            {
                horizontalDirection = executor.transform.forward;
                horizontalDirection.y = 0f;
            }

            if (horizontalDirection.sqrMagnitude < 0.01f)
            {
                horizontalDirection = Vector3.forward;
            }

            horizontalDirection.Normalize();

            Vector3 finalDirection = horizontalDirection + Vector3.up * restartUpRatio;
            finalDirection.Normalize();

            float safeCharge01 = Mathf.Clamp01(charge01);
            float force = Mathf.Lerp(restartMinForce, restartMaxForce, safeCharge01);

            ServerFinishPausedRestartToPlaying(ball);
            ball.ServerKickRestart(
                executor,
                finalDirection,
                force,
                restartReControlCooldown);

            centerMessage = string.Empty;
            centerMessageExpireTime = 0d;
            ServerClearSetPieceState();
        }

        [Server]
        private void ServerFinishPausedRestartToPlaying(MPNetworkBall ball)
        {
            if (currentPauseStartTime > 0d)
            {
                accumulatedPauseSeconds += NetworkTime.time - currentPauseStartTime;
                currentPauseStartTime = 0d;
            }

            nextRuleEventAllowedTime = NetworkTime.time + ruleEventLockoutSeconds;
            phase = MPMatchState.Playing;
        }

        [Server]
        private Vector3 ServerGetRestartGroundPoint(
            MPRestartLocation location,
            Vector3 boundaryPoint)
        {
            Vector3 result = boundaryPoint;
            result.y = restartGroundBallY;

            switch (location)
            {
                case MPRestartLocation.LeftTopCorner:
                    return ServerTransformPointOrFallback(leftTopCornerPoint, result);

                case MPRestartLocation.LeftBottomCorner:
                    return ServerTransformPointOrFallback(leftBottomCornerPoint, result);

                case MPRestartLocation.RightTopCorner:
                    return ServerTransformPointOrFallback(rightTopCornerPoint, result);

                case MPRestartLocation.RightBottomCorner:
                    return ServerTransformPointOrFallback(rightBottomCornerPoint, result);

                case MPRestartLocation.LeftGoalKick:
                    return ServerTransformPointOrFallback(leftGoalKickPoint, result);

                case MPRestartLocation.RightGoalKick:
                    return ServerTransformPointOrFallback(rightGoalKickPoint, result);

                case MPRestartLocation.TouchLineTop:
                case MPRestartLocation.TouchLineBottom:
                    return result;

                default:
                    return result;
            }
        }

        private Vector3 ServerTransformPointOrFallback(Transform point, Vector3 fallback)
        {
            if (point == null)
            {
                return fallback;
            }

            Vector3 result = point.position;
            result.y = restartGroundBallY;
            return result;
        }

        private static MPSetPieceMode ServerGetSetPieceMode(MPRuleEventType eventType)
        {
            if (eventType == MPRuleEventType.ThrowIn)
            {
                return MPSetPieceMode.ElevatedThrow;
            }

            if (eventType == MPRuleEventType.CornerKick
                || eventType == MPRuleEventType.GoalKick
                || eventType == MPRuleEventType.PenaltyKick)
            {
                return MPSetPieceMode.GroundKick;
            }

            return MPSetPieceMode.None;
        }

        private void ServerGetSetPieceForceSettings(
            MPRuleEventType eventType,
            out float minForce,
            out float maxForce,
            out float upRatio)
        {
            switch (eventType)
            {
                case MPRuleEventType.ThrowIn:
                    minForce = throwInMinForce;
                    maxForce = throwInMaxForce;
                    upRatio = throwInUpRatio;
                    return;

                case MPRuleEventType.GoalKick:
                    minForce = goalKickMinForce;
                    maxForce = goalKickMaxForce;
                    upRatio = goalKickUpRatio;
                    return;

                case MPRuleEventType.CornerKick:
                case MPRuleEventType.PenaltyKick:
                    minForce = cornerMinForce;
                    maxForce = cornerMaxForce;
                    upRatio = cornerUpRatio;
                    return;

                default:
                    minForce = 0f;
                    maxForce = 0f;
                    upRatio = 0f;
                    return;
            }
        }

        [Server]
        private NetworkIdentity ServerFindNearestPlayerInTeam(
            MPTeam team,
            Vector3 point)
        {
            NetworkIdentity bestIdentity = null;
            float bestSqrDistance = float.MaxValue;

            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (conn == null || conn.identity == null)
                {
                    continue;
                }

                MPNetworkPlayerController player =
                    conn.identity.GetComponent<MPNetworkPlayerController>();

                if (player == null || player.Team != team)
                {
                    continue;
                }

                float sqrDistance =
                    (conn.identity.transform.position - point).sqrMagnitude;

                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    bestIdentity = conn.identity;
                }
            }

            return bestIdentity;
        }

        private Vector3 ServerGetExecutorPosition(
            MPRestartLocation location,
            Vector3 restartGroundPoint)
        {
            Vector3 insideDirection = ServerGetInsideDirection(location);

            if (insideDirection.sqrMagnitude < 0.01f)
            {
                insideDirection = -restartGroundPoint;
                insideDirection.y = 0f;
            }

            if (insideDirection.sqrMagnitude < 0.01f)
            {
                insideDirection = Vector3.back;
            }

            insideDirection.Normalize();

            Vector3 position = restartGroundPoint + insideDirection * executorOffsetFromBall;
            position.y = restartGroundBallY;
            return position;
        }

        private Quaternion ServerGetExecutorRotation(
            Vector3 playerPosition,
            Vector3 restartGroundPoint)
        {
            Vector3 lookDirection = restartGroundPoint - playerPosition;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude < 0.01f)
            {
                lookDirection = Vector3.forward;
            }

            return Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }

        private static Vector3 ServerGetInsideDirection(MPRestartLocation location)
        {
            switch (location)
            {
                case MPRestartLocation.TouchLineTop:
                    return Vector3.back;

                case MPRestartLocation.TouchLineBottom:
                    return Vector3.forward;

                case MPRestartLocation.LeftTopCorner:
                    return (Vector3.right + Vector3.back).normalized;

                case MPRestartLocation.LeftBottomCorner:
                    return (Vector3.right + Vector3.forward).normalized;

                case MPRestartLocation.RightTopCorner:
                    return (Vector3.left + Vector3.back).normalized;

                case MPRestartLocation.RightBottomCorner:
                    return (Vector3.left + Vector3.forward).normalized;

                case MPRestartLocation.LeftGoalKick:
                    return Vector3.right;

                case MPRestartLocation.RightGoalKick:
                    return Vector3.left;

                default:
                    return Vector3.zero;
            }
        }

        [Server]
        private void ServerClearSetPieceState()
        {
            restartEventType = MPRuleEventType.None;
            restartAwardTeam = MPTeam.None;
            restartLocation = MPRestartLocation.None;
            restartMode = MPSetPieceMode.None;
            restartPoint = Vector3.zero;
            restartBallNetId = 0;
            restartExecutorNetId = 0;
            restartMinForce = 0f;
            restartMaxForce = 0f;
            restartUpRatio = 0f;
        }

        [Server]
        private void AddScore(MPTeam team)
        {
            switch (team)
            {
                case MPTeam.RedLeft:
                    redLeftScore++;
                    break;

                case MPTeam.BlueRight:
                    blueRightScore++;
                    break;
            }
        }

        [Server]
        private void ServerEndGame()
        {
            if (phase != MPMatchState.Playing)
            {
                return;
            }

            phase = MPMatchState.TimeUp;
            shutdownTime = NetworkTime.time + shutdownDelaySeconds;

            centerMessage = "GAME OVER";
            centerMessageExpireTime = shutdownTime;
        }

        [Server]
        private void ServerCloseGame()
        {
            if (phase == MPMatchState.Closing)
            {
                return;
            }

            phase = MPMatchState.Closing;

            RpcQuitGame();

            Invoke(nameof(ServerStopNetwork), 0.2f);
        }

        [Server]
        private void RefreshReadyState()
        {
            int required = 0;
            int ready = 0;

            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (conn == null || conn.identity == null)
                {
                    continue;
                }

                bool isHostPlayer =
                    NetworkClient.active
                    && conn.identity != null
                    && conn.identity.isLocalPlayer;

                if (!includeHostInReadyCheck && isHostPlayer)
                {
                    continue;
                }

                MPPlayerReadyState readyState =
                    conn.identity.GetComponent<MPPlayerReadyState>();

                if (readyState == null)
                {
                    continue;
                }

                required++;

                if (readyState.IsReady)
                {
                    ready++;
                }
            }

            requiredReadyPlayers = required;
            readyPlayers = ready;
            allClientsReady = required > 0 && ready == required;
        }

        [ClientRpc]
        private void RpcQuitGame()
        {
            StartCoroutine(QuitGameClientSide());
        }

        private IEnumerator QuitGameClientSide()
        {
            yield return new WaitForSeconds(0.1f);

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        [Server]
        private void ServerStopNetwork()
        {
            if (NetworkManager.singleton == null)
            {
                return;
            }

            if (NetworkServer.active && NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopHost();
            }
            else if (NetworkServer.active)
            {
                NetworkManager.singleton.StopServer();
            }
        }

        private static string FormatSeconds(double seconds)
        {
            seconds = System.Math.Max(0d, seconds);

            int totalSeconds = Mathf.FloorToInt((float)seconds);
            int minutes = totalSeconds / 60;
            int secs = totalSeconds % 60;

            return $"{minutes:00}:{secs:00}";
        }

        public static bool CanControlCamera(uint playerNetId)
        {
            if (Instance == null)
            {
                return false;
            }

            if (Instance.phase == MPMatchState.Playing)
            {
                return true;
            }

            if (Instance.phase == MPMatchState.SetPiece)
            {
                return playerNetId != 0
                    && Instance.restartExecutorNetId == playerNetId;
            }

            return false;
        }
    }
}
#else
namespace MultiplePlayers
{
    public class MPGameSession : MonoBehaviour
    {
        [TextArea(4, 8)]
        [SerializeField]
        private string setupHint =
            "Mirror is not installed yet. Add USE_MIRROR to enable MPGameSession.";
    }
}
#endif
