using System;
using Hyunwoo.BloodPack;
using Hyunwoo.CoreLib;
using Hyunwoo.CoreLib.ModuleSystem;
using Hyunwoo.HumanBodySystem;
using Hyunwoo.Raycasting2D;
using UnityEngine;

namespace Hyunwoo.Player
{
    public class Player : ModuleOwner
    {
        [SerializeField] private PlayerInputSO playerInput;
        [SerializeField] private RaycastPlayer2D raycastPlayer2D;
        
        protected override void InitializeModules()
        {
            base.InitializeModules();
            playerInput.SetEnable();
        }
    
        private void Update()
        {
            raycastPlayer2D.MoveDir = playerInput.MoveDir;
            
            foreach (var moduleDictValue in _moduleDict.Values)
            {
                moduleDictValue.Work();
            }
        }
    
        private void OnDestroy()
        {
            playerInput.SetDisable();
        }
    }
}