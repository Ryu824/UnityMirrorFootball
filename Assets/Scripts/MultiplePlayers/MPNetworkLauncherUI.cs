using UnityEngine;

#if USE_MIRROR
using Mirror;

namespace MultiplePlayers
{
    public class MPNetworkLauncherUI : MonoBehaviour
    {
        [SerializeField] private MPNetworkManager networkManager;
        [SerializeField] private string defaultAddress = "localhost";

        private string currentAddress;

        private void Awake()
        {
            if (networkManager == null)
            {
                networkManager = FindObjectOfType<MPNetworkManager>();
            }

            currentAddress = defaultAddress;
        }

        private void OnGUI()
        {
            if (networkManager == null)
            {
                return;
            }

            const int width = 260;
            GUILayout.BeginArea(new Rect(16, 16, width, 220), GUI.skin.box);
            GUILayout.Label("Mirror Multiplayer");
            GUILayout.Label($"Mode: {GetModeLabel()}");
            GUILayout.Label("Address");
            currentAddress = GUILayout.TextField(currentAddress);

            GUI.enabled = !NetworkClient.isConnected && !NetworkServer.active;
            if (GUILayout.Button("Start Host"))
            {
                networkManager.networkAddress = currentAddress;
                networkManager.StartHost();
            }

            if (GUILayout.Button("Start Client"))
            {
                networkManager.networkAddress = currentAddress;
                networkManager.StartClient();
            }

            if (GUILayout.Button("Start Server"))
            {
                networkManager.networkAddress = currentAddress;
                networkManager.StartServer();
            }

            GUI.enabled = NetworkClient.isConnected || NetworkServer.active;
            if (GUILayout.Button("Stop"))
            {
                if (NetworkServer.active && NetworkClient.isConnected)
                {
                    networkManager.StopHost();
                }
                else if (NetworkClient.isConnected)
                {
                    networkManager.StopClient();
                }
                else if (NetworkServer.active)
                {
                    networkManager.StopServer();
                }
            }

            GUI.enabled = true;
            GUILayout.Space(8f);
            GUILayout.Label("Use localhost / 127.0.0.1 for same-machine tests.");
            GUILayout.EndArea();
        }

        private static string GetModeLabel()
        {
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                return "Host";
            }

            if (NetworkClient.isConnected)
            {
                return "Client";
            }

            if (NetworkServer.active)
            {
                return "Server";
            }

            return "Offline";
        }
    }
}
#else
namespace MultiplePlayers
{
    public class MPNetworkLauncherUI : MonoBehaviour
    {
        [TextArea(4, 8)]
        [SerializeField] private string setupHint =
            "Mirror is not installed yet.\n" +
            "This component becomes a Host / Client launcher after Mirror is available and USE_MIRROR is defined.";
    }
}
#endif
