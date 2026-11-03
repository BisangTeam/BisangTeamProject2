using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Seonho._01.Scripts.LineRenderer
{
    public class HeartRateManager : MonoBehaviour
    {
        [SerializeField] private HeartRate _heartRate1;
        [SerializeField] private HeartRateWave _heartRateWave;
        [SerializeField] private HeartRate _heartRate2;
        
        [SerializeField] private int _heartCount;
        [SerializeField] private int _heartWaveCount;

        [SerializeField] private bool _onMonitoring;

        private void Awake()
        {
            _heartRate1 = transform.GetChild(0).GetComponentInChildren<HeartRate>();
            _heartRateWave = GetComponentInChildren<HeartRateWave>();
            _heartRate2 = transform.GetChild(2).GetComponentInChildren<HeartRate>();

            Initialize();

            _heartRate1.StartCoroutine(_heartRate1.ComingLineLogic());
        }

        private void Update()
        {
            if (_heartRate1.end && !_onMonitoring)
            {
                _onMonitoring = true;
            }
        }

        IEnumerator HeartRateLogic(float value)
        {
            _heartRate1.StartCoroutine(_heartRate1.ComingLineLogic());
            yield return new WaitForSeconds(_heartRate1.lineRenderer.positionCount * _heartRate1.goingTime);
            HeartWave(value);
        }
        
        void HeartWave(float value)
        {
            for (int i = 1; i < _heartRateWave.lineRenderer.positionCount - 1; i++)
            {
                if (i == 1)
                {
                    _heartRateWave.ChangeLine(i, -value / 2.5f); // 0.6 1.5 0.4
                }
                else if (i == 2)
                {
                    _heartRateWave.ChangeLine(i, value); // 0.6 1.5 0.4
                }
                else if (i == 3)
                {
                    _heartRateWave.ChangeLine(i, -value / 3.8f); // 0.6 1.5 0.4
                }

                _heartRateWave.ResetLine(i);
            }

            _heartRate2.StartCoroutine(_heartRate2.ComingLineLogic());
        }

        void Initialize()
        {
            _heartRate1.InitializeLineRenderer(_heartCount);
            _heartRate2.InitializeLineRenderer(_heartCount);
            _heartRateWave.InitializeLineRenderer(_heartWaveCount);
        }
    }
}
