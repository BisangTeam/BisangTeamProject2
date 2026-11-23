using System;
using UnityEngine;

namespace Hyunwoo.Mission
{
    public enum MonsterActionType
    {
        Appear,
        MoveTo,
        Wait,
        Disappear,
        StartChase
    }

    [Serializable]
    public class MonsterAction
    {
        public MonsterActionType type;
        [Tooltip("Appear 또는 MoveTo에서 사용할 Points 아래 오브젝트 이름")]
        public string pointName;
        [Tooltip("MoveTo 이동 속도 (유닛/초)"), Min(0.01f)] public float speed = 3f;
        [Tooltip("Wait 대기 시간 (초)"), Min(0f)] public float seconds = 0.5f;
    }

    [CreateAssetMenu(fileName = "StoryMission", menuName = "SO/Mission/Story Mission")]
    public class StoryMissionSO : MissionDataSO
    {
        public const string SequenceFinished = "MonsterSequenceFinished";

        [Tooltip("위에서 아래로 실행합니다. 연출 종료를 목표로 삼으려면 GameFlag의 Target Id를 MonsterSequenceFinished로 설정하세요.")]
        public MonsterAction[] monsterActions = Array.Empty<MonsterAction>();
    }
}
