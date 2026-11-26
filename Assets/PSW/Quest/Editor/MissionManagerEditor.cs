using System.Collections.Generic;
using Hyunwoo.Mission;
using Hyunwoo.Save;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(MissionManager))]
[CanEditMultipleObjects]
public sealed class MissionManagerEditor : Editor
{
    private string _message;
    private MessageType _messageType;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var manager = (MissionManager)target;
        Scene scene = manager.gameObject.scene;
        bool isPlaying = Application.IsPlaying(manager);
        bool canReset = targets.Length == 1 && scene.IsValid() && scene.isLoaded &&
                        PrefabStageUtility.GetPrefabStage(manager.gameObject) == null &&
                        (isPlaying ? manager.didStart : !string.IsNullOrEmpty(scene.path));

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!canReset))
        {
            if (GUILayout.Button(new GUIContent("미션 진행도 초기화", "이 씬의 저장된 미션 진행도와 미션 장치 사용 기록을 초기화합니다.")))
            {
                bool success = ResetProgress(manager, CheckpointFile.Location);
                _messageType = success ? MessageType.Info : MessageType.Error;
                _message = success
                    ? (isPlaying ? "첫 미션부터 다시 시작했습니다." : "다음 플레이에서 첫 미션부터 시작합니다.")
                    : "체크포인트를 초기화할 수 없습니다. 콘솔에서 저장 파일 상태를 확인하세요.";
            }
        }

        if (_message != null) EditorGUILayout.HelpBox(_message, _messageType);
    }

    internal static bool ResetProgress(MissionManager manager, string checkpointPath)
    {
        Scene scene = manager.gameObject.scene;
        var triggers = new List<MissionTrigger2D>();
        var deviceIds = new HashSet<string>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (var trigger in root.GetComponentsInChildren<MissionTrigger2D>(true))
            {
                if (trigger.Missions != manager) continue;
                triggers.Add(trigger);
                if (!string.IsNullOrEmpty(trigger.SavedDeviceId)) deviceIds.Add(trigger.SavedDeviceId);
            }
        }

        if (!string.IsNullOrEmpty(scene.path) &&
            !CheckpointFile.ResetMissionProgress(checkpointPath, scene.path, deviceIds)) return false;

        if (Application.IsPlaying(manager))
        {
            foreach (var trigger in triggers) trigger.ResetProgress();
            manager.RestartMissions();
        }
        return true;
    }
}
