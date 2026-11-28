using UnityEngine;
using UnityEngine.Rendering;

namespace Hyunwoo
{
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(-100)]
    public sealed class LowResCameraOutput : MonoBehaviour
    {
        [SerializeField] private Camera outputCamera;

        private Camera _source;
        private RenderTexture _lowRes;
        private RenderTexture _displaySize;

        private void OnEnable()
        {
            _source = GetComponent<Camera>();
            _lowRes = _source.targetTexture;
            UpdateTarget();
            RenderPipelineManager.endCameraRendering += CopyToLowRes;
        }

        private void Update() => UpdateTarget();

        private void UpdateTarget()
        {
            if (_lowRes == null || outputCamera == null || !outputCamera.isActiveAndEnabled)
            {
                _source.targetTexture = null;
                ReleaseDisplayTexture();
                return;
            }

            // uGUI and the organ hit tests use this camera's pixel coordinates.
            int width = Mathf.Max(1, outputCamera.pixelWidth);
            int height = Mathf.Max(1, outputCamera.pixelHeight);
            if (width == _lowRes.width && height == _lowRes.height)
            {
                _source.targetTexture = _lowRes;
                ReleaseDisplayTexture();
                return;
            }

            if (_displaySize != null && _displaySize.width == width && _displaySize.height == height) return;

            var descriptor = _lowRes.descriptor;
            descriptor.width = width;
            descriptor.height = height;
            var next = new RenderTexture(descriptor) { name = "QuickDither Scene", filterMode = FilterMode.Point };
            _source.targetTexture = next;
            ReleaseDisplayTexture();
            _displaySize = next;
        }

        private void CopyToLowRes(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _source || _displaySize == null) return;

            var previous = RenderTexture.active;
            Graphics.Blit(_displaySize, _lowRes);
            RenderTexture.active = previous;
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= CopyToLowRes;
            _source.targetTexture = _lowRes;
            ReleaseDisplayTexture();
        }

        private void ReleaseDisplayTexture()
        {
            if (_displaySize == null) return;
            _displaySize.Release();
            Destroy(_displaySize);
            _displaySize = null;
        }
    }
}
