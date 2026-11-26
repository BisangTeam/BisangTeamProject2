using UnityEngine;

namespace Seonho._09.SO.Scripts
{
    [CreateAssetMenu(fileName = "DocumentDataSO", menuName = "SO/DocumentWritingDataSO")]
    public class DocumentWritingDataSO : ScriptableObject
    {
        [field:SerializeField] public string Theme { get; private set; }
        [field:SerializeField, TextArea(4, 16)] public string Writing { get; private set; }
        [field:SerializeField, Tooltip("제목과 본문 뒤에 기울여 보여 줄 원본 이미지. 비우면 첫 번째 첨부 이미지 또는 기본 종이를 사용합니다.")]
        public Sprite Paper { get; private set; }
        [field:Tooltip("마우스 휠로 넘겨 보는 원본 스캔이나 삽화")]
        [field:SerializeField] public Sprite[] Image{ get; private set; }
    }
}
