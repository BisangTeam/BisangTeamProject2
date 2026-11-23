using System;
using DG.Tweening;
using UnityEngine;

namespace Seonho._01.Scripts
{
    public class DocumentView : MonoBehaviour
    {
        [SerializeField] private float duration;
         private AudioSource _audioCompo;

         private void Awake()
         {
            _audioCompo = GetComponent<AudioSource>();
         }

         private void OnEnable()
        { 
            SlideViewDocument();
        }

        void SlideViewDocument()
        {
            gameObject.transform.position = new Vector3(0, -10, 0);
            gameObject.SetActive(true);
            gameObject.transform.DOMove(new Vector3(0,-1.5f,0), duration).SetEase(Ease.OutQuint);
            _audioCompo.Play();
        }
    }
}
