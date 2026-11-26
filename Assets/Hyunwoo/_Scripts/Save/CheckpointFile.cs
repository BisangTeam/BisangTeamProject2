// 체크포인트 저장과 이어하기를 다시 켜려면 다음 줄의 주석을 해제한다.
// #define ENABLE_CHECKPOINT_SAVES

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Hyunwoo.Save
{
    public static class CheckpointFile
    {
#if ENABLE_CHECKPOINT_SAVES
        public static bool Enabled => true;
#else
        public static bool Enabled => false;
#endif

        public static string Location => Path.Combine(Application.persistentDataPath, "checkpoint-v1.json");

        public static CheckpointData Read(string path)
        {
            if (!Enabled) return null;
            return ReadOne(path) ?? ReadOne(path + ".bak");
        }

        private static CheckpointData ReadOne(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                var data = JsonUtility.FromJson<CheckpointData>(File.ReadAllText(path));
                if (data != null && data.version == 1 && !string.IsNullOrEmpty(data.scenePath) &&
                    !string.IsNullOrEmpty(data.pointId) && data.levels != null && data.FindLevel(data.scenePath) != null)
                {
                    data.levels.RemoveAll(level => level == null);
                    data.organs ??= new();
                    foreach (var level in data.levels) level.devices ??= new();
                    return data;
                }
                Debug.LogWarning($"Unrecognized checkpoint: {path}");
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            {
                Debug.LogWarning($"Could not read checkpoint: {e.Message}");
            }
            return null;
        }

        public static bool ResetMissionProgress(string path, string scenePath, ISet<string> missionDeviceIds)
        {
            if (!Enabled) return true;
            CheckpointData data = Read(path);
            if (data == null) return !File.Exists(path) && !File.Exists(path + ".bak");

            LevelProgress level = data.FindLevel(scenePath);
            if (level == null) return true;

            int clearedDevices = level.devices.RemoveAll(device => missionDeviceIds.Contains(device.id));
            if (!level.hasMission && string.IsNullOrEmpty(level.missionId) &&
                (level.objectiveCounts == null || level.objectiveCounts.Length == 0) && clearedDevices == 0)
                return true;

            level.hasMission = false;
            level.missionId = null;
            level.objectiveCounts = Array.Empty<int>();
            return Write(path, data);
        }

        public static bool Write(string path, CheckpointData data)
        {
            if (!Enabled) return false;
            string temporary = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogError($"Could not save checkpoint: {e.Message}");
                return false;
            }
        }
    }
}
