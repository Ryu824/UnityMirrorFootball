using Mirror;
using UnityEngine;

namespace MultiplePlayers
{
    public class MPPlayerTeamState : NetworkBehaviour
    {
        [Header("Team State")]
        [SyncVar] public MPTeamId TeamId = MPTeamId.None;
        [SyncVar] public MPPlayerPosition Position = MPPlayerPosition.None;
        [SyncVar] public MPControlType ControlType = MPControlType.Human;

        public bool HasValidSelection =>
            TeamId != MPTeamId.None && Position != MPPlayerPosition.None;
        public MPTeam MatchTeam => ToMatchTeam(TeamId);

        [Command]
        public void CmdRequestSelectTeam(MPTeamId team, MPPlayerPosition position)
        {
            if (!CanChangeSelection())
                return;

            if (team == MPTeamId.None || position == MPPlayerPosition.None)
                return;

            MPTeamRoster roster = MPTeamRoster.Instance;
            if (roster == null)
            {
                Debug.LogWarning("[TeamState] No MPTeamRoster found.");
                return;
            }

            if (!roster.ServerCanSelect(this, team, position))
            {
                Debug.Log("[TeamState] Selection rejected by server.");
                return;
            }

            TeamId = team;
            Position = position;
            ServerApplyMatchTeam();

            Debug.Log($"[TeamState] {netId} selected {TeamId} / {Position}");
        }

        [Server]
        public void ServerApplyMatchTeam()
        {
            MPNetworkPlayerController playerController =
                GetComponent<MPNetworkPlayerController>();

            if (playerController != null)
            {
                playerController.ServerSetTeam(ToMatchTeam(TeamId));
            }
        }

        public static MPTeam ToMatchTeam(MPTeamId teamId)
        {
            switch (teamId)
            {
                case MPTeamId.Red:
                    return MPTeam.RedLeft;

                case MPTeamId.Blue:
                    return MPTeam.BlueRight;

                default:
                    return MPTeam.None;
            }
        }

        [Server]
        private bool CanChangeSelection()
        {
            MPGameSession session = MPGameSession.Instance;
            if (session == null)
                return true;

            // 只允许 Lobby 阶段选队
            return session.IsLobby;
        }
    }
}
