using UnityEngine;
using UnityEngine.UI;

namespace Jun.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class HeartGifPlayer : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;

        private const float SecondsPerFrame = 0.1f;
        // Heart_02 is the fullest frame in this sprite sequence.
        private const int PeakFrame = 2;
        private Heartbeat heartbeat;
        private Image image;
        private float elapsed;
        private int displayedFrame;

        private void Awake()
        {
            image = GetComponent<Image>();
            heartbeat = GetComponentInParent<Heartbeat>();
        }

        private void OnEnable()
        {
            elapsed = 0f;
            displayedFrame = 0;
            if (frames != null && frames.Length != 0)
                image.sprite = frames[0];
        }

        private void LateUpdate()
        {
            if (frames == null || frames.Length < 2)
                return;

            int nextFrame;
            if (heartbeat != null)
            {
                if (!heartbeat.isActiveAndEnabled) return;
                nextFrame = (PeakFrame + Mathf.RoundToInt(heartbeat.PhaseSinceStrongBeat * frames.Length)) % frames.Length;
            }
            else
            {
                elapsed = (elapsed + Time.deltaTime) % (frames.Length * SecondsPerFrame);
                nextFrame = Mathf.Min((int)(elapsed / SecondsPerFrame), frames.Length - 1);
            }
            if (nextFrame == displayedFrame)
                return;

            image.sprite = frames[nextFrame];
            displayedFrame = nextFrame;
        }
    }
}
