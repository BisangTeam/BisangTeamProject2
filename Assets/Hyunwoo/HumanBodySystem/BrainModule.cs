using UnityEngine;

namespace Hyunwoo.HumanBodySystem
{
    public class BrainModule : AbstractViscera
    {
        [SerializeField] private MineMapEvent miniMap;
        [SerializeField] private GameObject miniMapDisplay;

        [Header("미니맵 꺼지는 수치")]
        [SerializeField] private float wantGauge = 0f;

        private void OnEnable()
        {
            OnBloodGaugeChanged += RefreshMapVisibility;
            RefreshMapVisibility();
        }

        private void OnDisable() => OnBloodGaugeChanged -= RefreshMapVisibility;

        private void RefreshMapVisibility()
        {
            bool hasBlood = BloodGauge > wantGauge;
            if (miniMapDisplay != null && miniMapDisplay.activeSelf != hasBlood)
                miniMapDisplay.SetActive(hasBlood);
        }

        protected override void WorkFunction(int index, float abillityValue)
        {
            miniMap.UpdateMineMap(abillityValue);
        }
    }

}
