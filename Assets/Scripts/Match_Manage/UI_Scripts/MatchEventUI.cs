using UnityEngine;
using UnityEngine.UI;

public class MatchEventUI : MonoBehaviour
{
    public Text centerEventText;
    private float displayTimer = 0f;

    // 这个方法会暴露在UnityEvent里，供RuleManager调用
    public void ShowMessage(string message)
    {
        centerEventText.text = message;
        centerEventText.gameObject.SetActive(true); // 显示
        displayTimer = 2.0f; // 显示2秒
    }

    void Update()
    {
        if (displayTimer > 0)
        {
            displayTimer -= Time.deltaTime;
            if (displayTimer <= 0)
            {
                centerEventText.gameObject.SetActive(false); // 时间到了隐藏
            }
        }
    }
}
