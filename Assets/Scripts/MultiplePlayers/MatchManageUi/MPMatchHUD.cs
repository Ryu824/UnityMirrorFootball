using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if USE_MIRROR
using Mirror;

namespace MultiplePlayers
{
    public class MPMatchHUD : MonoBehaviour
    {
        [Header("Lifecycle")]
        [SerializeField] private bool managePhasePanels = true;

        [Header("Panels")]
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject timerPanel;
        [SerializeField] private GameObject scorePanel;
        [SerializeField] private GameObject centerMessagePanel;
        [SerializeField] private GameObject gameOverPanel;

        [Header("Lobby UI")]
        [SerializeField] private TMP_Text readyInfoText;
        [SerializeField] private Button readyButton;
        [SerializeField] private TMP_Text readyButtonText;
        [SerializeField] private Button startGameButton;
        [SerializeField] private TMP_Text startGameButtonText;

        [Header("Timer UI")]
        [SerializeField] private TMP_Text elapsedText;
        [SerializeField] private TMP_Text remainingText;

        [Header("Score UI - Reserved For Later")]
        [SerializeField] private TMP_Text scoreText;

        [Header("Center Message UI - Reserved For Later")]
        [SerializeField] private TMP_Text centerMessageText;

        [Header("Game Over UI")]
        [SerializeField] private TMP_Text gameOverText;
        [SerializeField] private TMP_Text shutdownText;

        private void Awake()
        {
            BindButtons();
            HideCenterMessage();
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        private void Update()
        {
            RefreshHUD();
        }

        private void BindButtons()
        {
            if (readyButton != null)
            {
                readyButton.onClick.AddListener(OnClickReady);
            }

            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(OnClickStartGame);
            }
        }

        private void UnbindButtons()
        {
            if (readyButton != null)
            {
                readyButton.onClick.RemoveListener(OnClickReady);
            }

            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveListener(OnClickStartGame);
            }
        }

        private void OnClickReady()
        {
            MPPlayerReadyState readyState = GetLocalReadyState();

            if (readyState == null)
            {
                Debug.LogWarning("[MPMatchHUD] Local MPPlayerReadyState not found.");
                return;
            }

            readyState.CmdSetReady(!readyState.IsReady);
        }

        private void OnClickStartGame()
        {
            MPGameSession session = MPGameSession.Instance;

            if (session == null)
            {
                Debug.LogWarning("[MPMatchHUD] MPGameSession not found.");
                return;
            }

            if (!NetworkServer.active)
            {
                Debug.LogWarning("[MPMatchHUD] Only host/server can start game.");
                return;
            }

            session.ServerTryStartGame();
        }

        private void RefreshHUD()
        {
            MPGameSession session = MPGameSession.Instance;

            bool online = NetworkClient.isConnected || NetworkServer.active;

            if (!online || session == null)
            {
                SetPanelActive(lobbyPanel, false);
                SetPanelActive(timerPanel, false);
                SetPanelActive(scorePanel, false);
                SetPanelActive(centerMessagePanel, false);
                SetPanelActive(gameOverPanel, false);
                return;
            }

            switch (session.Phase)
            {
                case MPMatchState.Lobby:
                    RefreshLobby(session);
                    break;

                case MPMatchState.Playing:
                case MPMatchState.RulePause:
                case MPMatchState.SetPiece:
                    RefreshPlaying(session);
                    break;

                case MPMatchState.TimeUp:
                case MPMatchState.Closing:
                    RefreshGameOver(session);
                    break;
            }

RefreshCenterMessage(session);
        }

        private void RefreshLobby(MPGameSession session)
        {
            if (managePhasePanels)
            {
                SetPanelActive(lobbyPanel, true);
                SetPanelActive(timerPanel, false);
                SetPanelActive(scorePanel, false);
                SetPanelActive(gameOverPanel, false);
            }

            bool isServerProcess = NetworkServer.active;
            bool canUseReadyButton = NetworkClient.isConnected;

            if (readyInfoText != null)
            {
                readyInfoText.text = $"Client Ready: {session.ReadyText}";
            }

            MPPlayerReadyState readyState = GetLocalReadyState();

            if (readyButton != null)
            {
                readyButton.gameObject.SetActive(canUseReadyButton);
                readyButton.interactable = canUseReadyButton && readyState != null;
            }

            if (readyButtonText != null)
            {
                bool isReady = readyState != null && readyState.IsReady;
                readyButtonText.text = isReady ? "Cancel Ready" : "Ready";
            }

            if (startGameButton != null)
            {
                startGameButton.gameObject.SetActive(isServerProcess);
                startGameButton.interactable = isServerProcess && session.AllClientsReady;
            }

            if (startGameButtonText != null)
            {
                startGameButtonText.text = session.AllClientsReady
                    ? "Start Game"
                    : "Waiting For Clients";
            }
        }

        private void RefreshPlaying(MPGameSession session)
        {
            if (managePhasePanels)
            {
                SetPanelActive(lobbyPanel, false);
                SetPanelActive(timerPanel, true);
                SetPanelActive(scorePanel, true);
                SetPanelActive(gameOverPanel, false);
            }

            if (elapsedText != null)
            {
                elapsedText.text = $"Elapsed: {session.ElapsedText}";
            }

            if (remainingText != null)
            {
                remainingText.text = $"Remaining: {session.RemainingText}";
            }

            if (scoreText != null)
            {
                scoreText.text = session.ScoreText;
            }
        }

        private void RefreshGameOver(MPGameSession session)
        {
            if (managePhasePanels)
            {
                SetPanelActive(lobbyPanel, false);
                SetPanelActive(timerPanel, false);
                SetPanelActive(scorePanel, true);
                SetPanelActive(gameOverPanel, true);
            }

            if (scoreText != null)
            {
                scoreText.text = session.ScoreText;
            }

            if (gameOverText != null)
            {
                gameOverText.text = shutdownText == null
                    ? $"GAME OVER\nClosing in {session.ShutdownText}s"
                    : "GAME OVER";
            }

            if (shutdownText != null)
            {
                shutdownText.text = $"Game will close in {session.ShutdownText}s";
            }
        }

        private MPPlayerReadyState GetLocalReadyState()
        {
            if (!NetworkClient.isConnected)
            {
                return null;
            }

            if (NetworkClient.localPlayer == null)
            {
                return null;
            }

            return NetworkClient.localPlayer.GetComponent<MPPlayerReadyState>();
        }

        private void HideCenterMessage()
        {
            SetPanelActive(centerMessagePanel, false);
        }

        // 后面进球提示会用这个方法。
        public void ShowCenterMessage(string message)
        {
            if (centerMessageText != null)
            {
                centerMessageText.text = message;
            }

            SetPanelActive(centerMessagePanel, true);
            CancelInvoke(nameof(HideCenterMessage));
            Invoke(nameof(HideCenterMessage), 2f);
        }

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null && panel.activeSelf != active)
            {
                panel.SetActive(active);
            }
        }

        private void RefreshCenterMessage(MPGameSession session)
        {
            if (session == null || !session.ShouldShowCenterMessage)
            {
                SetPanelActive(centerMessagePanel, false);
                return;
            }

            if (centerMessageText != null)
            {
                centerMessageText.text = session.CenterMessage;
            }

            SetPanelActive(centerMessagePanel, true);
        }

        public void SetManagedByExternalRoot(bool managedExternally)
        {
            managePhasePanels = !managedExternally;

            if (managedExternally)
            {
                SetPanelActive(lobbyPanel, false);
                SetPanelActive(gameOverPanel, false);
            }
        }
    }
}
#else
namespace MultiplePlayers
{
    public class MPMatchHUD : MonoBehaviour
    {
        [TextArea(4, 8)]
        [SerializeField]
        private string setupHint =
            "Mirror is not installed yet. Add USE_MIRROR to enable MPMatchHUD.";
    }
}
#endif
