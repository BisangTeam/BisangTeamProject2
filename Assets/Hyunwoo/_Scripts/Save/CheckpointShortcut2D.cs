using Hyunwoo.Raycasting2D.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hyunwoo.Save
{
    [RequireComponent(typeof(CheckpointDevice), typeof(BoxCollider2D))]
    public sealed class CheckpointShortcut2D : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D player;
        [SerializeField] private Transform exit;
        private CheckpointDevice _state;
        private BoxCollider2D _entrance;

        private void Awake()
        {
            _state = GetComponent<CheckpointDevice>();
            _entrance = GetComponent<BoxCollider2D>();
        }

        private void Update()
        {
            if (Time.timeScale > 0f && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) TryPass();
        }

        public bool TryPass()
        {
            if (RaycastPaperView.IsReading || _state.Used || !_entrance.OverlapPoint(player.position)) return false;
            player.position = exit.position;
            player.rotation = exit.eulerAngles.z;
            player.linearVelocity = Vector2.zero;
            _state.SetUsed(true);
            return true;
        }
    }
}
