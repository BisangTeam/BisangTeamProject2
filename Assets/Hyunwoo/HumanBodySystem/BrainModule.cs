using UnityEngine;

namespace Hyunwoo.HumanBodySystem
{
    public class BrainModule : AbstractViscera
    {
        [SerializeField] private MineMapEvent miniMap;
        protected override void WorkFunction(int index, float abillityValue)
        {
            miniMap.UpdateMineMap(abillityValue);
        }
    }

}
