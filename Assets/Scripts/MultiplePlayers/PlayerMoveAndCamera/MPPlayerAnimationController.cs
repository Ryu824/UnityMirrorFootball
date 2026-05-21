using Mirror;
using UnityEngine;

namespace MultiplePlayers
{
    public class MPPlayerAnimationController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;

        [Header("Movement Animation")]
        [SerializeField] private float runSpeedReference = 5f;
        [SerializeField] private float speedDampTime = 0.08f;

        [Header("Network Send")]
        [SerializeField] private float sendInterval = 0.06f;
        [SerializeField] private float minSpeedChangeToSend = 0.02f;

        [Header("Debug")]
        [SerializeField] private bool debugAnimation = false;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int JumpHash = Animator.StringToHash("Jump");

        [SyncVar(hook = nameof(OnSyncedSpeedChanged))]
        private float syncedSpeed;

        [SyncVar(hook = nameof(OnSyncedGroundedChanged))]
        private bool syncedGrounded = true;

        private Vector3 lastPosition;

        private float localGrounded01 = 1f;
        private bool localGrounded = true;

        private float remoteTargetSpeed;
        private bool remoteTargetGrounded = true;

        private float nextSendTime;
        private float lastSentSpeed = -1f;
        private bool lastSentGrounded = true;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            lastPosition = transform.position;
        }

        private void Update()
        {
            if (animator == null)
                return;

            if (isLocalPlayer)
            {
                UpdateLocalAnimation();
            }
            else
            {
                UpdateRemoteAnimation();
            }

            lastPosition = transform.position;
        }

        private void UpdateLocalAnimation()
        {
            float speed = CalculateSpeed01();

            animator.SetFloat(SpeedHash, speed, speedDampTime, Time.deltaTime);
            animator.SetBool(IsGroundedHash, localGrounded);

            bool shouldSend =
                Time.time >= nextSendTime &&
                (Mathf.Abs(speed - lastSentSpeed) >= minSpeedChangeToSend ||
                 localGrounded != lastSentGrounded);

            if (shouldSend)
            {
                nextSendTime = Time.time + sendInterval;
                lastSentSpeed = speed;
                lastSentGrounded = localGrounded;

                CmdSetAnimationState(speed, localGrounded);
            }

            if (debugAnimation)
            {
                Debug.Log($"[Anim Local] speed={speed:F2}, grounded={localGrounded}");
            }
        }

        private void UpdateRemoteAnimation()
        {
            // 远端每帧持续平滑到目标值，而不是只在 SyncVar hook 中写一次。
            animator.SetFloat(SpeedHash, remoteTargetSpeed, speedDampTime, Time.deltaTime);
            animator.SetBool(IsGroundedHash, remoteTargetGrounded);

            if (debugAnimation)
            {
                Debug.Log($"[Anim Remote] targetSpeed={remoteTargetSpeed:F2}, grounded={remoteTargetGrounded}");
            }
        }

        private float CalculateSpeed01()
        {
            Vector3 delta = transform.position - lastPosition;
            delta.y = 0f;

            float worldSpeed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            return Mathf.Clamp01(worldSpeed / runSpeedReference);
        }

        public void SetGrounded(bool grounded)
        {
            localGrounded = grounded;
            localGrounded01 = grounded ? 1f : 0f;

            if (isLocalPlayer && animator != null)
            {
                animator.SetBool(IsGroundedHash, grounded);
                CmdSetAnimationState(lastSentSpeed < 0f ? 0f : lastSentSpeed, grounded);
            }

            if (debugAnimation)
            {
                Debug.Log($"[Anim] SetGrounded={grounded}");
            }
        }

        public void PlayJump()
        {
            if (!isLocalPlayer || animator == null)
                return;

            animator.SetTrigger(JumpHash);
            CmdPlayJumpAnimation();

            if (debugAnimation)
            {
                Debug.Log("[Anim Local] Jump trigger sent.");
            }
        }

        [Command]
        private void CmdSetAnimationState(float speed, bool grounded)
        {
            speed = Mathf.Clamp01(speed);

            syncedSpeed = speed;
            syncedGrounded = grounded;

            // Host 作为服务器时，SyncVar hook 不应作为唯一表现更新来源。
            // 这里手动更新服务器/Host 上看到的远端玩家动画目标。
            if (!isLocalPlayer)
            {
                remoteTargetSpeed = speed;
                remoteTargetGrounded = grounded;
            }
        }

        [Command]
        private void CmdPlayJumpAnimation()
        {
            RpcPlayJumpAnimation();

            // Host 作为服务器时，手动给 Host 视角播放远端 Client 的跳跃动画。
            if (!isLocalPlayer && animator != null)
            {
                animator.SetTrigger(JumpHash);
            }
        }

        [ClientRpc(includeOwner = false)]
        private void RpcPlayJumpAnimation()
        {
            if (animator == null)
                return;

            animator.SetTrigger(JumpHash);

            if (debugAnimation)
            {
                Debug.Log("[Anim Remote] Jump trigger received.");
            }
        }

        private void OnSyncedSpeedChanged(float oldValue, float newValue)
        {
            if (isLocalPlayer)
                return;

            remoteTargetSpeed = newValue;
        }

        private void OnSyncedGroundedChanged(bool oldValue, bool newValue)
        {
            if (isLocalPlayer)
                return;

            remoteTargetGrounded = newValue;
        }

        [Server]
        public void ServerSetMoveSpeed(float speed01)
        {
            float clamped = Mathf.Clamp01(speed01);
            syncedSpeed = clamped;

            if (!isLocalPlayer)
            {
                remoteTargetSpeed = clamped;
            }
        }

        [Server]
        public void ServerSetGrounded(bool grounded)
        {
            syncedGrounded = grounded;

            if (!isLocalPlayer)
            {
                remoteTargetGrounded = grounded;
            }
        }
    }
}
