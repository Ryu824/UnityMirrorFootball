using UnityEngine;

namespace MultiplePlayers
{
    public class MPAIFormationPoint : MonoBehaviour
    {
        [Header("Formation Identity")]
        public MPTeamId teamId;
        public MPPlayerPosition position;
        public int index;
    }
}
