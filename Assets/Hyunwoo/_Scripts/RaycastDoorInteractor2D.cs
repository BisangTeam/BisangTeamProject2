using UnityEngine;
using UnityEngine.InputSystem;

namespace Hyunwoo.Raycasting2D.Scripts
{
    // 기존 Player 입력 코드를 바꾸지 않고 같은 플레이어에 붙여 사용한다.
    public class RaycastDoorInteractor2D : MonoBehaviour
    {
        [SerializeField] private LayerMask wallMask;
        [SerializeField, Min(0.01f)] private float interactionDistance = 1.6f;

        private readonly RaycastHit2D[] _hits = new RaycastHit2D[16];

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.fKey.wasPressedThisFrame) TryInteract();
        }

        public bool TryInteract()
        {
            Physics2D.SyncTransforms();
            var filter = new ContactFilter2D();
            filter.SetLayerMask(wallMask);
            filter.useTriggers = false;
            Collider2D nearest = ClosestBlocker(transform.up, interactionDistance, filter);
            RaycastDoor2D door = nearest != null ? nearest.GetComponentInParent<RaycastDoor2D>() : null;
            if (door != null && door.isActiveAndEnabled && door.gameObject.scene == gameObject.scene)
            {
                door.Toggle();
                return true;
            }

            // 열린 문은 옆 벽에 가려져 있으므로, 문이 있던 입구를 대상으로 검사한다.
            RaycastDoor2D selected = null;
            float closest = interactionDistance;
            foreach (RaycastDoor2D candidate in RaycastDoor2D.Active)
            {
                if (candidate == null || !candidate.isActiveAndEnabled || candidate.OpenAmount <= 0f ||
                    candidate.gameObject.scene != gameObject.scene) continue;

                Vector2 offset = candidate.InteractionPosition - (Vector2)transform.position;
                float distance = offset.magnitude;
                if (distance < 0.001f || distance > closest) continue;
                Vector2 direction = offset / distance;
                if (Vector2.Dot(transform.up, direction) < 0.85f) continue;

                // 입구까지의 시선이 막혀 있으면 벽 너머에서 문을 닫지 못한다.
                Collider2D blocker = ClosestBlocker(direction, Mathf.Max(0f, distance - 0.01f), filter);
                if (blocker != null && blocker.GetComponentInParent<RaycastDoor2D>() != candidate) continue;
                selected = candidate;
                closest = distance;
            }
            if (selected == null) return false;
            selected.Toggle();
            return true;
        }

        private Collider2D ClosestBlocker(Vector2 direction, float range, ContactFilter2D filter)
        {
            int count = Physics2D.Raycast(transform.position, direction, filter, _hits, range);
            Collider2D nearest = null;
            float distance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider2D hit = _hits[i].collider;
                if (hit == null || hit.transform.IsChildOf(transform) || hit.gameObject.scene != gameObject.scene) continue;
                if (_hits[i].distance >= distance) continue;
                nearest = hit;
                distance = _hits[i].distance;
            }

            return nearest;
        }
    }
}
