using UnityEngine;

#if USE_MIRROR
using Mirror;

namespace MultiplePlayers
{
    public class MPNetworkManager : NetworkManager
    {
        [Header("Player Spawn")]
        [SerializeField] private string playerNamePrefix = "Player";

        [Header("Match Ball")]
        [SerializeField] private GameObject ballPrefab;
        [SerializeField] private Vector3 ballSpawnPosition = new Vector3(0f, 1f, 0f);
        [SerializeField] private Vector3 ballSpawnEulerAngles = Vector3.zero;

        private GameObject spawnedBall;
        private int serverPlayerTeamIndex;

        public override void OnStartServer()
        {
            base.OnStartServer();

            serverPlayerTeamIndex = 0;
            SpawnMatchBall();
        }

        [Server]
        private MPTeam ServerGetNextTeam()
        {
            MPTeam result = serverPlayerTeamIndex % 2 == 0
                ? MPTeam.RedLeft
                : MPTeam.BlueRight;

            serverPlayerTeamIndex++;
            return result;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (ballPrefab != null)
            {
                NetworkClient.RegisterPrefab(ballPrefab);
            }
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            Transform start = GetStartPosition();
            Vector3 spawnPosition = start != null ? start.position : Vector3.zero;
            Quaternion spawnRotation = start != null ? start.rotation : Quaternion.identity;

            GameObject player = Instantiate(playerPrefab, spawnPosition, spawnRotation);

            MPNetworkPlayerController playerController =
                player.GetComponent<MPNetworkPlayerController>();

            if (playerController != null)
            {
                playerController.ServerSetDisplayName($"{playerNamePrefix} {numPlayers + 1}");
                playerController.ServerSetTeam(ServerGetNextTeam());
            }

            NetworkServer.AddPlayerForConnection(conn, player);

            if (MPGameSession.Instance != null)
            {
                MPGameSession.Instance.ServerNotifyPlayerListOrReadyChanged();
            }
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);

            if (MPGameSession.Instance != null)
            {
                MPGameSession.Instance.ServerNotifyPlayerListOrReadyChanged();
            }

            Debug.Log($"Client disconnected: {conn.connectionId}");
        }

        public override void OnStopServer()
        {
            spawnedBall = null;
            serverPlayerTeamIndex = 0;

            base.OnStopServer();
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            Debug.Log($"Connected to server: {networkAddress}");
        }

        [Server]
        private void SpawnMatchBall()
        {
            if (ballPrefab == null || spawnedBall != null)
            {
                return;
            }

            Quaternion spawnRotation = Quaternion.Euler(ballSpawnEulerAngles);
            spawnedBall = Instantiate(ballPrefab, ballSpawnPosition, spawnRotation);
            NetworkServer.Spawn(spawnedBall);
        }
    }
}
#else
namespace MultiplePlayers
{
    public class MPNetworkManager : MonoBehaviour
    {
        [TextArea(4, 8)]
        [SerializeField]
        private string setupHint =
            "Mirror is not installed yet.\n" +
            "1. Install Mirror.\n" +
            "2. Add USE_MIRROR to Player Settings > Scripting Define Symbols.\n" +
            "3. Replace this placeholder with the Mirror-backed implementation automatically.";
    }
}
#endif
