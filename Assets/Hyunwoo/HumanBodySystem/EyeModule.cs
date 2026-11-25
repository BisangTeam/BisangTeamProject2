using Hyunwoo.Raycasting2D.Scripts;
using UnityEngine;

namespace Hyunwoo.HumanBodySystem
{
    public class EyeModule : AbstractViscera
    {
        [SerializeField] private RaycastRenderer2D raycastRenderer;
        protected override void WorkFunction(int index, float abillityValue)
        {
            raycastRenderer.ShadowStrength = abillityValue;
        }
    }
}