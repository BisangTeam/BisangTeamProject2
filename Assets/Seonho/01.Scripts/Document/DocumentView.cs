using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Seonho._01.Scripts
{
    public class DocumentView : MonoBehaviour
    {
        [SerializeField] private float duration;

        private void OnEnable()
        { 
            SlideViewDocument();
        }

        void SlideViewDocument()
        {
            gameObject.transform.position = new Vector3(0, -10, 0);
            gameObject.SetActive(true);
            gameObject.transform.DOMove(new Vector3(0,-1.5f,0), duration).SetEase(Ease.OutQuint);
        }
    }
}
