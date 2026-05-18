using UnityEngine;

#if USE_MIRROR
using Mirror;

namespace MultiplePlayers
{
    /// <summary>
    /// 挂在 Player Prefab 上。
    /// 只处理定位球 / 重新开球阶段的本地输入：方向 + 力度。
    /// 真正的发球执行仍然由服务器验证后完成。
    /// </summary>
    public class MPSetPieceController : NetworkBehaviour
    {
        [Header("Charge")]
        [SerializeField] private float maxChargeTime = 1.2f;

        [Header("Aim")]
        [SerializeField] private float aimYawSpeed = 180f;
        [SerializeField] private bool useCameraForwardWhenAvailable = true;

        private bool localCharging;
        private float localChargeStartTime;

        private bool serverCharging;
        private double serverChargeStartTime;

        private void Update()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            MPGameSession session = MPGameSession.Instance;

            if (session == null || !session.IsSetPiece || session.RestartExecutorNetId != netId)
            {
                localCharging = false;
                return;
            }

            // HandleAimInput();
            HandleChargeInput();
        }

        // private void HandleAimInput()
        // {
        //     float mouseX = Input.GetAxis("Mouse X");

        //     if (Mathf.Abs(mouseX) <= 0.001f)
        //     {
        //         return;
        //     }

        //     transform.Rotate(Vector3.up, mouseX * aimYawSpeed * Time.deltaTime, Space.World);
        // }

        private void HandleChargeInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                localCharging = true;
                localChargeStartTime = Time.time;
                CmdBeginSetPieceCharge();
            }

            if (Input.GetMouseButtonUp(0))
            {
                Vector3 aimDirection = GetAimDirection();
                localCharging = false;
                CmdReleaseSetPiece(aimDirection);
            }
        }

        private Vector3 GetAimDirection()
        {
            if (useCameraForwardWhenAvailable && Camera.main != null)
            {
                Vector3 cameraForward = Camera.main.transform.forward;
                cameraForward.y = 0f;

                if (cameraForward.sqrMagnitude > 0.01f)
                {
                    return cameraForward.normalized;
                }
            }

            Vector3 forward = transform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.01f)
            {
                return Vector3.forward;
            }

            return forward.normalized;
        }

        [Command]
        private void CmdBeginSetPieceCharge()
        {
            MPGameSession session = MPGameSession.Instance;

            if (session == null || !session.ServerCanPlayerUseSetPiece(netIdentity))
            {
                serverCharging = false;
                return;
            }

            serverCharging = true;
            serverChargeStartTime = NetworkTime.time;
        }

        [Command]
        private void CmdReleaseSetPiece(Vector3 aimDirection)
        {
            MPGameSession session = MPGameSession.Instance;

            if (session == null || !session.ServerCanPlayerUseSetPiece(netIdentity))
            {
                serverCharging = false;
                return;
            }

            if (!serverCharging)
            {
                return;
            }

            serverCharging = false;

            double chargeDuration = NetworkTime.time - serverChargeStartTime;
            float charge01 = Mathf.Clamp01((float)(chargeDuration / maxChargeTime));

            session.ServerExecuteSetPiece(netIdentity, aimDirection, charge01);
        }
    }
}
#else
namespace MultiplePlayers
{
    public class MPSetPieceController : MonoBehaviour
    {
        [TextArea(4, 8)]
        [SerializeField]
        private string setupHint =
            "Mirror is not installed yet. Add USE_MIRROR to enable MPSetPieceController.";
    }
}
#endif
