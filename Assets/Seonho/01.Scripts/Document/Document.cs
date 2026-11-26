using System.Collections.Generic;
using Seonho._09.SO.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Seonho._01.Scripts
{
    public class Document : MonoBehaviour
    {
        [SerializeField] private DocumentWritingDataSO _documentDataSO;
        
        [SerializeField] private TextMeshProUGUI _theme;
        [SerializeField] private TextMeshProUGUI _writing;
        [SerializeField] private Image _image;

        [SerializeField] private ScrollRect _scroll;

        private readonly List<Image> _pages = new();

        private void Awake()
        {
            // Report 씬의 문서는 자식 순서로 제목, 본문, 삽화를 지정한다.
            if (_theme == null && transform.childCount > 0)
                _theme = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            if (_writing == null && transform.childCount > 1)
                _writing = transform.GetChild(1).GetComponent<TextMeshProUGUI>();

            if (_image != null)
                _pages.Add(_image);
            else
                for (int i = 2; i < transform.childCount; i++)
                    if (transform.GetChild(i).TryGetComponent(out Image page)) _pages.Add(page);
        }

        private void OnEnable() => Refresh();

        public void Show(DocumentWritingDataSO data)
        {
            _documentDataSO = data;
            if (gameObject.activeSelf) Refresh();
            else gameObject.SetActive(true);
        }

        public void Close() => gameObject.SetActive(false);

        private void Refresh()
        {
            if (_documentDataSO == null) return;
            if (_theme != null) _theme.text = _documentDataSO.Theme;
            if (_writing != null)
            {
                _writing.text = _documentDataSO.Writing;
                if (_scroll != null) _writing.gameObject.SetActive(!string.IsNullOrWhiteSpace(_writing.text));
            }

            Sprite[] images = _documentDataSO.Image;
            int count = images?.Length ?? 0;
            if (_image != null)
                while (_pages.Count < count) _pages.Add(Instantiate(_image, _image.transform.parent));

            for (int i = 0; i < _pages.Count; i++)
            {
                if (i < count) _pages[i].sprite = images[i];
                if (_image != null) _pages[i].gameObject.SetActive(i < count && images[i] != null);
            }

            if (_scroll == null) return;
            ResizePages();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_scroll.content);
            _scroll.StopMovement();
            _scroll.verticalNormalizedPosition = 1f;
        }

        private void OnRectTransformDimensionsChange() => ResizePages();

        private void ResizePages()
        {
            if (_scroll == null || _scroll.viewport == null) return;
            float width = _scroll.viewport.rect.width;
            foreach (Image page in _pages)
                if (page.sprite != null && page.TryGetComponent(out LayoutElement layout))
                    layout.preferredHeight = width * page.sprite.rect.height / page.sprite.rect.width;
        }
    }
}
