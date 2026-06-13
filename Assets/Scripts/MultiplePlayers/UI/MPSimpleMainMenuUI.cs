using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MultiplePlayers
{
    public class MPSimpleMainMenuUI : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string gameSceneName = "MultiplePlayers";

        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Main Buttons")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Settings Controls")]
        [SerializeField] private Button backButton;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Dropdown qualityDropdown;

        private bool isLoading;

        private void Awake()
        {
            Bind();
            SyncSettingsControls();
        }

        private void Start()
        {
            ShowMainPanel();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void StartGame()
        {
            if (isLoading)
            {
                return;
            }

            isLoading = true;
            SetMainButtonsInteractable(false);
            SceneManager.LoadScene(gameSceneName);
        }

        public void ShowSettingsPanel()
        {
            SyncSettingsControls();
            SetPanelVisible(mainPanel, false);
            SetPanelVisible(settingsPanel, true);
        }

        public void ShowMainPanel()
        {
            SetPanelVisible(settingsPanel, false);
            SetPanelVisible(mainPanel, true);
            SetMainButtonsInteractable(true);
            isLoading = false;
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Bind()
        {
            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(StartGame);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(ShowSettingsPanel);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(QuitGame);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(ShowMainPanel);
            }

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

        private void Unbind()
        {
            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveListener(StartGame);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(ShowSettingsPanel);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(QuitGame);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(ShowMainPanel);
            }

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
                qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
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

        private void SetMainButtonsInteractable(bool interactable)
        {
            if (startGameButton != null)
            {
                startGameButton.interactable = interactable;
            }

            if (settingsButton != null)
            {
                settingsButton.interactable = interactable;
            }

            if (quitButton != null)
            {
                quitButton.interactable = interactable;
            }
        }

        private static void SetPanelVisible(GameObject panel, bool visible)
        {
            if (panel == null)
            {
                return;
            }

            panel.SetActive(visible);
        }
    }
}
