using Hyunwoo.Raycasting2D.Scripts;
using UnityEngine;

namespace Hyunwoo.Save
{
    public sealed class CheckpointDevice : MonoBehaviour
    {
        [SerializeField] private string deviceId;
        [SerializeField] private bool used;
        [SerializeField] private GameObject availableView;
        [SerializeField] private GameObject usedView;
        [SerializeField] private RaycastDoor2D door;

        public string Id => deviceId;
        public bool Used => door != null ? door.TargetOpen : used;

        private void Start() => RefreshViews();

        public void SetUsed(bool value)
        {
            used = value;
            if (door != null) door.RestoreOpen(value);
            RefreshViews();
        }

        private void RefreshViews()
        {
            if (availableView != null) availableView.SetActive(!Used);
            if (usedView != null) usedView.SetActive(Used);
        }
    }
}
