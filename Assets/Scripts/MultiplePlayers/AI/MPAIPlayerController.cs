using Mirror;
using UnityEngine;

namespace MultiplePlayers
{
    [RequireComponent(typeof(Rigidbody))]
    public class MPAIPlayerController : NetworkBehaviour
    {
        private enum AIState
        {
            Idle,
            ReturnToFormation,
            ChaseBall,
            KickOrPass
        }

        [Header("Identity")]
        [SerializeField] private MPTeamId teamId = MPTeamId.None;
        [SerializeField] private MPPlayerPosition position = MPPlayerPosition.None;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float rotationSpeed = 540f;
        [SerializeField] private float stopDistance = 0.35f;

        [Header("Ball Detection")]
        [SerializeField] private float chaseDistance = 14f;
        [SerializeField] private float kickDistance = 1.8f;
        [SerializeField] private float kickCooldown = 0.8f;

        [Header("Goalkeeper")]
        [SerializeField] private float goalkeeperChaseDistanceFromOwnGoal = 16f;
        [SerializeField] private float goalkeeperMaxLeaveGoalDistance = 12f;

        [Header("Kick / Pass")]
        [SerializeField] private float passForce = 9f;
        [SerializeField] private float clearForce = 13f;
        [SerializeField] private float passUpRatio = 0.05f;
        [SerializeField] private float clearUpRatio = 0.12f;
        [SerializeField] private float minForwardPassDistance = 2f;
        [SerializeField] private float maxPassDistance = 24f;

        [Header("Debug")]
        [SerializeField] private bool verboseLog = false;

        private Rigidbody rb;
        private Transform formationPoint;
        private MPNetworkBall ball;
        private AIState currentState;
        private double nextKickAllowedTime;

        public MPTeamId TeamId => teamId;
        public MPPlayerPosition Position => position;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        [Server]
        public void ServerInitialize(
            MPTeamId newTeam,
            MPPlayerPosition newPosition,
            Transform newFormationPoint)
        {
            teamId = newTeam;
            position = newPosition;
            formationPoint = newFormationPoint;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            MPPlayerTeamState teamState =
                GetComponent<MPPlayerTeamState>();

            if (teamState != null)
            {
                teamId = teamState.TeamId;
                position = teamState.Position;
            }

            ServerFindBall();
        }

        [ServerCallback]
        private void FixedUpdate()
        {
            if (!ServerCanAct())
            {
                ServerSetState(AIState.Idle);
                ServerStopMove();
                return;
            }

            if (ball == null)
                ServerFindBall();

            if (ball == null)
            {
                ServerSetState(AIState.Idle);
                ServerStopMove();
                return;
            }

            ServerUpdateState();

            switch (currentState)
            {
                case AIState.Idle:
                    ServerStopMove();
                    break;

                case AIState.ReturnToFormation:
                    ServerMoveTo(ServerGetFormationPosition());
                    break;

                case AIState.ChaseBall:
                    ServerMoveTo(ball.transform.position);
                    break;

                case AIState.KickOrPass:
                    ServerStopMove();
                    ServerTryKickOrPass();
                    break;
            }
        }

        [Server]
        private bool ServerCanAct()
        {
            return MPGameSession.IsMatchControllable;
        }

        [Server]
        private void ServerUpdateState()
        {
            Vector3 ballPos = ball.transform.position;
            float sqrToBall = (transform.position - ballPos).sqrMagnitude;

            if (sqrToBall <= kickDistance * kickDistance)
            {
                ServerSetState(AIState.KickOrPass);
                return;
            }

            MPAIPlayerController bestChaser = null;

            if (MPAIManager.Instance != null)
            {
                bestChaser =
                    MPAIManager.Instance.ServerFindBestChaser(teamId, ballPos);
            }

            if (bestChaser == this)
            {
                ServerSetState(AIState.ChaseBall);
                return;
            }

            ServerSetState(AIState.ReturnToFormation);
        }

        [Server]
        public bool ServerCanChaseBallAt(Vector3 ballPosition)
        {
            float sqrToBall = (transform.position - ballPosition).sqrMagnitude;

            if (sqrToBall > chaseDistance * chaseDistance)
                return false;

            if (position == MPPlayerPosition.Goalkeeper)
            {
                Vector3 ownGoal = MPTeamUtility.GetOwnGoalPosition(teamId);

                float sqrBallToOwnGoal =
                    (ballPosition - ownGoal).sqrMagnitude;

                float sqrSelfToOwnGoal =
                    (transform.position - ownGoal).sqrMagnitude;

                bool ballNearGoal =
                    sqrBallToOwnGoal <=
                    goalkeeperChaseDistanceFromOwnGoal *
                    goalkeeperChaseDistanceFromOwnGoal;

                bool keeperNotTooFar =
                    sqrSelfToOwnGoal <=
                    goalkeeperMaxLeaveGoalDistance *
                    goalkeeperMaxLeaveGoalDistance;

                return ballNearGoal && keeperNotTooFar;
            }

            return true;
        }

        [Server]
        private void ServerMoveTo(Vector3 target)
        {
            Vector3 current = rb.position;
            Vector3 direction = target - current;
            direction.y = 0f;

            if (direction.sqrMagnitude <= stopDistance * stopDistance)
            {
                ServerStopMove();
                return;
            }

            Vector3 moveDir = direction.normalized;
            Vector3 nextPosition =
                current + moveDir * moveSpeed * Time.fixedDeltaTime;

            rb.MovePosition(nextPosition);

            Quaternion targetRotation =
                Quaternion.LookRotation(moveDir, Vector3.up);

            Quaternion nextRotation =
                Quaternion.RotateTowards(
                    rb.rotation,
                    targetRotation,
                    rotationSpeed * Time.fixedDeltaTime);

            rb.MoveRotation(nextRotation);

            ServerSendMoveSpeed(moveSpeed);
        }

        [Server]
        private void ServerStopMove()
        {
            ServerSendMoveSpeed(0f);
        }

        [Server]
        private Vector3 ServerGetFormationPosition()
        {
            if (formationPoint != null)
                return formationPoint.position;

            if (MPAIManager.Instance != null)
            {
                Transform found =
                    MPAIManager.Instance.ServerFindFormationPoint(
                        teamId,
                        position,
                        0);

                if (found != null)
                {
                    formationPoint = found;
                    return formationPoint.position;
                }
            }

            return transform.position;
        }

        [Server]
        private void ServerTryKickOrPass()
        {
            if (NetworkTime.time < nextKickAllowedTime)
                return;

            if (ball == null)
                return;

            Transform targetTeammate =
                ServerFindForwardTeammate();

            if (targetTeammate != null)
            {
                Vector3 direction = targetTeammate.position - ball.transform.position;
                direction.y = 0f;

                if (direction.sqrMagnitude < 0.01f)
                    direction = MPTeamUtility.GetAttackDirection(teamId);

                direction.Normalize();

                Vector3 finalDirection =
                    (direction + Vector3.up * passUpRatio).normalized;

                ServerApplyBallForce(finalDirection, passForce);

                if (verboseLog)
                {
                    Debug.Log($"[MPAI] {name} pass to {targetTeammate.name}");
                }
            }
            else
            {
                Vector3 direction = MPTeamUtility.GetAttackDirection(teamId);
                Vector3 finalDirection =
                    (direction + Vector3.up * clearUpRatio).normalized;

                ServerApplyBallForce(finalDirection, clearForce);

                if (verboseLog)
                {
                    Debug.Log($"[MPAI] {name} clear forward");
                }
            }

            nextKickAllowedTime = NetworkTime.time + kickCooldown;
        }

        [Server]
        private Transform ServerFindForwardTeammate()
        {
            MPPlayerTeamState[] players =
                FindObjectsByType<MPPlayerTeamState>(FindObjectsSortMode.None);

            Transform best = null;
            float bestScore = float.NegativeInfinity;

            Vector3 selfPos = transform.position;
            Vector3 attackDir = MPTeamUtility.GetAttackDirection(teamId);

            foreach (MPPlayerTeamState player in players)
            {
                if (player == null)
                    continue;

                if (player.TeamId != teamId)
                    continue;

                if (player.transform == transform)
                    continue;

                Vector3 teammatePos = player.transform.position;
                Vector3 toMate = teammatePos - selfPos;
                toMate.y = 0f;

                float distance = toMate.magnitude;

                if (distance < 1.5f || distance > maxPassDistance)
                    continue;

                float forwardAmount = Vector3.Dot(toMate, attackDir);

                if (forwardAmount < minForwardPassDistance)
                    continue;

                // 越靠前越优先，同时轻微惩罚横向距离太大的队友
                float lateralPenalty =
                    Mathf.Abs(Vector3.Dot(toMate, Vector3.forward)) * 0.15f;

                float score = forwardAmount - lateralPenalty;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = player.transform;
                }
            }

            return best;
        }

        [Server]
        private void ServerApplyBallForce(Vector3 direction, float force)
        {
            if (ball == null)
                return;

            ball.ServerStopAndClearControl();

            Rigidbody ballRb = ball.GetComponent<Rigidbody>();

            if (ballRb == null)
            {
                Debug.LogWarning("[MPAI] Ball has no Rigidbody.");
                return;
            }

            ballRb.isKinematic = false;
            ballRb.linearVelocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;
            ballRb.AddForce(direction.normalized * force, ForceMode.Impulse);
        }

        [Server]
        private void ServerFindBall()
        {
            ball = FindFirstObjectByType<MPNetworkBall>();
        }

        [Server]
        private void ServerSetState(AIState newState)
        {
            currentState = newState;
        }

        [Server]
        private void ServerSendMoveSpeed(float speed)
        {
            // 如果你的 MPPlayerAnimationController 里有 ServerSetMoveSpeed(float)，会自动调用。
            // 如果没有，也不会报错。
            SendMessage(
                "ServerSetMoveSpeed",
                speed,
                SendMessageOptions.DontRequireReceiver);
        }
    }
}