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

        private void Start()
        {
            redGoalkeeperButton.onClick.AddListener(() =>
                Select(MPTeamId.Red, MPPlayerPosition.Goalkeeper));

            redDefenderButton.onClick.AddListener(() =>
                Select(MPTeamId.Red, MPPlayerPosition.Defender));

            redMidfielderButton.onClick.AddListener(() =>
                Select(MPTeamId.Red, MPPlayerPosition.Midfielder));

            redForwardButton.onClick.AddListener(() =>
                Select(MPTeamId.Red, MPPlayerPosition.Forward));

            blueGoalkeeperButton.onClick.AddListener(() =>
                Select(MPTeamId.Blue, MPPlayerPosition.Goalkeeper));

            blueDefenderButton.onClick.AddListener(() =>
                Select(MPTeamId.Blue, MPPlayerPosition.Defender));

            blueMidfielderButton.onClick.AddListener(() =>
                Select(MPTeamId.Blue, MPPlayerPosition.Midfielder));

            blueForwardButton.onClick.AddListener(() =>
                Select(MPTeamId.Blue, MPPlayerPosition.Forward));
        }

        private void Update()
        {
            TryFindLocalPlayer();

            bool show = localTeamState != null &&
                        MPGameSession.Instance != null &&
                        MPGameSession.Instance.IsLobby;

            if (panelRoot != null)
                panelRoot.SetActive(show);

            if (localTeamState != null && selectionText != null)
            {
                selectionText.text =
                    $"当前选择：{localTeamState.TeamId} / {localTeamState.Position}";
            }
        }

        private void TryFindLocalPlayer()
        {
            if (localTeamState != null)
                return;

            if (NetworkClient.localPlayer == null)
                return;

            localTeamState =
                NetworkClient.localPlayer.GetComponent<MPPlayerTeamState>();
        }

        private void Select(MPTeamId team, MPPlayerPosition position)
        {
            if (localTeamState == null)
                TryFindLocalPlayer();

            if (localTeamState == null)
                return;

            localTeamState.CmdRequestSelectTeam(team, position);
        }
    }
}