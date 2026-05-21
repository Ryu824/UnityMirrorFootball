using UnityEngine;

namespace MultiplePlayers
{
    public class MPTeamUtility : MonoBehaviour
    {
        public static MPTeamUtility Instance { get; private set; }

        [Header("Goal Positions")]
        [SerializeField] private Vector3 redGoalPosition = new Vector3(-24f, 0f, 0f);
        [SerializeField] private Vector3 blueGoalPosition = new Vector3(24f, 0f, 0f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static Vector3 GetAttackDirection(MPTeamId team)
        {
            switch (team)
            {
                case MPTeamId.Red:
                    return Vector3.right;

                case MPTeamId.Blue:
                    return Vector3.left;

                default:
                    return Vector3.zero;
            }
        }

        public static Vector3 GetOpponentGoalPosition(MPTeamId team)
        {
            return team == MPTeamId.Red
                ? GetBlueGoalPosition()
                : GetRedGoalPosition();
        }

        public static Vector3 GetOwnGoalPosition(MPTeamId team)
        {
            return team == MPTeamId.Red
                ? GetRedGoalPosition()
                : GetBlueGoalPosition();
        }

        public static MPTeam ToMatchTeam(MPTeamId team)
        {
            return MPPlayerTeamState.ToMatchTeam(team);
        }

        private static Vector3 GetRedGoalPosition()
        {
            return Instance != null ? Instance.redGoalPosition : new Vector3(-24f, 0f, 0f);
        }

        private static Vector3 GetBlueGoalPosition()
        {
            return Instance != null ? Instance.blueGoalPosition : new Vector3(24f, 0f, 0f);
        }
    }
}
