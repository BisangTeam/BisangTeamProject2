using System.Collections.Generic;
using Seonho._09.SO.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

namespace Hyunwoo.Raycasting2D.Scripts
{
    public sealed class RaycastPaperView : MonoBehaviour, IPointerClickHandler, IScrollHandler
    {
        [SerializeField] private Image paper;
        [SerializeField] private Sprite blankPaper;
        [SerializeField] private Color backdropTint = new Color(0.49f, 0.47f, 0.44f);
        [SerializeField] private TextMeshProUGUI noteTitle;
        [SerializeField] private TextMeshProUGUI noteBody;
        [SerializeField] private HeldPaperMotion motion;

        private static RaycastPaperView _openView;
        public static bool IsReading => _openView != null;

        private readonly List<Sprite> _pages = new();
        private bool _hasTranscription;
        private string _heading;
        private int _headingPage;
        private int _headingPageCount;
        private EventSystem _events;
        private bool _previousNavigation;
        private int _page;
        private int _openedFrame;
        private Vector2? _pickupPoint;
        private bool _closing;
        private Vector2[] _paperVertices;
        private ushort[] _paperTriangles;

        public void Show(DocumentWritingDataSO data, RectTransform readingArea, Camera areaCamera, Vector2? pickupPoint = null)
        {
            if (IsReading || data == null || readingArea == null) return;

            _pages.Clear();
            if (data.Image != null)
                foreach (Sprite attachment in data.Image)
                    if (attachment != null) _pages.Add(attachment);

            _heading = data.Theme;
            bool hasWriting = !string.IsNullOrWhiteSpace(data.Writing);
            bool includeCover = data.Paper != null || hasWriting || _pages.Count == 0;
            _hasTranscription = includeCover && (hasWriting || !string.IsNullOrWhiteSpace(_heading));
            if (includeCover)
            {
                Sprite cover = data.Paper;
                if (cover == null) cover = _pages.Count > 0 ? _pages[0] : blankPaper;
                if (cover != null) _pages.Insert(0, cover);
            }
            if (_pages.Count == 0)
            {
                Debug.LogError("기록물의 Paper 또는 열람 프리팹의 Blank Paper를 연결하세요.", this);
                return;
            }

            _openView = this;
            _closing = false;
            _openedFrame = Time.frameCount;
            _pickupPoint = pickupPoint;
            _events = EventSystem.current;
            if (_events != null)
            {
                _previousNavigation = _events.sendNavigationEvents;
                _events.sendNavigationEvents = false;
            }
            gameObject.SetActive(true);
            _headingPageCount = 0;
            noteTitle.text = _heading;
            noteBody.text = data.Writing;
            ShowPage(0);
            motion.Raise(readingArea, areaCamera);
        }

        private void ShowPage(int index, bool fromNextPage = false)
        {
            _page = index;
            bool showText = index == 0 && _hasTranscription;
            Sprite image = _pages[index];
            paper.sprite = image;
            paper.color = showText ? backdropTint : Color.white;
            motion.FitPage(image, showText);
            _paperVertices = image.vertices;
            _paperTriangles = image.triangles;
            noteTitle.gameObject.SetActive(showText && !string.IsNullOrWhiteSpace(_heading));
            noteBody.gameObject.SetActive(showText && !string.IsNullOrWhiteSpace(noteBody.text));
            noteBody.pageToDisplay = 1;
            if (noteBody.isActiveAndEnabled && fromNextPage)
            {
                noteBody.ForceMeshUpdate();
                noteBody.pageToDisplay = Mathf.Max(1, noteBody.textInfo.pageCount);
            }
        }

        private void LateUpdate()
        {
            if (_page != 0 || !_hasTranscription) return;
            // 창 크기에 따라 TMP가 다시 나눈 본문 페이지와 첨부 매수를 함께 센다.
            int textPages = noteBody.isActiveAndEnabled ? Mathf.Max(1, noteBody.textInfo.pageCount) : 1;
            int total = textPages + _pages.Count - 1;
            int page = Mathf.Min(noteBody.pageToDisplay, textPages);
            if (noteBody.pageToDisplay != page) noteBody.pageToDisplay = page;
            if (!noteTitle.isActiveAndEnabled || (page == _headingPage && total == _headingPageCount)) return;

            _headingPage = page;
            _headingPageCount = total;
            noteTitle.text = total > 1 ? $"{_heading} ({page}/{total})" : _heading;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            eventData.Use();
            if (_closing || Time.frameCount == _openedFrame) return;
            // 종이가 아직 손에 닿기 전에 원래 기록물을 한 번 더 눌러도 떨어뜨리지 않는다.
            float clickTolerance = _events != null ? _events.pixelDragThreshold : 5f;
            if (!motion.IsRaised && _pickupPoint.HasValue &&
                (eventData.position - _pickupPoint.Value).sqrMagnitude <= clickTolerance * clickTolerance) return;
            if (!ContainsDocument(eventData.position)) Close();
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (_closing || eventData.scrollDelta.y == 0f || !ContainsDocument(eventData.position)) return;
            eventData.Use();
            int direction = eventData.scrollDelta.y < 0f ? 1 : -1;
            if (noteBody.isActiveAndEnabled)
            {
                int textPage = noteBody.pageToDisplay + direction;
                if (textPage >= 1 && textPage <= noteBody.textInfo.pageCount)
                {
                    noteBody.pageToDisplay = textPage;
                    return;
                }
            }
            int next = _page + direction;
            if (next >= 0 && next < _pages.Count) ShowPage(next, direction < 0);
        }

        private bool ContainsDocument(Vector2 screenPoint)
        {
            RectTransform sheet = paper.rectTransform;
            if (!RectTransformUtility.RectangleContainsScreenPoint((RectTransform)sheet.parent, screenPoint)) return false;
            if (ContainsText(noteTitle, screenPoint) || ContainsText(noteBody, screenPoint)) return true;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(sheet, screenPoint, null, out Vector2 local) ||
                !sheet.rect.Contains(local)) return false;

            Sprite sprite = paper.sprite;
            Vector2 uv = Rect.PointToNormalized(sheet.rect, local);
            Vector2 pixel = Vector2.Scale(uv, sprite.rect.size);
            Texture2D texture = sprite.texture;
            if (!sprite.packed && texture.isReadable && !GraphicsFormatUtility.IsCrunchFormat(texture.format))
            {
                pixel += sprite.rect.min;
                return texture.GetPixel(Mathf.FloorToInt(pixel.x), Mathf.FloorToInt(pixel.y)).a > 0.05f;
            }

            // 읽기 불가/아틀라스 스프라이트는 임포터가 만든 종이 윤곽선을 사용한다.
            Vector2 point = (pixel - sprite.pivot) / sprite.pixelsPerUnit;
            for (int i = 0; i < _paperTriangles.Length; i += 3)
            {
                Vector2 a = _paperVertices[_paperTriangles[i]];
                Vector2 b = _paperVertices[_paperTriangles[i + 1]];
                Vector2 c = _paperVertices[_paperTriangles[i + 2]];
                float ab = Edge(a, b, point);
                float bc = Edge(b, c, point);
                float ca = Edge(c, a, point);
                if ((ab >= 0f && bc >= 0f && ca >= 0f) || (ab <= 0f && bc <= 0f && ca <= 0f)) return true;
            }
            return false;
        }

        private static bool ContainsText(TMP_Text text, Vector2 screenPoint)
        {
            if (!text.isActiveAndEnabled || text.canvasRenderer.GetInheritedAlpha() <= 0.01f ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(text.rectTransform, screenPoint, null, out Vector2 point))
                return false;

            TMP_TextInfo info = text.textInfo;
            for (int i = 0; i < info.lineCount; i++)
            {
                TMP_LineInfo line = info.lineInfo[i];
                if (line.visibleCharacterCount == 0 || info.characterInfo[line.firstCharacterIndex].pageNumber + 1 != text.pageToDisplay)
                    continue;
                if (point.x >= line.lineExtents.min.x && point.x <= line.lineExtents.max.x &&
                    point.y >= line.descender && point.y <= line.ascender) return true;
            }
            return false;
        }

        private static float Edge(Vector2 a, Vector2 b, Vector2 point) =>
            (b.x - a.x) * (point.y - a.y) - (b.y - a.y) * (point.x - a.x);

        public void Close()
        {
            if (_openView != this || _closing) return;
            _closing = true;
            motion.Lower(() => gameObject.SetActive(false));
        }

        private void OnDisable()
        {
            if (_openView != this) return;
            _openView = null;
            if (_events != null) _events.sendNavigationEvents = _previousNavigation;
            _events = null;
        }
    }
}
