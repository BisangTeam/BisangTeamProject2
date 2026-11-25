using System;
using UnityEngine;

namespace Hyunwoo.HumanBodySystem
{
    public abstract class AbstractHumanBody : MonoBehaviour
    {
        [Header("기관별 혈액 최대치")]
        [SerializeField] private float maxGauge = 100;

        private float _bloodGauge;
        
        public event Action OnBloodGaugeChanged; 
        
        public float BloodGauge
        {
            get => _bloodGauge;
            set
            {
                float before = _bloodGauge;
                _bloodGauge = Mathf.Clamp(value, 0f, maxGauge);

                if (!Mathf.Approximately(_bloodGauge, before))
                    OnBloodGaugeChanged?.Invoke();
            }
        }

        public float BloodGaugeRatio => BloodGauge / maxGauge;

        public abstract void ConnectionBlood();

        public abstract void DisConnectionBlood();
    }
}