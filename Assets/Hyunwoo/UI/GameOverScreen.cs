using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Hyunwoo.UI
{
    public class GameOverScreen : MonoBehaviour
    {
        [SerializeField] private GameObject[] gameplayObjects;

        private float _previousTimeScale;
        private bool _pausedGame;
        private bool _restarting;

        public void Show()
        {
            if (_pausedGame) return;

            _pausedGame = true;
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            // 시간이 멈춰도 Update는 실행되므로 맵 조작과 장기 입력도 끈다.
            foreach (GameObject obj in gameplayObjects)
                obj.SetActive(false);
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                Restart();
        }

        public void Restart()
        {
            if (_restarting) return;
            _restarting = true;
            SceneManager.LoadScene(gameObject.scene.buildIndex);
        }

        private void OnDisable()
        {
            if (!_pausedGame) return;
            Time.timeScale = _previousTimeScale;
            _pausedGame = false;
        }
    }
}
