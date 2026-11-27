using System;
using UnityEngine;
using IBloodReservoir = Hyunwoo.HumanBodySystem.IBloodReservoir;

namespace Hyunwoo.HumanBodySystem
{
    public abstract class AbstractHumanBody : MonoBehaviour, IBloodReservoir
    {
        [Header("기관별 혈액 최대치")]
        [SerializeField, Min(0.01f)] private float maxGauge = 100;

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

        public float Capacity => maxGauge;
        public float BloodGaugeRatio => maxGauge > 0f ? BloodGauge / maxGauge : 0f;

        public abstract void ConnectionBlood();

        public abstract void DisConnectionBlood();
    }
}