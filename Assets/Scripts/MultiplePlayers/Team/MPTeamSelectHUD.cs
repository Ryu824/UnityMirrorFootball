using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MultiplePlayers
{
    public class MPTeamSelectHUD : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Buttons")]
        [SerializeField] private Button redGoalkeeperButton;
        [SerializeField] private Button redDefenderButton;
        [SerializeField] private Button redMidfielderButton;
        [SerializeField] private Button redForwardButton;
        [SerializeField] private Button blueGoalkeeperButton;
        [SerializeField] private Button blueDefenderButton;
        [SerializeField] private Button blueMidfielderButton;
        [SerializeField] private Button blueForwardButton;

        [Header("Text")]
        [SerializeField] private TMP_Text selectionText;

        private MPPlayerTeamState localTeamState;
        private bool selectionLocked;

        private void Start()
        {
            BindButton(redGoalkeeperButton, MPTeamId.Red, MPPlayerPosition.Goalkeeper);
            BindButton(redDefenderButton, MPTeamId.Red, MPPlayerPosition.Defender);
            BindButton(redMidfielderButton, MPTeamId.Red, MPPlayerPosition.Midfielder);
            BindButton(redForwardButton, MPTeamId.Red, MPPlayerPosition.Forward);
            BindButton(blueGoalkeeperButton, MPTeamId.Blue, MPPlayerPosition.Goalkeeper);
            BindButton(blueDefenderButton, MPTeamId.Blue, MPPlayerPosition.Defender);
            BindButton(blueMidfielderButton, MPTeamId.Blue, MPPlayerPosition.Midfielder);
            BindButton(blueForwardButton, MPTeamId.Blue, MPPlayerPosition.Forward);
            SetButtonsInteractable(false);
        }

        private void OnDestroy()
        {
            UnbindLocalTeamState();
        }

        private void Update()
        {
            TryFindLocalPlayer();

            if (localTeamState != null && localTeamState.HasValidSelection)
            {
                selectionLocked = true;
            }

            bool show =
                !selectionLocked &&
                localTeamState != null &&
                MPGameSession.Instance != null &&
                MPGameSession.Instance.IsLobby;

            if (panelRoot != null)
            {
                panelRoot.SetActive(show);
            }

            SetButtonsInteractable(show);

            if (selectionText != null)
            {
                if (localTeamState == null)
                {
                    selectionText.text = "Start Host or Client first, then choose a team.";
                }
                else
                {
                    selectionText.text = localTeamState.HasValidSelection
                        ? $"Selected: {localTeamState.TeamId} / {localTeamState.Position}"
                        : "Choose a team and position.";
                }
            }
        }

        private void TryFindLocalPlayer()
        {
            if (localTeamState != null)
            {
                return;
            }

            if (NetworkClient.localPlayer == null)
            {
                return;
            }

            localTeamState =
                NetworkClient.localPlayer.GetComponent<MPPlayerTeamState>();

            if (localTeamState != null)
            {
                localTeamState.SelectionRequestResolved += OnSelectionRequestResolved;
            }
        }

        private void Select(MPTeamId team, MPPlayerPosition position)
        {
            if (selectionLocked)
            {
                return;
            }

            if (localTeamState == null)
            {
                TryFindLocalPlayer();
            }

            if (localTeamState == null)
            {
                if (selectionText != null)
                {
                    selectionText.text = "Waiting for local player...";
                }

                return;
            }

            localTeamState.CmdRequestSelectTeam(team, position);
        }

        private void OnSelectionRequestResolved(bool success, string message)
        {
            if (!success)
            {
                if (selectionText != null)
                {
                    selectionText.text = string.IsNullOrWhiteSpace(message)
                        ? "Selection rejected by server."
                        : message;
                }

                return;
            }

            selectionLocked = true;
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void BindButton(
            Button button,
            MPTeamId team,
            MPPlayerPosition position)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.AddListener(() => Select(team, position));
        }

        private void SetButtonsInteractable(bool interactable)
        {
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
                localTeamState = null;
            }
        }
    }
}
