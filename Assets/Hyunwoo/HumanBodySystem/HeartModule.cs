using System;
using Cysharp.Threading.Tasks;
using Hyunwoo.CoreLib.ModuleSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hyunwoo.HumanBodySystem
{
    public class HeartModule : MonoBehaviour, IModule
    {
        [Header("Settings/HeartModule")]
        [field: SerializeField] public float DelaySeconds { get; set; } = 2;
        [field: SerializeField] public float DefaultGetBloodValue { get; set; } = 5f;
        
        [Header("Compo")]
        [SerializeField] private HeartGauge heartGauge;

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
                    _getBloodValue = DefaultGetBloodValue;
                else
                    _getBloodValue = DefaultGetBloodValue / _connectCount;
            } 
        }
        
        public event Action<float> OnHeartRate;
        
        private float _getBloodValue;
        
        public void Initialize(ModuleOwner owner)
        {
            _getBloodValue = DefaultGetBloodValue;
        }

        public void Work()
        {
        }

        private void Start()
        {
            TestAsync().Forget();
        }
        
        // Unitask로 왜 썻는지
        private async UniTask TestAsync()
        {
            BloodGauge += _getBloodValue;
            OnHeartRate?.Invoke(_getBloodValue);
            //_bloodGauge -= _getBloodValue * ConnectCount;
            BloodGauge -= _getBloodValue * ConnectCount;
            
            await UniTask.Delay((int)(DelaySeconds * 1000));
            
            TestAsync().Forget();
        }


        private void ChangeHeartRate(float value)
        {
            
        }
        
        #region Test
        [Header("Test")]
        [SerializeField] private EyeModule eyeModule;
        [SerializeField] private EarModule earModule;
        public void Update()
        {
            if (Keyboard.current.qKey.wasPressedThisFrame)
                eyeModule.ConnectionBlood();
            
            if (Keyboard.current.aKey.wasPressedThisFrame)
                eyeModule.DisConnectionBlood();
            
            if (Keyboard.current.wKey.wasPressedThisFrame)
                earModule.ConnectionBlood();
            
            if (Keyboard.current.sKey.wasPressedThisFrame)
                earModule.DisConnectionBlood();
        }
        #endregion
    }
}