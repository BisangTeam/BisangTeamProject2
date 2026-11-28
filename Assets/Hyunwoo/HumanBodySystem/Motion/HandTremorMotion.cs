using UnityEngine;

namespace Hyunwoo.HumanBodySystem.Motion
{
    /// <summary>
    /// 손목을 축으로 떤다. 혈액이 차오를수록 잘고 빠른 떨림이 커지고,
    /// 비어 있으면 손끝이 처진 채 굵고 느리게 떨며 힘없이 흔들린다.
    /// </summary>
    public sealed class HandTremorMotion : OrganMotion
    {
        [SerializeField, Min(0f), Tooltip("평소 떨림 각도. 혈액이 없으면 두 배로 굵게 떤다.")] private float idleTremor = 0.27f;
        [SerializeField, Min(0f), Tooltip("혈액 충전 떨림 각도")] private float fillTremor = 1.44f;
        [SerializeField, Min(0.1f), Tooltip("떨림 속도 (Hz)")] private float tremorFrequency = 9f;
        [SerializeField, Min(0f), Tooltip("박동 경련 각도")] private float pulseTwitch = 0.72f;
        [SerializeField, Tooltip("빈 혈액 처짐 각도")] private float emptyDroop = 1.2f;
        [SerializeField, Min(0f), Tooltip("빈 혈액 흔들림 각도. 피가 없을 때 손이 힘없이 천천히 흔들린다.")] private float emptySway = 1.2f;

        protected override Vector2 DefaultPivot => new(0.93f, 0.45f);

        protected override OrganPose Evaluate(float deltaTime)
        {
            float starved = 1f - Vitality;
            // 피가 돌면 잘고 빠르게, 비어 있으면 굵고 느리게 떤다.
            float fine = 0.75f * Noise(tremorFrequency, 0) + 0.25f * Noise(tremorFrequency * 2.1f, 1);
            float weak = Noise(tremorFrequency * 0.4f, 4);
            float shake = Mathf.Lerp(weak, fine, Vitality);
            float amount = idleTremor * (1f + starved) + fillTremor * Filling;

            OrganPose pose = OrganPose.Rest;
            // 손끝이 손목 왼쪽에 있어서 양의 각도가 손끝을 아래로 내린다.
            pose.Angle = shake * amount
                + pulseTwitch * PulseShake(11f, 0.12f)
                + starved * (emptyDroop + emptySway * Noise(0.45f, 5));
            pose.Offset = new Vector2(Noise(tremorFrequency * 1.3f, 2), Noise(tremorFrequency * 1.1f, 3)) * (0.4f * amount);
            return pose;
        }
    }
}
