using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace MultiplePlayers
{
    public class MPLobbyPanel : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup panelCanvasGroup;

        [Header("UI")]
        [SerializeField] private Text playerInfoText;
        [SerializeField] private Button readyButton;
        [SerializeField] private Text readyButtonText;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Text startGameButtonText;

        private MPPlayerReadyState localReadyState;
        private MPPlayerTeamState localTeamState;

        public GameObject PanelRoot => panelRoot;
        public CanvasGroup PanelCanvasGroup => panelCanvasGroup;

        private void Awake()
        {
            EnsureFallbackUi();
            BindButtons();
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        public void Initialize(
            MPPlayerReadyState readyState,
            MPPlayerTeamState teamState)
        {
            localReadyState = readyState;
            localTeamState = teamState;
            Refresh();
        }

        public void Show()
        {
            MPUIVisibilityUtility.Show(panelRoot, panelCanvasGroup);
            Refresh();
        }

        public void Hide()
        {
            MPUIVisibilityUtility.Hide(panelRoot, panelCanvasGroup);
        }

        public void Refresh()
        {
            MPGameSession session = MPGameSession.Instance;
            bool hasSelection = localTeamState != null && localTeamState.HasValidSelection;
            bool isHost = NetworkServer.active && NetworkClient.isConnected;

            if (playerInfoText != null)
            {
                string teamText = hasSelection
                    ? $"{localTeamState.TeamId} / {localTeamState.Position}"
                    : "No team selected";
                string readyText = session != null
                    ? $"Ready: {session.ReadyText}"
                    : "Ready: -";
                playerInfoText.text = $"{teamText}\n{readyText}";
            }

            if (readyButton != null)
            {
                readyButton.interactable = hasSelection && localReadyState != null;
            }

            if (readyButtonText != null)
            {
                bool isReady = localReadyState != null && localReadyState.IsReady;
                readyButtonText.text = isReady ? "Cancel Ready" : "Ready";
            }

            if (startGameButton != null)
            {
                startGameButton.gameObject.SetActive(isHost);
                startGameButton.interactable =
                    isHost && session != null && session.AllClientsReady;
            }

            if (startGameButtonText != null)
            {
                if (session == null)
                {
                    startGameButtonText.text = "Start Game";
                }
                else
                {
                    startGameButtonText.text = session.AllClientsReady
                        ? "Start Game"
                        : "Waiting For Clients";
                }
            }
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
            if (localReadyState == null)
            {
                return;
            }

            localReadyState.CmdSetReady(!localReadyState.IsReady);
            Refresh();
        }

        private void OnClickStartGame()
        {
            if (!NetworkServer.active)
            {
                return;
            }

            MPGameSession.Instance?.ServerTryStartGame();
            Refresh();
        }

        private void EnsureFallbackUi()
        {
            if (panelRoot != null && panelCanvasGroup != null)
            {
                return;
            }

            MPRuntimeUIFactory.EnsureEventSystem();
            Canvas canvas = MPRuntimeUIFactory.EnsureOverlayCanvas("GameOverlayCanvas", 50);

            GameObject panel = MPRuntimeUIFactory.CreatePanel(
                canvas.transform,
                "LobbyPanel",
                new Color(0.05f, 0.08f, 0.12f, 0.94f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-300f, -180f),
                new Vector2(300f, 180f),
                out CanvasGroup canvasGroup);

            panelRoot = panel;
            panelCanvasGroup = canvasGroup;

            MPRuntimeUIFactory.CreateText(
                panel.transform,
                "Title",
                "Lobby",
                30,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(24f, -64f),
                new Vector2(-24f, -16f));

            playerInfoText = MPRuntimeUIFactory.CreateText(
                panel.transform,
                "PlayerInfoText",
                "No team selected",
                22,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(24f, -40f),
                new Vector2(-24f, 40f));

            readyButton = MPRuntimeUIFactory.CreateButton(
                panel.transform,
                "ReadyButton",
                "Ready",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(-120f, 24f),
                new Vector2(120f, 80f));
            readyButtonText = readyButton.GetComponentInChildren<Text>();

            startGameButton = MPRuntimeUIFactory.CreateButton(
                panel.transform,
                "StartGameButton",
                "Start Game",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(-140f, 92f),
                new Vector2(140f, 148f),
                new Color(0.17f, 0.52f, 0.28f, 0.96f));
            startGameButtonText = startGameButton.GetComponentInChildren<Text>();
        }
    }
}
