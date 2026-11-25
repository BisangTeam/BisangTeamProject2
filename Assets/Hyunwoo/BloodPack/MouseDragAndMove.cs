using Hyunwoo.HumanBodySystem;
using Hyunwoo.Save;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hyunwoo.BloodPack
{
    public class MouseDragAndMove : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // Drag : 위치만 계속해서 반환 + 현재 위치가 기관이면 확대
        // End : 현재 위치가 기관이면 사라지고 AbstractHumanBody.ConnectionBlood 실행시켜주기

        [Header("혈액팩 1개당 주입량")]
        [SerializeField] private float insertBloodAmount = 50;

        [SerializeField] private HeartModule heartModule;
        [SerializeField] private PlayerInventory inventory;
        private const string ItemId = "blood-pack";
        private Vector2 _homePosition;
        
        [SerializeField] private RectTransform canvasRect;
        private RectTransform _heartRect;
        private RectTransform _myRect;

        private Vector2 _myRectSize;
        
        private bool _isTouched;
        
        private Vector3[] _myRectCorners = new Vector3[4];
        private Vector3[] _heartRectCorners = new Vector3[4];
        
        private void Awake()
        {
            _heartRect = heartModule.GetComponent<RectTransform>();
            _myRect = GetComponent<RectTransform>();
            _homePosition = _myRect.anchoredPosition;
            
            _myRectSize = _myRect.sizeDelta;    
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            
        }

        public void OnDrag(PointerEventData eventData)
        {
            MovePos(eventData);

            if (CalculateRectCorners())
                _myRect.sizeDelta = _myRectSize * 1.4f;
            else
                _myRect.sizeDelta = _myRectSize;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!CalculateRectCorners()) return;
            
            if (inventory != null && !inventory.TryConsume(ItemId)) return;
            heartModule.InsertBlood(insertBloodAmount);
            _myRect.anchoredPosition = _homePosition;
            _myRect.sizeDelta = _myRectSize;
            if (inventory == null) gameObject.SetActive(false);
        }

        private void MovePos(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 pos))
                _myRect.anchoredPosition = pos;
        }
        
        private bool CalculateRectCorners()
        {
            _myRect.GetWorldCorners(_myRectCorners);
            _heartRect.GetWorldCorners(_heartRectCorners);
            
            Vector3 myRectMin = _myRectCorners[0];
            Vector3 myRectMax = _myRectCorners[2];

            Vector3 heartRectMin = _heartRectCorners[0];
            Vector3 heartRectMax = _heartRectCorners[2];

            if (myRectMin.x > heartRectMax.x) return false;
            if (myRectMin.y > heartRectMax.y) return false;
            if (heartRectMin.x > myRectMax.x) return false;
            if (heartRectMin.y > myRectMax.y) return false;
            
            return true;
        }
    }
}
