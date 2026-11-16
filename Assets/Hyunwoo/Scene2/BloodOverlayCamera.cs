using UnityEngine;

namespace Hyunwoo
{
    [RequireComponent(typeof(Camera))]
    public class BloodOverlayCamera : MonoBehaviour
    {
        [SerializeField] private Camera inputCamera;
        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            // 혈관 입력은 Main Camera로 계산한다. 그릴 때도 같은 투영을 사용해야
            // RenderTexture와 게임 창의 비율이 달라도 혈관이 기관 위치에 겹친다.
            if (inputCamera != null) _camera.projectionMatrix = inputCamera.projectionMatrix;
        }
    }
}
