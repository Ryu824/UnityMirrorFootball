using UnityEngine;

#if USE_MIRROR
using Mirror;

namespace MultiplePlayers
{
    public class MPPlayerBallController : NetworkBehaviour
    {
        [Header("Detect Ball")]
        [SerializeField] private LayerMask ballLayer;
        [SerializeField] private float detectCenterHeight = -0.75f;
        [SerializeField] private float detectForwardOffset = 1.0f;
        [SerializeField] private Vector3 detectBoxHalfExtents = new Vector3(0.35f, 0.25f, 0.8f);
        [SerializeField] private float maxControlDistance = 2.0f;
        [SerializeField] private float minForwardDot = 0.7f;

        [Header("Locked Dribble Position")]
        [SerializeField] private float dribbleCenterHeight = -0.75f;
        [SerializeField] private float dribbleForwardOffset = 1.15f;
        [SerializeField] private float dribbleSideOffset = 0f;
        [SerializeField] private float dribbleBallLift = 0.12f;

        [Tooltip("球跟随玩家前方位置的最大速度。数值越小，越不会绕人瞬移，但跟脚感会弱一点。")]
        [SerializeField] private float dribbleFollowMaxSpeed = 10f;

        [Tooltip("玩家转身太快时，球和目标点距离超过这个值就直接解除控球，避免绕圈。")]
        [SerializeField] private float releaseControlIfBallTooFar = 2.2f;

        [Header("Ground Kick - Left Mouse")]
        [SerializeField] private float minKickForce = 9f;
        [SerializeField] private float maxKickForce = 26f;

        [Header("Lob Kick - Right Mouse")]
        [SerializeField] private float minLobKickForce = 12f;
        [SerializeField] private float maxLobKickForce = 32f;
        [SerializeField] private float lobUpRatio = 0.45f;

        [Header("Kick Charge")]
        [SerializeField] private float maxChargeTime = 1.2f;
        [SerializeField] private float reControlCooldown = 0.45f;

        [Header("Debug")]
        [SerializeField] private bool drawDebug = true;

        [SyncVar] private NetworkIdentity controlledBallIdentity;

        private double serverChargeStartTime;
        private bool serverIsCharging;
        private bool serverChargingLobKick;

        private MPNetworkBall ControlledBall
        {
            get
            {
                if (controlledBallIdentity == null)
                    return null;

                return controlledBallIdentity.GetComponent<MPNetworkBall>();
            }
        }

        private void Update()
        {
            if (!isLocalPlayer)
                return;

            if (!CanControlMatch())
                return;

            HandleKickInput();
        }

        private bool CanControlMatch()
        {
            return MPGameSession.Instance == null || MPGameSession.IsMatchControllable;
        }

        private void HandleKickInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                CmdBeginKickCharge(false);
            }

            if (Input.GetMouseButtonUp(0))
            {
                Vector3 aimDirection = transform.forward;
                CmdReleaseKick(aimDirection);
            }

            if (Input.GetMouseButtonDown(1))
            {
                CmdBeginKickCharge(true);
            }

            if (Input.GetMouseButtonUp(1))
            {
                Vector3 aimDirection = transform.forward + Vector3.up * lobUpRatio;
                CmdReleaseKick(aimDirection);
            }
        }

        [Command]
        private void CmdBeginKickCharge(bool isLobKick)
        {
            if (!CanControlMatch())
            {
                serverIsCharging = false;
                return;
            }

            if (ControlledBall == null)
                return;

            serverIsCharging = true;
            serverChargingLobKick = isLobKick;
            serverChargeStartTime = NetworkTime.time;
        }

        [Command]
        private void CmdReleaseKick(Vector3 aimDirection)
        {
            if (!CanControlMatch())
            {
                serverIsCharging = false;
                serverChargingLobKick = false;
                controlledBallIdentity = null;
                return;
            }

            MPNetworkBall ball = ControlledBall;

            if (ball == null)
                return;

            if (!serverIsCharging)
                return;

            serverIsCharging = false;

            double chargeDuration = NetworkTime.time - serverChargeStartTime;
            float charge01 = Mathf.Clamp01((float)(chargeDuration / maxChargeTime));

            float force = serverChargingLobKick
                ? Mathf.Lerp(minLobKickForce, maxLobKickForce, charge01)
                : Mathf.Lerp(minKickForce, maxKickForce, charge01);

            serverChargingLobKick = false;

            ServerRegisterLastTouch(ball);
            ball.ServerKick(netIdentity, aimDirection, force, reControlCooldown);

            controlledBallIdentity = null;
        }

        [ServerCallback]
        private void FixedUpdate()
        {
            if (!CanControlMatch())
            {
                ServerReleaseControlledBall();
                return;
            }

            MPNetworkBall ball = ControlledBall;

            if (ball == null)
            {
                TryFindAndControlBall();
            }
            else
            {
                UpdateLockedDribbling(ball);
            }
        }

        [Server]
        private void ServerReleaseControlledBall()
        {
            MPNetworkBall ball = ControlledBall;

            if (ball != null)
            {
                ball.ServerStopAndClearControl();
            }

            controlledBallIdentity = null;
            serverIsCharging = false;
            serverChargingLobKick = false;
        }

        [Server]
        private void TryFindAndControlBall()
        {
            if (!CanControlMatch())
            {
                return;
            }

            Vector3 center =
                transform.position
                + transform.forward * detectForwardOffset
                + Vector3.up * detectCenterHeight;

            Collider[] hits = Physics.OverlapBox(
                center,
                detectBoxHalfExtents,
                transform.rotation,
                ballLayer,
                QueryTriggerInteraction.Ignore
            );

            MPNetworkBall bestBall = null;
            float bestDistance = float.MaxValue;

            foreach (Collider hit in hits)
            {
                MPNetworkBall ball = hit.GetComponentInParent<MPNetworkBall>();

                if (ball == null)
                    continue;

                if (!ball.CanBeControlledBy(netIdentity))
                    continue;

                Vector3 toBall = ball.transform.position - transform.position;
                toBall.y = 0f;

                float distance = toBall.magnitude;

                if (distance > maxControlDistance)
                    continue;

                if (distance <= 0.01f)
                    continue;

                float forwardDot = Vector3.Dot(transform.forward, toBall.normalized);

                if (forwardDot < minForwardDot)
                    continue;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestBall = ball;
                }
            }

            if (bestBall == null)
                return;

            if (!bestBall.TryStartDribble(netIdentity))
                return;

            ServerRegisterLastTouch(bestBall);
            controlledBallIdentity = bestBall.netIdentity;
        }

        [Server]
        private void UpdateLockedDribbling(MPNetworkBall ball)
        {
            if (!CanControlMatch())
            {
                controlledBallIdentity = null;
                serverIsCharging = false;
                serverChargingLobKick = false;
                return;
            }

            if (ball.state != MPBallState.Dribbling)
            {
                controlledBallIdentity = null;
                return;
            }

            Vector3 targetPosition = GetDribbleTargetPosition();

            float distanceToTarget = Vector3.Distance(ball.transform.position, targetPosition);

            if (distanceToTarget > releaseControlIfBallTooFar)
            {
                ball.StopDribble(netIdentity);
                controlledBallIdentity = null;
                serverIsCharging = false;
                serverChargingLobKick = false;
                return;
            }

            Vector3 nextPosition = Vector3.MoveTowards(
                ball.transform.position,
                targetPosition,
                dribbleFollowMaxSpeed * Time.fixedDeltaTime
            );

            ball.ServerLockDribblePosition(nextPosition);
        }

        [Server]
        private void ServerRegisterLastTouch(MPNetworkBall ball)
        {
            if (ball == null)
            {
                return;
            }

            ball.ServerRegisterTouch(netIdentity);
        }

        private Vector3 GetDribbleTargetPosition()
        {
            Vector3 right = transform.right;

            return transform.position
                   + transform.forward * dribbleForwardOffset
                   + right * dribbleSideOffset
                   + Vector3.up * (dribbleCenterHeight + dribbleBallLift);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebug)
                return;

            Gizmos.color = Color.cyan;

            Vector3 center =
                transform.position
                + transform.forward * detectForwardOffset
                + Vector3.up * detectCenterHeight;

            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, detectBoxHalfExtents * 2f);
            Gizmos.matrix = oldMatrix;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(GetDribbleTargetPosition(), 0.15f);
        }
    }
}
#else
namespace MultiplePlayers
{
    public class MPPlayerBallController : MonoBehaviour
    {
        [TextArea(4, 8)]
        [SerializeField]
        private string setupHint =
            "Mirror is not installed yet. Add USE_MIRROR to enable MPPlayerBallController.";
    }
}
#endif
