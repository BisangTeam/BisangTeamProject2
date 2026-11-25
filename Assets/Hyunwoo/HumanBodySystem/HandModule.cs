using UnityEngine;
using UnityEngine.UI;

namespace Hyunwoo.HumanBodySystem
{
    public sealed class HandModule : AbstractViscera
    {
        public bool CanMark { get; private set; }

        protected override void WorkFunction(int index, float abilityValue)
        {
            bool available = abilityValue > 0f;
            if (available == CanMark) return;
            CanMark = available;
        }
    }
}
