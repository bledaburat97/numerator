using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class CardHolderView : MonoBehaviour, ICardHolderView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private RectTransform possibleHolderIndicatorHolderTransform;
        [SerializeField] private PossibleHolderIndicatorView possibleHolderIndicatorPrefab;
        [SerializeField] private Image highlightImage;
        
        private PossibleHolderIndicatorViewFactory _possibleHolderIndicatorViewFactory;
        public void Init(CardHolderModel model, PossibleHolderIndicatorViewFactory possibleHolderIndicatorViewFactory)
        {
            transform.localScale = Vector3.one;
            transform.localPosition = model.localPosition;
            rectTransform.sizeDelta = model.size;
            _possibleHolderIndicatorViewFactory = possibleHolderIndicatorViewFactory;
        }
        
        public IPossibleHolderIndicatorView CreatePossibleHolderIndicatorView()
        {
            return _possibleHolderIndicatorViewFactory.Spawn(possibleHolderIndicatorHolderTransform, possibleHolderIndicatorPrefab);
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

        public void SetHighlightStatus(bool status)
        {
            highlightImage.gameObject.SetActive(status);
        }
    }

    public interface ICardHolderView
    {
        void Init(CardHolderModel model, PossibleHolderIndicatorViewFactory possibleHolderIndicatorViewFactory);
        Vector3 GetPosition();
        Vector3 GetSize();
        RectTransform GetRectTransform();
        IPossibleHolderIndicatorView CreatePossibleHolderIndicatorView();
        void SetHighlightStatus(bool status);
    }
}