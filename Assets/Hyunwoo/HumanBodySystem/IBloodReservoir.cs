using UnityEngine;

namespace Hyunwoo.HumanBodySystem
{
    public interface IBloodReservoir
    {
        float BloodGauge { get; set; }
        float Capacity { get; }
    }

    public static class BloodTransfer
    {
        public static float Move(IBloodReservoir from, IBloodReservoir to, float requested)
        {
            float room = to.Capacity - to.BloodGauge;
            float amount = Mathf.Min(requested, Mathf.Min(from.BloodGauge, room));
            if (amount <= 0f || ReferenceEquals(from, to)) return 0f;

            from.BloodGauge -= amount;
            to.BloodGauge += amount;
            return amount;
        }
    }
}
