using System;
using Hyunwoo.HumanBodySystem;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hyunwoo.BloodPack
{
    public class MouseDragAndMove : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // Drag : 위치만 계속해서 반환 + 현재 위치가 기관이면 확대
        // End : 현재 위치가 기관이면 사라지고 AbstractHumanBody.ConnectionBlood 실행시켜주기
        
        [SerializeField] private HeartModule heartModule;
        
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
            
            _myRectSize = _myRect.sizeDelta;    
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            
        }

        public void OnDrag(PointerEventData eventData)
        {
            MovePos(eventData.position);

            if (CalculateRectCorners())
                _myRect.sizeDelta = _myRectSize * 1.4f;
            else
                _myRect.sizeDelta = _myRectSize;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!CalculateRectCorners()) return;
            
            heartModule.InsertBlood(50);
            gameObject.SetActive(false);
        }

        private void MovePos(Vector2 eventDataPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventDataPos, null, 
                out Vector2 pos);
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
