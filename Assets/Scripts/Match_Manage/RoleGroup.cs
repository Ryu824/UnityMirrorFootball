using UnityEngine;

public class RoleGroup : MonoBehaviour
{
    [Header("战术参数 (直接在此调整)")]
    [Tooltip("基础站位偏移")]
    public Vector3 homePositionOffset = new Vector3(0, 0, 5f);

    [Tooltip("进攻压上距离：无球进攻时保持在球后多少米")]
    public float pushUpDistance = 5f;

    [Tooltip("射门距离：进入该距离尝试射门")]
    public float shootDistance = 15f;

    [Tooltip("射门力度：数值越大球速越快 (建议 15-30)")]
    public float shootForce = 20f; // 【新增】就是这一行解决了报错

    [Header("跑位范围")]
    public BoxCollider activityZone;

    // 编辑器可视化辅助
    private void Reset()
    {
        // 自动添加碰撞体
        if (activityZone == null)
        {
            activityZone = GetComponent<BoxCollider>();
            if (activityZone == null)
            {
                activityZone = gameObject.AddComponent<BoxCollider>();
                activityZone.isTrigger = true; // 通常作为触发器使用
                activityZone.center = new Vector3(0, 0, 10f);
                activityZone.size = new Vector3(20f, 5f, 20f);
            }
        }
    }
}
