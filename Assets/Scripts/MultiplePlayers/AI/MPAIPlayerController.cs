using Mirror;
using UnityEngine;

namespace MultiplePlayers
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkIdentity))]
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
        [SerializeField] private float looseBallRecontrolCooldown = 0.35f;

        [Header("Goalkeeper")]
        [SerializeField] private float goalkeeperChaseDistanceFromOwnGoal = 16f;
        [SerializeField] private float goalkeeperMaxLeaveGoalDistance = 12f;

        [Header("Kick / Pass")]
        [SerializeField] private float passForce = 9f;
        [SerializeField] private float clearForce = 13f;
        [SerializeField] private float shootForce = 15f;
        [SerializeField] private float passUpRatio = 0.05f;
        [SerializeField] private float clearUpRatio = 0.12f;
        [SerializeField] private float shootUpRatio = 0.08f;
        [SerializeField] private float minForwardPassDistance = 2f;
        [SerializeField] private float maxPassDistance = 24f;

        [Header("Debug")]
        [SerializeField] private bool verboseLog = false;

        private Rigidbody rb;
        private NetworkIdentity cachedIdentity;
        private MPPlayerAnimationController animationController;
        private Transform formationPoint;
        private Vector3 formationOffset;
        private MPNetworkBall ball;
        private AIState currentState;
        private double nextKickAllowedTime;

        public MPTeamId TeamId => teamId;
        public MPPlayerPosition Position => position;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            cachedIdentity = GetComponent<NetworkIdentity>();
            animationController = GetComponent<MPPlayerAnimationController>();
        }

        [Server]
        public void ServerInitialize(
            MPTeamId newTeam,
            MPPlayerPosition newPosition,
            Transform newFormationPoint,
            Vector3 newFormationOffset)
        {
            teamId = newTeam;
            position = newPosition;
            formationPoint = newFormationPoint;
            formationOffset = newFormationOffset;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            MPPlayerTeamState teamState = GetComponent<MPPlayerTeamState>();

            if (teamState != null)
            {
                teamId = teamState.TeamId;
                position = teamState.Position;
            }

            ServerFindBall();
            ServerSetAnimationGrounded(true);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (isServer)
                return;

            if (rb == null)
            {
                rb = GetComponent<Rigidbody>();
            }

            rb.isKinematic = true;
            rb.useGravity = false;
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
            {
                ServerFindBall();
            }

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
            Vector3 ballPosition = ball.transform.position;
            float sqrToBall = (transform.position - ballPosition).sqrMagnitude;

            if (sqrToBall <= kickDistance * kickDistance)
            {
                ServerSetState(AIState.KickOrPass);
                return;
            }

            MPAIPlayerController bestChaser = null;

            if (MPAIManager.Instance != null)
            {
                bestChaser = MPAIManager.Instance.ServerGetBestChaser(teamId, ballPosition);
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

            if (position != MPPlayerPosition.Goalkeeper)
                return true;

            Vector3 ownGoal = MPTeamUtility.GetOwnGoalPosition(teamId);
            float sqrBallToOwnGoal = (ballPosition - ownGoal).sqrMagnitude;
            float sqrSelfToOwnGoal = (transform.position - ownGoal).sqrMagnitude;

            bool ballNearGoal =
                sqrBallToOwnGoal <=
                goalkeeperChaseDistanceFromOwnGoal * goalkeeperChaseDistanceFromOwnGoal;

            bool keeperNotTooFar =
                sqrSelfToOwnGoal <=
                goalkeeperMaxLeaveGoalDistance * goalkeeperMaxLeaveGoalDistance;

            return ballNearGoal && keeperNotTooFar;
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

            Vector3 moveDirection = direction.normalized;
            Vector3 nextPosition = current + moveDirection * moveSpeed * Time.fixedDeltaTime;
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            Quaternion nextRotation = Quaternion.RotateTowards(
                rb.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime);

            rb.MovePosition(nextPosition);
            rb.MoveRotation(nextRotation);

            ServerSendMoveSpeed(1f);
        }

        [Server]
        private void ServerStopMove()
        {
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
            rb.angularVelocity = Vector3.zero;
            ServerSendMoveSpeed(0f);
        }

        [Server]
        private Vector3 ServerGetFormationPosition()
        {
            if (formationPoint != null)
            {
                return formationPoint.position + formationOffset;
            }

            if (MPAIManager.Instance != null)
            {
                Transform found = MPAIManager.Instance.ServerFindFormationPoint(teamId, position, 0);

                if (found != null)
                {
                    formationPoint = found;
                    return formationPoint.position + formationOffset;
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

            Transform targetTeammate = ServerFindForwardTeammate();

            if (targetTeammate != null)
            {
                Vector3 direction = targetTeammate.position - ball.transform.position;
                direction.y = 0f;

                if (direction.sqrMagnitude < 0.01f)
                {
                    direction = MPTeamUtility.GetAttackDirection(teamId);
                }

                Vector3 finalDirection =
                    (direction.normalized + Vector3.up * passUpRatio).normalized;

                ServerApplyBallForce(finalDirection, passForce);

                if (verboseLog)
                {
                    Debug.Log($"[MPAI] {name} pass to {targetTeammate.name}");
                }
            }
            else
            {
                Vector3 clearDirection = MPTeamUtility.GetAttackDirection(teamId);
                Vector3 shootDirection =
                    MPTeamUtility.GetOpponentGoalPosition(teamId) - ball.transform.position;
                shootDirection.y = 0f;

                if (shootDirection.sqrMagnitude > 0.01f)
                {
                    Vector3 finalDirection =
                        (shootDirection.normalized + Vector3.up * shootUpRatio).normalized;
                    ServerApplyBallForce(finalDirection, shootForce);

                    if (verboseLog)
                    {
                        Debug.Log($"[MPAI] {name} shoot forward");
                    }
                }
                else
                {
                    Vector3 finalDirection =
                        (clearDirection.normalized + Vector3.up * clearUpRatio).normalized;
                    ServerApplyBallForce(finalDirection, clearForce);

                    if (verboseLog)
                    {
                        Debug.Log($"[MPAI] {name} clear forward");
                    }
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
            Vector3 selfPosition = transform.position;
            Vector3 attackDirection = MPTeamUtility.GetAttackDirection(teamId);
            Vector3 lateralAxis = Vector3.Cross(Vector3.up, attackDirection);

            foreach (MPPlayerTeamState player in players)
            {
                if (player == null || player.TeamId != teamId || player.transform == transform)
                    continue;

                Vector3 toTeammate = player.transform.position - selfPosition;
                toTeammate.y = 0f;

                float distance = toTeammate.magnitude;

                if (distance < 1.5f || distance > maxPassDistance)
                    continue;

                float forwardAmount = Vector3.Dot(toTeammate, attackDirection);

                if (forwardAmount < minForwardPassDistance)
                    continue;

                float lateralPenalty =
                    Mathf.Abs(Vector3.Dot(toTeammate, lateralAxis)) * 0.15f;

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
            if (ball == null || cachedIdentity == null)
                return;

            ball.ServerKickLooseBall(
                cachedIdentity,
                direction.normalized,
                force,
                looseBallRecontrolCooldown);
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
        private void ServerSendMoveSpeed(float speed01)
        {
            if (animationController != null)
            {
                animationController.ServerSetMoveSpeed(speed01);
            }
        }

        [Server]
        private void ServerSetAnimationGrounded(bool grounded)
        {
            if (animationController != null)
            {
                animationController.ServerSetGrounded(grounded);
            }
        }
    }
}
