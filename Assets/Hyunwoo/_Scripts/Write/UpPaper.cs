using System;
using DG.Tweening;
using UnityEngine;

public class UpPaper : MonoBehaviour
{
    [SerializeField] private float endValue= 0;
    [SerializeField] private float duration = 0.2f;

    private void Start()
    {
        Sequence seq = DOTween.Sequence();
        
        seq.Append(transform.DOMoveY(endValue, duration).SetEase(Ease.InQuad));
    }
}
