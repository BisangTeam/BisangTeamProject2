using UnityEngine;

namespace Hyunwoo.HumanBodySystem
{
    public sealed class HandModule : AbstractViscera
    {
        [SerializeField, Min(0f), InspectorName("손자국 1회 혈액 소모량")] private float bloodPerMark = 5f;
        private bool _markAbilityEnabled;

        public bool CanMark => _markAbilityEnabled && BloodGauge >= bloodPerMark;

        public void SpendBloodForMark()
        {
            BloodGauge -= bloodPerMark;
        }

        protected override void WorkFunction(int index, float abilityValue)
        {
            _markAbilityEnabled = abilityValue > 0f;
        }
    }
}
