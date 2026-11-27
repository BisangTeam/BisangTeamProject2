using System;
using System.Collections.Generic;
using Seonho._09.SO.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hyunwoo.Raycasting2D.Scripts
{
    [RequireComponent(typeof(RaycastDocumentReader))]
    public sealed class ReadDocumentArchive : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset font;
        private readonly List<DocumentWritingDataSO> _documents = new();
        private readonly List<GameObject> _rows = new();
        private const int PageSize = 5;
        private RaycastDocumentReader _reader;
        private RectTransform _list;
        private int _page;
        private bool _returnToList;
        private static ReadDocumentArchive _open;
        public static bool IsOpen => _open != null;

        private void Awake()
        {
            _reader = GetComponent<RaycastDocumentReader>();
            Row((RectTransform)transform, "기록물 [J]", 12f, 12f, 120f, Toggle);
            _list = (RectTransform)new GameObject("Read documents", typeof(RectTransform), typeof(Image)).transform;
            _list.SetParent(transform, false);
            Place(_list, 12f, 55f, 350f, 66f);
            _list.GetComponent<Image>().color = new Color32(22, 26, 26, 247);
            _list.gameObject.SetActive(false);
        }

        public void Remember(DocumentWritingDataSO document)
        {
            if (!_documents.Contains(document)) _documents.Add(document);
        }

        private void Update()
        {
            if (_returnToList && !_reader.IsDocumentOpen)
            {
                _returnToList = false;
                OpenList();
                return;
            }
            var keys = Keyboard.current;
            if (keys == null) return;
            if (keys.jKey.wasPressedThisFrame) Toggle();
            else if (_open == this && keys.escapeKey.wasPressedThisFrame) Close();
        }

        private void Toggle()
        {
            if (_open == this) { Close(); return; }
            if (RaycastPaperView.IsReading || Time.timeScale <= 0f) return;
            _page = 0;
            OpenList();
        }

        private void OpenList()
        {
            _open = this;
            _list.gameObject.SetActive(true);
            DrawPage();
        }

        private void DrawPage()
        {
            foreach (var row in _rows) { row.SetActive(false); Destroy(row); }
            _rows.Clear();
            int start = _page * PageSize;
            int count = Mathf.Min(PageSize, _documents.Count - start);
            if (count == 0)
                _rows.Add(Row(_list, "아직 읽은 기록이 없습니다.", 10f, 10f, 330f, null));
            for (int i = 0; i < count; i++)
            {
                var document = _documents[start + i];
                string title = string.IsNullOrWhiteSpace(document.Theme) ? document.name : document.Theme;
                _rows.Add(Row(_list, title, 10f, 10f + i * 40f, 330f, () =>
                {
                    Close();
                    _reader.OpenDocument(document, GetComponentInParent<Canvas>().rootCanvas.worldCamera);
                    _returnToList = _reader.IsDocumentOpen;
                }));
            }
            float bottom = 10f + Mathf.Max(1, count) * 40f;
            if (_documents.Count > PageSize)
            {
                if (_page > 0) _rows.Add(Row(_list, "이전", 10f, bottom, 90f, () => { _page--; DrawPage(); }));
                if (start + count < _documents.Count)
                    _rows.Add(Row(_list, "다음", 250f, bottom, 90f, () => { _page++; DrawPage(); }));
                bottom += 40f;
            }
            _list.sizeDelta = new Vector2(350f, bottom + 6f);
        }

        private GameObject Row(RectTransform parent, string title, float x, float y, float width, Action click)
        {
            var row = new GameObject(title, typeof(RectTransform), typeof(Image));
            RectTransform rect = (RectTransform)row.transform;
            rect.SetParent(parent, false);
            Place(rect, x, y, width, 36f);
            var background = row.GetComponent<Image>();
            background.color = new Color32(51, 59, 56, 243);
            if (click != null)
            {
                var button = row.AddComponent<Button>();
                button.targetGraphic = background;
                var colors = button.colors;
                colors.normalColor = new Color32(202, 210, 202, 255);
                colors.highlightedColor = Color.white;
                colors.pressedColor = new Color32(164, 181, 166, 255);
                colors.selectedColor = colors.normalColor;
                colors.fadeDuration = 0.10f;
                button.colors = colors;
                button.onClick.AddListener(() => click());
            }
            var label = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.rectTransform.SetParent(rect, false);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(6f, 0f);
            label.rectTransform.offsetMax = new Vector2(-6f, 0f);
            label.font = font;
            label.fontSize = 18f;
            label.color = new Color32(232, 233, 216, 255);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            label.text = title;
            return row;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private void Close()
        {
            if (_open == this) _open = null;
            if (_list != null) _list.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _returnToList = false;
            Close();
        }
    }
}
