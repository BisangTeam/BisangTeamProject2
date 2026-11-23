using UnityEngine;
using UnityEngine.InputSystem;

namespace Hyunwoo.CoreLib
{
    [CreateAssetMenu(fileName = "PlayerInput", menuName = "SO/PlayerInput")]
    public class PlayerInputSO : ScriptableObject, Controls.IPlayerActions
    {
        public Controls Controls;
        
        public Vector2 MoveDir { get; private set; }
        public Vector2 TurnDir { get; private set; }

        public void SetEnable()
        {
            if (Controls == null)
            {
                Controls = new Controls();
                Controls.Player.SetCallbacks(this);
            }
            
            Controls.Player.Enable();
        }

        public void SetDisable()
        {
            if (Controls != null)
                Controls.Player.Disable();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            MoveDir = context.ReadValue<Vector2>();
        }

        public void OnTurn(InputAction.CallbackContext context)
        {
            TurnDir = context.ReadValue<Vector2>();
        }
    }
}
