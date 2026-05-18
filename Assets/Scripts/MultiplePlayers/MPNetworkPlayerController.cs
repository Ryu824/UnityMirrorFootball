using UnityEngine;
using Mirror;

namespace MultiplePlayers
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkIdentity))]
    public class MPNetworkPlayerController : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 7f;

        [Tooltip("人物朝移动方向转身的最大角速度，单位：度/秒。")]
        [SerializeField] private float turnSpeedDegrees = 540f;

        [Header("Ground Check")]
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float groundCheckRadius = 0.25f;
        [SerializeField] private float groundCheckDistance = 0.2f;
        [SerializeField] private float groundCheckOriginOffset = 0.15f;

        [Header("Jump")]
        [SerializeField] private float jumpSpeed = 7f;
        [SerializeField] private bool isGround = true;

        [Tooltip("起跳后短时间内忽略地面碰撞，避免刚起跳就被 OnCollisionStay 判定为落地。")]
        [SerializeField] private float groundIgnoreTimeAfterJump = 0.15f;

        [Tooltip("接触点法线 Y 大于这个值时，认为玩家站在地面上。")]
        [SerializeField] private float groundNormalThreshold = 0.5f;

        [Header("Debug")]
        [SyncVar(hook = nameof(OnDisplayNameChanged))]
        [SerializeField] private string displayName = "Player";

        [SerializeField] private bool debugJump = false;

        [Header("Team")]
        [SyncVar]
        [SerializeField] private MPTeam team = MPTeam.None;
        public MPTeam Team => team;

        private Rigidbody rb;
        private MPThirdPersonCameraController cameraController;
        private MPPlayerAnimationController animationController;

        private Vector3 moveDir;
        private bool jumpRequested;

        private Quaternion desiredFacingRotation;
        private float lastJumpTime = -999f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            cameraController = GetComponent<MPThirdPersonCameraController>();
            animationController = GetComponent<MPPlayerAnimationController>();

            desiredFacingRotation = transform.rotation;

            // 防止胶囊体因为撞球、撞其他玩家而发生物理翻滚。
            // 保留 Y 轴旋转，让代码仍然可以控制人物转向。
            rb.constraints =
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!isLocalPlayer)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            rb.isKinematic = false;
            rb.useGravity = true;

            desiredFacingRotation = transform.rotation;

            if (animationController != null)
            {
                animationController.SetGrounded(isGround);
            }

            gameObject.name = $"{displayName} (Local)";
        }

        private void Update()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            if (!CanControlMatch())
            {
                moveDir = Vector3.zero;
                jumpRequested = false;
                return;
            }

            ReadMovementInput();
            ReadJumpInput();
        }

        private void FixedUpdate()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            if (!CanControlMatch())
            {
                FreezeLocalMovement();
                return;
            }

            UpdateGroundedBySphereCast();

            ApplyLocalMovement();
            ApplyLocalJump();
        }

        private void ReadMovementInput()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector2 input = new Vector2(horizontal, vertical);

            if (input.sqrMagnitude < 0.001f)
            {
                moveDir = Vector3.zero;
                return;
            }

            // 移动方向相对摄像机。
            // 摄像机只提供方向，不直接控制人物旋转。
            if (cameraController != null)
            {
                moveDir = cameraController.GetMoveDirection(input);
            }
            else
            {
                moveDir = new Vector3(horizontal, 0f, vertical).normalized;
            }
        }

        private void ReadJumpInput()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Debug.Log($"[JumpDebug] Space pressed. isLocalPlayer={isLocalPlayer}, CanControlMatch={CanControlMatch()}, isGround={isGround}");
                jumpRequested = true;
            }
        }

        private bool CheckGrounded()
        {
            Vector3 origin = transform.position + Vector3.up * groundCheckOriginOffset;

            bool hitGround = Physics.SphereCast(
                origin,
                groundCheckRadius,
                Vector3.down,
                out RaycastHit hit,
                groundCheckDistance,
                groundMask,
                QueryTriggerInteraction.Ignore
            );

            if (debugJump)
            {
                Debug.Log(hitGround
                    ? $"[GroundCheck] grounded: {hit.collider.name}"
                    : "[GroundCheck] not grounded");
            }

            return hitGround;
        }

        private bool CanControlMatch()
        {
            return MPGameSession.Instance == null || MPGameSession.IsMatchControllable;
        }

        private void ApplyLocalMovement()
        {
#if UNITY_6000_0_OR_NEWER
            Vector3 velocity = rb.linearVelocity;
#else
            Vector3 velocity = rb.velocity;
#endif

            velocity.x = moveDir.x * moveSpeed;
            velocity.z = moveDir.z * moveSpeed;

#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = velocity;
#else
            rb.velocity = velocity;
#endif

            ApplyPlayerRotation();
        }

        private void ApplyPlayerRotation()
        {
            if (moveDir.sqrMagnitude > 0.001f)
            {
                desiredFacingRotation = Quaternion.LookRotation(moveDir, Vector3.up);
            }

            // 清除碰撞带来的角速度，避免胶囊体撞球或撞玩家后自转。
            rb.angularVelocity = Vector3.zero;

            Quaternion newRotation = Quaternion.RotateTowards(
                rb.rotation,
                desiredFacingRotation,
                turnSpeedDegrees * Time.fixedDeltaTime
            );

            rb.MoveRotation(newRotation);
        }

        private void ApplyLocalJump()
        {
            if (!jumpRequested)
                return;

            jumpRequested = false;

            if (!isGround)
            {
                if (debugJump)
                    Debug.Log("[Jump] rejected: not grounded.");

                return;
            }

            isGround = false;

            if (animationController != null)
            {
                animationController.SetGrounded(false);
                animationController.PlayJump();
            }

#if UNITY_6000_0_OR_NEWER
    Vector3 velocity = rb.linearVelocity;
#else
            Vector3 velocity = rb.velocity;
#endif

            velocity.y = jumpSpeed;

#if UNITY_6000_0_OR_NEWER
    rb.linearVelocity = velocity;
#else
            rb.velocity = velocity;
#endif

            if (debugJump)
                Debug.Log("[Jump] executed.");
        }

        private void FreezeLocalMovement()
        {
            moveDir = Vector3.zero;
            jumpRequested = false;

#if UNITY_6000_0_OR_NEWER
            Vector3 velocity = rb.linearVelocity;
#else
            Vector3 velocity = rb.velocity;
#endif

            velocity.x = 0f;
            velocity.z = 0f;

#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = velocity;
#else
            rb.velocity = velocity;
#endif

            rb.angularVelocity = Vector3.zero;
        }

        // private void OnCollisionEnter(Collision collision)
        // {
        //     UpdateGroundState(collision);
        // }

        // private void OnCollisionStay(Collision collision)
        // {
        //     UpdateGroundState(collision);
        // }
        private void UpdateGroundedBySphereCast()
        {
            bool groundedNow = CheckGrounded();

            if (groundedNow != isGround)
            {
                isGround = groundedNow;

                if (animationController != null)
                {
                    animationController.SetGrounded(isGround);
                }

                if (debugJump)
                {
                    Debug.Log($"[Ground] IsGrounded changed: {isGround}");
                }
            }
        }
        private void UpdateGroundState(Collision collision)
        {
            if (!isLocalPlayer)
            {
                return;
            }

            // 起跳后一小段时间内不接收地面判定，
            // 否则 OnCollisionStay 可能会立刻把 IsGrounded 改回 true。
            if (Time.time - lastJumpTime < groundIgnoreTimeAfterJump)
            {
                return;
            }

            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y > groundNormalThreshold)
                {
                    if (!isGround)
                    {
                        isGround = true;

                        if (animationController != null)
                        {
                            animationController.SetGrounded(true);
                        }

                        if (debugJump)
                        {
                            Debug.Log($"[Ground] grounded by {collision.collider.name}");
                        }
                    }

                    return;
                }
            }
        }

        private void OnDisplayNameChanged(string oldValue, string newValue)
        {
            gameObject.name = isLocalPlayer ? $"{newValue} (Local)" : newValue;
        }

        [Server]
        public void ServerTeleportForRestart(Vector3 position, Quaternion rotation)
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody>();
            }

#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;

            rb.position = position;
            rb.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);

            TargetTeleportForRestart(connectionToClient, position, rotation);
            RpcTeleportForRestartObservers(position, rotation);
        }

        [TargetRpc]
        private void TargetTeleportForRestart(
            NetworkConnectionToClient target,
            Vector3 position,
            Quaternion rotation)
        {
            ApplyTeleportForRestart(position, rotation);
        }

        [ClientRpc(includeOwner = false)]
        private void RpcTeleportForRestartObservers(Vector3 position, Quaternion rotation)
        {
            ApplyTeleportForRestart(position, rotation);
        }

        private void ApplyTeleportForRestart(Vector3 position, Quaternion rotation)
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody>();
            }

#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;

            rb.position = position;
            rb.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);

            moveDir = Vector3.zero;
            jumpRequested = false;
            isGround = true;
            desiredFacingRotation = rotation;

            if (animationController != null)
            {
                animationController.SetGrounded(true);
            }
        }

        [Server]
        public void ServerSetDisplayName(string newDisplayName)
        {
            displayName = newDisplayName;
        }

        [Server]
        public void ServerSetTeam(MPTeam newTeam)
        {
            team = newTeam;
        }
    }
}