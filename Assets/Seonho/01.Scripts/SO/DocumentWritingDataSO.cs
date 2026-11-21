using UnityEngine;
using UnityEngine.UI;

namespace Seonho._09.SO.Scripts
{
    [CreateAssetMenu(fileName = "DocumentDataSO", menuName = "SO/DocumentWritingDataSO")]
    public class DocumentWritingDataSO : ScriptableObject
    {
        [field:SerializeField] public string Theme { get; private set; }
        [field:SerializeField] public string Writing { get; private set; }
        [field:SerializeField] public Sprite[] Image{ get; private set; }
    }
}
