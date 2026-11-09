using System;
using System.Reflection;
using UnityEngine;

namespace Hyunwoo.HumanBodySystem
{
    public abstract class AbstractHumanBody : MonoBehaviour
    {
        private float _bloodGauge;
        
        public event Action OnBloodGaugeChanged; 
        
        public float BloodGauge
        {
            get => _bloodGauge;
            set
            {
                float before = _bloodGauge;
                _bloodGauge = Mathf.Clamp(value, 0f, 100f); 
                
                if (!Mathf.Approximately(_bloodGauge, before))
                    OnBloodGaugeChanged?.Invoke();
            }
        } 

        public abstract void ConnectionBlood();

        public abstract void DisConnectionBlood();
    }
}