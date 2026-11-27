using Seonho._09.SO.Scripts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hyunwoo.Raycasting2D.Scripts
{
    [RequireComponent(typeof(RawImage))]
    public sealed class RaycastDocumentReader : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private RaycastRenderer2D raycastRenderer;
        [SerializeField] private GameObject paperPrefab;
        [SerializeField] private RectTransform panelParent;

        private RaycastPaperView _panel;
        public bool IsDocumentOpen => _panel != null && _panel.isActiveAndEnabled;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || raycastRenderer == null ||
                RaycastPaperView.IsReading) return;

            RaycastSprite2D sprite = raycastRenderer.SpriteAtScreenPoint(eventData.position, eventData.pressEventCamera);
            if (sprite == null || !sprite.isActiveAndEnabled || sprite.Document == null) return;

            OpenDocument(sprite.Document, eventData.pressEventCamera, eventData.position);
            eventData.Use();
        }

        public void OpenDocument(DocumentWritingDataSO document, Camera camera, Vector2? pickupPoint = null)
        {
            if (document == null || RaycastPaperView.IsReading) return;
            if (_panel == null)
            {
                // 이전 씬에서 컴포넌트 참조가 유실된 경우에도 공용 종이를 열 수 있다.
                GameObject prefab = paperPrefab != null ? paperPrefab : Resources.Load<GameObject>("RecordReaderPanel");
                if (prefab == null || !prefab.TryGetComponent(out RaycastPaperView view))
                {
                    Debug.LogError("기록물 종이 프리팹에서 RaycastPaperView를 찾을 수 없습니다.", this);
                    return;
                }
                _panel = Instantiate(view);
                if (_panel.gameObject.scene != gameObject.scene)
                    SceneManager.MoveGameObjectToScene(_panel.gameObject, gameObject.scene);
            }
            RectTransform readingArea = panelParent != null ? panelParent : (RectTransform)transform;
            _panel.Show(document, readingArea, camera, pickupPoint);
            if (RaycastPaperView.IsReading && TryGetComponent(out ReadDocumentArchive archive))
                archive.Remember(document);
        }

        private void Update()
        {
            if (_panel == null || !_panel.isActiveAndEnabled) return;
            if (raycastRenderer == null || !raycastRenderer.isActiveAndEnabled)
                _panel.gameObject.SetActive(false);
            else if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                _panel.Close();
        }

        private void OnDisable()
        {
            if (_panel != null) _panel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_panel != null) Destroy(_panel.gameObject);
        }
    }
}
