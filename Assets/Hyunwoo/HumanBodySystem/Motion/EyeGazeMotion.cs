using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hyunwoo.HumanBodySystem.Motion
{
    /// <summary>
    /// 눈알이 마우스 포인터 쪽으로 돌아간다. 그 위에 짧은 잔눈질과 미세 떨림을 얹는다.
    /// 혈액이 차오르면 잔눈질이 잦아지고, 비어 있으면 굼뜨게 따라가며 아래로 처진다.
    /// </summary>
    public sealed class EyeGazeMotion : OrganMotion
    {
        [Header("눈알")]
        [SerializeField, Tooltip("눈알 그림(Frame). 이 그림에만 회전 셰이더를 씌운다.")]
        private Image eyeball;
        [SerializeField] private Shader gazeShader;
        [SerializeField, Tooltip("그림 안 눈알 중심 (0~1)")] private Vector2 sphereCenter = new(0.5f, 0.5f);
        [SerializeField, Range(0.1f, 0.5f), Tooltip("그림 안 눈알 반지름 (너비 대비)")] private float sphereRadius = 0.493f;

        [Header("시선")]
        [SerializeField, Range(0f, 50f), Tooltip("혈액이 가득 찼을 때의 최대 시선 각도")] private float maxGazeAngle = 11.7f;
        [SerializeField, Range(0f, 1f), Tooltip("혈액이 비었을 때 시선 범위 (최대 각도 대비). 혈액이 찰수록 최대 각도까지 늘어난다.")]
        private float emptyReach = 0.2f;
        [SerializeField, Min(0.1f), Tooltip("좌우·아래 끝까지 돌아가는 거리 (눈 반지름 배수)")] private float followDistance = 4f;
        [SerializeField, Min(0.1f), Tooltip("위쪽 끝까지 돌아가는 거리 (눈 반지름 배수). 눈이 화면 위쪽에 있어 포인터가 눈보다 높이 갈 여유가 적으므로, 위로는 조금만 올라가도 끝까지 올려다본다.")]
        private float upFollowDistance = 1.5f;
        [SerializeField, Min(0f), Tooltip("시선 방향으로 밀리는 거리")] private float socketShift = 0.2f;

        [Header("잔눈질")]
        [SerializeField, Min(0f), Tooltip("잔눈질 각도")] private float saccadeAngle = 1f;
        [SerializeField, Tooltip("잔눈질 간격 (최소, 최대 초)")] private Vector2 saccadeInterval = new(0.6f, 2.4f);
        [SerializeField, Range(0.05f, 1f), Tooltip("충전 중 간격 배율")] private float fillIntervalScale = 0.3f;
        [SerializeField, Min(0f), Tooltip("미세 떨림 각도")] private float microTremor = 0.2f;

        [Header("혈액")]
        [SerializeField, Min(0f), Tooltip("박동 팽창")] private float pulseSwell = 0.025f;
        [SerializeField, Min(0f), Tooltip("빈 혈액 시선 처짐 각도. 위를 보는 각도에서 그대로 빠지므로 빈 혈액 시선 범위보다 작게 둔다.")] private float emptyDroop = 0.5f;

        private static readonly int GazeId = Shader.PropertyToID("_Gaze");
        private static readonly int SphereId = Shader.PropertyToID("_EyeSphere");

        private Material _material;
        private Material _originalMaterial;
        private Camera _canvasCamera;
        // 쉬는 자세의 눈알 중심과 오른쪽 끝. 장기 루트 기준이라 그림이 흔들려도 기준점은 그대로다.
        private Vector3 _centerLocal;
        private Vector3 _edgeLocal;
        private Vector2 _gaze;
        private Vector2 _saccade;
        private Vector2 _wander;
        private float _nextSaccade;

        protected override void Awake()
        {
            base.Awake();
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                _canvasCamera = canvas.rootCanvas.worldCamera;
            if (eyeball == null) return;

            RectTransform eye = eyeball.rectTransform;
            Rect rect = eye.rect;
            Vector2 center = rect.min + Vector2.Scale(sphereCenter, rect.size);
            _centerLocal = transform.InverseTransformPoint(eye.TransformPoint(center));
            _edgeLocal = transform.InverseTransformPoint(eye.TransformPoint(center + Vector2.right * (sphereRadius * rect.width)));

            _originalMaterial = eyeball.material == eyeball.defaultMaterial ? null : eyeball.material;
            if (gazeShader != null && gazeShader.isSupported)
                _material = new Material(gazeShader) { name = "Eyeball Gaze (Instance)", hideFlags = HideFlags.DontSave };
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _gaze = _saccade = _wander = Vector2.zero;
            _nextSaccade = Clock;
            if (_material == null) return;
            eyeball.material = _material;
            ApplySphere();
            _material.SetVector(GazeId, Vector4.zero);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_material != null && eyeball != null) eyeball.material = _originalMaterial;
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }

        protected override OrganPose Evaluate(float deltaTime)
        {
            if (Clock >= _nextSaccade)
            {
                _saccade = Random.insideUnitCircle * (saccadeAngle * (1f + Filling));
                // 포인터를 읽지 못할 때는 혼자 두리번거린다.
                _wander = Random.insideUnitCircle * (0.5f * maxGazeAngle);
                float wait = Random.Range(saccadeInterval.x, saccadeInterval.y);
                _nextSaccade = Clock + wait * Mathf.Lerp(1f, fillIntervalScale, Filling);
            }

            // 혈액이 비었을 때는 조금만 움직이고, 찰수록 최대 각도까지 넓게 움직인다.
            float reach = Mathf.Lerp(emptyReach, 1f, Vitality);
            Vector2 aim = ((TryPointerDirection(out Vector2 toPointer) ? toPointer * maxGazeAngle : _wander) + _saccade) * reach;
            aim.y -= emptyDroop * (1f - Vitality);
            // 피가 돌면 시선이 재빨리 따라가고, 비어 있으면 굼뜨게 끌려간다.
            _gaze = Vector2.Lerp(_gaze, aim, Smooth(deltaTime, Mathf.Lerp(0.3f, 0.06f, Vitality)));

            Vector2 tremor = new Vector2(Noise(12f, 0), Noise(12f, 1)) * (microTremor * (0.3f + 0.7f * Vitality + Filling));
            Vector2 look = Vector2.ClampMagnitude(_gaze + tremor, 55f);
            if (_material != null)
                _material.SetVector(GazeId, new Vector4(look.x * Mathf.Deg2Rad, look.y * Mathf.Deg2Rad, 0f, 0f));

            OrganPose pose = OrganPose.Rest;
            pose.Offset = look * (socketShift / Mathf.Max(1f, maxGazeAngle));
            pose.Scale = Vector2.one * (1f + pulseSwell * PulseSwell(0.06f, 0.2f));
            return pose;
        }

        /// <summary>
        /// 눈알 중심에서 포인터까지의 방향. 길이는 0~1이다.
        /// 좌우와 위아래를 따로 채워서, 포인터가 멀리 옆에 있어도 눈보다 조금 높으면 위를 함께 본다.
        /// </summary>
        private bool TryPointerDirection(out Vector2 direction)
        {
            direction = Vector2.zero;
            Mouse mouse = Mouse.current;
            if (mouse == null || eyeball == null) return false;

            Vector2 center = RectTransformUtility.WorldToScreenPoint(_canvasCamera, transform.TransformPoint(_centerLocal));
            Vector2 edge = RectTransformUtility.WorldToScreenPoint(_canvasCamera, transform.TransformPoint(_edgeLocal));
            float radius = Vector2.Distance(center, edge);
            if (radius < 0.5f) return false;

            Vector2 offset = (mouse.position.ReadValue() - center) / radius;
            float vertical = offset.y > 0f ? upFollowDistance : followDistance;
            direction = new Vector2(Mathf.Clamp(offset.x / followDistance, -1f, 1f), Mathf.Clamp(offset.y / vertical, -1f, 1f));
            // 대각선으로 둘 다 끝까지 가면 원 밖으로 나가므로 최대 각도 안으로 되돌린다.
            direction = Vector2.ClampMagnitude(direction, 1f);
            return true;
        }

        /// <summary>셰이더에 스프라이트 안 눈알 구의 위치를 텍스처 좌표로 알려 준다.</summary>
        private void ApplySphere()
        {
            Sprite sprite = eyeball.overrideSprite;
            if (sprite == null || sprite.texture == null) return;

            Rect area = sprite.packed && sprite.packingMode == SpritePackingMode.Tight ? sprite.rect : sprite.textureRect;
            float width = sprite.texture.width;
            float height = sprite.texture.height;
            Vector2 center = area.position + Vector2.Scale(sphereCenter, area.size);
            float radius = sphereRadius * area.width;
            _material.SetVector(SphereId, new Vector4(center.x / width, center.y / height, radius / width, radius / height));
        }
    }
}
