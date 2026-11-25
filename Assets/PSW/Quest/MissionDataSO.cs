using System.Collections.Generic;
using UnityEngine;

//이벤트 종류
public enum MissionEventType
{
    Interact,
    EnterArea,
    Collect,
    GameFlag
}

//목표 세팅
[System.Serializable]
public class MissionObjective
{
    public string description;
    public MissionEventType eventType;
    public string targetId;
    public int requiredCount = 1;
}

//SO
[CreateAssetMenu(fileName = "NewMission", menuName = "SO/MissionData")]
public class MissionDataSO : ScriptableObject
{
    [SerializeField] private string saveId;
    public string SaveId => string.IsNullOrEmpty(saveId) ? name : saveId;
    public string missionTitle;
    public List<MissionObjective> objectives;
    public MissionDataSO nextMission;
}
