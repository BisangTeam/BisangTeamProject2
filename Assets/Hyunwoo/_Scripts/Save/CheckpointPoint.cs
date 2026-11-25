using UnityEngine;

namespace Hyunwoo.Save
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class CheckpointPoint : MonoBehaviour
    {
        [SerializeField] private string pointId;
        public string Id => pointId;

        private void Reset() => GetComponent<BoxCollider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent(out CheckpointSave save))
                save.Reach(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent(out CheckpointSave save))
                save.Leave(this);
        }
    }
}
