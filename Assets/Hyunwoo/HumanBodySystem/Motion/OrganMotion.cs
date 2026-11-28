using System.Collections.Generic;
using UnityEngine;

namespace Hyunwoo.HumanBodySystem.Motion
{
    public struct OrganPose
    {
        /// <summary>장기 루트 기준 이동량.</summary>
        public Vector2 Offset;
        /// <summary>움직임 중심 기준 회전(도). 양수는 반시계 방향.</summary>
        public float Angle;
        /// <summary>움직임 중심 기준 크기 배율.</summary>
        public Vector2 Scale;

        public static OrganPose Rest => new OrganPose { Scale = Vector2.one };
    }

    /// <summary>
    /// 장기 그림에 혈액 상태를 따라가는 미세한 움직임을 준다.
    /// 장기 루트는 MedicalHover 떨림과 혈관 연결 판정이 쓰므로, 그 아래 Frame과 Mask만 함께 움직인다.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class OrganMotion : MonoBehaviour
    {
        [SerializeField] private AbstractViscera organ;
        [SerializeField, Tooltip("함께 움직일 그림. 비워 두면 자식 Frame과 Mask를 쓴다.")]
        private RectTransform[] parts;
        [SerializeField, Tooltip("움직임 중심 (그림 안 0~1)")]
        private Vector2 pivot = new(0.5f, 0.5f);

        [Header("혈액 반응")]
        [SerializeField, Min(1f), Tooltip("최대 반응 충전 속도 (초당 %)")]
        private float fullReactionFillRate = 35f;
        [SerializeField, Min(0f), Tooltip("심장에서 출발한 파동이 장기에 도착하는 데 걸리는 박자 수")]
        private float pulseArrivalDelay = 0.9f;

        private const float SettleSeconds = 0.25f;
        private const float RateSmoothing = 0.25f;
        private const float VitalitySmoothing = 0.35f;

        private Vector2[] _restPositions;
        private Quaternion[] _restRotations;
        private Vector3[] _restScales;
        private float _seed;
        private float _lastBlood;
        private float _rate;
        private float _settle;
        private readonly Queue<double> _pulseStarts = new();
        private float _lastPulse = float.NegativeInfinity;

        /// <summary>천천히 따라가는 혈액 비율. 0이면 비었고 1이면 가득 찼다.</summary>
        protected float Vitality { get; private set; }
        /// <summary>지금 혈액이 차오르는 정도. 0이면 그대로이거나 줄고, 1이면 최대 반응.</summary>
        protected float Filling { get; private set; }
        /// <summary>일시정지 중에는 멈추는 움직임 전용 시계.</summary>
        protected float Clock { get; private set; }
        /// <summary>마지막 혈관 박동이 장기에 닿은 뒤 지난 시간.</summary>
        protected float SincePulse => Clock - _lastPulse;
        protected virtual Vector2 DefaultPivot => new(0.5f, 0.5f);

        protected virtual void Reset()
        {
            organ = GetComponent<AbstractViscera>();
            pivot = DefaultPivot;
        }

        protected virtual void Awake()
        {
            if (organ == null) organ = GetComponent<AbstractViscera>();
            if (parts == null || parts.Length == 0)
                parts = new[] { transform.Find("Frame") as RectTransform, transform.Find("Mask") as RectTransform };

            _restPositions = new Vector2[parts.Length];
            _restRotations = new Quaternion[parts.Length];
            _restScales = new Vector3[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == null) continue;
                _restPositions[i] = parts[i].anchoredPosition;
                _restRotations[i] = parts[i].localRotation;
                _restScales[i] = parts[i].localScale;
            }
            _seed = Random.Range(0f, 100f);
        }

        protected virtual void OnEnable()
        {
            if (organ == null) return;
            organ.BloodSupplied += OnBloodSupplied;
            Vitality = organ.BloodGaugeRatio;
            _lastBlood = organ.BloodGauge;
            _rate = 0f;
            Filling = 0f;
            _settle = SettleSeconds;
        }

        protected virtual void OnDisable()
        {
            if (organ != null) organ.BloodSupplied -= OnBloodSupplied;
            _pulseStarts.Clear();
            _lastPulse = float.NegativeInfinity;
            Apply(OrganPose.Rest);
        }

        private void OnBloodSupplied()
        {
            _pulseStarts.Enqueue(organ.LastPulseBeat);
        }

        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            if (organ == null || deltaTime <= 0f) return;

            Clock += deltaTime;
            TrackBlood(deltaTime);
            if (!organ.ConnectedToHeart) _pulseStarts.Clear();
            while (_pulseStarts.Count > 0 && organ.HeartBeatsElapsed - _pulseStarts.Peek() >= pulseArrivalDelay)
            {
                _pulseStarts.Dequeue();
                _lastPulse = Clock;
            }
            Apply(Evaluate(deltaTime));
        }

        private void TrackBlood(float deltaTime)
        {
            float blood = organ.BloodGauge;
            float gained = blood - _lastBlood;
            _lastBlood = blood;
            // 시작 직후 기본값이나 저장 기록으로 한꺼번에 채워지는 혈액은 반응으로 치지 않는다.
            if (_settle > 0f)
            {
                _settle -= deltaTime;
                gained = 0f;
            }

            // 심장 공급과 혈액 소모가 엇갈려 들어오므로 순수 변화량을 부드럽게 만든 뒤 오르는 쪽만 본다.
            _rate += (gained / deltaTime - _rate) * Smooth(deltaTime, RateSmoothing);
            float percentPerSecond = Mathf.Max(0f, _rate) * 100f / organ.Capacity;
            Filling = Mathf.Clamp01(percentPerSecond / fullReactionFillRate);
            Vitality = Mathf.Lerp(Vitality, organ.BloodGaugeRatio, Smooth(deltaTime, VitalitySmoothing));
        }

        private void Apply(OrganPose pose)
        {
            if (parts == null || _restScales == null) return;
            Quaternion turn = Quaternion.Euler(0f, 0f, pose.Angle);
            for (int i = 0; i < parts.Length; i++)
            {
                RectTransform part = parts[i];
                if (part == null) continue;

                Vector3 restScale = _restScales[i];
                Vector3 scale = new(restScale.x * pose.Scale.x, restScale.y * pose.Scale.y, restScale.z);
                Quaternion rotation = _restRotations[i] * turn;
                // 움직임 중심이 제자리에 남도록 회전과 크기 변화로 밀린 만큼 위치를 되돌린다.
                Rect rect = part.rect;
                Vector3 center = new((pivot.x - part.pivot.x) * rect.width, (pivot.y - part.pivot.y) * rect.height, 0f);
                Vector3 drift = _restRotations[i] * Vector3.Scale(restScale, center) - rotation * Vector3.Scale(scale, center);

                part.anchoredPosition = _restPositions[i] + (Vector2)drift + pose.Offset;
                part.localRotation = rotation;
                part.localScale = scale;
            }
        }

        protected abstract OrganPose Evaluate(float deltaTime);

        /// <summary>대략 -1~1 사이에서 부드럽게 흔들리는 값. 채널마다 다른 흐름을 쓴다.</summary>
        protected float Noise(float frequency, int channel) =>
            (Mathf.PerlinNoise(Clock * frequency, _seed + channel * 17.31f) - 0.5f) * 2f;

        /// <summary>혈관 박동이 닿은 뒤 빠르게 흔들리다 잦아드는 값(-1~1).</summary>
        protected float PulseShake(float frequency, float decay) => Ring(SincePulse, frequency, decay);

        /// <summary>혈관 박동이 닿을 때 부풀었다 가라앉는 값(0~1).</summary>
        protected float PulseSwell(float rise, float decay)
        {
            float time = SincePulse;
            if (time < 0f) return 0f;
            return time < rise ? Mathf.SmoothStep(0f, 1f, time / rise) : Mathf.Exp(-(time - rise) / decay);
        }

        /// <summary>시작 시점부터 진동하며 잦아드는 값(-1~1).</summary>
        protected static float Ring(float time, float frequency, float decay) =>
            time < 0f || time > decay * 8f ? 0f : Mathf.Sin(time * frequency * 2f * Mathf.PI) * Mathf.Exp(-time / decay);

        protected static float Smooth(float deltaTime, float seconds) => 1f - Mathf.Exp(-deltaTime / seconds);
    }
}
