using System;
using UnityEngine;
using UnityEngine.UI;

namespace MultiplePlayers
{
    public class MPTeamSelectPanel : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private Text statusText;

        [Header("Buttons")]
        [SerializeField] private Button redGoalkeeperButton;
        [SerializeField] private Button redDefenderButton;
        [SerializeField] private Button redMidfielderButton;
        [SerializeField] private Button redForwardButton;
        [SerializeField] private Button blueGoalkeeperButton;
        [SerializeField] private Button blueDefenderButton;
        [SerializeField] private Button blueMidfielderButton;
        [SerializeField] private Button blueForwardButton;

        private MPPlayerTeamState localTeamState;
        private bool requestPending;
        private bool selectionLocked;

        public GameObject PanelRoot => panelRoot;
        public CanvasGroup PanelCanvasGroup => panelCanvasGroup;
        public bool SelectionLocked => selectionLocked;

        public event Action SelectionConfirmed;

        private void Awake()
        {
            EnsureFallbackUi();
            BindButtons();
            SetStatus("Choose a team and position.");
        }

        private void OnDestroy()
        {
            UnbindButtons();
            UnbindLocalTeamState();
        }

        public void Initialize(MPPlayerTeamState teamState)
        {
            if (localTeamState == teamState)
            {
                RefreshButtonState();
                return;
            }

            UnbindLocalTeamState();

            localTeamState = teamState;
            requestPending = false;
            selectionLocked =
                localTeamState != null && localTeamState.HasValidSelection;

            if (localTeamState != null)
            {
                localTeamState.SelectionRequestResolved += OnSelectionRequestResolved;

                if (localTeamState.HasValidSelection)
                {
                    SetStatus(
                        $"Selected: {localTeamState.TeamId} / {localTeamState.Position}");
                }
            }

            RefreshButtonState();
        }

        public void Show()
        {
            MPUIVisibilityUtility.Show(panelRoot, panelCanvasGroup);
            RefreshButtonState();
        }

        public void Hide()
        {
            MPUIVisibilityUtility.Hide(panelRoot, panelCanvasGroup);
        }

        private void BindButtons()
        {
            BindButton(redGoalkeeperButton, MPTeamId.Red, MPPlayerPosition.Goalkeeper);
            BindButton(redDefenderButton, MPTeamId.Red, MPPlayerPosition.Defender);
            BindButton(redMidfielderButton, MPTeamId.Red, MPPlayerPosition.Midfielder);
            BindButton(redForwardButton, MPTeamId.Red, MPPlayerPosition.Forward);
            BindButton(blueGoalkeeperButton, MPTeamId.Blue, MPPlayerPosition.Goalkeeper);
            BindButton(blueDefenderButton, MPTeamId.Blue, MPPlayerPosition.Defender);
            BindButton(blueMidfielderButton, MPTeamId.Blue, MPPlayerPosition.Midfielder);
            BindButton(blueForwardButton, MPTeamId.Blue, MPPlayerPosition.Forward);
        }

        private void UnbindButtons()
        {
            UnbindButton(redGoalkeeperButton);
            UnbindButton(redDefenderButton);
            UnbindButton(redMidfielderButton);
            UnbindButton(redForwardButton);
            UnbindButton(blueGoalkeeperButton);
            UnbindButton(blueDefenderButton);
            UnbindButton(blueMidfielderButton);
            UnbindButton(blueForwardButton);
        }

        private void BindButton(
            Button button,
            MPTeamId teamId,
            MPPlayerPosition position)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.AddListener(() => OnClickSelection(teamId, position));
        }

        private void UnbindButton(Button button)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
        }

        private void OnClickSelection(MPTeamId teamId, MPPlayerPosition position)
        {
            if (selectionLocked)
            {
                return;
            }

            if (localTeamState == null)
            {
                SetStatus("Waiting for local player...");
                return;
            }

            requestPending = true;
            SetStatus($"Requesting {teamId} / {position}...");
            RefreshButtonState();
            localTeamState.CmdRequestSelectTeam(teamId, position);
        }

        private void OnSelectionRequestResolved(bool success, string message)
        {
            requestPending = false;

            if (success && localTeamState != null && localTeamState.HasValidSelection)
            {
                selectionLocked = true;
                SetStatus(string.IsNullOrWhiteSpace(message)
                    ? $"Selected: {localTeamState.TeamId} / {localTeamState.Position}"
                    : message);
                RefreshButtonState();
                SelectionConfirmed?.Invoke();
                return;
            }

            SetStatus(string.IsNullOrWhiteSpace(message)
                ? "Selection rejected by server."
                : message);
            RefreshButtonState();
        }

        private void RefreshButtonState()
        {
            bool interactable =
                !selectionLocked && !requestPending && localTeamState != null;

            SetInteractable(redGoalkeeperButton, interactable);
            SetInteractable(redDefenderButton, interactable);
            SetInteractable(redMidfielderButton, interactable);
            SetInteractable(redForwardButton, interactable);
            SetInteractable(blueGoalkeeperButton, interactable);
            SetInteractable(blueDefenderButton, interactable);
            SetInteractable(blueMidfielderButton, interactable);
            SetInteractable(blueForwardButton, interactable);
        }

        private static void SetInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        private void UnbindLocalTeamState()
        {
            if (localTeamState != null)
            {
                localTeamState.SelectionRequestResolved -= OnSelectionRequestResolved;
            }

            localTeamState = null;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
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
                "TeamSelectPanel",
                new Color(0.05f, 0.08f, 0.12f, 0.94f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-420f, -250f),
                new Vector2(420f, 250f),
                out CanvasGroup canvasGroup);

            panelRoot = panel;
            panelCanvasGroup = canvasGroup;

            MPRuntimeUIFactory.CreateText(
                panel.transform,
                "Title",
                "Choose Team and Position",
                30,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(24f, -64f),
                new Vector2(-24f, -16f));

            statusText = MPRuntimeUIFactory.CreateText(
                panel.transform,
                "StatusText",
                "Choose a team and position.",
                22,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(24f, 16f),
                new Vector2(-24f, 72f));

            GridLayoutGroup grid = MPRuntimeUIFactory.CreateGrid(
                panel.transform,
                "ButtonGrid",
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(24f, 84f),
                new Vector2(-24f, -84f),
                new Vector2(180f, 68f),
                new Vector2(12f, 12f),
                2);

            redGoalkeeperButton =
                CreateSelectionButton(grid.transform, "Red GK", new Color(0.7f, 0.16f, 0.16f));
            redDefenderButton =
                CreateSelectionButton(grid.transform, "Red DF", new Color(0.7f, 0.16f, 0.16f));
            redMidfielderButton =
                CreateSelectionButton(grid.transform, "Red MF", new Color(0.7f, 0.16f, 0.16f));
            redForwardButton =
                CreateSelectionButton(grid.transform, "Red FW", new Color(0.7f, 0.16f, 0.16f));
            blueGoalkeeperButton =
                CreateSelectionButton(grid.transform, "Blue GK", new Color(0.15f, 0.35f, 0.72f));
            blueDefenderButton =
                CreateSelectionButton(grid.transform, "Blue DF", new Color(0.15f, 0.35f, 0.72f));
            blueMidfielderButton =
                CreateSelectionButton(grid.transform, "Blue MF", new Color(0.15f, 0.35f, 0.72f));
            blueForwardButton =
                CreateSelectionButton(grid.transform, "Blue FW", new Color(0.15f, 0.35f, 0.72f));
        }

        private static Button CreateSelectionButton(
            Transform parent,
            string label,
            Color color)
        {
            Button button = MPRuntimeUIFactory.CreateButton(
                parent,
                label.Replace(" ", string.Empty) + "Button",
                label,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                color);

            LayoutElement element = button.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 68f;
            return button;
        }
    }
}
