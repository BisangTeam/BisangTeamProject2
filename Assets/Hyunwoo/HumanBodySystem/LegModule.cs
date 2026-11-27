using Hyunwoo.Raycasting2D.Scripts;
using UnityEngine;

namespace Hyunwoo.HumanBodySystem
{
    public class LegModule : AbstractViscera
    {
        [SerializeField] private PlayerMove playerMove;

        protected override void WorkFunction(int index, float abillityValue)
        {
            playerMove.MoveSpeed = abillityValue;            
        }
    }
}