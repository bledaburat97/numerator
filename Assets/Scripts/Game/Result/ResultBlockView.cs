using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class ResultBlockView : MonoBehaviour, IResultBlockView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private ResultImageView resultImagePrefab;
        [SerializeField] private NonDraggableCardItemView nonDraggableCardItemPrefab;
        [SerializeField] private RectTransform cardsHolder;
        [SerializeField] private RectTransform topResultHolder;
        [SerializeField] private RectTransform middleResultHolder;
        [SerializeField] private RectTransform bottomResultHolder;
        [SerializeField] private RectTransform cardsHolderBgImageRectTransform;
        [SerializeField] private float spacingBetweenCardsAndResult;
        private ResultImageViewFactory _resultImageViewFactory;
        private NonDraggableCardItemViewFactory _nonDraggableCardItemViewFactory;
        
        public void Init(NonDraggableCardItemViewFactory nonDraggableCardItemViewFactory, ResultImageViewFactory resultImageViewFactory, float width)
        {
            _nonDraggableCardItemViewFactory = nonDraggableCardItemViewFactory;
            _resultImageViewFactory = resultImageViewFactory;
            rectTransform.localScale = Vector3.one;
            rectTransform.sizeDelta = new Vector2(width, rectTransform.sizeDelta.y);
            cardsHolderBgImageRectTransform.sizeDelta = new Vector2(width - 52, cardsHolderBgImageRectTransform.sizeDelta.y);
        }

        public IResultImageView CreateResultImage(ResultHolderType resultHolderType)
        {
            if (resultHolderType == ResultHolderType.Top)
            {
                return _resultImageViewFactory.Spawn(topResultHolder, resultImagePrefab);
            }
            if (resultHolderType == ResultHolderType.Middle)
            {
                return _resultImageViewFactory.Spawn(middleResultHolder, resultImagePrefab);
            }
            return _resultImageViewFactory.Spawn(bottomResultHolder, resultImagePrefab);
        }
        
        public void SetCardsHolderLocalPosition()
        {
            //resultsHolder.anchoredPosition = new Vector2(0f, 0.5f);
            //cardsHolder.localPosition = new Vector2(0f, 0f);
        }

        public void SetResultHolderLocalPosition()
        {
            //resultsHolder.anchoredPosition = new Vector2(0f, 0.5f);
            //LayoutRebuilder.ForceRebuildLayoutImmediate(cardsHolder);
            //resultsHolder.localPosition = new Vector2(cardsHolder.rect.width + spacingBetweenCardsAndResult, 0f);
            //rectTransform.sizeDelta = new Vector2( cardsHolder.rect.width + resultsHolder.sizeDelta.x + spacingBetweenCardsAndResult, rectTransform.sizeDelta.y);
        }

        public INonDraggableCardItemView CreateCardItem()
        {
            return _nonDraggableCardItemViewFactory.Spawn(cardsHolder, nonDraggableCardItemPrefab);
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }
    }

    public interface IResultBlockView
    {
        void Init(NonDraggableCardItemViewFactory nonDraggableCardItemViewFactory, ResultImageViewFactory resultImageViewFactory, float width);
        IResultImageView CreateResultImage(ResultHolderType resultHolderType);
        void SetResultHolderLocalPosition();
        void SetCardsHolderLocalPosition();
        INonDraggableCardItemView CreateCardItem();
        void Destroy();
    }

    public enum ResultHolderType
    {
        Top,
        Middle,
        Bottom,
    }
}