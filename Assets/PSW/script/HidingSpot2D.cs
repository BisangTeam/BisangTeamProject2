using UnityEngine;

namespace Hyunwoo.Raycasting2D.Scripts
{
    public enum HidingSpotType
    {
        UnderDesk,
        Cabinet
    }

    [RequireComponent(typeof(BoxCollider2D))]
    public class HidingSpot2D : MonoBehaviour
    {
        [SerializeField] private HidingSpotType type;
        [SerializeField] private Transform enterPoint;
        [SerializeField] private Transform exitPoint;

        [Header("숨을 때의 화면")]
        [SerializeField, Range(10f, 150f)] private float hiddenFieldOfView = 35f;
        [SerializeField, Range(-0.3f, 0.3f)] private float verticalViewOffset = 0f;

        public HidingSpotType Type => type;
        public Transform EnterPoint => enterPoint;
        public Transform ExitPoint => exitPoint;
        public float HiddenFieldOfView => hiddenFieldOfView;
        public float VerticalViewOffset => verticalViewOffset;

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HidingController2D controller =
                other.GetComponentInParent<HidingController2D>();

            if (controller != null)
                controller.SetNearbySpot(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            HidingController2D controller =
                other.GetComponentInParent<HidingController2D>();

            if (controller != null)
                controller.ClearNearbySpot(this);
        }
    }
}