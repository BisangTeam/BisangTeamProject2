using UnityEngine;

namespace Seonho._01.Scripts.StereophonicSound
{
    public class RealizeSpace : MonoBehaviour
    {
        [SerializeField] private LoudPos _loudPosCompo;

        [SerializeField] private CurrentSpacePos _player;
        [SerializeField] private CurrentSpacePos _enemy;
    
        [SerializeField] private float time;

        private void Update()
        {
            ChangeAudioResonance();
        }

        void ChangeAudioResonance()
        {
            if (_player.selectedSpace == TypeSpace.corridor && _player.selectedSpace == TypeSpace.corridor)
            {
                _loudPosCompo.DistanceRever();
                return;
            }
            if (_player.GetSpace() == _enemy.GetSpace())
            {
                time = 0.1f;
                _loudPosCompo.AudioResonance(1000 ,0.1f);
                return;
            }
            
            GetAudioResonance(_player.selectedSpace);
            GetAudioResonance(_enemy.selectedSpace);

            time = 0.1f;
        
            float resonance = Mathf.Abs(GetAudioResonance(_enemy.selectedSpace) - GetAudioResonance(_player.selectedSpace));
            _loudPosCompo.AudioResonance(resonance, time);
        }

        float GetAudioResonance(TypeSpace space)
        {
            float value = 0;
        
            if (space == TypeSpace.corridor)
            {
                value = 500;
                time -= 0.045f;
            }
            else if (space == TypeSpace.smallRoom)
            {
                value = 300;
                time -= 0.025f;
            }
            else if (space == TypeSpace.largeRoom)
            {
                value = 1000;
                time -= 0.05f;
            }

            return value;
        }
    }
}