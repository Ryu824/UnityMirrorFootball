using UnityEngine;

#if USE_MIRROR
using Mirror;

namespace MultiplePlayers
{
    public enum MPBallState : byte
    {
        Free,
        Dribbling,
        Kicked
    }

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkIdentity))]
    public class MPNetworkBall : NetworkBehaviour
    {
        [Header("Basic Ball Settings")]
        [SerializeField] private float resetHeight = -10f;
        [SerializeField] private float maxSpeed = 25f;

        [Header("Kick State")]
        [SerializeField] private float kickedToFreeSpeedThreshold = 0.35f;

        [Header("Network State")]
        [SyncVar] public MPBallState state = MPBallState.Free;
        [SyncVar] public uint controllerNetId;
        [SyncVar] public double canBeControlledTime;

        [Header("Rule Tracking")]
        [SyncVar]
        [SerializeField] private MPTeam lastTouchTeam = MPTeam.None;

        [SyncVar]
        [SerializeField] private uint lastTouchPlayerNetId;

        public MPTeam LastTouchTeam => lastTouchTeam;
        public uint LastTouchPlayerNetId => lastTouchPlayerNetId;

        private Rigidbody rb;
        private RigidbodyConstraints originalConstraints;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private Collider ballCollider;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            ballCollider = GetComponent<Collider>();
            originalConstraints = rb.constraints;
        }

        private void Start()
        {
            ValidateNetworkSyncSetup();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            spawnPosition = transform.position;
            spawnRotation = transform.rotation;

            state = MPBallState.Free;
            controllerNetId = 0;
            canBeControlledTime = 0;
            ServerClearLastTouch();
        }

        [ServerCallback]
        private void FixedUpdate()
        {
            if (MPGameSession.Instance != null && MPGameSession.Instance.Phase == MPMatchState.SetPiece)
            {
                return;
            }

            if (!MPGameSession.IsMatchControllable)
            {
                ServerStopAndClearControl();
                return;
            }

            LimitSpeed();
            CheckResetHeight();
            CheckKickedBallCanBecomeFree();
        }

        [Server]
        private void LimitSpeed()
        {
            if (state == MPBallState.Dribbling)
                return;

            if (rb.velocity.magnitude > maxSpeed)
            {
                rb.velocity = rb.velocity.normalized * maxSpeed;
            }
        }

        [Server]
        private void CheckResetHeight()
        {
            if (transform.position.y < resetHeight)
            {
                ResetToSpawn();
            }
        }

        [Server]
        private void CheckKickedBallCanBecomeFree()
        {
            if (state != MPBallState.Kicked)
                return;

            if (NetworkTime.time < canBeControlledTime)
                return;

            if (rb.velocity.magnitude <= kickedToFreeSpeedThreshold)
            {
                state = MPBallState.Free;
            }
        }

        [Server]
        public bool CanBeControlledBy(NetworkIdentity playerIdentity)
        {
            if (!MPGameSession.IsMatchControllable)
                return false;

            if (playerIdentity == null)
                return false;

            if (NetworkTime.time < canBeControlledTime)
                return false;

            if (state == MPBallState.Dribbling && controllerNetId != playerIdentity.netId)
                return false;

            return true;
        }

        [Server]
        public bool TryStartDribble(NetworkIdentity playerIdentity)
        {
            if (!MPGameSession.IsMatchControllable)
                return false;

            if (!CanBeControlledBy(playerIdentity))
                return false;

            ServerRegisterTouch(playerIdentity);

            state = MPBallState.Dribbling;
            controllerNetId = playerIdentity.netId;

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = originalConstraints | RigidbodyConstraints.FreezeRotation;

            if (ballCollider != null)
                ballCollider.isTrigger = true;

            return true;
        }

        [Server]
        public void StopDribble(NetworkIdentity playerIdentity)
        {
            if (playerIdentity == null)
                return;

            if (controllerNetId != playerIdentity.netId)
                return;

            state = MPBallState.Free;
            controllerNetId = 0;

            rb.constraints = originalConstraints;

            if (ballCollider != null)
                ballCollider.isTrigger = false;
        }

        [Server]
        public void ServerLockDribblePosition(Vector3 targetPosition)
        {
            if (!MPGameSession.IsMatchControllable)
                return;

            if (state != MPBallState.Dribbling)
                return;

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.position = targetPosition;

            Physics.SyncTransforms();
        }

        [Server]
        public void ServerKick(
            NetworkIdentity kicker,
            Vector3 direction,
            float force,
            float reControlCooldown)
        {
            if (MPGameSession.Instance != null && !MPGameSession.IsMatchControllable)
                return;

            if (kicker == null)
                return;

            if (state != MPBallState.Dribbling)
                return;

            if (controllerNetId != kicker.netId)
                return;

            ServerRegisterTouch(kicker);

            if (direction.sqrMagnitude < 0.01f)
                direction = kicker.transform.forward;

            direction.Normalize();

            state = MPBallState.Kicked;
            controllerNetId = 0;
            canBeControlledTime = NetworkTime.time + reControlCooldown;

            rb.constraints = originalConstraints;

            if (ballCollider != null)
                ballCollider.isTrigger = false;

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.AddForce(direction * force, ForceMode.VelocityChange);
        }

        [Server]
        public void ServerStopAndClearControl(float controlCooldown = 0.2f)
        {
            state = MPBallState.Free;
            controllerNetId = 0;
            canBeControlledTime = NetworkTime.time + controlCooldown;

            rb.constraints = originalConstraints;

            if (ballCollider != null)
            {
                ballCollider.isTrigger = false;
            }

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Physics.SyncTransforms();
        }

        [Server]
        public void ServerPlaceForRestart(Vector3 position, bool freezeBall)
        {
            state = MPBallState.Free;
            controllerNetId = 0;
            canBeControlledTime = NetworkTime.time + 0.2f;

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.constraints = freezeBall
                ? originalConstraints | RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation
                : originalConstraints;

            if (ballCollider != null)
            {
                ballCollider.isTrigger = false;
            }

            rb.position = position;
            transform.position = position;

            Physics.SyncTransforms();
        }

        [Server]
        public void ServerKickRestart(
            NetworkIdentity executor,
            Vector3 direction,
            float force,
            float reControlCooldown)
        {
            if (executor == null)
            {
                return;
            }

            if (direction.sqrMagnitude < 0.01f)
            {
                direction = executor.transform.forward;
            }

            direction.Normalize();

            ServerRegisterTouch(executor);

            state = MPBallState.Kicked;
            controllerNetId = 0;
            canBeControlledTime = NetworkTime.time + reControlCooldown;

            rb.constraints = originalConstraints;

            if (ballCollider != null)
            {
                ballCollider.isTrigger = false;
            }

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(direction * force, ForceMode.VelocityChange);
        }

        [Server]
        public void ResetToSpawn()
        {
            state = MPBallState.Free;
            controllerNetId = 0;
            canBeControlledTime = NetworkTime.time + 0.2f;

            rb.constraints = originalConstraints;

            if (ballCollider != null)
            {
                ballCollider.isTrigger = false;
            }

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.position = spawnPosition;
            rb.rotation = spawnRotation;
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);

            ServerClearLastTouch();
            Physics.SyncTransforms();
        }

        [Server]
        public void ServerRegisterTouch(NetworkIdentity playerIdentity)
        {
            if (playerIdentity == null)
            {
                return;
            }

            MPNetworkPlayerController player =
                playerIdentity.GetComponent<MPNetworkPlayerController>();

            if (player == null || player.Team == MPTeam.None)
            {
                return;
            }

            lastTouchTeam = player.Team;
            lastTouchPlayerNetId = playerIdentity.netId;
        }

        [Server]
        public MPTeam ServerGetEffectiveLastTouchTeam()
        {
            if (state == MPBallState.Dribbling && controllerNetId != 0)
            {
                if (NetworkServer.spawned.TryGetValue(
                        controllerNetId,
                        out NetworkIdentity controllerIdentity))
                {
                    MPNetworkPlayerController controller =
                        controllerIdentity.GetComponent<MPNetworkPlayerController>();

                    if (controller != null && controller.Team != MPTeam.None)
                    {
                        return controller.Team;
                    }
                }
            }

            return lastTouchTeam;
        }

        [Server]
        public void ServerClearLastTouch()
        {
            lastTouchTeam = MPTeam.None;
            lastTouchPlayerNetId = 0;
        }

        private void ValidateNetworkSyncSetup()
        {
            if (TryGetComponent(out NetworkRigidbodyReliable networkRigidbody))
            {
                if (networkRigidbody.syncDirection != SyncDirection.ServerToClient)
                {
                    Debug.LogWarning(
                        $"{name} should use NetworkRigidbodyReliable with SyncDirection = ServerToClient so the server can drive ball physics.",
                        this);
                }

                return;
            }

            Debug.LogWarning(
                $"{name} has no NetworkRigidbodyReliable. Ball collisions will happen on the server, but clients will not receive proper rigidbody sync.",
                this);
        }
    }
}
#else
namespace MultiplePlayers
{
    public class MPNetworkBall : MonoBehaviour
    {
        [TextArea(4, 8)]
        [SerializeField]
        private string setupHint =
            "Mirror is not installed yet.\n" +
            "After installing Mirror, add USE_MIRROR to enable networked ball spawning and syncing.";
    }
}
#endif
