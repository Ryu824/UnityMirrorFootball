using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MultiplePlayers
{
    public class MPNetworkMenuUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MPNetworkManager networkManager;
        [SerializeField] private MPMainMenuUI mainMenuUI;

        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup panelCanvasGroup;

        [Header("Inputs")]
        [SerializeField] private InputField ipInputField;
        [SerializeField] private InputField portInputField;
        [SerializeField] private Text statusText;

        [Header("Buttons")]
        [SerializeField] private Button localGameButton;
        [SerializeField] private Button startHostButton;
        [SerializeField] private Button startClientButton;
        [SerializeField] private Button startServerButton;
        [SerializeField] private Button backButton;

        [Header("Defaults")]
        [SerializeField] private string defaultIpAddress = "127.0.0.1";
        [SerializeField] private ushort defaultPort = 7777;
        [SerializeField] private string localGameSceneName = "MultiplePlayers";

        private bool launchInProgress;

        public GameObject PanelRoot => panelRoot;
        public CanvasGroup PanelCanvasGroup => panelCanvasGroup;

        private void Awake()
        {
            if (networkManager == null)
            {
                networkManager = FindFirstObjectByType<MPNetworkManager>();
            }

            if (mainMenuUI == null)
            {
                mainMenuUI = GetComponent<MPMainMenuUI>();
            }

            EnsureFallbackUi();
            BindButtons();
            ApplyDefaults();
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        private void BindButtons()
        {
            if (localGameButton != null)
            {
                localGameButton.onClick.AddListener(OnClickLocalGame);
            }

            if (startHostButton != null)
            {
                startHostButton.onClick.AddListener(OnClickStartHost);
            }

            if (startClientButton != null)
            {
                startClientButton.onClick.AddListener(OnClickStartClient);
            }

            if (startServerButton != null)
            {
                startServerButton.onClick.AddListener(OnClickStartServer);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnClickBack);
            }
        }

        private void UnbindButtons()
        {
            if (localGameButton != null)
            {
                localGameButton.onClick.RemoveListener(OnClickLocalGame);
            }

            if (startHostButton != null)
            {
                startHostButton.onClick.RemoveListener(OnClickStartHost);
            }

            if (startClientButton != null)
            {
                startClientButton.onClick.RemoveListener(OnClickStartClient);
            }

            if (startServerButton != null)
            {
                startServerButton.onClick.RemoveListener(OnClickStartServer);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnClickBack);
            }
        }

        private void ApplyDefaults()
        {
            if (ipInputField != null && string.IsNullOrWhiteSpace(ipInputField.text))
            {
                ipInputField.text = defaultIpAddress;
            }

            if (portInputField != null && string.IsNullOrWhiteSpace(portInputField.text))
            {
                portInputField.text = defaultPort.ToString();
            }
        }

        private void EnsureFallbackUi()
        {
            if (panelRoot != null && panelCanvasGroup != null)
            {
                return;
            }

            MPRuntimeUIFactory.EnsureEventSystem();
            Canvas canvas = MPRuntimeUIFactory.EnsureOverlayCanvas("MainMenuCanvas");

            GameObject panel = MPRuntimeUIFactory.CreatePanel(
                canvas.transform,
                "NetworkLaunchPanel",
                new Color(0.08f, 0.11f, 0.15f, 0.94f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-280f, -260f),
                new Vector2(280f, 260f),
                out CanvasGroup canvasGroup);

            panelRoot = panel;
            panelCanvasGroup = canvasGroup;

            RectTransform layoutRoot = MPRuntimeUIFactory.CreateVerticalContainer(
                panel.transform,
                "Layout",
                Vector2.zero,
                Vector2.one,
                new Vector2(24f, 24f),
                new Vector2(-24f, -24f),
                14f);

            MPRuntimeUIFactory.CreateText(
                layoutRoot,
                "Title",
                "Network Launch",
                30,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                new Vector2(0f, 0f));

            ipInputField = MPRuntimeUIFactory.CreateInputField(
                layoutRoot,
                "IpInputField",
                defaultIpAddress,
                "IP Address",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            MPRuntimeUIFactory.AddFixedHeight(ipInputField, 52f);

            portInputField = MPRuntimeUIFactory.CreateInputField(
                layoutRoot,
                "PortInputField",
                defaultPort.ToString(),
                "Port",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            MPRuntimeUIFactory.AddFixedHeight(portInputField, 52f);

            statusText = MPRuntimeUIFactory.CreateText(
                layoutRoot,
                "Status",
                "Choose Local Game for a direct scene launch, or use Host / Client / Server for Mirror networking.",
                18,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            localGameButton = MPRuntimeUIFactory.CreateButton(
                layoutRoot,
                "LocalGameButton",
                "Local Game",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                new Color(0.18f, 0.55f, 0.32f, 0.95f));
            MPRuntimeUIFactory.AddFixedHeight(localGameButton, 56f);

            startHostButton = MPRuntimeUIFactory.CreateButton(
                layoutRoot,
                "StartHostButton",
                "Start Host",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            MPRuntimeUIFactory.AddFixedHeight(startHostButton, 56f);

            startClientButton = MPRuntimeUIFactory.CreateButton(
                layoutRoot,
                "StartClientButton",
                "Start Client",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            MPRuntimeUIFactory.AddFixedHeight(startClientButton, 56f);

            startServerButton = MPRuntimeUIFactory.CreateButton(
                layoutRoot,
                "StartServerButton",
                "Start Server",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            MPRuntimeUIFactory.AddFixedHeight(startServerButton, 56f);

            backButton = MPRuntimeUIFactory.CreateButton(
                layoutRoot,
                "BackButton",
                "Back",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                new Color(0.25f, 0.25f, 0.25f, 0.95f));
            MPRuntimeUIFactory.AddFixedHeight(backButton, 52f);
        }

        private void OnClickLocalGame()
        {
            if (launchInProgress)
            {
                return;
            }

            StartCoroutine(LoadLocalGameRoutine());
        }

        private void OnClickStartHost()
        {
            if (launchInProgress)
            {
                return;
            }

            if (networkManager == null)
            {
                SetStatus("MPNetworkManager not found.");
                return;
            }

            StartCoroutine(StartHostRoutine());
        }

        private void OnClickStartClient()
        {
            if (launchInProgress)
            {
                return;
            }

            if (networkManager == null)
            {
                SetStatus("MPNetworkManager not found.");
                return;
            }

            StartCoroutine(StartClientRoutine());
        }

        private void OnClickStartServer()
        {
            if (launchInProgress)
            {
                return;
            }

            if (networkManager == null)
            {
                SetStatus("MPNetworkManager not found.");
                return;
            }

            StartCoroutine(StartServerRoutine());
        }

        private void OnClickBack()
        {
            if (launchInProgress)
            {
                return;
            }

            if (mainMenuUI != null)
            {
                mainMenuUI.ShowMainMenu();
            }
        }

        private void ApplyAddressAndPort()
        {
            string address =
                ipInputField == null || string.IsNullOrWhiteSpace(ipInputField.text)
                    ? defaultIpAddress
                    : ipInputField.text.Trim();

            networkManager.networkAddress = address;

            if (!TryGetDesiredPort(out ushort port))
            {
                port = defaultPort;
            }

            TryApplyPort(port);
        }

        private bool TryGetDesiredPort(out ushort port)
        {
            port = defaultPort;

            if (portInputField == null || string.IsNullOrWhiteSpace(portInputField.text))
            {
                return false;
            }

            return ushort.TryParse(portInputField.text.Trim(), out port);
        }

        private void TryApplyPort(ushort port)
        {
            Transport transport =
                networkManager.transport != null
                    ? networkManager.transport
                    : Transport.active ?? networkManager.GetComponent<Transport>();

            if (transport is PortTransport portTransport)
            {
                portTransport.Port = port;
                return;
            }

            Debug.Log(
                "[MPNetworkMenuUI] Current transport does not expose a safe runtime port API. " +
                "Port input remains UI-only and existing transport config is preserved.");
        }

        private IEnumerator LoadLocalGameRoutine()
        {
            launchInProgress = true;
            SetButtonsInteractable(false);
            SetStatus("Loading game scene...");
            yield return null;

            SceneManager.LoadScene(localGameSceneName);
        }

        private IEnumerator StartHostRoutine()
        {
            launchInProgress = true;
            SetButtonsInteractable(false);
            ApplyAddressAndPort();
            SetStatus($"Starting host on {networkManager.networkAddress}:{GetPortText()}...");
            yield return null;

            try
            {
                networkManager.StartHost();
            }
            catch (System.Exception exception)
            {
                launchInProgress = false;
                SetButtonsInteractable(true);
                SetStatus($"Host failed: {exception.Message}");
                Debug.LogException(exception);
            }
        }

        private IEnumerator StartClientRoutine()
        {
            launchInProgress = true;
            SetButtonsInteractable(false);
            ApplyAddressAndPort();
            SetStatus($"Connecting to {networkManager.networkAddress}:{GetPortText()}...");
            yield return null;

            try
            {
                networkManager.StartClient();
                StartCoroutine(ClientConnectTimeoutRoutine());
            }
            catch (System.Exception exception)
            {
                launchInProgress = false;
                SetButtonsInteractable(true);
                SetStatus($"Client failed: {exception.Message}");
                Debug.LogException(exception);
            }
        }

        private IEnumerator StartServerRoutine()
        {
            launchInProgress = true;
            SetButtonsInteractable(false);
            ApplyAddressAndPort();
            SetStatus($"Starting server on port {GetPortText()}...");
            yield return null;

            try
            {
                networkManager.StartServer();
            }
            catch (System.Exception exception)
            {
                launchInProgress = false;
                SetButtonsInteractable(true);
                SetStatus($"Server failed: {exception.Message}");
                Debug.LogException(exception);
            }
        }

        private IEnumerator ClientConnectTimeoutRoutine()
        {
            float timeoutAt = Time.unscaledTime + 8f;

            while (Time.unscaledTime < timeoutAt)
            {
                if (NetworkClient.isConnected)
                {
                    SetStatus("Connected. Loading game scene...");
                    yield break;
                }

                yield return null;
            }

            if (!NetworkClient.isConnected)
            {
                networkManager.StopClient();
                launchInProgress = false;
                SetButtonsInteractable(true);
                SetStatus("Client connection timed out. Start a Host or Server first, then try Client again.");
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (localGameButton != null)
            {
                localGameButton.interactable = interactable;
            }

            if (startHostButton != null)
            {
                startHostButton.interactable = interactable;
            }

            if (startClientButton != null)
            {
                startClientButton.interactable = interactable;
            }

            if (startServerButton != null)
            {
                startServerButton.interactable = interactable;
            }

            if (backButton != null)
            {
                backButton.interactable = interactable;
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }

            Debug.Log($"[MPNetworkMenuUI] {message}");
        }

        private string GetPortText()
        {
            return portInputField == null || string.IsNullOrWhiteSpace(portInputField.text)
                ? defaultPort.ToString()
                : portInputField.text.Trim();
        }
    }
}
