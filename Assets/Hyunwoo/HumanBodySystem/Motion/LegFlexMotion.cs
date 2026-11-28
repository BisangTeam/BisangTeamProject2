using Hyunwoo.Raycasting2D.Scripts;
using UnityEngine;

namespace Hyunwoo.HumanBodySystem.Motion
{
    /// <summary>걸을 때마다 허벅지 근육이 수축하고, 혈액이 차오르면 근육이 잘게 떨린다. 비어 있으면 가늘어진다.</summary>
    public sealed class LegFlexMotion : OrganMotion
    {
        [SerializeField] private PlayerMove player;
        [SerializeField, Min(0f), Tooltip("걷기 근육 수축")] private float walkFlex = 0.009f;
        [SerializeField, Min(0f), Tooltip("달리기 근육 수축")] private float sprintFlex = 0.021f;
        [SerializeField, Min(0.1f), Tooltip("걸음 빈도 (Hz)")] private float stepFrequency = 1.8f;
        [SerializeField, Min(0.1f), Tooltip("달리기 걸음 빈도 (Hz)")] private float sprintStepFrequency = 3f;
        [SerializeField, Min(0f), Tooltip("걸음 흔들림 각도")] private float stepSwing = 0.36f;
        [SerializeField, Min(0f), Tooltip("충전 중 근육 떨림")] private float fillFlutter = 0.0072f;
        [SerializeField, Min(0f), Tooltip("박동 수축")] private float pulseFlex = 0.0072f;
        [SerializeField, Min(0f), Tooltip("빈 혈액 위축")] private float emptyThin = 0.03f;

        private Rigidbody2D _body;
        private float _gait;
        private float _sprint;
        private float _stepPhase;

        protected override Vector2 DefaultPivot => new(0.4f, 0.9f);

        protected override void Awake()
        {
            base.Awake();
            if (player == null) player = FindAnyObjectByType<PlayerMove>();
            if (player != null) _body = player.GetComponent<Rigidbody2D>();
        }

        protected override OrganPose Evaluate(float deltaTime)
        {
            bool walking = _body != null && player.isActiveAndEnabled && _body.linearVelocity.sqrMagnitude > 0.01f;
            _gait = Mathf.MoveTowards(_gait, walking ? 1f : 0f, 4f * deltaTime);
            _sprint = Mathf.MoveTowards(_sprint, walking && player.IsSprinting ? 1f : 0f, 4f * deltaTime);
            _stepPhase = Mathf.Repeat(_stepPhase + deltaTime * Mathf.Lerp(stepFrequency, sprintStepFrequency, _sprint), 1f);

            float stride = Mathf.Sin(_stepPhase * 2f * Mathf.PI);
            // 한 걸음 주기에 양발이 한 번씩 디디므로 근육은 흔들림의 두 배 빠르기로 수축한다.
            float flex = Mathf.Lerp(walkFlex, sprintFlex, _sprint) * _gait * (0.4f + 0.6f * Vitality) * stride * stride
                + pulseFlex * PulseSwell(0.06f, 0.18f);
            float flutter = fillFlutter * Filling;
            float thin = emptyThin * (1f - Vitality);

            OrganPose pose = OrganPose.Rest;
            // 근육은 짧아지면서 옆으로 부푼다.
            pose.Scale = new Vector2(1f + flex - thin + flutter * Noise(15f, 0), 1f - 0.6f * flex + 0.5f * flutter * Noise(15f, 1));
            pose.Angle = stepSwing * _gait * stride;
            return pose;
        }
    }
}
