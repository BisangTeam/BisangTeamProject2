using System.Collections;
using Hyunwoo.Raycasting2D.Scripts;
using UnityEngine;

namespace Hyunwoo.Mission
{
    [RequireComponent(typeof(MissionManager))]
    public class MonsterDirector2D : MonoBehaviour
    {
        [SerializeField] private MonsterChase2D monster;
        [Tooltip("등장 위치와 이동 목적지를 자식으로 가진 오브젝트")]
        [SerializeField] private Transform points;

        private MissionManager _missions;

        private void Awake()
        {
            _missions = GetComponent<MissionManager>();
            if (monster == null)
            {
                Debug.LogError("미션 연출에 사용할 괴물을 연결하세요.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _missions.MissionChanged += BeginMission;
            BeginMission(_missions.GetcurrentMission());
        }

        private void OnDisable()
        {
            _missions.MissionChanged -= BeginMission;
            StopAllCoroutines();
            if (monster != null) HideMonster();
        }

        private void BeginMission(MissionDataSO mission)
        {
            StopAllCoroutines();
            HideMonster();
            if (mission is StoryMissionSO story && story.monsterActions.Length > 0)
                StartCoroutine(Play(story));
        }

        private IEnumerator Play(StoryMissionSO story)
        {
            foreach (MonsterAction action in story.monsterActions)
            {
                switch (action.type)
                {
                    case MonsterActionType.Appear:
                        if (!TryGetPoint(action.pointName, out Vector3 spawn)) yield break;
                        monster.StopChase();
                        monster.transform.position = spawn;
                        monster.gameObject.SetActive(true);
                        break;

                    case MonsterActionType.MoveTo:
                        if (!TryGetPoint(action.pointName, out Vector3 destination)) yield break;
                        monster.StopChase();
                        while (monster.transform.position != destination)
                        {
                            monster.transform.position = Vector3.MoveTowards(monster.transform.position,
                                destination, Mathf.Max(0.01f, action.speed) * Time.deltaTime);
                            yield return null;
                        }
                        break;

                    case MonsterActionType.Wait:
                        yield return new WaitForSeconds(action.seconds);
                        break;

                    case MonsterActionType.Disappear:
                        HideMonster();
                        break;

                    case MonsterActionType.StartChase:
                        if (!monster.StartChase()) yield break;
                        break;
                }
            }

            // 즉시 끝나는 연출도 다음 미션을 같은 호출 안에서 연달아 시작하지 않는다.
            yield return null;
            if (_missions.GetcurrentMission() == story)
                _missions.ProcessAction(MissionEventType.GameFlag, StoryMissionSO.SequenceFinished);
        }

        private bool TryGetPoint(string pointName, out Vector3 position)
        {
            Transform point = points != null && !string.IsNullOrEmpty(pointName) ? points.Find(pointName) : null;
            if (point != null)
            {
                position = point.position;
                return true;
            }

            Debug.LogError($"미션 '{_missions.GetcurrentMission().name}'의 연출 지점 '{pointName}'을 찾을 수 없습니다.", this);
            position = default;
            return false;
        }

        private void HideMonster()
        {
            monster.StopChase();
            monster.gameObject.SetActive(false);
        }
    }
}
