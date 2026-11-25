using UnityEngine;

namespace Seonho._01.Scripts.StereophonicSound
{
    public class CurrentSpacePos : MonoBehaviour
    {
        public TypeSpace selectedSpace;
    
        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.TryGetComponent(out CurrentSpacePos space))
            {
                if (space.selectedSpace == TypeSpace.corridor)
                {
                    selectedSpace = TypeSpace.corridor;
                }
                else if (space.selectedSpace == TypeSpace.smallRoom)
                {
                    selectedSpace = TypeSpace.smallRoom;
                }
                else if (space.selectedSpace == TypeSpace.largeRoom)
                {
                    selectedSpace = TypeSpace.largeRoom;
                }
            }
        }

        public TypeSpace GetSpace()
        {
            return selectedSpace;
        }
    }
    
    public enum TypeSpace
    {
        corridor,
        smallRoom,
        largeRoom
    }
}
