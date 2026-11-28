using UnityEngine;

namespace Hyunwoo.HumanBodySystem.Motion
{
    /// <summary>
    /// 혈관 박동마다 부풀고 평소에는 숨 쉬듯 움직인다. 혈액이 차오르면 잘게 웅웅 떨린다.
    /// 비어 있으면 줄어든 채 숨이 가빠지고 불규칙하게 꿈틀거린다.
    /// </summary>
    public sealed class BrainThrobMotion : OrganMotion
    {
        [SerializeField, Min(0f), Tooltip("박동 팽창")] private float pulseSwell = 0.027f;
        [SerializeField, Min(0f), Tooltip("호흡 크기. 혈액이 없으면 1.5배로 커진다.")] private float breath = 0.0072f;
        [SerializeField, Min(0.5f), Tooltip("호흡 주기 (초)")] private float breathPeriod = 4.2f;
        [SerializeField, Min(0.5f), Tooltip("빈 혈액 호흡 주기 (초). 피가 없으면 숨이 가빠진다.")] private float emptyBreathPeriod = 1.8f;
        [SerializeField, Min(0f), Tooltip("충전 중 떨림")] private float fillBuzz = 0.0108f;
        [SerializeField, Min(0f), Tooltip("빈 혈액 수축")] private float emptyShrink = 0.025f;
        [SerializeField, Min(0f), Tooltip("빈 혈액 꿈틀거림. 피가 없을 때 불규칙하게 커졌다 작아진다.")] private float emptyWobble = 0.009f;

        private float _breathPhase;

        protected override Vector2 DefaultPivot => new(0.5f, 0.1f);

        protected override OrganPose Evaluate(float deltaTime)
        {
            float starved = 1f - Vitality;
            // 주기가 바뀌어도 튀지 않도록 호흡 위상을 쌓아 간다.
            _breathPhase = Mathf.Repeat(_breathPhase + deltaTime / Mathf.Lerp(breathPeriod, emptyBreathPeriod, starved), 1f);

            float swell = pulseSwell * PulseSwell(0.07f, 0.22f);
            float breathe = breath * (1f + 0.5f * starved) * Mathf.Sin(_breathPhase * 2f * Mathf.PI);
            float wobble = emptyWobble * starved * Noise(1.6f, 3);
            float size = 1f + swell + breathe + wobble - emptyShrink * starved;
            float buzz = fillBuzz * Filling;

            OrganPose pose = OrganPose.Rest;
            // 박동은 옆으로 조금 더 부풀어 납작한 뇌 모양을 살린다.
            pose.Scale = new Vector2(size + 0.3f * swell + buzz * Noise(13f, 0), size + buzz * Noise(13f, 1));
            pose.Angle = 0.54f * Filling * Noise(5f, 2);
            return pose;
        }
    }
}
