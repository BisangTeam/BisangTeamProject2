using System;
using System.IO;
using UnityEngine;

namespace Hyunwoo.Save
{
    public static class CheckpointFile
    {
        public static string Location => Path.Combine(Application.persistentDataPath, "checkpoint-v1.json");

        public static CheckpointData Read(string path)
        {
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

        public static bool Write(string path, CheckpointData data)
        {
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
