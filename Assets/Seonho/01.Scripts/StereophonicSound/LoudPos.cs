using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Seonho._01.Scripts.StereophonicSound
{
    [RequireComponent(typeof(AudioSource), typeof(AudioLowPassFilter))]
    public class LoudPos : MonoBehaviour
    {
        [field:SerializeField, Range(0, 1)]
        public float EarBlood { get; set; }
        
        [Header("Compo")]
        [SerializeField] private AudioSource _audioCompo;
        
        [Header("List")]
        private List<Vector3> _path = new List<Vector3>(); // 찾아낸 경로를 저장
        
        [Header("Wall Tilemap")] [SerializeField] private Tilemap _wallTilemap; // 타일맵
        
        [Header("Target")]
        [SerializeField] private Transform _player; // 플레이어 위치
        [SerializeField] private Transform _soundSource; // 소리 발생 위치
        [SerializeField] private Transform _listener;
        
        [Header("Setting")]
        [SerializeField] private int _padding = 15; // 여유 공간
        [SerializeField] private float _soundDis; // 소리 거리
        [SerializeField] private float _audioMinDistance; // 음향 최대 치 범위
        [SerializeField] private float _audioMaxDistance; // 음향 감소 시작 범위
        [SerializeField] private float _cornerVolume;

        [Header("Ear Blood")]
        [SerializeField, Range(10f, 22000f)] private float _emptyEarCutoff = 500f;
        [SerializeField, Range(10f, 22000f)] private float _fullEarCutoff = 18000f;
        [SerializeField, Range(0f, 1f)] private float _emptyEarVolume = 0.2f;
        [SerializeField, Min(0.01f)] private float _hearingResponseTime = 0.15f;

        private AudioLowPassFilter _lowPass;
        private float _currentEarBlood;
        private float _pathVolume;
        private float _nextSearchTime; // 경로 탐색 시간 => 계속하면 연산 비용이 커져서 

        //동서남북 대각선
        private Vector2Int[] _directions =
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

        private void Awake()
        {
            _audioCompo = GetComponent<AudioSource>();
            _lowPass = GetComponent<AudioLowPassFilter>();
            _audioCompo.spatialBlend = 1f;
            _currentEarBlood = Mathf.Clamp01(EarBlood);
            ApplyHearing();
        }

        private void Update()
        {
            if (Time.time >= _nextSearchTime)
            {
                _nextSearchTime = Time.time + 0.1f;
                if (FindPath())
                    DrawPath();

                _pathVolume = CalculatePathVolume();
            }

            float blend = 1f - Mathf.Exp(-Time.deltaTime / _hearingResponseTime);
            _currentEarBlood = Mathf.Lerp(_currentEarBlood, Mathf.Clamp01(EarBlood), blend);
            ApplyHearing();
        }

        private bool FindPath()
        {
            _path.Clear();
            if (_player == null || _soundSource == null || _wallTilemap == null)
                return false;
            
            // WorldToCell => 월드 좌표를 타일맵 셀 좌표로 변한
            Vector3Int startCell3 = _wallTilemap.WorldToCell(_soundSource.position); // 타일맵 소리 위치
            Vector3Int endCell3 = _wallTilemap.WorldToCell(_player.position); // 타일맵 플레이어 위치

            Vector2Int startCell = new Vector2Int(startCell3.x, startCell3.y); // 타일맵 시작 지점 저장
            Vector2Int endCell = new Vector2Int(endCell3.x, endCell3.y); // 타일맵 끝 지점 저장

            Vector2Int minCell = Vector2Int.Min(startCell, endCell) - Vector2Int.one * _padding; // 타일맵 전체 최소 좌표
            Vector2Int maxCell = Vector2Int.Max(startCell, endCell) + Vector2Int.one * _padding; // 타일맵 전체 최대 좌표

            // 1을 더하는 이유 = 만약 x값이 5 - 3이 된다고 가정하면 2가 된다.
            int width = maxCell.x - minCell.x + 1; // 가로 크기 구하기
            int height = maxCell.y - minCell.y + 1; // 세로 크기 구하기

            bool[,] blocked = new bool[width, height]; // 해당 타일에 벽이 있는가?
            bool[,] visited = new bool[width, height]; // 해당 타일에 방문했는가?
            float[,] cost = new float[width, height]; // 해당 타일에 비용은 얼마인가?

            Vector2Int[,] parent = new Vector2Int[width, height]; // 현재 타일에 도착하기 전에 저장된 경로 배열

            // 벽 타일 검사
            for (int x = 0; x < width; x++) // x좌표
            {
                for (int y = 0; y < height; y++) // y좌표
                {
                    Vector2Int cell = minCell + new Vector2Int(x, y); // 최솟값에다가 현재 좌표 더하기

                    blocked[x, y] = _wallTilemap.HasTile(new Vector3Int(cell.x, cell.y, startCell3.z)); // 해당 타일맵 좌표에 타일이 설치되어 있으면 True 아니면 False

                    // 시작은 모든 타일에 비용을 비싸게
                    cost[x, y] = float.PositiveInfinity; // 양의 무한대 값
                }
            }

            Vector2Int start = startCell - minCell; // 시작점을 배열 값으로 저장할 수 있게 함
            Vector2Int end = endCell - minCell; // 끝점을 배열 값으로 저장할 수 있게 함

            if (blocked[start.x, start.y] || blocked[end.x, end.y]) // 시작점이나 끝점에 블럭이 설치 되어 있을 때
                return false;

            List<Vector2Int> open = new List<Vector2Int>(); // 탐색할 경로 위치

            cost[start.x, start.y] = 0; // 소리 발생 지점에 비용 가장 높게 하기
            open.Add(start); // 시작 위치에 대기열 추가

            bool found = false;

            // A* 탐색
            while (open.Count > 0) // 개수가 0 이상이라면
            {
                int best = 0; // 최고 비용 지정

                for (int i = 1; i < open.Count; i++) // 모든 open 리스트 개수 실행
                {
                    if (GetDistance(open[i], end, cost) < GetDistance(open[best], end, cost)) // 현재 경로 비용이 현재 가장 큰 비용보다 작다면
                        best = i;
                }

                Vector2Int current = open[best]; // 최고 비용 위치를 넣는다.
                open.RemoveAt(best); // 가장 높은 비용 삭제

                if (current == end) // 현재 비용과 끝 지점이 같다면
                {
                    found = true; // 경로 찾기 성공
                    break; // 반복문 종료
                }

                if (visited[current.x, current.y]) // 해당 타일에 방문을 했는가?
                    continue; // 반복문 재실행

                visited[current.x, current.y] = true; // 해당 타일을 참으로 바꾼다.

                foreach (Vector2Int dir in _directions) // 모든 방향을 검사한다.
                {
                    Vector2Int next = current + dir; // 현재 위치에서 방향 더하기

                    if (next.x < 0 || next.x >= width || next.y < 0 || next.y >= height) // 배열 내에 있는지 검사
                        continue;

                    if (blocked[next.x, next.y] || visited[next.x, next.y]) // 타일이 있는지 검사 or 타일을 밟았는가
                        continue;

                    // 대각선으로 벽 모서리 통과 방지
                    if (dir.x != 0 && dir.y != 0) // 양옆에 벽이 있는지 없는지 확인
                    {
                        if (blocked[current.x + dir.x, current.y] || blocked[current.x, current.y + dir.y]) // 현재 x위치에서 dir.x만큼 더하고, 현재 y위치에서 dir.y를 더했을 때 하나라도 블록이 있다면?
                            continue;
                    }
                    
                    float moveCost = dir.x != 0 && dir.y != 0 ? 1.4142f : 1f; // 대각선이면 1.4142(대각선 길이) 아니하면 1(직선 길이)

                    float newCost = cost[current.x, current.y] + moveCost; // 현재 비용에서 움직임 비용 더하기

                    if (newCost >= cost[next.x, next.y]) // 만약 새 비용이 다음 경로 비용보다 클 경우
                        continue;

                    cost[next.x, next.y] = newCost; // 비용에 새 비용으로 바꾸기
                    parent[next.x, next.y] = current; // 기존에 저장된 경로를 현재 경로로 바꾸기

                    if (!open.Contains(next)) // next값이 없을 경우
                        open.Add(next); // 추가
                }
            }

            if (!found)
                return false; // 못 찾았다면 false 리턴

            // 경로 복원
            List<Vector2Int> cells = new List<Vector2Int>();
            Vector2Int trace = end; // 역추적을 하기 위한 변수 저장

            cells.Add(trace); // 셀에 역추적할 위치를 추가

            while (trace != start) // 추적 위치랑 시작 위치랑 같지 않다면 => 동일 할 떄 까지 반복
            {
                trace = parent[trace.x, trace.y]; // 역추적 위치를 도착 경로에 저장
                cells.Add(trace); // 셀에 역추적 방향 추가
            }

            cells.Reverse(); // 리스트를 행을 반대로 하기

            _path.Clear(); // 경로안에 있는 내용 삭제
            _path.Add(_soundSource.position); // 처음 위치를 소리 발새 위치로 저장

            for (int i = 1; i < cells.Count; i++) // 셀 카운트 만큼
            {
                Vector2Int cell = minCell + cells[i]; // 시작되는 위치에서 저장된 경로를 더해주기

                _path.Add(_wallTilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, startCell3.z))); // 타일맵에 있는 경로를 넣기
            }

            _path.Add(_player.position); // 마지막에 플레이어 위치 추가 

            return true; // 트루 반환
        }

        private float GetDistance(Vector2Int cell, Vector2Int end, float[,] cost) // cell 현재 검사 중인 셀의 좌표, 목표 셀의 좌표, 각 셀까지 이동하는데에 드는 비용
        {
            int dx = Mathf.Abs(cell.x - end.x); // 현재 검사 중인 셀.x - 목표 셀.x
            int dy = Mathf.Abs(cell.y - end.y); // 현재 검사 중인 셀.y - 목표 셀.y

            // 얼마나 이동해야 할지 계산
            // 0.4142를 추가하는 이유 = 만약 대각선을 이동할 경우 루트2인 0.4142f을 더해 준 것이다.
            // 최대값 : 가장 긴 비용 계산, 최솟값 : 대각선 비용계산을 위한 것 -> 다음에 봤을 때도 모르면 지피티한테 물어보기
            float h = Mathf.Max(dx, dy) + 0.4142f * Mathf.Min(dx, dy);
            
            return cost[cell.x, cell.y] + h; // 현재 검사 중인 셀에서 h만큼 이동
        }

        private void DrawPath()
        {
            for (int i = 0; i < _path.Count - 1; i++) // 경로 개수
            {
                Debug.DrawLine(_path[i], _path[i + 1], Color.green, 0.15f); // 라인 그리기
            }
        }

        private float CalculatePathVolume()
        {
            _soundDis = 0f;
            if (_path.Count == 0)
                return 0f;

            for (int i = 1; i < _path.Count; i++)
            {
                _soundDis += Vector3.Distance(_path[i - 1], _path[i]);
            }

            if (_soundDis >= _audioMaxDistance)
                return 0f;
            if (_soundDis < _audioMinDistance)
                return 1f;

            float distanceVolume = 1f - Mathf.InverseLerp(_audioMinDistance, _audioMaxDistance, _soundDis);
            float pathLoss = (_path.Count - 1) * _cornerVolume;
            return Mathf.Clamp01(distanceVolume - pathLoss);
        }

        private void ApplyHearing()
        {
            // 같은 양의 피가 차면 컷오프도 같은 비율로 오른다. 기본값에서 절반은 3,000 Hz다.
            float cutoff = _emptyEarCutoff * Mathf.Pow(_fullEarCutoff / _emptyEarCutoff, _currentEarBlood);
            if (!Mathf.Approximately(_lowPass.cutoffFrequency, cutoff))
                _lowPass.cutoffFrequency = cutoff;

            float hearingVolume = Mathf.Lerp(_emptyEarVolume, 1f, _currentEarBlood);
            _audioCompo.volume = _pathVolume * hearingVolume;
        }
        
        private void LateUpdate()
        {
            if (_listener != null && _player != null && _listener != _player)
                _listener.SetPositionAndRotation(_player.position, _player.rotation);
        }
    }
}