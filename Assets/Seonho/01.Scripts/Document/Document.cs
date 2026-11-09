using System;
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

        private void OnEnable()
        {
            SetUI();
        }

        void SetUI()
        {
            transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _documentDataSO.Theme;
            transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = _documentDataSO.Writing;
        }
    }
}
