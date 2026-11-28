using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(100)] // Draw after the heart has distributed this frame's blood.
public class JanggiGuage : MonoBehaviour, IMeshModifier
{
    [SerializeField] private RectTransform bloodRect;
    [SerializeField] private Graphic emptyOverlay;
    [SerializeField, InspectorName("혈액 표시 높이 (아래 / 위)"), Tooltip("스프라이트의 투명 여백을 제외한 높이입니다. X는 장기의 아래 끝, Y는 위 끝이며 0~1로 표시합니다.")]
    private Vector2 visibleHeight = new(0f, 1f);

    private float _gauge;
    public float Gauge 
    { 
        get => _gauge;
        set
        {
            value = Mathf.Clamp(value, 0f, 100f);
            if (_gauge == value) return;
            _gauge = value;
            UpdateGauge();
        } 
    }

    [SerializeField] private float emptyPosY = -200f;
    [SerializeField] private float fullPosY = 0f;

    [SerializeField] private float fillDuration = 0.4f;
    [SerializeField] private Ease fillEase = Ease.OutQuad;

    private Quaternion _levelRotation;
    private float _shown;
    private float _from;
    private float _elapsed;
    private float _tilt;
    private float _tiltVelocity;
    private static readonly int WaveAmplitude = Shader.PropertyToID("_WaveAmplitude");
    private Image _organOverlay;

    private void Awake()
    {
        _levelRotation = bloodRect.localRotation;
        _organOverlay = GetOrganOverlay();
        if (_organOverlay != null && _organOverlay.canvas != null)
            _organOverlay.canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
        ApplyLevel();
    }

    private Image GetOrganOverlay()
    {
        return emptyOverlay is Image image && image.rectTransform == bloodRect && bloodRect == transform &&
               image.overrideSprite != null && image.material.HasProperty(WaveAmplitude) ? image : null;
    }

    public void UpdateGauge()
    {
        _from = _shown;
        _elapsed = 0f;
    }

    private void LateUpdate()
    {
        if (Time.deltaTime <= 0f) return;

        float lastLevel = _shown;
        if (_shown != _gauge)
        {
            // Small flow increments reach their level this frame. A large injection still eases in.
            float duration = fillDuration * Mathf.Abs(_gauge - _from) / 100f;
            _elapsed += Time.deltaTime;
            _shown = _elapsed >= duration ? _gauge : DOVirtual.EasedValue(_from, _gauge, _elapsed / duration, fillEase);
            ApplyLevel();
        }
        float fillSpeed = (_shown - lastLevel) / Time.deltaTime;
        float targetTilt = Mathf.Clamp(fillSpeed * 0.08f, -4f, 4f);
        float lastTilt = _tilt;
        _tilt = Mathf.SmoothDamp(_tilt, targetTilt, ref _tiltVelocity, 0.12f);
        if (_organOverlay == null)
            bloodRect.localRotation = _levelRotation * Quaternion.Euler(0f, 0f, _tilt);
        else if (_shown != lastLevel || !Mathf.Approximately(_tilt, lastTilt))
            emptyOverlay.SetVerticesDirty();
    }

    private void ApplyLevel()
    {
        float level = _shown / 100f;
        if (_organOverlay == null)
        {
            Vector2 position = bloodRect.anchoredPosition;
            position.y = Mathf.Lerp(emptyPosY, fullPosY, level);
            bloodRect.anchoredPosition = position;
        }
        if (emptyOverlay != null)
        {
            float firstDrops = Mathf.Sqrt(Mathf.Clamp01(level * 8f));
            emptyOverlay.canvasRenderer.SetAlpha(1f - 0.24f * firstDrops);
        }
    }

    public void ModifyMesh(VertexHelper vertices)
    {
        // Unity also builds this mesh for the Scene view before Awake runs.
        Image image = GetOrganOverlay();
        if (image == null) return;

        Rect rect = image.GetPixelAdjustedRect();
        if (rect.width <= 0f || rect.height <= 0f) return;
        if (image.preserveAspect)
        {
            Vector2 art = image.overrideSprite.rect.size;
            Vector2 size = art * Mathf.Min(rect.width / art.x, rect.height / art.y);
            rect.position += Vector2.Scale(rect.size - size, bloodRect.pivot);
            rect.size = size;
        }
        float level = Mathf.Clamp01(_shown / 100f);
        float surface = Mathf.Lerp(visibleHeight.x, visibleHeight.y, level);
        // 비었거나 가득 찼을 때는 물결과 기울기도 0이 된다.
        float edge = Mathf.Clamp01(8f * Mathf.Min(level, 1f - level));
        float waveScale = 2f * (visibleHeight.y - visibleHeight.x) * edge;
        float slope = Mathf.Tan(_tilt * Mathf.Deg2Rad) * edge / rect.height;

        UIVertex vertex = default;
        for (int i = 0; i < vertices.currentVertCount; i++)
        {
            vertices.PopulateUIVertex(ref vertex, i);
            Vector4 spriteUV = vertex.uv0;
            Vector2 position = Rect.PointToNormalized(rect, vertex.position);
            float tiltedSurface = surface + (vertex.position.x - rect.center.x) * slope;
            float heightAboveWater = position.y - tiltedSurface;
            if (level <= 0f) heightAboveWater = 1f;
            if (level >= 1f) heightAboveWater = -1f;
            vertex.uv0 = new Vector4(1f - position.x, 0f, 0f, 0f);
            // SimpleWater는 UV2에서 원본 그림 좌표, 파도 크기, 수면 위 높이를 읽는다.
            vertex.uv2 = new Vector4(spriteUV.x, spriteUV.y, 1f + waveScale, heightAboveWater);
            vertices.SetUIVertex(vertex, i);
        }
    }

    void IMeshModifier.ModifyMesh(Mesh mesh)
    {
        using var vertices = new VertexHelper(mesh);
        ModifyMesh(vertices);
        vertices.FillMesh(mesh);
    }
}
