using System;
using System.Collections.Generic;
using NavMeshPlus.Components;
using UnityEngine;
using UnityEngine.AI;

namespace Seonho._01.Scripts.StereophonicSound
{
    public class SoundNav : MonoBehaviour
    {
        // [Header("Target")]
        // [SerializeField] private Transform _player;
        // [SerializeField] private Transform _soundSource;
        //
        // [Header("Layer")] [SerializeField] private LayerMask _whatIsWall;
        //
        // private void Update()
        // {
        //     CheckWall();
        // }
        //
        // private void CheckWall()
        // {
        //     Vector2 direction = _soundSource.position - _player.position;
        //
        //     float distance = direction.magnitude;
        //
        //     RaycastHit2D hit = Physics2D.Raycast(_player.position, direction.normalized, distance, _whatIsWall);
        //
        //     Debug.DrawRay(_player.position, direction, hit.collider != null ? Color.red : Color.green);
        //
        //     if (hit.collider != null)
        //     {
        //         Debug.Log("벽 있음");
        //     }
        //     else
        //     {
        //         Debug.Log("벽 없음");
        //     }
        // }

        [Header("Target")] [SerializeField] private Transform _player;
        [SerializeField] private Transform _soundSource;

        [Header("Wall")] [SerializeField] private LayerMask _whatIsWall;

        [Header("Setting")] [SerializeField] private float _cellSize = 0.25f;
        [SerializeField] private float _padding = 3f;

        private List<Vector2> _path = new List<Vector2>();

        private float _nextSearchTime;

        private static readonly Vector2Int[] Directions =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1, -1)
        };

        private void Update()
        {
            if (Time.time < _nextSearchTime)
                return;

            _nextSearchTime = Time.time + 0.2f;

            if (FindPath())
            {
                Debug.Log("경로 탐색 성공!");
                DrawPath();
            }
            else
            {
                Debug.Log("경로 탐색 실패!");
            }
        }

        private bool FindPath()
        {
            Vector2 start = _soundSource.position;
            Vector2 end = _player.position;

            Vector2 origin = Vector2.Min(start, end)
                             - Vector2.one * _padding;

            int width = Mathf.CeilToInt(
                (Mathf.Max(start.x, end.x) - origin.x + _padding) / _cellSize);

            int height = Mathf.CeilToInt(
                (Mathf.Max(start.y, end.y) - origin.y + _padding) / _cellSize);

            bool[,] blocked = new bool[width, height];
            float[,] cost = new float[width, height];
            bool[,] visited = new bool[width, height];

            Vector2Int[,] parent = new Vector2Int[width, height];

            // 벽 검사
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2 pos = origin +
                                  (new Vector2(x, y) + Vector2.one * 0.5f) * _cellSize;

                    blocked[x, y] = Physics2D.OverlapCircle(
                        pos, _cellSize * 0.45f, _whatIsWall
                    );
                    cost[x, y] = float.PositiveInfinity;
                }
            }

            Vector2Int startCell = ToCell(start, origin, width, height);
            Vector2Int endCell = ToCell(end, origin, width, height);

            blocked[startCell.x, startCell.y] = false;
            blocked[endCell.x, endCell.y] = false;

            List<Vector2Int> open = new List<Vector2Int>();

            cost[startCell.x, startCell.y] = 0;
            open.Add(startCell);

            bool found = false;

            // A* 탐색
            while (open.Count > 0)
            {
                int best = 0;

                for (int i = 1; i < open.Count; i++)
                {
                    if (Score(open[i], endCell, cost) <
                        Score(open[best], endCell, cost))
                        best = i;
                }

                Vector2Int current = open[best];
                open.RemoveAt(best);

                if (current == endCell)
                {
                    found = true;
                    break;
                }

                if (visited[current.x, current.y])
                    continue;

                visited[current.x, current.y] = true;

                foreach (Vector2Int dir in Directions)
                {
                    Vector2Int next = current + dir;

                    if (next.x < 0 || next.x >= width ||
                        next.y < 0 || next.y >= height)
                        continue;

                    if (blocked[next.x, next.y] ||
                        visited[next.x, next.y])
                        continue;

                    // 대각선으로 벽 모서리 통과 방지
                    if (dir.x != 0 && dir.y != 0)
                    {
                        if (blocked[current.x + dir.x, current.y] ||
                            blocked[current.x, current.y + dir.y])
                            continue;
                    }

                    float moveCost = dir.x != 0 && dir.y != 0
                        ? 1.4142f
                        : 1f;

                    float newCost = cost[current.x, current.y] + moveCost;

                    if (newCost >= cost[next.x, next.y])
                        continue;

                    cost[next.x, next.y] = newCost;
                    parent[next.x, next.y] = current;

                    if (!open.Contains(next))
                        open.Add(next);
                }
            }

            if (!found)
            {
                _path.Clear();
                return false;
            }

            // 경로 복원
            _path.Clear();
            Vector2Int cell = endCell;

            _path.Add(end);

            while (cell != startCell)
            {
                Vector2 pos = origin +
                              (new Vector2(cell.x, cell.y) + Vector2.one * 0.5f)
                              * _cellSize;

                _path.Add(pos);
                cell = parent[cell.x, cell.y];
            }

            _path.Add(start);
            _path.Reverse();

            return true;
        }

        private Vector2Int ToCell(
            Vector2 pos, Vector2 origin, int width, int height)
        {
            int x = Mathf.Clamp(
                Mathf.FloorToInt((pos.x - origin.x) / _cellSize),
                0, width - 1);

            int y = Mathf.Clamp(
                Mathf.FloorToInt((pos.y - origin.y) / _cellSize),
                0, height - 1);

            return new Vector2Int(x, y);
        }

        private float Score(Vector2Int cell, Vector2Int end, float[,] cost)
        {
            int dx = Mathf.Abs(cell.x - end.x);
            int dy = Mathf.Abs(cell.y - end.y);

            float heuristic = Mathf.Max(dx, dy)
                              + 0.4142f * Mathf.Min(dx, dy);

            return cost[cell.x, cell.y] + heuristic;
        }

        private void DrawPath()
        {
            for (int i = 0; i < _path.Count - 1; i++)
            {
                Debug.DrawLine(
                    _path[i],
                    _path[i + 1],
                    Color.green,
                    0.3f
                );
            }
        }
    }
}
