using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hyunwoo.Save
{
    public sealed class CheckpointLaunch : MonoBehaviour
    {
        [SerializeField] private string firstScene;

        private void Start()
        {
            CheckpointData data = CheckpointFile.Read(CheckpointFile.Location);
            string path = data != null && SceneUtility.GetBuildIndexByScenePath(data.scenePath) >= 0
                ? data.scenePath : firstScene;
            SceneManager.LoadScene(SceneUtility.GetBuildIndexByScenePath(path));
        }
    }
}
