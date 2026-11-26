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
        private Image image;
        private float elapsed;
        private int displayedFrame;

        private void Awake()
        {
            image = GetComponent<Image>();
        }

        private void OnEnable()
        {
            elapsed = 0f;
            displayedFrame = 0;
            if (frames != null && frames.Length != 0)
                image.sprite = frames[0];
        }

        private void Update()
        {
            if (frames == null || frames.Length < 2)
                return;

            elapsed = (elapsed + Time.deltaTime) % (frames.Length * SecondsPerFrame);
            int nextFrame = Mathf.Min((int)(elapsed / SecondsPerFrame), frames.Length - 1);
            if (nextFrame == displayedFrame)
                return;

            image.sprite = frames[nextFrame];
            displayedFrame = nextFrame;
        }
    }
}
