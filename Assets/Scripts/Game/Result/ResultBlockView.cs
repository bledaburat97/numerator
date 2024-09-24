using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class ResultBlockView : MonoBehaviour, IResultBlockView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private ResultView resultPrefab;
        [SerializeField] private NonDraggableCardItemView nonDraggableCardItemPrefab;
        [SerializeField] private RectTransform cardsHolder;
        [SerializeField] private RectTransform resultsHolder;
        [SerializeField] private float spacingBetweenCardsAndResult;
        private ResultViewFactory _resultViewFactory;
        private NonDraggableCardItemViewFactory _nonDraggableCardItemViewFactory;
        
        
        public void Init(NonDraggableCardItemViewFactory nonDraggableCardItemViewFactory, ResultViewFactory resultViewFactory)
        {
            _nonDraggableCardItemViewFactory = nonDraggableCardItemViewFactory;
            _resultViewFactory = resultViewFactory;
            rectTransform.localScale = Vector3.one;
        }

        public IResultView CreateResult()
        {
            return _resultViewFactory.Spawn(resultsHolder, resultPrefab);
        }
        
        public void SetCardsHolderLocalPosition()
        {
            resultsHolder.anchoredPosition = new Vector2(0f, 0.5f);
            cardsHolder.localPosition = new Vector2(0f, 0f);
        }

        public void SetResultHolderLocalPosition()
        {
            resultsHolder.anchoredPosition = new Vector2(0f, 0.5f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardsHolder);
            resultsHolder.localPosition = new Vector2(cardsHolder.rect.width + spacingBetweenCardsAndResult, 0f);
            rectTransform.sizeDelta =
                new Vector2( cardsHolder.rect.width + resultsHolder.sizeDelta.x + spacingBetweenCardsAndResult,
                    rectTransform.sizeDelta.y);
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
        void Init(NonDraggableCardItemViewFactory nonDraggableCardItemViewFactory, ResultViewFactory resultViewFactory);
        IResultView CreateResult();
        void SetResultHolderLocalPosition();
        void SetCardsHolderLocalPosition();
        INonDraggableCardItemView CreateCardItem();
        void Destroy();
    }
}