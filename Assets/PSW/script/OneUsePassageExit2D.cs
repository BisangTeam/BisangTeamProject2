using UnityEngine;

namespace Hyunwoo.Raycasting2D.Scripts
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class OneUsePassageExit2D : MonoBehaviour
    {
        [SerializeField] private RaycastDoor2D entranceDoor;

        private bool used;

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (used ||
                other.GetComponentInParent<PlayerMove>() == null)
                return;

            if (entranceDoor == null)
            {
                Debug.LogError("통로 입구 문을 연결하세요.", this);
                return;
            }

            used = true;
            entranceDoor.CloseAndLock();
        }
    }
}