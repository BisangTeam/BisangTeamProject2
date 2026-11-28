using System;
using Hyunwoo.Raycasting2D.Scripts;
using UnityEngine;

public class Heartbeat : MonoBehaviour
{
    [Header("박동 속도 (분당 횟수)")]
    [SerializeField, Min(1f)] private float restingBpm = 60f;
    [SerializeField, Min(1f)] private float encounterBpm = 120f;
    [SerializeField] private MonsterChase2D monster;

    [Header("작은 박동")]
    [SerializeField] private float firstScale = 1.08f;
    [SerializeField, Min(0.01f)] private float firstDuration = 0.07f;

    [Header("큰 박동")]
    [SerializeField] private float secondScale = 1.15f;
    [SerializeField, Min(0.01f)] private float secondDuration = 0.10f;

    [Header("두 박동 뒤 쉬는 시간")]
    [SerializeField, Min(0f)] private float restTime = 0.8f;

    public event Action StrongBeat;
    public float StrongPulse { get; private set; }
    public float PhaseSinceStrongBeat { get; private set; }
    public float BeatAdvance { get; private set; }
    public int StrongBeatsThisFrame { get; private set; }
    public float MonsterDistance => monster != null ? monster.DistanceToPlayer : float.PositiveInfinity;

    private Vector3 _baseScale;
    private float _bpm;
    private float _phase;

    private void Awake() => _baseScale = transform.localScale;

    private void OnEnable()
    {
        _bpm = restingBpm;
        _phase = 0f;
        UpdatePose(0f);
    }

    private void Update()
    {
        float targetBpm = monster != null && monster.PlayerInSight ? encounterBpm : restingBpm;
        _bpm = Mathf.MoveTowards(_bpm, targetBpm, 80f * Time.deltaTime);
        UpdatePose(_bpm * Time.deltaTime / 60f);
    }

    private void UpdatePose(float phaseAdvance)
    {
        float patternDuration = 2f * (firstDuration + secondDuration) + restTime;
        float strongPeak = 2f * firstDuration + secondDuration;
        float peakPhase = strongPeak / patternDuration;
        float nextPhase = _phase + phaseAdvance;
        BeatAdvance = phaseAdvance;
        StrongBeatsThisFrame = Mathf.FloorToInt(nextPhase - peakPhase) - Mathf.FloorToInt(_phase - peakPhase);
        _phase = Mathf.Repeat(nextPhase, 1f);
        PhaseSinceStrongBeat = Mathf.Repeat(_phase - peakPhase, 1f);

        float time = _phase * patternDuration;
        float smallPulse = Pulse(time, firstDuration, firstDuration);
        StrongPulse = Pulse(time, strongPeak, secondDuration);
        transform.localScale = _baseScale * (1f + (firstScale - 1f) * smallPulse + (secondScale - 1f) * StrongPulse);
        if (StrongBeatsThisFrame > 0) StrongBeat?.Invoke();
    }

    private static float Pulse(float time, float peak, float duration) =>
        Mathf.SmoothStep(1f, 0f, Mathf.Abs(time - peak) / duration);

    private void OnDisable()
    {
        StrongPulse = 0f;
        transform.localScale = _baseScale;
    }
}
