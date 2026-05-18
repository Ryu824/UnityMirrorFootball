using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("体力设置")]
    [Tooltip("最大体力值")]
    public float maxStamina = 100f;
    [Tooltip("冲刺每秒消耗的体力")]
    public float sprintCostRate = 20f;
    [Tooltip("行走时每秒恢复的体力")]
    public float walkRegenRate = 10f;
    [Tooltip("静止时每秒恢复的体力")]
    public float idleRegenRate = 25f;

    // 当前体力值（公共只读属性）
    public float CurrentStamina { get; private set; }
    public float StaminaPercentage => CurrentStamina / maxStamina;

    private void Awake()
    {
        CurrentStamina = maxStamina;
    }

    /// <summary>
    /// 尝试消耗体力用于冲刺
    /// </summary>
    /// <param name="amount">消耗量</param>
    /// <returns>如果体力足够且消耗成功返回 true，否则返回 false</returns>
    public bool TryUseStamina(float amount)
    {
        if (CurrentStamina >= amount)
        {
            CurrentStamina -= amount;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 恢复体力
    /// </summary>
    /// <param name="amount">恢复量</param>
    public void RecoverStamina(float amount)
    {
        CurrentStamina = Mathf.Min(CurrentStamina + amount, maxStamina);
    }

    // 辅助方法：根据状态自动处理体力逻辑
    public void HandleStamina(bool isSprinting, bool isMoving)
    {
        if (isSprinting)
        {
            // 冲刺时消耗体力（由Controller调用TryUseStamina处理，这里不做自动消耗，防止逻辑冲突）
        }
        else
        {
            // 非冲刺状态下恢复体力
            float regenRate = isMoving ? walkRegenRate : idleRegenRate;
            RecoverStamina(regenRate * Time.deltaTime);
        }
    }
}
