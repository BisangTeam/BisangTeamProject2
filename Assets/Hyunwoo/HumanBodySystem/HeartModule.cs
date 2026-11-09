using System;
using Cysharp.Threading.Tasks;
using Hyunwoo.CoreLib.ModuleSystem;
using UnityEngine;

namespace Hyunwoo.HumanBodySystem
{
    public class HeartModule : MonoModule
    {
        [Header("Settings")]
        [SerializeField] private float delaySeconds = 2;
        [SerializeField] private float defaultGetBloodValue = 5f;
        
        [Header("Compo")]
        [SerializeField] private HeartGauge heartGauge;

        #region Properties
        
        private float _bloodGauge;
        public float BloodGauge
        {
            get => _bloodGauge;
            set
            {
                // 여기서 왜 return을 했더라
                // bloodGauge가 차오르는걸 막기위해서?
                //if (ConnectCount >= 1) return;
                
                _bloodGauge = Mathf.Clamp(value, 0, 100);
                heartGauge.Gauge = BloodGauge;
            }
        }
        
        private int _connectCount;
        public int ConnectCount
        {
            get => _connectCount;
            set
            {
                _connectCount = value;
                if (_connectCount <= 0)
                    _getBloodValue = defaultGetBloodValue;
                else
                    _getBloodValue = defaultGetBloodValue / _connectCount;
            } 
        }
        
        #endregion  
        
        public event Action<float> OnHeartRate;
        
        private float _getBloodValue;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _getBloodValue = defaultGetBloodValue;
        }

        private void Start()
        {
            TestAsync().Forget();
        }
        
        // UniTask로 왜 썻는지
        private async UniTask TestAsync()
        {
            BloodGauge += _getBloodValue;
            OnHeartRate?.Invoke(_getBloodValue);

            BloodGauge -= _getBloodValue * ConnectCount;
            
            await UniTask.Delay((int)(delaySeconds * 1000));
            
            TestAsync().Forget();
        }

        public void InsertBlood(float value)
        {
            BloodGauge += value;
        }
        
        private void ChangeHeartRate(float value)
        {
            
        }
    }
}