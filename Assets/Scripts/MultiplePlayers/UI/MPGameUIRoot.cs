using System.Collections;
using Mirror;
using UnityEngine;

namespace MultiplePlayers
{
    public class MPGameUIRoot : MonoBehaviour
    {
        [Header("Optional Scene References")]
        [SerializeField] private MPMatchHUD matchHUD;
        [SerializeField] private MPTeamSelectHUD legacyTeamSelectHud;
        [SerializeField] private MPTeamSelectPanel teamSelectPanel;
        [SerializeField] private MPLobbyPanel lobbyPanel;

        [Header("Optional HUD Roots")]
        [SerializeField] private GameObject matchHUDPanel;
        [SerializeField] private CanvasGroup matchHUDCanvasGroup;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private CanvasGroup gameOverCanvasGroup;

        [Header("Status")]
        [SerializeField] private bool logLifecycle = false;

        private MPPlayerTeamState localTeamState;
        private MPPlayerReadyState localReadyState;
        private bool selectionLocked;
        private Coroutine waitForLocalPlayerRoutine;

        private void Awake()
        {
            EnsureFallbackUi();
            DisableLegacyTeamSelect();

            if (matchHUD == null)
            {
                matchHUD = FindFirstObjectByType<MPMatchHUD>(FindObjectsInactive.Include);
            }

            if (matchHUD != null)
            {
                matchHUD.SetManagedByExternalRoot(true);

                if (matchHUDPanel == null)
                {
                    matchHUDPanel = matchHUD.gameObject;
                }
            }

            HideAllPlayerUi();
        }

        private void Start()
        {
            if (NetworkServer.active && !NetworkClient.isConnected)
            {
                HideAllPlayerUi();
                DisableServerOnlySceneCamera();
                return;
            }

            waitForLocalPlayerRoutine = StartCoroutine(WaitForLocalPlayerRoutine());
        }

        private void Update()
        {
            if (NetworkServer.active && !NetworkClient.isConnected)
            {
                HideAllPlayerUi();
                DisableServerOnlySceneCamera();
                return;
            }

            if ((localTeamState == null || localReadyState == null) &&
                waitForLocalPlayerRoutine == null)
            {
                waitForLocalPlayerRoutine = StartCoroutine(WaitForLocalPlayerRoutine());
            }

            if (localTeamState == null || localReadyState == null)
            {
                return;
            }

            if (localTeamState.HasValidSelection)
            {
                selectionLocked = true;
            }

            lobbyPanel?.Refresh();
            RefreshPanelsForState();
        }

        private IEnumerator WaitForLocalPlayerRoutine()
        {
            HideAllPlayerUi();

            while (NetworkClient.localPlayer == null)
            {
                if (NetworkServer.active && !NetworkClient.isConnected)
                {
                    waitForLocalPlayerRoutine = null;
                    yield break;
                }

                yield return null;
            }

            if (NetworkClient.localPlayer == null)
            {
                waitForLocalPlayerRoutine = null;
                yield break;
            }

            localTeamState = NetworkClient.localPlayer.GetComponent<MPPlayerTeamState>();
            localReadyState = NetworkClient.localPlayer.GetComponent<MPPlayerReadyState>();
            selectionLocked = localTeamState != null && localTeamState.HasValidSelection;

            if (logLifecycle)
            {
                Debug.Log("[MPGameUIRoot] Local player initialized.");
            }

            teamSelectPanel?.Initialize(localTeamState);
            lobbyPanel?.Initialize(localReadyState, localTeamState);
            RefreshPanelsForState();
            waitForLocalPlayerRoutine = null;
        }

        private void OnDestroy()
        {
            if (waitForLocalPlayerRoutine != null)
            {
                StopCoroutine(waitForLocalPlayerRoutine);
            }

            if (teamSelectPanel != null)
            {
                teamSelectPanel.SelectionConfirmed -= OnSelectionConfirmed;
            }
        }

        private void RefreshPanelsForState()
        {
            MPGameSession session = MPGameSession.Instance;
            if (session == null)
            {
                ShowConnectingState();
                return;
            }

            if (session.Phase == MPMatchState.Lobby)
            {
                if (!selectionLocked && localTeamState != null && localTeamState.HasValidSelection)
                {
                    selectionLocked = true;
                }

                if (!selectionLocked)
                {
                    teamSelectPanel?.Show();
                    lobbyPanel?.Hide();
                    HideMatchHud();
                }
                else
                {
                    teamSelectPanel?.Hide();
                    lobbyPanel?.Show();
                    HideMatchHud();
                }

                return;
            }

            teamSelectPanel?.Hide();
            lobbyPanel?.Hide();
            ShowMatchHud();
        }

        private void OnSelectionConfirmed()
        {
            selectionLocked = true;
            teamSelectPanel?.Hide();
            lobbyPanel?.Show();
        }

        private void HideAllPlayerUi()
        {
            teamSelectPanel?.Hide();
            lobbyPanel?.Hide();
            HideMatchHud();
            MPUIVisibilityUtility.Hide(gameOverPanel, gameOverCanvasGroup);
        }

        private void ShowConnectingState()
        {
            teamSelectPanel?.Show();
            HideMatchHud();
            lobbyPanel?.Hide();
        }

        private void ShowMatchHud()
        {
            if (matchHUDPanel != null)
            {
                if (matchHUDCanvasGroup != null)
                {
                    MPUIVisibilityUtility.Show(matchHUDPanel, matchHUDCanvasGroup);
                }
                else
                {
                    matchHUDPanel.SetActive(true);
                }
            }
        }

        private void HideMatchHud()
        {
            if (matchHUDPanel != null)
            {
                if (matchHUDCanvasGroup != null)
                {
                    MPUIVisibilityUtility.Hide(matchHUDPanel, matchHUDCanvasGroup);
                }
                else
                {
                    matchHUDPanel.SetActive(false);
                }
            }
        }

        private void DisableLegacyTeamSelect()
        {
            if (legacyTeamSelectHud == null)
            {
                legacyTeamSelectHud =
                    FindFirstObjectByType<MPTeamSelectHUD>(FindObjectsInactive.Include);
            }

            if (legacyTeamSelectHud == null)
            {
                return;
            }

            legacyTeamSelectHud.enabled = false;
            legacyTeamSelectHud.gameObject.SetActive(false);
        }

        private void DisableServerOnlySceneCamera()
        {
            Camera sceneCamera = Camera.main;
            if (sceneCamera != null)
            {
                sceneCamera.enabled = false;
            }

            AudioListener listener =
                sceneCamera != null
                    ? sceneCamera.GetComponent<AudioListener>()
                    : FindFirstObjectByType<AudioListener>();

            if (listener != null)
            {
                listener.enabled = false;
            }
        }

        private void EnsureFallbackUi()
        {
            if (teamSelectPanel == null)
            {
                GameObject teamSelectObject = new GameObject("MPTeamSelectPanel");
                teamSelectPanel = teamSelectObject.AddComponent<MPTeamSelectPanel>();
            }

            if (lobbyPanel == null)
            {
                GameObject lobbyObject = new GameObject("MPLobbyPanel");
                lobbyPanel = lobbyObject.AddComponent<MPLobbyPanel>();
            }

            teamSelectPanel.SelectionConfirmed -= OnSelectionConfirmed;
            teamSelectPanel.SelectionConfirmed += OnSelectionConfirmed;
        }
    }
}
