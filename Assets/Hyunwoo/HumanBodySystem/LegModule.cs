using Hyunwoo.Raycasting2D.Scripts;
using UnityEngine;

namespace Hyunwoo.HumanBodySystem
{
    public class LegModule : AbstractViscera
    {
        [SerializeField] private PlayerMove playerMove;
        [SerializeField, Min(1f), InspectorName("달리기 속도 배율")] private float sprintSpeedMultiplier = 1.7f;
        [SerializeField, Min(1f), InspectorName("달리기 혈액 소모 배율")] private float sprintDrainMultiplier = 2f;

        protected override float DrainRateMultiplier =>
            playerMove != null && playerMove.IsSprinting ? sprintDrainMultiplier : 1f;

        protected override void WorkFunction(int index, float walkSpeed)
        {
            playerMove.MoveSpeed = walkSpeed;
            playerMove.SprintSpeed = BloodGauge > 0f ? walkSpeed * sprintSpeedMultiplier : 0f;
        }
    }
}
