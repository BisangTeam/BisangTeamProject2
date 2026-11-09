using UnityEngine;
using UnityEngine.UI;

namespace Seonho._09.SO.Scripts
{
    [CreateAssetMenu(fileName = "DocumentDataSO", menuName = "SO/DocumentWritingDataSO")]
    public class DocumentWritingDataSO : ScriptableObject
    {
        public string Theme;
        public string Writing;
    }
}
