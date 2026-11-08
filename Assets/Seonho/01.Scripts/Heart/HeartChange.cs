 using System;
 using System.Collections;
 using System.Numerics;
 using DG.Tweening;
 using Unity.VisualScripting;
 using UnityEngine;
 using UnityEngine.InputSystem;
 using Sequence = DG.Tweening.Sequence;
 using Vector3 = UnityEngine.Vector3;

 namespace Seonho._01.Scripts.Heart
 {
     public class HeartChange : MonoBehaviour
     {
         [SerializeField] private Vector3[] _heartSequenceAct;
         
         [SerializeField] private float _nextTime;
         
         private Sequence heartSequence;

         private void OnEnable()
         {
             HeartBeat();
         }

         private void Update()
         {
             
             if(Keyboard.current.escapeKey.wasPressedThisFrame)
                HeartBeat();
         }

         void HeartBeat()
         {
             transform.localScale = _heartSequenceAct[1];
             
             heartSequence = DOTween.Sequence();
             heartSequence.Append(transform.DOScale(_heartSequenceAct[0], 0.3f));
             heartSequence.Append(transform.DOScale(_heartSequenceAct[1], 0.3f));
             heartSequence.Append(transform.DOScale(_heartSequenceAct[2], 0.3f));
         }
     }
 }
