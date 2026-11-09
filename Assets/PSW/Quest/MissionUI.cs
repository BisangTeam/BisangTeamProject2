using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionUI : MonoBehaviour
{
    public static MissionUI instance;

    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    public void UpdateUI()
    {
        MissionDataSO mission = MissionManager.instance.GetcurrentMission();

        if (mission == null) return;

        titleText.text = mission.missionTitle;

        string result = "";
        for (int i = 0; i < mission.objectives.Count; i++)
        {
            MissionObjective obj = mission.objectives[i];
            int count = MissionManager.instance.GetCurrentCount(i);

            result += $"- {obj.description} ({count}/{obj.requiredCount})\n";
        }

        descriptionText.text = result;
    }

    public void ShowAllClear()
    {
        titleText.text = "모든 임무 완료";
        descriptionText.text = "더 이상 진행할 미션이 없습니다";
    }
}