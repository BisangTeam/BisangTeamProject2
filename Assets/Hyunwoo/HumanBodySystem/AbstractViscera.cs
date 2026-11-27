using System;
using System.Collections.Generic;
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
        [SerializeField, Min(0f)] protected float reduceGauge = 1f;

        [Header("혈액 소모 간격(초)")]
        [SerializeField, Min(0.05f)] protected float updateTime = 0.1f;
        
        [Header("기관별 능력 단계 수 && 각 단계에 필요한 최소 혈액량")]
        [SerializeField] protected List<GaugeStruct> wantGaugeList; 

        [Header("Components")]
        [SerializeField] private JanggiGuage gaugeUI;
        
        protected ModuleOwner _owner;
        protected HeartModule _heartModule;

        private float _elapsed;
        public bool ConnectedToHeart => _heartModule != null && _heartModule.IsConnected(this);
        public event Action BloodSupplied;

        public void InsertBlood(float value)
        {
            BloodGauge += value;
        }

        public void PullOut(float value, out float blood)
        {
            blood = Mathf.Clamp(value, 0f, BloodGauge);
            BloodGauge -= blood;
        }
        
        private void Start()
        {
            if (gaugeUI != null) OnBloodGaugeChanged += RefreshGauge;
            BloodGauge = defaultBloodGague;
            RefreshGauge();
        }
        
        private void Update()
        {
            if (reduceGauge <= 0f) return;
            float interval = Mathf.Max(0.05f, updateTime);
            _elapsed += Time.deltaTime;
            int ticks = Mathf.FloorToInt(_elapsed / interval);
            if (ticks == 0) return;
            _elapsed -= ticks * interval;
            BloodGauge -= ticks * reduceGauge;
        }

        private void RefreshGauge()
        {
            if (gaugeUI != null) gaugeUI.Gauge = 100f * BloodGaugeRatio;
        }

        internal void ReceiveFromHeart(float share)
        {
            if (share <= 0f || BloodGauge >= Capacity) return;
            BloodGauge += share;
            BloodSupplied?.Invoke();
        }

        #region Implementation
        
        public void Work()
        {
            if (wantGaugeList == null || wantGaugeList.Count == 0) return;
            for (int i = 0; i < wantGaugeList.Count; i++)
            {
                if (BloodGauge <= wantGaugeList[i].needBloodGauge)
                {
                    WorkFunction(i, wantGaugeList[i].abillityValue);
                    return;
                }
            }
            
            // 마지막 혈액 기준을 넘으면 가장 높은 단계의 능력을 사용한다.
            int last = wantGaugeList.Count - 1;
            WorkFunction(last, wantGaugeList[last].abillityValue);
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
            if (_heartModule != null) _heartModule.Connect(this);
        }

        public override void DisConnectionBlood()
        {
            if (_heartModule != null) _heartModule.Disconnect(this);
        }
        protected virtual void OnDestroy()
        {
            DisConnectionBlood();
            OnBloodGaugeChanged -= RefreshGauge;
        }
        #endregion
    }
}
