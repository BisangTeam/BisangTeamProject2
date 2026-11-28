using System;
using System.Collections.Generic;
using System.Linq;
using Hyunwoo.CoreLib.ModuleSystem;
using UnityEngine;

namespace Hyunwoo.HumanBodySystem
{
    [DefaultExecutionOrder(-10)] // 장기가 혈액을 소모한 뒤, 혈관과 게이지를 그리기 전에 공급한다.
    public class HeartModule : MonoModule
    {
        [InspectorName("박동당 총 공급량"), SerializeField, Min(0f)] private float defaultGetBloodValue = 5f;
        [InspectorName("Heartbeat가 없을 때 박동 간격 (초)"), SerializeField, Min(0.05f)] private float delaySeconds = 2f;

        private readonly HashSet<AbstractViscera> _organs = new();
        private Heartbeat _heartbeat;
        internal double ElapsedBeats { get; private set; }
        public event Action Beat;
        public bool CanReceiveBloodPack => _organs.Any(organ => organ.BloodGauge < organ.Capacity);

        internal bool IsConnected(AbstractViscera organ) => _organs.Contains(organ);
        internal void Connect(AbstractViscera organ) => _organs.Add(organ);
        internal void Disconnect(AbstractViscera organ) => _organs.Remove(organ);

        private void Awake() => _heartbeat = GetComponent<Heartbeat>();

        private void LateUpdate()
        {
            float advance;
            float pumped;
            float phase;
            int beats;
            if (_heartbeat != null)
            {
                if (!_heartbeat.isActiveAndEnabled) return;
                advance = _heartbeat.BeatAdvance;
                beats = _heartbeat.StrongBeatsThisFrame;
                phase = _heartbeat.PhaseSinceStrongBeat;
                pumped = PumpedAt(phase) - PumpedAt(phase - advance);
            }
            else
            {
                advance = Time.deltaTime / Mathf.Max(0.05f, delaySeconds);
                double next = ElapsedBeats + advance;
                beats = (int)(Math.Floor(next) - Math.Floor(ElapsedBeats));
                phase = (float)(next - Math.Floor(next));
                pumped = advance;
            }

            ElapsedBeats += advance;
            double? pulseStart = beats > 0 ? ElapsedBeats - phase : null;
            DistributeBlood(defaultGetBloodValue * pumped, pulseStart);
            for (int i = 0; i < beats; i++) Beat?.Invoke();
        }

        private static float PumpedAt(float phase)
        {
            // 한 박자 분량 중 60%는 큰 박동 직후에, 나머지 40%는 박자 전체에 걸쳐 보낸다.
            float cycles = Mathf.Floor(phase);
            float fraction = phase - cycles;
            return cycles + 0.4f * fraction + 0.6f * Mathf.SmoothStep(0f, 1f, fraction / 0.35f);
        }

        public void DistributeBlood(float total) => DistributeBlood(total, ElapsedBeats);

        private void DistributeBlood(float total, double? pulseStart)
        {
            if (total <= 0f || _organs.Count == 0) return;

            float share = total / _organs.Count;
            foreach (var organ in _organs)
                organ.ReceiveFromHeart(share, pulseStart);
        }
    }
}
