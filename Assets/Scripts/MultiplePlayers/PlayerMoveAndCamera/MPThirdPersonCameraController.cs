using UnityEngine;
using Mirror;

namespace MultiplePlayers
{
    public class MPThirdPersonCameraController : NetworkBehaviour
    {
        [Header("Camera Target")]
        [SerializeField] private Transform cameraLookTarget;
        [SerializeField] private float targetHeight = 1.4f;

        [Header("Camera Distance")]
        [SerializeField] private float distance = 5f;
        [SerializeField] private float minPitch = -25f;
        [SerializeField] private float maxPitch = 65f;

        [Header("Mouse")]
        [SerializeField] private float mouseSensitivityX = 180f;
        [SerializeField] private float mouseSensitivityY = 120f;

        [Header("Smoothing")]
        [SerializeField] private float positionSmoothTime = 0.03f;

        private Camera mainCamera;
        private Vector3 cameraVelocity;

        private float yaw;
        private float pitch = 20f;

        private void Awake()
        {
            if (cameraLookTarget == null)
            {
                cameraLookTarget = transform;
            }
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            mainCamera = Camera.main;

            if (mainCamera == null)
            {
                Debug.LogWarning("[Camera] Camera.main is null. Please ensure your scene has a camera tagged MainCamera.");
                return;
            }

            yaw = transform.eulerAngles.y;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        private void Update()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            UpdateCursorState();

            if (!CanControlCamera())
            {
                return;
            }

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            yaw += mouseX * mouseSensitivityX * Time.deltaTime;
            pitch -= mouseY * mouseSensitivityY * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        private void LateUpdate()
        {
            if (!isLocalPlayer || mainCamera == null)
            {
                return;
            }

            UpdateCameraPosition();
        }

        private void UpdateCameraPosition()
        {
            Vector3 targetPosition = GetTargetPosition();

            Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredPosition = targetPosition + cameraRotation * new Vector3(0f, 0f, -distance);

            mainCamera.transform.position = Vector3.SmoothDamp(
                mainCamera.transform.position,
                desiredPosition,
                ref cameraVelocity,
                positionSmoothTime
            );

            mainCamera.transform.rotation = Quaternion.LookRotation(
                targetPosition - mainCamera.transform.position,
                Vector3.up
            );
        }

        private Vector3 GetTargetPosition()
        {
            if (cameraLookTarget == null)
            {
                return transform.position + Vector3.up * targetHeight;
            }

            return cameraLookTarget.position + Vector3.up * targetHeight;
        }

        public Vector3 GetMoveDirection(Vector2 input)
        {
            if (input.sqrMagnitude < 0.001f)
            {
                return Vector3.zero;
            }

            Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = right * input.x + forward * input.y;

            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }

            return moveDirection;
        }

        private bool CanControlCamera()
        {
            if (MPGameSession.Instance == null)
            {
                return true;
            }

            return MPGameSession.CanControlCamera(netIdentity.netId);
        }

        private void UpdateCursorState()
        {
            if (CanControlCamera())
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}