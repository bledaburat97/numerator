using UnityEngine;

namespace Views
{
    public class CardHolderView : MonoBehaviour, ICardHolderView
    {
        [SerializeField] private RectTransform rectTransform;
        
        public void Init(CardHolderModel model)
        {
            transform.localScale = Vector3.one;
            transform.localPosition = model.localPosition;
            rectTransform.sizeDelta = model.size;
        }

        public Vector3 GetPosition()
        {
            return transform.position;
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }

        public Vector3 GetSize()
        {
            return rectTransform.sizeDelta;
        }
    }

    public interface ICardHolderView
    {
        void Init(CardHolderModel model);
        Vector3 GetPosition();
        Vector3 GetSize();
        RectTransform GetRectTransform();
    }
}