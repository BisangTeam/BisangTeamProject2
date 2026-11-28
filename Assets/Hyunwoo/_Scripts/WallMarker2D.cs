using System;
using System.Collections.Generic;
using Hyunwoo.HumanBodySystem;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace Hyunwoo.Raycasting2D.Scripts
{
    [Serializable]
    public struct WallFace
    {
        public string mapId;
        public Vector3Int cell;
        public Vector3Int side;
        // 타일맵 기준의 접촉 위치와 표면 방향을 체크포인트에 기록한다.
        public Vector2 localPoint;
        public Vector2 localNormal;
    }

    public sealed class WallMarker2D : MonoBehaviour
    {
        [Serializable]
        private struct WallMap
        {
            public string id;
            public Tilemap tilemap;
        }

        private const string MainMapId = "main";
        [SerializeField] private HandModule hand;
        [SerializeField] private Tilemap walls;
        [Tooltip("추가 벽 타일맵마다 세이브에서 계속 사용할 고유 ID를 지정하세요. main은 기본 벽에 사용됩니다.")]
        [SerializeField] private WallMap[] additionalWalls = Array.Empty<WallMap>();
        [SerializeField] private LayerMask wallMask;
        [SerializeField, Min(0.1f)] private float reach = 1.6f;
        [SerializeField] private RaycastWallDecoration2D markPrefab;
        [SerializeField] private Key markKey = Key.M;

        private readonly List<(WallFace location, RaycastWallDecoration2D decoration)> _marks = new();
        private readonly Dictionary<string, Tilemap> _mapsById = new();
        private readonly Dictionary<Tilemap, string> _idsByMap = new();
        private readonly RaycastHit2D[] _hits = new RaycastHit2D[1];

        private void Awake()
        {
            if (walls != null) RegisterMap(MainMapId, walls);
            foreach (WallMap entry in additionalWalls)
                RegisterMap(entry.id, entry.tilemap);
        }

        private void RegisterMap(string id, Tilemap map)
        {
            if (map == null || string.IsNullOrWhiteSpace(id) || _mapsById.ContainsKey(id) || _idsByMap.ContainsKey(map))
            {
                Debug.LogWarning("벽 표식에 사용할 타일맵과 고유 ID를 확인하세요.", this);
                return;
            }
            _mapsById.Add(id, map);
            _idsByMap.Add(map, id);
        }

        private void Update()
        {
            if (Time.timeScale > 0f && Keyboard.current != null && Keyboard.current[markKey].wasPressedThisFrame)
                TryPlace();
        }

        public bool TryPlace()
        {
            if (RaycastPaperView.IsReading || hand == null || !hand.isActiveAndEnabled || !hand.CanMark || markPrefab == null)
                return false;
            Physics2D.SyncTransforms();
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(wallMask);
            if (Physics2D.Raycast(transform.position, transform.up, filter, _hits, reach) == 0) return false;
            RaycastHit2D hit = _hits[0];
            if (hit.fraction == 0f || !hit.collider.TryGetComponent(out Tilemap map) || !_idsByMap.TryGetValue(map, out string mapId))
                return false;

            Vector3Int cell = map.WorldToCell(hit.point - hit.normal * 0.001f);
            if (!map.HasTile(cell)) return false;
            Vector2 center = map.GetCellCenterWorld(cell);
            Vector2 axisX = (Vector2)map.GetCellCenterWorld(cell + Vector3Int.right) - center;
            Vector2 axisY = (Vector2)map.GetCellCenterWorld(cell + Vector3Int.up) - center;
            float x = Vector2.Dot(hit.normal, axisX.normalized);
            float y = Vector2.Dot(hit.normal, axisY.normalized);
            Vector3Int side = Mathf.Abs(x) > Mathf.Abs(y)
                ? (x > 0f ? Vector3Int.right : Vector3Int.left)
                : (y > 0f ? Vector3Int.up : Vector3Int.down);
            Place(map, new WallFace
            {
                mapId = mapId,
                cell = cell,
                side = side,
                localPoint = map.transform.InverseTransformPoint(hit.point),
                localNormal = map.transform.InverseTransformDirection(hit.normal)
            });
            hand.SpendBloodForMark();
            return true;
        }

        private void Place(Tilemap map, WallFace face)
        {
            Vector3 position = map.transform.TransformPoint(face.localPoint);
            Vector2 outward = map.transform.TransformDirection(face.localNormal);
            float angle = Mathf.Atan2(outward.y, outward.x) * Mathf.Rad2Deg - 90f;
            var mark = Instantiate(markPrefab, position, Quaternion.Euler(0f, 0f, angle), map.transform);
            mark.SupportingWall = map.gameObject;
            _marks.Add((face, mark));
        }

        public List<WallFace> Capture()
        {
            var locations = new List<WallFace>(_marks.Count);
            foreach (var mark in _marks) locations.Add(mark.location);
            return locations;
        }

        public void Restore(List<WallFace> faces)
        {
            foreach (var mark in _marks)
            {
                mark.decoration.gameObject.SetActive(false);
                Destroy(mark.decoration.gameObject);
            }
            _marks.Clear();
            if (faces == null) return;
            foreach (WallFace saved in faces)
            {
                WallFace face = saved;
                // 타일맵 ID를 저장하기 전에는 기본 벽에만 표식을 남길 수 있었다.
                if (string.IsNullOrEmpty(face.mapId)) face.mapId = MainMapId;
                if (!_mapsById.TryGetValue(face.mapId, out Tilemap map) || !map.HasTile(face.cell)) continue;
                if (face.side.z != 0 || face.side.sqrMagnitude != 1) continue;
                if (face.localNormal == Vector2.zero)
                {
                    // 이전 저장 파일은 타일 면의 중앙에 손자국을 찍었다.
                    Vector3 center = map.GetCellCenterLocal(face.cell);
                    Vector3 neighbor = map.GetCellCenterLocal(face.cell + face.side);
                    face.localPoint = (center + neighbor) * 0.5f;
                    face.localNormal = (neighbor - center).normalized;
                }
                Place(map, face);
            }
        }
    }
}
