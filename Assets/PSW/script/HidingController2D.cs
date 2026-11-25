using UnityEngine;

namespace Hyunwoo.Raycasting2D.Scripts
{
    public class HidingController2D : MonoBehaviour
    {
        [Header("플레이어 컴포넌트")]
        [SerializeField] private PlayerMove playerMove;
        [SerializeField] private RaycastRenderer2D raycastRenderer;
        [SerializeField] private MonsterChase2D monster;

        private HidingSpot2D nearbySpot;
        private HidingSpot2D activeSpot;
        private Quaternion rotationBeforeHiding;
        
        [SerializeField, Min(1f)] private float cabinetTurnSpeed = 50f;
        [SerializeField, Range(1f, 45f)] private float cabinetMaxTurnAngle = 15f;
        [SerializeField, Min(1f)] private float cabinetReturnSpeed = 60f;

        private Rigidbody2D playerBody;
        private float cabinetCenterAngle;
        private float cabinetLookOffset;

        public bool IsHidden => activeSpot != null;
        public bool MonsterSawEntry { get; private set; }

        private void Awake()
        {
            if (playerMove == null)
                playerMove = GetComponent<PlayerMove>();

            playerBody = GetComponent<Rigidbody2D>();
        }

        public void SetNearbySpot(HidingSpot2D spot)
        {
            nearbySpot = spot;
        }

        public void ClearNearbySpot(HidingSpot2D spot)
        {
            if (nearbySpot == spot)
                nearbySpot = null;
        }

        public bool TryHandleInteraction()
        {
            if (IsHidden)
            {
                ExitHiding();
                return true;
            }

            if (nearbySpot != null)
            {
                EnterHiding(nearbySpot);
                return true;
            }

            return false;
        }

        private void EnterHiding(HidingSpot2D spot)
        {
            if (spot.EnterPoint == null || spot.ExitPoint == null)
            {
                Debug.LogError(
                    "숨는 장소의 Enter Point와 Exit Point를 연결하세요.",
                    spot
                );
                return;
            }

            activeSpot = spot;
            rotationBeforeHiding = transform.rotation;

            MonsterSawEntry =
                monster != null &&
                monster.CanSeePosition(transform.position);
            Vector2 enterToExit =
                spot.ExitPoint.position - spot.EnterPoint.position;

            if (enterToExit.sqrMagnitude > 0.0001f)
            {
                cabinetCenterAngle =
                    Mathf.Atan2(enterToExit.y, enterToExit.x) * Mathf.Rad2Deg - 90f;
            }
            else
            {
                cabinetCenterAngle = spot.EnterPoint.eulerAngles.z;
            }

            cabinetLookOffset = 0f;

            playerBody.position = spot.EnterPoint.position;
            playerBody.rotation = cabinetCenterAngle;
            playerBody.linearVelocity = Vector2.zero;

            playerMove.CanMove = false;

            if (raycastRenderer != null)
            {
                raycastRenderer.SetHideView(
                    spot.HiddenFieldOfView,
                    spot.VerticalViewOffset
                );
            }

            if (monster != null)
                monster.SetHiddenPlayer(IsHidden, MonsterSawEntry);
        }
        
        private void FixedUpdate()
        {
            if (!IsHidden ||
                activeSpot.Type != HidingSpotType.Cabinet ||
                playerMove == null ||
                playerBody == null)
            {
                return;
            }

            float turnInput = playerMove.TurnDir;

            if (Mathf.Abs(turnInput) > 0.01f)
            {
                // PlayerMove의 기본 회전 방향과 같은 부호를 사용합니다.
                cabinetLookOffset +=
                    -turnInput * cabinetTurnSpeed * Time.fixedDeltaTime;

                cabinetLookOffset = Mathf.Clamp(
                    cabinetLookOffset,
                    -cabinetMaxTurnAngle,
                    cabinetMaxTurnAngle
                );
            }
            else
            {
                // 회전 키를 놓으면 정면 방향으로 부드럽게 돌아옵니다.
                cabinetLookOffset = Mathf.MoveTowards(
                    cabinetLookOffset,
                    0f,
                    cabinetReturnSpeed * Time.fixedDeltaTime
                );
            }

            playerBody.MoveRotation(cabinetCenterAngle + cabinetLookOffset);
        }

        private void ExitHiding()
        {
            if (activeSpot == null)
                return;

            Transform exitPoint = activeSpot.ExitPoint;

            transform.SetPositionAndRotation(
                exitPoint.position,
                exitPoint.rotation == Quaternion.identity
                    ? rotationBeforeHiding
                    : exitPoint.rotation
            );

            activeSpot = null;
            MonsterSawEntry = false;

            playerMove.CanMove = true;

            if (raycastRenderer != null)
                raycastRenderer.ResetHideView();

            if (monster != null)
                monster.SetHiddenPlayer(false, false);
        }

        private void OnDisable()
        {
            if (IsHidden)
                ExitHiding();
        }
    }
}