using Mirror;
using UnityEngine;

namespace MultiplePlayers
{
    [RequireComponent(typeof(Collider))]
    public class MPBoundaryReporter : MonoBehaviour
    {
        [Header("Boundary")]
        [SerializeField] private MPBoundaryType boundaryType = MPBoundaryType.None;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void Awake()
        {
            Collider col = GetComponent<Collider>();

            if (!col.isTrigger)
            {
                Debug.LogWarning(
                    $"[MPBoundaryReporter] {name} collider is not Trigger. It should be Trigger.",
                    this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!NetworkServer.active)
            {
                return;
            }

            MPNetworkBall ball = other.GetComponentInParent<MPNetworkBall>();

            if (ball == null && other.attachedRigidbody != null)
            {
                ball = other.attachedRigidbody.GetComponent<MPNetworkBall>();
            }

            if (ball == null)
            {
                return;
            }

            MPGameSession session = MPGameSession.Instance;

            if (session == null)
            {
                Debug.LogWarning("[MPBoundaryReporter] MPGameSession.Instance is null.", this);
                return;
            }

            Vector3 boundaryPoint = ball.transform.position;

            if (debugLog)
            {
                Debug.Log(
                    $"[MPBoundaryReporter] Ball entered {boundaryType}, point={boundaryPoint}, lastTouch={ball.LastTouchTeam}",
                    this);
            }

            session.ServerReportBoundary(ball, boundaryType, boundaryPoint);
        }
    }
}
