using Hyunwoo.UI;
using UnityEngine;
using UnityEngine.AI;

namespace Hyunwoo.Raycasting2D
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterChase2D : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private GameOverScreen gameOverScreen;
        [SerializeField] private LayerMask wallMask;
        [SerializeField, Min(0.01f)] private float catchDistance = 0.42f;

        private NavMeshAgent _agent;
        private float _nextPathUpdate;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            // 이 맵은 XY 평면이고, 화면 속 괴물은 항상 정면을 보여 준다.
            _agent.updateRotation = false;
            _agent.updateUpAxis = false;
        }

        private void Update()
        {
            if (!_agent.isOnNavMesh) return;

            Vector2 monsterPosition = transform.position;
            Vector2 playerPosition = player.position;
            if (Vector2.Distance(monsterPosition, playerPosition) <= catchDistance &&
                !Physics2D.Linecast(monsterPosition, playerPosition, wallMask))
            {
                _agent.isStopped = true;
                enabled = false;
                gameOverScreen.Show();
                return;
            }

            if (Time.time < _nextPathUpdate) return;
            _nextPathUpdate = Time.time + 0.2f;

            // 플레이어가 벽에 바짝 붙어도 가장 가까운 통로 지점까지 따라간다.
            var filter = new NavMeshQueryFilter { agentTypeID = _agent.agentTypeID, areaMask = _agent.areaMask };
            if (NavMesh.SamplePosition(player.position, out NavMeshHit target, 0.5f, filter))
                _agent.SetDestination(target.position);
        }
    }
}
