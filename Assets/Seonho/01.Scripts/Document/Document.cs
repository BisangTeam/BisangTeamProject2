using Seonho._09.SO.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Seonho._01.Scripts
{
    public class Document : MonoBehaviour
    {
        [SerializeField] private DocumentWritingDataSO _documentDataSO;
        
        [SerializeField] private TextMeshProUGUI _theme;
        [SerializeField] private TextMeshProUGUI _writing;
        [SerializeField] private Image _image;

        private void OnEnable()
        {
            SetTextUI();
            SetImageUI();
        }

        void SetTextUI()
        {
            transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _documentDataSO.Theme;
            transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = _documentDataSO.Writing;
        }

        void SetImageUI()
        {
            if (_documentDataSO.Image == null) return;
            
            for (int i = 0; i < _documentDataSO.Image.Length; i++)
            {
                transform.GetChild(i + 2).GetComponent<Image>().sprite = _documentDataSO.Image[i];
            }
        }
    }
}
