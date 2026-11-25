using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hyunwoo.Raycasting2D.Scripts
{
    public sealed class RaycastLight2D : MonoBehaviour
    {
        [SerializeField, Range(0f, 2f)] private float brightness = 0.85f;
        [SerializeField, Min(0.05f)] private float radius = 2.4f;
        [SerializeField] private bool flicker;
        [SerializeField, Min(0.1f)] private float flickerSpeed = 4f;

        private static readonly List<RaycastLight2D> Active = new();

        public readonly struct Sample
        {
            private readonly Vector2 _center;
            private readonly float _inverseRadiusSquared;
            private readonly float _brightness;

            public Sample(Vector2 center, float radius, float brightness)
            {
                _center = center;
                _inverseRadiusSquared = 1f / (radius * radius);
                _brightness = brightness;
            }

            public float At(Vector2 point)
            {
                float falloff = Mathf.Max(0f, 1f - (point - _center).sqrMagnitude * _inverseRadiusSquared);
                return _brightness * falloff * falloff;
            }
        }

        private void OnEnable() => Active.Add(this);
        private void OnDisable() => Active.Remove(this);

        public static void Collect(Scene scene, RaycastView2D view, float time, List<Sample> samples)
        {
            samples.Clear();
            foreach (RaycastLight2D light in Active)
            {
                Vector2 center = light.transform.position;
                float reach = view.MaxDistance + light.radius;
                if (light.gameObject.scene != scene || (center - view.Origin).sqrMagnitude > reach * reach) continue;

                float intensity = light.brightness;
                if (light.flicker)
                {
                    float noise = Mathf.PerlinNoise(center.x + 17.3f, center.y + time * light.flickerSpeed);
                    intensity *= Mathf.Lerp(0.12f, 1f, Mathf.InverseLerp(0.32f, 0.66f, noise));
                }
                samples.Add(new Sample(center, light.radius, intensity));
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
