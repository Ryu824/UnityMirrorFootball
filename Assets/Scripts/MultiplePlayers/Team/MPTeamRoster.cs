using Mirror;
using UnityEngine;

namespace MultiplePlayers
{
    public class MPTeamRoster : NetworkBehaviour
    {
        public static MPTeamRoster Instance { get; private set; }

        [Header("MVP Limits")]
        [SerializeField] private int maxHumanPlayersPerTeam = 5;
        [SerializeField] private bool allowSamePosition = true;

        private void Awake()
        {
            Instance = this;
        }

        [Server]
        public bool ServerCanSelect(
            MPPlayerTeamState requester,
            MPTeamId requestedTeam,
            MPPlayerPosition requestedPosition)
        {
            if (requester == null)
                return false;

            int teamCount = 0;

            MPPlayerTeamState[] allPlayers =
                FindObjectsByType<MPPlayerTeamState>(FindObjectsSortMode.None);

            foreach (MPPlayerTeamState player in allPlayers)
            {
                if (player == null || player == requester)
                    continue;

                if (player.TeamId == requestedTeam &&
                    player.ControlType == MPControlType.Human)
                {
                    teamCount++;
                }

                if (!allowSamePosition &&
                    player.TeamId == requestedTeam &&
                    player.Position == requestedPosition)
                {
                    return false;
                }
            }

            return teamCount < maxHumanPlayersPerTeam;
        }

        [Server]
        public bool ServerAllHumanPlayersHaveTeam()
        {
            MPPlayerTeamState[] allPlayers =
                FindObjectsByType<MPPlayerTeamState>(FindObjectsSortMode.None);

            foreach (MPPlayerTeamState player in allPlayers)
            {
                if (player.ControlType != MPControlType.Human)
                    continue;

                if (!player.HasValidSelection)
                    return false;
            }

            return true;
        }
    }
}