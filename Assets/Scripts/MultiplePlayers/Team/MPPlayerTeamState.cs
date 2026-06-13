using Mirror;
using System;
using UnityEngine;

namespace MultiplePlayers
{
    public class MPPlayerTeamState : NetworkBehaviour
    {
        [Header("Team State")]
        [SyncVar] public MPTeamId TeamId = MPTeamId.None;
        [SyncVar] public MPPlayerPosition Position = MPPlayerPosition.None;
        [SyncVar] public MPControlType ControlType = MPControlType.Human;

        public event Action<bool, string> SelectionRequestResolved;

        public bool LastSelectionAccepted { get; private set; }
        public string LastSelectionMessage { get; private set; } = string.Empty;

        public bool HasValidSelection =>
            TeamId != MPTeamId.None && Position != MPPlayerPosition.None;
        public MPTeam MatchTeam => ToMatchTeam(TeamId);

        [Command]
        public void CmdRequestSelectTeam(MPTeamId team, MPPlayerPosition position)
        {
            if (!CanChangeSelection())
            {
                TargetNotifySelectionResult(
                    connectionToClient,
                    false,
                    "Team selection is only available in Lobby.");
                return;
            }

            if (team == MPTeamId.None || position == MPPlayerPosition.None)
            {
                TargetNotifySelectionResult(
                    connectionToClient,
                    false,
                    "Please choose both a team and a position.");
                return;
            }

            MPTeamRoster roster = MPTeamRoster.Instance;
            if (roster == null)
            {
                Debug.LogWarning("[TeamState] No MPTeamRoster found.");
                TargetNotifySelectionResult(
                    connectionToClient,
                    false,
                    "Team roster is not available on server.");
                return;
            }

            if (!roster.ServerCanSelect(this, team, position))
            {
                Debug.Log("[TeamState] Selection rejected by server.");
                TargetNotifySelectionResult(
                    connectionToClient,
                    false,
                    $"Server rejected {team} / {position}.");
                return;
            }

            TeamId = team;
            Position = position;
            ServerApplyMatchTeam();
            TargetNotifySelectionResult(
                connectionToClient,
                true,
                $"Selected {TeamId} / {Position}.");

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

        [TargetRpc]
        private void TargetNotifySelectionResult(
            NetworkConnectionToClient target,
            bool success,
            string message)
        {
            LastSelectionAccepted = success;
            LastSelectionMessage = message ?? string.Empty;
            SelectionRequestResolved?.Invoke(success, LastSelectionMessage);
        }
    }
}
