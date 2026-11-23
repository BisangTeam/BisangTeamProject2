using System.Collections.Generic;
using UnityEngine;

namespace Hyunwoo.Raycasting2D.Scripts
{
    // Transform.position의 XY는 사물 발밑의 맵 위치다.
    // 실제 회전이나 SpriteRenderer 처리는 하지 않고 RaycastRenderer2D가 결과 화면에 그린다.
    public class RaycastSprite2D : MonoBehaviour
    {
        // 화면에 그릴 독립 PNG 이미지. Sprite가 아닌 Texture2D를 넣는다.
        [SerializeField] private Texture2D image;
        // 게임 안에서의 실제 높이. 벽 높이가 1일 때 1이면 벽과 같은 높이다.
        [SerializeField, Min(0.01f)] private float height = 1f;
        // 바닥에서 띄울 높이. 0이면 발밑이 바닥에 붙는다.
        [SerializeField, Min(0f)] private float elevation = 0f;

        // static이므로 모든 사물이 표시 대상 목록 하나를 공유한다.
        private static readonly List<RaycastSprite2D> ActiveSprites = new();

        // 렌더러에는 목록을 읽는 용도로만 공개한다.
        public static IReadOnlyList<RaycastSprite2D> Active => ActiveSprites;
        // 렌더러가 이 사물의 이미지를 읽는다.
        public Texture2D Image => image;
        // 렌더러가 화면에 그릴 크기를 계산할 때 쓰는 실제 높이다.
        public float Height => height;
        // 렌더러가 발밑의 화면 위치를 계산할 때 쓰는 바닥 위 높이다.
        public float Elevation => elevation;

        private void OnEnable()
        {
            // 활성화되면 자동 등록한다. 이미 있으면 중복으로 넣지 않는다.
            if (!ActiveSprites.Contains(this))
                ActiveSprites.Add(this);
        }

        private void OnDisable()
        {
            // 비활성화되거나 파괴되면 표시 대상에서 빠진다.
            ActiveSprites.Remove(this);
        }
    }
}
