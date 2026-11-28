using UnityEngine;

namespace Hyunwoo.HumanBodySystem.Motion
{
    /// <summary>
    /// 귀 뿌리를 축으로 천천히 기울고, 혈액이 닿으면 소리를 듣듯 씰룩인다.
    /// 비어 있으면 처진 채 가끔 느리고 힘없이 씰룩인다.
    /// </summary>
    public sealed class EarTwitchMotion : OrganMotion
    {
        [SerializeField, Min(0f), Tooltip("평소 기울임 각도. 혈액이 없어도 이만큼은 움직인다.")] private float idleSway = 0.72f;
        [SerializeField, Min(0f), Tooltip("박동 씰룩임 각도")] private float pulseTwitch = 1.08f;
        [SerializeField, Min(0f), Tooltip("충전 중 씰룩임 각도")] private float fillTwitch = 2.16f;
        [SerializeField, Tooltip("충전 중 씰룩임 간격 (최소, 최대 초)")] private Vector2 twitchInterval = new(0.35f, 1.1f);
        [SerializeField, Tooltip("빈 혈액 처짐 각도")] private float emptyDroop = 1f;
        [SerializeField, Min(0f), Tooltip("빈 혈액 씰룩임 각도. 피가 없을 때 가끔 힘없이 씰룩인다.")] private float emptyTwitch = 1.08f;
        [SerializeField, Tooltip("빈 혈액 씰룩임 간격 (최소, 최대 초)")] private Vector2 emptyTwitchInterval = new(1.2f, 3.5f);

        private float _twitchAt = float.NegativeInfinity;
        private float _twitchSize;
        private float _twitchFrequency = 9f;
        private float _twitchDecay = 0.1f;
        private float _nextTwitch;

        protected override Vector2 DefaultPivot => new(0.12f, 0.45f);

        protected override OrganPose Evaluate(float deltaTime)
        {
            float starved = 1f - Vitality;
            if (Clock >= _nextTwitch)
            {
                if (Filling > 0.15f)
                    StartTwitch(fillTwitch * Filling, 9f, 0.1f, twitchInterval);
                else if (starved > 0.3f)
                    // 피가 없을 때는 느리고 굼뜨게 씰룩인다.
                    StartTwitch(emptyTwitch * starved, 5f, 0.18f, emptyTwitchInterval);
            }

            OrganPose pose = OrganPose.Rest;
            // 귀 뿌리가 왼쪽에 있어서 양의 각도가 귓바퀴를 위로 세운다.
            pose.Angle = idleSway * Noise(0.35f, 0)
                + _twitchSize * Ring(Clock - _twitchAt, _twitchFrequency, _twitchDecay)
                + pulseTwitch * PulseShake(8f, 0.14f)
                - emptyDroop * starved;
            return pose;
        }

        private void StartTwitch(float size, float frequency, float decay, Vector2 interval)
        {
            _twitchAt = Clock;
            _twitchSize = size * Random.Range(0.6f, 1f);
            _twitchFrequency = frequency;
            _twitchDecay = decay;
            _nextTwitch = Clock + Random.Range(interval.x, interval.y);
        }
    }
}
