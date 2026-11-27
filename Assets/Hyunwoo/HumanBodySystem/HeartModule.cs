using System;
using System.Collections.Generic;
using System.Linq;
using Hyunwoo.CoreLib.ModuleSystem;
using UnityEngine;

namespace Hyunwoo.HumanBodySystem
{
    public class HeartModule : MonoModule
    {
        [InspectorName("박동당 총 공급량"), SerializeField, Min(0f)] private float defaultGetBloodValue = 5f;
        [InspectorName("박동 간격 (초)"), SerializeField, Min(0.05f)] private float delaySeconds = 2f;

        private readonly HashSet<AbstractViscera> _organs = new();
        private float _elapsed;
        public event Action Beat;
        public bool CanReceiveBloodPack => _organs.Any(organ => organ.BloodGauge < organ.Capacity);

        internal bool IsConnected(AbstractViscera organ) => _organs.Contains(organ);
        internal void Connect(AbstractViscera organ) => _organs.Add(organ);
        internal void Disconnect(AbstractViscera organ) => _organs.Remove(organ);

        private void LateUpdate()
        {
            float interval = Mathf.Max(0.05f, delaySeconds);
            _elapsed += Time.deltaTime;
            while (_elapsed >= interval)
            {
                _elapsed -= interval;
                DistributeBlood(defaultGetBloodValue);
                Beat?.Invoke();
            }
        }

        public void DistributeBlood(float total)
        {
            if (_organs.Count == 0) return;

            float share = total / _organs.Count;
            foreach (var organ in _organs)
                organ.ReceiveFromHeart(share);
        }
    }
}
