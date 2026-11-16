using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hyunwoo.CoreLib
{
    [CreateAssetMenu(fileName = "PlayerInput", menuName = "SO/PlayerInput")]
    public class PlayerInputSO : ScriptableObject, Controls.IPlayerActions
    {
        public Controls Controls;
        
        public Vector2 MoveDir { get; private set; }
        
        // public event Action OnUpKeyPressed;
        // public event Action OnDownKeyPressed;
        // public event Action OnLeftKeyPressed;
        // public event Action OnRightKeyPressed;
        
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
            Debug.Log(MoveDir);
        }
    }
}
