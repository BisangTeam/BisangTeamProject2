using Hyunwoo.CoreLib;
using Hyunwoo.CoreLib.ModuleSystem;
using UnityEngine;
using Hyunwoo.Raycasting2D.Scripts;

namespace Hyunwoo.Player
{
    public class Player : ModuleOwner
    {
        [SerializeField] private PlayerInputSO playerInput;
        [SerializeField] private PlayerMove raycastPlayer2D;
        
        protected override void InitializeModules()
        {
            base.InitializeModules();
            playerInput.SetEnable();
        }

        private void Update()
        {
            raycastPlayer2D.MoveDir = playerInput.MoveDir;
            raycastPlayer2D.TurnDir = playerInput.TurnDir.x;
            
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