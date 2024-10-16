using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class PossibleHolderIndicatorView : MonoBehaviour, IPossibleHolderIndicatorView
    {
        [SerializeField] private TMP_Text imageText;
        [SerializeField] private Image crossImage;
        [SerializeField] private RectTransform rectTransform;
        public void Init(PossibleHolderIndicatorModel model)
        {
            transform.localScale = Vector3.one;
            transform.localPosition = model.localPosition;
        }

        public void SetLocalScale()
        {
            transform.localScale = Vector3.one;
        }

        public void SetLocalPosition(Vector2 localPosition)
        {
            transform.localPosition = localPosition;
        }

        public void SetText(string text)
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

        public void DestroyObject()
        {
            Destroy(gameObject);
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }
        
        public void SetSize(Vector2 size)
        {
            rectTransform.sizeDelta = size;
        }
    }

    public interface IPossibleHolderIndicatorView
    {
        void Init(PossibleHolderIndicatorModel model);
        void SetStatus(bool status);
        void SetParent(RectTransform parent);
        void DestroyObject();
        void SetLocalScale();
        void SetLocalPosition(Vector2 localPosition);
        void SetText(string text);
        RectTransform GetRectTransform();
        void SetSize(Vector2 size);

    }
}