using UnityEngine;

public class TeamMaterial : MonoBehaviour
{
    public enum Team
    {
        TeamA, // 红
        TeamB  // 蓝
    }

    [Header("队伍设置")]
    public Team team = Team.TeamA;

    [Header("渲染器（SkinnedMeshRenderer 或 MeshRenderer）")]
    public Renderer npcRenderer;

    [Header("要替换的材质槽索引")]
    [Tooltip("从 0 开始，对应 Materials 里的第几个槽")]
    public int materialIndex = 0;

    [Header("队伍材质")]
    public Material redMaterial;  // 拖 NPC_Mat_Red 进来
    public Material blueMaterial; // 拖 NPC_Mat_Blue 进来

    private Material[] _baseMaterials; // 缓存原始材质列表

    void Awake()
    {
        if (npcRenderer == null)
            npcRenderer = GetComponentInChildren<Renderer>();

        // 记住当前所有材质槽（用 sharedMaterials 读原始资源）
        _baseMaterials = npcRenderer.sharedMaterials;
    }

    void Start()
    {
        ApplyTeamMaterial();
    }

    // 外部切换队伍时调用
    public void SetTeam(Team newTeam)
    {
        team = newTeam;
        ApplyTeamMaterial();
    }

    void ApplyTeamMaterial()
    {
        if (npcRenderer == null) return;
        if (materialIndex < 0 || materialIndex >= _baseMaterials.Length)
        {
            Debug.LogError($"materialIndex {materialIndex} 超出范围，当前材质槽数量 {_baseMaterials.Length}", this);
            return;
        }
        if (redMaterial == null || blueMaterial == null)
        {
            Debug.LogError("队伍材质未设置，请在 Inspector 中拖入 Red/Blue Material", this);
            return;
        }

        // 根据队伍选择目标材质
        Material teamMat = team == Team.TeamA ? redMaterial : blueMaterial;

        // 新建一个材质数组，只在指定索引替换
        Material[] newMats = new Material[_baseMaterials.Length];
        for (int i = 0; i < _baseMaterials.Length; i++)
        {
            newMats[i] = (i == materialIndex) ? teamMat : _baseMaterials[i];
        }

        // 关键：赋值给 materials，会自动为这个 Renderer 实例化独立材质
        npcRenderer.materials = newMats;
    }
}
