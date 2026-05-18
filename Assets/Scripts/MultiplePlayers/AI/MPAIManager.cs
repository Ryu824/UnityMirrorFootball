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
            Instance = this;
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

            List<MPPlayerPosition> spawnPositions =
                ServerBuildSpawnPositionList(team, need);

            for (int i = 0; i < spawnPositions.Count; i++)
            {
                MPPlayerPosition position = spawnPositions[i];

                Transform spawnPoint =
                    ServerFindFormationPoint(team, position, i);

                Vector3 spawnPos = spawnPoint != null
                    ? spawnPoint.position
                    : ServerGetFallbackSpawnPosition(team, i);

                Quaternion spawnRot = spawnPoint != null
                    ? spawnPoint.rotation
                    : Quaternion.LookRotation(MPTeamUtility.GetAttackDirection(team), Vector3.up);

                GameObject aiObject = Instantiate(aiPlayerPrefab, spawnPos, spawnRot);

                MPPlayerTeamState teamState =
                    aiObject.GetComponent<MPPlayerTeamState>();

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
                    aiController.ServerInitialize(team, position, spawnPoint);
                }
                else
                {
                    Debug.LogWarning("[MPAIManager] AI prefab has no MPAIPlayerController.");
                }

                NetworkServer.Spawn(aiObject);
            }
        }

        [Server]
        private List<MPPlayerPosition> ServerBuildSpawnPositionList(
            MPTeamId team,
            int need)
        {
            List<MPPlayerPosition> result = new List<MPPlayerPosition>();

            if (need <= 0)
                return result;

            HashSet<MPPlayerPosition> occupied = new HashSet<MPPlayerPosition>();

            MPPlayerTeamState[] allPlayers =
                FindObjectsByType<MPPlayerTeamState>(FindObjectsSortMode.None);

            foreach (MPPlayerTeamState player in allPlayers)
            {
                if (player == null || player.TeamId != team)
                    continue;

                if (player.Position != MPPlayerPosition.None)
                    occupied.Add(player.Position);
            }

            // 先补齐缺失的基础位置：门将、后卫、中场、前锋
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

            // 如果还没凑够 5v5，就按顺序补重复位置
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
            int count = 0;

            MPPlayerTeamState[] allPlayers =
                FindObjectsByType<MPPlayerTeamState>(FindObjectsSortMode.None);

            foreach (MPPlayerTeamState player in allPlayers)
            {
                if (player != null && player.TeamId == team)
                    count++;
            }

            return count;
        }

        [Server]
        public MPAIPlayerController ServerFindBestChaser(
            MPTeamId team,
            Vector3 ballPosition)
        {
            MPAIPlayerController best = null;
            float bestSqrDistance = float.MaxValue;

            MPAIPlayerController[] allAIs =
                FindObjectsByType<MPAIPlayerController>(FindObjectsSortMode.None);

            foreach (MPAIPlayerController ai in allAIs)
            {
                if (ai == null || ai.TeamId != team)
                    continue;

                if (!ai.ServerCanChaseBallAt(ballPosition))
                    continue;

                float sqrDistance =
                    (ai.transform.position - ballPosition).sqrMagnitude;

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
    }
}