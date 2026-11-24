using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Hyunwoo.CoreLib;
using Hyunwoo.CoreLib.ModuleSystem;
using UnityEngine;

namespace Hyunwoo.HumanBodySystem
{
    [Serializable]
    public struct GaugeStruct
    {
        public float needBloodGauge;
        public float abillityValue;
    }

    public abstract class AbstractViscera : AbstractHumanBody, IModule, IAfterInitModule
    {
        [Header("게임 시작 시 기관별 혈액량")]
        [SerializeField] private float defaultBloodGague;

        [Header("1회 혈액 소모량")]
        [SerializeField] protected float reduceGauge = 1f;

        [Header("혈액 소모 간격(초)")]
        [SerializeField] protected float updateTime = 0.5f;
        
        [Header("기관별 능력 단계 수 && 각 단계에 필요한 최소 혈액량")]
        [SerializeField] protected List<GaugeStruct> wantGaugeList; 

        [Header("Components")]
        [SerializeField] private JanggiGuage gaugeUI;
        
        protected ModuleOwner _owner;
        protected HeartModule _heartModule;

        public void InsertBlood(float value)
        {
            BloodGauge += value;
        }

        public void PullOut(float value, out float blood)
        {
            blood = Mathf.Clamp(BloodGauge - value, 0, BloodGauge);
            
            BloodGauge = blood;
        }
        
        private void Start()
        {
            UpdateReduceGauge().Forget();
        }
        
        private async UniTask UpdateReduceGauge()
        {
            ReduceGauge();
            await UniTask.Delay((int)(updateTime * 1000), cancellationToken: destroyCancellationToken);
            UpdateReduceGauge().Forget();
        }

        private void ReduceGauge()
        {
            BloodGauge -= reduceGauge;
            gaugeUI.Gauge = BloodGauge;
        }

        private void OnHandleFillGauge(float value)
        {
            BloodGauge += value;
            gaugeUI.Gauge = BloodGauge;
            Debug.Log($"{gameObject.name}: Gauge: {BloodGauge}");
        }
        
        #region Implementation
        
        public void Work()
        {
            for (int i = 0; i < wantGaugeList.Count; i++)
            {
                if (BloodGauge <= wantGaugeList[i].needBloodGauge)
                {
                    WorkFunction(i, wantGaugeList[i].abillityValue);
                    return;
                }
            }
            
            // 리스트에 있는 값보다 높은 값이 들어왔을때 예외 처리 => 최현우가 작성
            WorkFunction(wantGaugeList.Count, wantGaugeList[wantGaugeList.Count].abillityValue);  
        }
        
        protected abstract void WorkFunction(int index, float abillityValue);

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
        }
        
        public void AfterInit()
        {
            _heartModule = _owner.GetModule<HeartModule>();
        }

        public override void ConnectionBlood()
        {
            _heartModule.OnHeartRate += OnHandleFillGauge;
            _heartModule.ConnectCount++;
        }

        public override void DisConnectionBlood()
        {
            _heartModule.OnHeartRate -= OnHandleFillGauge;            
            _heartModule.ConnectCount--;
        }
        #endregion
    }
}
