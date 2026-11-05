 using System;
 using System.Collections;
 using System.Numerics;
 using DG.Tweening;
 using Unity.VisualScripting;
 using UnityEngine;
 using Sequence = DG.Tweening.Sequence;
 using Vector3 = UnityEngine.Vector3;

 namespace Seonho._01.Scripts.Heart
 {
     public class HeartChange : MonoBehaviour
     {
         [SerializeField] private Vector3[] _heartSequence;
         
         [SerializeField] private float _nextTime;
         
         [SerializeField] private float _contractTime;
         [SerializeField] private float _expandTime;

         private Sequence heartSequence;

         public Action OnHeart;

         private void OnEnable()
         {
             StartHeart();
         }

         void StartHeart()
         {
             transform.localScale = _heartSequence[1];
             
             heartSequence = DOTween.Sequence();
             //heartSequence.
             heartSequence.AppendInterval(_nextTime);
         }
         
         public IEnumerator HeartChangeCoroutine()
         {
             //HeartContract();
             yield return new WaitForSeconds(_nextTime + _contractTime);
             HeartExpand();
         }
         
         void HeartContract(float value)
         {
             transform.DOScale(new Vector3(), _contractTime);
         }

         void HeartExpand()
         {
             transform.DOScale(Vector3.one,  _expandTime);
         }
     }
 }
