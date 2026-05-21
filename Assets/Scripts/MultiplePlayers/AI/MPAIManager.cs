using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace MultiplePlayers
{
    public class MPAIManager : NetworkBehaviour
    {
        public static MPAIManager Instance { get; private set; }

        [Header("AI Spawn")]
        [SerializeField] private GameObject aiPlayerPrefab;
        [SerializeField] private int targetPlayersPerTeam = 5;

        [Header("Formation Offsets")]
        [SerializeField] private float duplicateFormationXOffset = 0.8f;
        [SerializeField] private float duplicateFormationZOffset = 1.2f;

        [Header("Debug")]
        [SerializeField] private bool verboseLog = true;

        private bool spawnedForCurrentMatch;

        private static readonly MPPlayerPosition[] corePositions =
        {
            MPPlayerPosition.Goalkeeper,
            MPPlayerPosition.Defender,
            MPPlayerPosition.Midfielder,
            MPPlayerPosition.Forward
        };

        private static readonly MPPlayerPosition[] duplicatePositionOrder =
        {
            MPPlayerPosition.Defender,
            MPPlayerPosition.Midfielder,
            MPPlayerPosition.Forward,
            MPPlayerPosition.Defender,
            MPPlayerPosition.Midfielder,
            MPPlayerPosition.Forward
        };

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

        public override void OnStartServer()
        {
            base.OnStartServer();
            spawnedForCurrentMatch = false;
        }

        public override void OnStopServer()
        {
            spawnedForCurrentMatch = false;
            base.OnStopServer();
        }

        [Server]
        public void ServerSpawnAIsForMatch()
        {
            if (spawnedForCurrentMatch)
                return;

            if (aiPlayerPrefab == null)
            {
                Debug.LogWarning("[MPAIManager] aiPlayerPrefab is not assigned.");
                return;
            }

            spawnedForCurrentMatch = true;

            ServerSpawnAIsForTeam(MPTeamId.Red);
            ServerSpawnAIsForTeam(MPTeamId.Blue);
        }

        [Server]
        private void ServerSpawnAIsForTeam(MPTeamId team)
        {
            int currentCount = ServerCountTeamPlayers(team);
            int need = Mathf.Max(0, targetPlayersPerTeam - currentCount);

            if (verboseLog)
            {
                Debug.Log($"[MPAIManager] Team {team}: current={currentCount}, need AI={need}");
            }

            List<MPPlayerPosition> spawnPositions = ServerBuildSpawnPositionList(team, need);
            Dictionary<MPPlayerPosition, int> positionSpawnCounts =
                new Dictionary<MPPlayerPosition, int>();

            for (int i = 0; i < spawnPositions.Count; i++)
            {
                MPPlayerPosition position = spawnPositions[i];
                int positionIndex = 0;

                if (positionSpawnCounts.TryGetValue(position, out int existingCount))
                {
                    positionIndex = existingCount;
                }

                positionSpawnCounts[position] = positionIndex + 1;

                Transform spawnPoint = ServerFindFormationPoint(team, position, positionIndex);
                Vector3 formationOffset = ServerGetFormationOffset(team, positionIndex);

                Vector3 spawnPosition = spawnPoint != null
                    ? spawnPoint.position + formationOffset
                    : ServerGetFallbackSpawnPosition(team, i) + formationOffset;

                Quaternion spawnRotation = spawnPoint != null
                    ? spawnPoint.rotation
                    : Quaternion.LookRotation(MPTeamUtility.GetAttackDirection(team), Vector3.up);

                GameObject aiObject = Instantiate(aiPlayerPrefab, spawnPosition, spawnRotation);

                MPPlayerTeamState teamState = aiObject.GetComponent<MPPlayerTeamState>();

                if (teamState != null)
                {
                    teamState.TeamId = team;
                    teamState.Position = position;
                    teamState.ControlType = MPControlType.AI;
                }
                else
                {
                    Debug.LogWarning("[MPAIManager] AI prefab has no MPPlayerTeamState.");
                }

                MPAIPlayerController aiController =
                    aiObject.GetComponent<MPAIPlayerController>();

                if (aiController != null)
                {
                    aiController.ServerInitialize(team, position, spawnPoint, formationOffset);
                }
                else
                {
                    Debug.LogWarning("[MPAIManager] AI prefab has no MPAIPlayerController.");
                }

                NetworkServer.Spawn(aiObject);
            }
        }

        [Server]
        private List<MPPlayerPosition> ServerBuildSpawnPositionList(MPTeamId team, int need)
        {
            List<MPPlayerPosition> result = new List<MPPlayerPosition>();

            if (need <= 0)
                return result;

            HashSet<MPPlayerPosition> occupied = new HashSet<MPPlayerPosition>();
            List<MPPlayerTeamState> allPlayers = ServerGetTeamPlayers(team);

            foreach (MPPlayerTeamState player in allPlayers)
            {
                if (player != null && player.Position != MPPlayerPosition.None)
                {
                    occupied.Add(player.Position);
                }
            }

            foreach (MPPlayerPosition position in corePositions)
            {
                if (result.Count >= need)
                    break;

                if (!occupied.Contains(position))
                {
                    result.Add(position);
                    occupied.Add(position);
                }
            }

            int duplicateIndex = 0;

            while (result.Count < need)
            {
                MPPlayerPosition position =
                    duplicatePositionOrder[duplicateIndex % duplicatePositionOrder.Length];

                result.Add(position);
                duplicateIndex++;
            }

            return result;
        }

        [Server]
        public int ServerCountTeamPlayers(MPTeamId team)
        {
            return ServerGetTeamPlayers(team).Count;
        }

        [Server]
        public List<MPPlayerTeamState> ServerGetTeamPlayers(MPTeamId team)
        {
            List<MPPlayerTeamState> result = new List<MPPlayerTeamState>();

            MPPlayerTeamState[] allPlayers =
                FindObjectsByType<MPPlayerTeamState>(FindObjectsSortMode.None);

            foreach (MPPlayerTeamState player in allPlayers)
            {
                if (player != null && player.TeamId == team)
                {
                    result.Add(player);
                }
            }

            return result;
        }

        [Server]
        public List<MPAIPlayerController> ServerGetTeamAIs(MPTeamId team)
        {
            List<MPAIPlayerController> result = new List<MPAIPlayerController>();

            MPAIPlayerController[] allAIs =
                FindObjectsByType<MPAIPlayerController>(FindObjectsSortMode.None);

            foreach (MPAIPlayerController ai in allAIs)
            {
                if (ai != null && ai.TeamId == team)
                {
                    result.Add(ai);
                }
            }

            return result;
        }

        [Server]
        public MPAIPlayerController ServerGetBestChaser(MPTeamId team, Vector3 ballPosition)
        {
            MPAIPlayerController best = null;
            float bestSqrDistance = float.MaxValue;
            List<MPAIPlayerController> allAIs = ServerGetTeamAIs(team);

            foreach (MPAIPlayerController ai in allAIs)
            {
                if (ai == null || !ai.ServerCanChaseBallAt(ballPosition))
                    continue;

                float sqrDistance = (ai.transform.position - ballPosition).sqrMagnitude;

                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    best = ai;
                }
            }

            return best;
        }

        [Server]
        public Transform ServerFindFormationPoint(
            MPTeamId team,
            MPPlayerPosition position,
            int index)
        {
            MPAIFormationPoint[] points =
                FindObjectsByType<MPAIFormationPoint>(FindObjectsSortMode.None);

            Transform fallback = null;

            foreach (MPAIFormationPoint point in points)
            {
                if (point == null)
                    continue;

                if (point.teamId != team || point.position != position)
                    continue;

                if (point.index == index)
                    return point.transform;

                if (fallback == null)
                    fallback = point.transform;
            }

            return fallback;
        }

        [Server]
        private Vector3 ServerGetFallbackSpawnPosition(MPTeamId team, int index)
        {
            float x = team == MPTeamId.Red ? -12f : 12f;
            float z = -6f + index * 3f;
            return new Vector3(x, 1f, z);
        }

        [Server]
        private Vector3 ServerGetFormationOffset(MPTeamId team, int positionIndex)
        {
            if (positionIndex <= 0)
                return Vector3.zero;

            float side = team == MPTeamId.Red ? 1f : -1f;
            float xOffset = duplicateFormationXOffset * positionIndex * side;
            float zOffset = positionIndex % 2 == 0
                ? duplicateFormationZOffset
                : -duplicateFormationZOffset;

            return new Vector3(xOffset, 0f, zOffset);
        }
    }
}
