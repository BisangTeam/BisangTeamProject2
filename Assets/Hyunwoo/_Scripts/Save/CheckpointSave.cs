using System.Collections.Generic;
using Hyunwoo.CoreLib.ModuleSystem;
using Hyunwoo.HumanBodySystem;
using Hyunwoo.Raycasting2D.Scripts;
using UnityEngine;

namespace Hyunwoo.Save
{
    [DefaultExecutionOrder(300)]
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerInventory))]
    public sealed class CheckpointSave : MonoBehaviour
    {
        [SerializeField] private HeartModule heart;
        [SerializeField] private MissionManager missions;
        [SerializeField] private WallMarker2D wallMarks;

        private readonly List<CheckpointPoint> _points = new();
        private readonly List<CheckpointDevice> _devices = new();
        private AbstractViscera[] _organs;
        private Rigidbody2D _body;
        private PlayerInventory _inventory;
        private CheckpointData _data;
        private CheckpointPoint _restoredPoint;
        private CheckpointPoint _pending;
        private float _messageUntil;
        private string _message;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _inventory = GetComponent<PlayerInventory>();
            _organs = heart.GetComponentInParent<ModuleOwner>().GetComponentsInChildren<AbstractViscera>(true);
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                _points.AddRange(root.GetComponentsInChildren<CheckpointPoint>(true));
                _devices.AddRange(root.GetComponentsInChildren<CheckpointDevice>(true));
            }
            _data = CheckpointFile.Read(CheckpointFile.Location);
            LevelProgress level = _data?.FindLevel(gameObject.scene.path);
            if (missions != null && level != null && level.hasMission)
                missions.PrepareResume(level.missionId, level.objectiveCounts);
        }

        private void Start()
        {
            if (_data == null) return;
            LevelProgress level = _data.FindLevel(gameObject.scene.path);
            if (level != null)
            {
                foreach (var state in level.devices)
                    _devices.Find(device => device.Id == state.id)?.SetUsed(state.used);
                if (wallMarks != null) wallMarks.Restore(level.marks);
            }

            heart.BloodGauge = _data.heartBlood;
            foreach (AbstractViscera organ in _organs)
            {
                int index = _data.organs.FindIndex(state => state.id == organ.GetType().Name);
                if (index < 0) continue;
                OrganBlood state = _data.organs[index];
                organ.BloodGauge = state.blood;
                if (state.connected && organ.TryGetComponent(out institution socket)) socket.RestoreConnection();
            }
            _inventory.Restore(_data.items);
            if (_data.scenePath != gameObject.scene.path) return;

            _restoredPoint = _points.Find(point => point.Id == _data.pointId);
            if (_restoredPoint == null) return;
            _body.position = _restoredPoint.transform.position;
            _body.rotation = _restoredPoint.transform.eulerAngles.z;
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            Physics2D.SyncTransforms();
            Notify($"Checkpoint {_restoredPoint.Id} loaded");
        }

        public void Reach(CheckpointPoint point)
        {
            if (!CheckpointFile.Enabled) return;
            if (point != _restoredPoint && !string.IsNullOrEmpty(point.Id)) _pending = point;
        }

        public void Leave(CheckpointPoint point)
        {
            if (point == _restoredPoint) _restoredPoint = null;
        }

        private void LateUpdate()
        {
            if (_pending == null) return;
            SaveAt(_pending);
            _pending = null;
        }

        private void SaveAt(CheckpointPoint point)
        {
            string path = gameObject.scene.path;
            _data ??= new CheckpointData();
            _data.scenePath = path;
            _data.pointId = point.Id;
            _data.heartBlood = heart.BloodGauge;
            foreach (AbstractViscera organ in _organs)
            {
                string id = organ.GetType().Name;
                _data.organs.RemoveAll(previous => previous.id == id);
                _data.organs.Add(new OrganBlood
                {
                    id = id,
                    blood = organ.BloodGauge,
                    connected = organ.TryGetComponent(out institution socket) && socket.Connected
                });
            }
            _data.items = _inventory.Capture();
            var level = new LevelProgress { scenePath = path, hasMission = missions != null };
            if (missions != null)
            {
                level.missionId = missions.GetcurrentMission()?.SaveId;
                level.objectiveCounts = missions.CaptureCounts();
            }
            foreach (CheckpointDevice device in _devices)
                level.devices.Add(new DeviceState { id = device.Id, used = device.Used });
            if (wallMarks != null) level.marks = wallMarks.Capture();
            _data.levels.RemoveAll(entry => entry.scenePath == path);
            _data.levels.Add(level);
            bool saved = CheckpointFile.Write(CheckpointFile.Location, _data);
            Notify(saved ? $"Checkpoint {point.Id} saved" : "Checkpoint save failed");
        }

        private void Notify(string message)
        {
            _message = message;
            _messageUntil = Time.unscaledTime + 2f;
        }

        private void OnGUI()
        {
            if (Time.unscaledTime < _messageUntil)
                GUI.Box(new Rect(14, Screen.height - 45, 210, 30), _message);
        }
    }
}
