using Seonho._01.Scripts.StereophonicSound;
using UnityEngine;

namespace Hyunwoo.HumanBodySystem
{
    public class EarModule : AbstractViscera
    {
        [SerializeField] private LoudPos loudPos;

        protected override void WorkFunction(int index, float abillityValue)
        {
            loudPos.EarBlood = BloodGaugeRatio;
        }
    }
}