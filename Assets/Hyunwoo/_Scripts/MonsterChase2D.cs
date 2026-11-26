using Hyunwoo.UI;
using UnityEngine;
using UnityEngine.AI;

namespace Hyunwoo.Raycasting2D.Scripts
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterChase2D : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private GameOverScreen gameOverScreen;
        [SerializeField] private LayerMask wallMask;
        [SerializeField, Min(0.01f)] private float catchDistance = 0.42f;
        [SerializeField] private HidingController2D hidingController;
        [SerializeField, Min(0.1f)] private float sightDistance = 6f;

        private NavMeshAgent _agent;
        private float _nextPathUpdate;
        private bool hiddenPlayer;
        private bool sawPlayerEnter;


        private NavMeshQueryFilter PathFilter => new NavMeshQueryFilter
        {
            agentTypeID = _agent.agentTypeID,
            areaMask = _agent.areaMask
        };

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            // 이 맵은 XY 평면이고, 화면 속 괴물은 항상 정면을 보여 준다.
            _agent.updateRotation = false;
            _agent.updateUpAxis = false;
            if (hidingController == null)
                hidingController = player.GetComponent<HidingController2D>();
        }

        public void StopChase()
        {
            enabled = false;
            // 처음부터 숨겨 둔 괴물은 Awake 전에도 연출에서 참조할 수 있다.
            if (_agent == null) _agent = GetComponent<NavMeshAgent>();
            _agent.enabled = false;
        }

        public bool StartChase()
        {
            gameObject.SetActive(true);
            if (!NavMesh.SamplePosition(transform.position, out NavMeshHit start, 0.5f, PathFilter))
            {
                Debug.LogError("괴물의 추격 시작 지점을 NavMesh 위에 배치하세요.", this);
                return false;
            }

            transform.position = start.position;
            _agent.enabled = true;
            if (!_agent.Warp(start.position))
            {
                StopChase();
                Debug.LogError("괴물을 추격 시작 지점에 배치할 수 없습니다.", this);
                return false;
            }

            _agent.isStopped = false;
            _nextPathUpdate = 0f;
            enabled = true;
            return true;
        }

        private void Update()
        {
            if (!_agent.isOnNavMesh) return;

            Vector2 monsterPosition = transform.position;
            Vector2 playerPosition = player.position;
            if (Vector2.Distance(monsterPosition, playerPosition) <= catchDistance &&
                !Physics2D.Linecast(monsterPosition, playerPosition, wallMask))
            {
                return;

                _agent.isStopped = true;
                enabled = false;
                gameOverScreen.Show();
                return;
            }

            if (Time.time < _nextPathUpdate) return;
            _nextPathUpdate = Time.time + 0.2f;

            // 플레이어가 벽에 바짝 붙어도 가장 가까운 통로 지점까지 따라간다.
            if (NavMesh.SamplePosition(player.position, out NavMeshHit target, 0.5f, PathFilter))
                _agent.SetDestination(target.position);
            
            if (hiddenPlayer && !sawPlayerEnter)
            {
                _agent.isStopped = true;
                return;
            }

            _agent.isStopped = false;
        }
        
        public bool CanSeePosition(Vector2 target)
        {
            if (Vector2.Distance(transform.position, target) > sightDistance)
                return false;

            return !Physics2D.Linecast(
                transform.position,
                target,
                wallMask
            );
        }

        public void SetHiddenPlayer(bool isHidden, bool wasSeen)
        {
            hiddenPlayer = isHidden;
            sawPlayerEnter = wasSeen;
        }
    }
}
