using System;
using System.Collections.Generic;
using Hyunwoo.Raycasting2D.Scripts;

namespace Hyunwoo.Save
{
    [Serializable]
    public sealed class CheckpointData
    {
        public int version = 1;
        public string scenePath;
        public string pointId;
        public List<OrganBlood> organs = new();
        public List<ItemQuantity> items = new();
        public List<LevelProgress> levels = new();

        public LevelProgress FindLevel(string path) => levels.Find(level => level != null && level.scenePath == path);
    }

    [Serializable]
    public struct OrganBlood
    {
        public string id;
        public float blood;
        public bool connected;
    }

    [Serializable]
    public struct ItemQuantity
    {
        public string id;
        public int quantity;
    }

    [Serializable]
    public struct DeviceState
    {
        public string id;
        public bool used;
    }

    [Serializable]
    public sealed class LevelProgress
    {
        public string scenePath;
        public bool hasMission;
        public string missionId;
        public int[] objectiveCounts;
        public List<DeviceState> devices = new();
        public List<WallFace> marks = new();
    }
}
