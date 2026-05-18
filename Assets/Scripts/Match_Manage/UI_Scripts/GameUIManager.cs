using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [Header("UI 面板引用")]
    public GameObject panelSettings;
    public GameObject panelHUD;
    public GameObject panelEnd;

    [Header("HUD 文本引用")]
    public Text textTime;
    public Text textScore;

    void Update()
    {
        // 如果裁判还没初始化，啥也不干
        if (RuleManager.Instance == null) return;

        MatchState state = RuleManager.Instance.CurrentState;

        // 1. 根据状态显隐面板
        panelSettings.SetActive(state == MatchState.PreMatch);
        panelHUD.SetActive(state == MatchState.Playing || state == MatchState.DeadBall || state == MatchState.GoalScored);
        panelEnd.SetActive(state == MatchState.Ended);

        // 2. 实时更新 HUD 数据
        if (panelHUD.activeSelf)
        {
            textTime.text = RuleManager.Instance.DisplayTime;
            textScore.text = $"{RuleManager.Instance.leftTeamScore}  -  {RuleManager.Instance.rightTeamScore}";
        }
    }
}
