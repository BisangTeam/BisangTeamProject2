using System;
using Hyunwoo.HumanBodySystem;
using UnityEngine;
using UnityEngine.UI;

namespace Hyunwoo.UI
{
    [RequireComponent(typeof(Slider))]
    public class GaugeSlider : MonoBehaviour
    {
        [SerializeField] private AbstractViscera module;
        private Slider _slider;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
        }

        private void OnEnable()
        {
            module.OnBloodGaugeChanged += HandleBloodGaugeChanged;
        }

        private void OnDisable()
        {
            module.OnBloodGaugeChanged -= HandleBloodGaugeChanged;
        }

        private void HandleBloodGaugeChanged()
        {
            _slider.value = module.BloodGauge / 100;
        }
    }
}
