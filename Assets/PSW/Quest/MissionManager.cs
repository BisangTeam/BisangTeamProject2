using System;
using System.Collections.Generic;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    public static MissionManager instance;
    public event Action<MissionDataSO> MissionChanged;
    
    [Header("시작")]
    [SerializeField] private MissionDataSO startMission;

    [Header("진행상황")]
    [SerializeField] private MissionDataSO currentMission;
    [SerializeField] private List<int> Counts = new List<int>();

    public MissionDataSO GetcurrentMission()
    {
        return currentMission;
    }

    public int GetCurrentCount(int index)
    {
        return Counts[index];
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetMission(startMission);
    }

    private void SetMission(MissionDataSO newmission)
    {
        currentMission = newmission;
        Counts.Clear();

        if (currentMission != null)
        {
            for (int i = 0; i < currentMission.objectives.Count; i++)
            {
                Counts.Add(0);
            }
        }

        if (MissionUI.instance != null)
        {
            if (currentMission != null) MissionUI.instance.UpdateUI();
            else MissionUI.instance.ShowAllClear();
        }
        MissionChanged?.Invoke(currentMission);
    }

    public void ProcessAction(MissionEventType eventType, string targetId)
    {
        if (currentMission == null) return;
        
        bool isUpdated = false;

        for (int i = 0; i < currentMission.objectives.Count; i++)
        {
            MissionObjective obj = currentMission.objectives[i];

            if (obj.eventType == eventType && obj.targetId == targetId)
            {
                if (Counts[i] < obj.requiredCount)
                {
                    Counts[i]++;
                    isUpdated = true;
                    Debug.Log($"미션 진행: {obj.description}: {Counts[i]}/{obj.requiredCount}");
                }
            }
        }
        
        if (isUpdated)
        {
            if (MissionUI.instance != null)
            {
                MissionUI.instance.UpdateUI();
            }

            CheckMissionClear();
        }
    }

    private void CheckMissionClear()
    {
        bool allClear = true;

        for (int i = 0; i < currentMission.objectives.Count; i++)
        {
            if (Counts[i] < currentMission.objectives[i].requiredCount)
            {
                allClear = false;
                break;
            }
        }

        if (allClear)
        {
            Debug.Log($"미션 완료 {currentMission.missionTitle}");

            SetMission(currentMission.nextMission);
        }
    }
}
