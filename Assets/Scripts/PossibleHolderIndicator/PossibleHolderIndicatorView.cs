using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class PossibleHolderIndicatorView : MonoBehaviour, IPossibleHolderIndicatorView
    {
        [SerializeField] private TMP_Text imageText;
        [SerializeField] private Image crossImage;
        
        public void Init(PossibleHolderIndicatorModel model)
        {
            transform.localScale = Vector3.one;
            transform.localPosition = model.localPosition;
            SetText(model.text);
        }

        private void SetText(string text)
        {
            imageText.text = text;
        }

        public void SetStatus(bool status)
        {
            imageText.gameObject.SetActive(status);
            crossImage.gameObject.SetActive(!status);
        }

        public void SetParent(RectTransform parent)
        {
            transform.SetParent(parent);
        }
    }

    public interface IPossibleHolderIndicatorView
    {
        void Init(PossibleHolderIndicatorModel model);
        void SetStatus(bool status);
        void SetParent(RectTransform parent);
    }
}