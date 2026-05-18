using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class BoundaryReporter : MonoBehaviour
{
    [Tooltip("在这个Inspector里，给这个边界选择正确的类型")]
    public BoundaryType boundaryType;

    private void Start()
    {
        // 强制确保这些判定边界是Trigger
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 只检测带有 Ball 标签的物体
        if (!other.CompareTag("Ball")) return;

        // 只有在比赛进行中才向裁判报告（防止死球状态下反复触发）
        if (RuleManager.Instance != null && RuleManager.Instance.CurrentState == MatchState.Playing)
        {
            RuleManager.Instance.OnBoundaryTriggered(boundaryType);
        }
    }
}
