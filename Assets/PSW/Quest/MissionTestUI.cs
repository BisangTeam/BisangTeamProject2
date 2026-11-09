using UnityEngine;

public class MissionTestUI : MonoBehaviour
{
    public void OnClick_Interact()
    {
        MissionManager.instance.ProcessAction(MissionEventType.Interact, "Device_01");
    }

    public void OnClick_EnterArea()
    {
        MissionManager.instance.ProcessAction(MissionEventType.EnterArea, "Room_A");
    }

    public void OnClick_Collect()
    {
        MissionManager.instance.ProcessAction(MissionEventType.Collect, "Item_Key");
    }

    public void OnClick_GameFlag()
    {
        MissionManager.instance.ProcessAction(MissionEventType.GameFlag, "Flag_BossDead");
    }
}