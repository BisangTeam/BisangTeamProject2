using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hyunwoo.BloodPack
{
    public class MouseDragAndMove : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // Drag : 위치만 계속해서 반환 + 현재 위치가 기관이면 확대
        // End : 현재 위치가 기관이면 사라지고 AbstractHumanBody.ConnectionBlood 실행시켜주기
        
        [SerializeField] private RectTransform canvasRect;
        private RectTransform _myRect;

        private bool _isTouched;
        
        private void Awake()
        {
            _myRect = GetComponent<RectTransform>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            
        }

        public void OnDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, null, out Vector2 pos);
            _myRect.anchoredPosition = pos;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            
        }

    }
}
