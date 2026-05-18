using UnityEngine;
using UnityEngine.UI;

public class MatchSettingsUI : MonoBehaviour
{
    public InputField timeInputField;
    public Button startButton;

    void Start()
    {
        // 确保一开始是显示的
        gameObject.SetActive(true);
        startButton.onClick.AddListener(OnStartClicked);
    }

    void OnStartClicked()
    {
        // 读取输入的时间，如果无效默认120
        float time = 120f;
        if (float.TryParse(timeInputField.text, out float result))
        {
            time = result;
        }

        // 隐藏面板
        gameObject.SetActive(false);

        // 通知裁判开始比赛
        if (RuleManager.Instance != null)
        {
            RuleManager.Instance.StartMatch(time);
        }
    }
}
