using UnityEngine;

#if USE_MIRROR
using Mirror;

namespace MultiplePlayers
{
    public class MPPlayerReadyState : NetworkBehaviour
    {
        [Header("Ready State")]
        [SyncVar]
        [SerializeField] private bool isReady;

        public bool IsReady => isReady;

        public override void OnStartServer()
        {
            base.OnStartServer();
            isReady = false;
        }

        [Command]
        public void CmdSetReady(bool ready)
        {
            MPPlayerTeamState teamState = GetComponent<MPPlayerTeamState>();

            if (teamState == null || !teamState.HasValidSelection)
            {
                Debug.Log("[Ready] Player cannot ready before selecting team and position.");
                return;
            }

            MPGameSession session = MPGameSession.Instance;

            if (session == null)
            {
                return;
            }

            if (!session.IsLobby)
            {
                return;
            }

            if (isReady == ready)
            {
                return;
            }

            isReady = ready;
            session.ServerNotifyPlayerListOrReadyChanged();
        }
    }
}
#else
namespace MultiplePlayers
{
    public class MPPlayerReadyState : MonoBehaviour
    {
        [TextArea(4, 8)]
        [SerializeField]
        private string setupHint =
            "Mirror is not installed yet. Add USE_MIRROR to enable MPPlayerReadyState.";
    }
}
#endif