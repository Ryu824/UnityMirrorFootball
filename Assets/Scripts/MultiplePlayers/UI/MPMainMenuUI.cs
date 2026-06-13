using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MultiplePlayers
{
    public class MPMainMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private CanvasGroup mainMenuCanvasGroup;
        [SerializeField] private GameObject networkLaunchPanel;
        [SerializeField] private CanvasGroup networkLaunchCanvasGroup;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private CanvasGroup settingsCanvasGroup;

        [Header("Buttons")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button settingsBackButton;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Dropdown qualityDropdown;

        [Header("Dependencies")]
        [SerializeField] private MPNetworkMenuUI networkMenuUI;

        private void Awake()
        {
            EnsureFallbackUi();
            EnsureSettingsControls();

            if (networkMenuUI == null)
            {
                networkMenuUI = GetComponent<MPNetworkMenuUI>();
            }

            BindButtons();
            BindSettingsControls();
        }

        private void Start()
        {
            SyncNetworkPanelRefs();
            SyncSettingsControls();
            ShowMainMenu();
        }

        private void OnDestroy()
        {
            UnbindButtons();
            UnbindSettingsControls();
        }

        public void ShowMainMenu()
        {
            SyncNetworkPanelRefs();
            MPUIVisibilityUtility.Show(mainMenuPanel, mainMenuCanvasGroup);
            MPUIVisibilityUtility.Hide(networkLaunchPanel, networkLaunchCanvasGroup);
            MPUIVisibilityUtility.Hide(settingsPanel, settingsCanvasGroup);
        }

        public void ShowNetworkLaunch()
        {
            SyncNetworkPanelRefs();
            MPUIVisibilityUtility.Hide(mainMenuPanel, mainMenuCanvasGroup);
            MPUIVisibilityUtility.Show(networkLaunchPanel, networkLaunchCanvasGroup);
            MPUIVisibilityUtility.Hide(settingsPanel, settingsCanvasGroup);
        }

        public void ShowSettings()
        {
            SyncSettingsControls();
            MPUIVisibilityUtility.Hide(mainMenuPanel, mainMenuCanvasGroup);
            MPUIVisibilityUtility.Hide(networkLaunchPanel, networkLaunchCanvasGroup);
            MPUIVisibilityUtility.Show(settingsPanel, settingsCanvasGroup);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void BindButtons()
        {
            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(ShowNetworkLaunch);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(ShowSettings);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(QuitGame);
            }

            if (settingsBackButton != null)
            {
                settingsBackButton.onClick.AddListener(ShowMainMenu);
            }
        }

        private void BindSettingsControls()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
            }

            if (qualityDropdown != null)
            {
                qualityDropdown.onValueChanged.AddListener(SetQualityLevel);
            }
        }

        private void UnbindButtons()
        {
            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveListener(ShowNetworkLaunch);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(ShowSettings);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(QuitGame);
            }

            if (settingsBackButton != null)
            {
                settingsBackButton.onClick.RemoveListener(ShowMainMenu);
            }
        }

        private void UnbindSettingsControls()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
            }

            if (qualityDropdown != null)
            {
                qualityDropdown.onValueChanged.RemoveListener(SetQualityLevel);
            }
        }

        private void EnsureFallbackUi()
        {
            if (mainMenuPanel != null && settingsPanel != null)
            {
                return;
            }

            MPRuntimeUIFactory.EnsureEventSystem();
            Canvas canvas = MPRuntimeUIFactory.EnsureOverlayCanvas("MainMenuCanvas");

            if (mainMenuPanel == null || mainMenuCanvasGroup == null)
            {
                GameObject panel = MPRuntimeUIFactory.CreatePanel(
                    canvas.transform,
                    "MainMenuPanel",
                    new Color(0.08f, 0.11f, 0.15f, 0.94f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(-260f, -220f),
                    new Vector2(260f, 220f),
                    out CanvasGroup canvasGroup);

                mainMenuPanel = panel;
                mainMenuCanvasGroup = canvasGroup;

                RectTransform layout = MPRuntimeUIFactory.CreateVerticalContainer(
                    panel.transform,
                    "Layout",
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(24f, 24f),
                    new Vector2(-24f, -24f),
                    16f);

                MPRuntimeUIFactory.CreateText(
                    layout,
                    "Title",
                    "Football MVP",
                    34,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero);

                startGameButton = MPRuntimeUIFactory.CreateButton(
                    layout,
                    "StartGameButton",
                    "Start Game",
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero);
                MPRuntimeUIFactory.AddFixedHeight(startGameButton, 58f);

                settingsButton = MPRuntimeUIFactory.CreateButton(
                    layout,
                    "SettingsButton",
                    "Settings",
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero);
                MPRuntimeUIFactory.AddFixedHeight(settingsButton, 58f);

                quitButton = MPRuntimeUIFactory.CreateButton(
                    layout,
                    "QuitButton",
                    "Quit",
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero,
                    new Color(0.35f, 0.18f, 0.18f, 0.95f));
                MPRuntimeUIFactory.AddFixedHeight(quitButton, 58f);
            }

            if (settingsPanel == null || settingsCanvasGroup == null)
            {
                GameObject panel = MPRuntimeUIFactory.CreatePanel(
                    canvas.transform,
                    "SettingsPanel",
                    new Color(0.08f, 0.11f, 0.15f, 0.94f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(-280f, -200f),
                    new Vector2(280f, 200f),
                    out CanvasGroup canvasGroup);

                settingsPanel = panel;
                settingsCanvasGroup = canvasGroup;
            }
        }

        private void EnsureSettingsControls()
        {
            if (settingsPanel == null)
            {
                return;
            }

            if (masterVolumeSlider != null && fullscreenToggle != null && qualityDropdown != null)
            {
                return;
            }

            Transform existingLayout = settingsPanel.transform.Find("SettingsRuntimeLayout");
            RectTransform layout =
                existingLayout as RectTransform ??
                MPRuntimeUIFactory.CreateVerticalContainer(
                    settingsPanel.transform,
                    "SettingsRuntimeLayout",
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(24f, 24f),
                    new Vector2(-24f, -24f),
                    16f);

            if (existingLayout == null)
            {
                MPRuntimeUIFactory.CreateText(
                    layout,
                    "Title",
                    "Settings",
                    32,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero);
            }

            if (masterVolumeSlider == null)
            {
                MPRuntimeUIFactory.CreateText(
                    layout,
                    "VolumeLabel",
                    "Master Volume",
                    22,
                    TextAnchor.MiddleLeft,
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero);

                masterVolumeSlider = MPRuntimeUIFactory.CreateSlider(
                    layout,
                    "MasterVolumeSlider",
                    0f,
                    1f,
                    AudioListener.volume,
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero);
                MPRuntimeUIFactory.AddFixedHeight(masterVolumeSlider, 36f);
            }

            if (fullscreenToggle == null)
            {
                fullscreenToggle = MPRuntimeUIFactory.CreateToggle(
                    layout,
                    "FullscreenToggle",
                    "Fullscreen",
                    Screen.fullScreen,
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero);
                MPRuntimeUIFactory.AddFixedHeight(fullscreenToggle, 42f);
            }

            if (qualityDropdown == null)
            {
                qualityDropdown = MPRuntimeUIFactory.CreateDropdown(
                    layout,
                    "QualityDropdown",
                    QualitySettings.names,
                    QualitySettings.GetQualityLevel(),
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero);
                MPRuntimeUIFactory.AddFixedHeight(qualityDropdown, 48f);
            }

            if (settingsBackButton == null)
            {
                settingsBackButton = MPRuntimeUIFactory.CreateButton(
                    layout,
                    "BackButton",
                    "Back",
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero,
                    new Color(0.25f, 0.25f, 0.25f, 0.95f));
                MPRuntimeUIFactory.AddFixedHeight(settingsBackButton, 54f);
            }
        }

        private void SyncNetworkPanelRefs()
        {
            if (networkMenuUI == null)
            {
                networkMenuUI = GetComponent<MPNetworkMenuUI>();
            }

            if (networkMenuUI == null)
            {
                return;
            }

            GameObject resolvedPanel = networkMenuUI.PanelRoot;
            CanvasGroup resolvedCanvasGroup = networkMenuUI.PanelCanvasGroup;

            if (resolvedPanel != null && resolvedPanel != networkLaunchPanel)
            {
                HideLegacyNetworkPanel(networkLaunchPanel, networkLaunchCanvasGroup);
                networkLaunchPanel = resolvedPanel;
                networkLaunchCanvasGroup = resolvedCanvasGroup;
            }

            if (networkLaunchPanel == null)
            {
                networkLaunchPanel = resolvedPanel;
            }

            if (networkLaunchCanvasGroup == null)
            {
                networkLaunchCanvasGroup = resolvedCanvasGroup;
            }
        }

        private void HideLegacyNetworkPanel(GameObject panel, CanvasGroup canvasGroup)
        {
            if (panel == null)
            {
                return;
            }

            MPUIVisibilityUtility.Hide(panel, canvasGroup);

            Canvas parentCanvas = panel.GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.gameObject != panel)
            {
                parentCanvas.gameObject.SetActive(false);
            }
        }

        private void SyncSettingsControls()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(AudioListener.volume);
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
            }

            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
                qualityDropdown.SetValueWithoutNotify(QualitySettings.GetQualityLevel());
                qualityDropdown.RefreshShownValue();
            }
        }

        private void SetMasterVolume(float volume)
        {
            AudioListener.volume = Mathf.Clamp01(volume);
        }

        private void SetFullscreen(bool fullscreen)
        {
            Screen.fullScreen = fullscreen;
        }

        private void SetQualityLevel(int qualityLevel)
        {
            if (qualityLevel < 0 || qualityLevel >= QualitySettings.names.Length)
            {
                return;
            }

            QualitySettings.SetQualityLevel(qualityLevel, true);
        }
    }
}
