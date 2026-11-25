using UnityEngine;
using Hyunwoo.Save;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Hyunwoo.Mission
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class MissionTrigger2D : MonoBehaviour
    {
        [SerializeField] private MissionManager missions;
        [SerializeField] private MissionDataSO mission;
        [Tooltip("이 트리거가 달성할 목표의 번호. 첫 번째 목표는 0입니다.")]
        [SerializeField, Min(0)] private int objectiveIndex;
        [SerializeField] private Rigidbody2D player;
        [SerializeField] private UnityEvent onAccepted = new UnityEvent();

        private BoxCollider2D _area;
        private bool _used;
        [SerializeField] private CheckpointDevice savedDevice;

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void Awake()
        {
            _area = GetComponent<BoxCollider2D>();
            if (missions == null || player == null || mission == null || mission.objectives == null ||
                objectiveIndex < 0 || objectiveIndex >= mission.objectives.Count)
            {
                Debug.LogError("미션 트리거의 매니저, 플레이어, 미션, 목표 번호를 확인하세요.", this);
                enabled = false;
                return;
            }
            missions.MissionChanged += ResetUse;
        }

        private void OnDestroy()
        {
            if (missions != null) missions.MissionChanged -= ResetUse;
        }

        private void ResetUse(MissionDataSO current)
        {
            if (current != mission) return;
            _used = false;
            if (savedDevice != null) savedDevice.SetUsed(false);
        }

        private void Update()
        {
            if (_used || (savedDevice != null && savedDevice.Used) ||
                missions.GetcurrentMission() != mission || Time.timeScale == 0f) return;
            if (!player.IsTouching(_area)) return;

            MissionObjective objective = mission.objectives[objectiveIndex];
            if (objective.eventType == MissionEventType.GameFlag) return;
            if (objective.eventType == MissionEventType.Interact &&
                (Keyboard.current == null || !Keyboard.current.fKey.wasPressedThisFrame)) return;
            if (missions.GetCurrentCount(objectiveIndex) >= objective.requiredCount) return;

            // 완료 처리 도중 다음 미션이 동기적으로 시작될 수 있다.
            _used = true;
            if (savedDevice != null) savedDevice.SetUsed(true);
            missions.ProcessAction(objective.eventType, objective.targetId);
            onAccepted.Invoke();
        }
    }
}
