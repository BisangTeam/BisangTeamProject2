using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Hyunwoo.CoreLib.ModuleSystem;
using UnityEngine;

namespace Hyunwoo.HumanBodySystem
{
    public class HeartModule : MonoModule
    {
        [Header("1회 혈액 생성량")]
        [SerializeField] private float defaultGetBloodValue = 5f;

        [Header("혈액 생성 간격(초)")]
        [SerializeField] private float delaySeconds = 2f;

        [Header("심장 혈액 저장 최대치")]
        [SerializeField] private float maxGauge;

        [Header("게임 시작 시 심장 혈액량")]
        [SerializeField] private float defaultBloodGague;

        [Header("Compo")]
        [SerializeField] private HeartGauge heartGauge;

        [Header("박동 연출")]
        [SerializeField] private RectTransform heartVisual;

        private Sequence _heartbeatSequence;

        #region Properties

        private float _bloodGauge;
        public float BloodGauge
        {
            get => _bloodGauge;
            set
            {
                _bloodGauge = Mathf.Clamp(value, 0, maxGauge);
                heartGauge.Gauge = BloodGauge;
            }
        }

        private int _connectCount;
        public int ConnectCount
        {
            get => _connectCount;
            set
            {
                _connectCount = value;
                if (_connectCount <= 0)
                    _getBloodValue = defaultGetBloodValue;
                else
                    _getBloodValue = defaultGetBloodValue / _connectCount;
            }
        }

        #endregion

        public event Action<float> OnHeartRate;

        private float _getBloodValue;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _getBloodValue = defaultGetBloodValue;
            BloodGauge = defaultBloodGague;
        }

        private void Start()
        {
            SetupHeartbeat();
            TestAsync().Forget();
        }

        private void SetupHeartbeat()
        {
            if (heartVisual == null)
                return;

            Vector3 baseScale = heartVisual.localScale;

            _heartbeatSequence = DOTween.Sequence();
            _heartbeatSequence.Append(heartVisual.DOScale(baseScale * 1.08f, 0.07f));
            _heartbeatSequence.Append(heartVisual.DOScale(baseScale, 0.07f));
            _heartbeatSequence.Append(heartVisual.DOScale(baseScale * 1.15f, 0.10f));
            _heartbeatSequence.Append(heartVisual.DOScale(baseScale, 0.10f));
            _heartbeatSequence.SetAutoKill(false);
            _heartbeatSequence.SetLink(gameObject);
            _heartbeatSequence.Pause();
        }

        private async UniTask TestAsync()
        {
            _heartbeatSequence?.Restart();

            BloodGauge += _getBloodValue;
            OnHeartRate?.Invoke(_getBloodValue);

            BloodGauge -= _getBloodValue * ConnectCount;

            await UniTask.Delay(
                (int)(delaySeconds * 1000),
                cancellationToken: destroyCancellationToken
            );

            TestAsync().Forget();
        }

        public void InsertBlood(float value)
        {
            BloodGauge += value;
        }

        private void ChangeHeartRate(float value)
        {
        }
    }
}