using System;
using Cysharp.Threading.Tasks;
using Hyunwoo.CoreLib.ModuleSystem;
using UnityEngine;

namespace Hyunwoo.HumanBodySystem
{
    public class HeartModule : MonoModule
    {
        [Header("1회 혈액 생성량")]
        [SerializeField] private float defaultGetBloodValue = 5f;

        [Header("혈액 생성 간격(초)")]
        [SerializeField] private float delaySeconds = 2;

        [Header("심장 혈액 저장 최대치")]
        [SerializeField] private float maxGauge;

        [Header("게임 시작 시 심장 혈액량")]
        [SerializeField] private float defaultBloodGague;

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
                
                _bloodGauge = Mathf.Clamp(value, 0, maxGauge);
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
        private float _getVisceraBloodValue;
        private float _reduceBloodValue;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _getBloodValue = defaultGetBloodValue;
            BloodGauge = defaultBloodGague;
        }

        private void Start()
        {
            TestAsync().Forget();
        }
        
        // UniTask로 왜 썻는지
        private async UniTask TestAsync()
        {
            _reduceBloodValue = BloodGauge >= 0.1f ? _getBloodValue * ConnectCount : 0;
            BloodGauge -= _reduceBloodValue;
            
            _getVisceraBloodValue = BloodGauge >= 0.1f ? defaultGetBloodValue : 0;
            OnHeartRate?.Invoke(_getVisceraBloodValue);

            BloodGauge += _getBloodValue;
            
            await UniTask.Delay((int)(delaySeconds * 1000), cancellationToken: destroyCancellationToken);
            
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
