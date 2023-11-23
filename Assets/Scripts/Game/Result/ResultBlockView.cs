﻿using UnityEngine;
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
        [SerializeField] private HorizontalLayoutGroup horizontalLayoutGroupOfCardsHolder;
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

        public void SetResultHolderLocalPosition(int numOfCards, float horizontalSizeOfCards)
        {
            resultsHolder.anchoredPosition = new Vector2(0f, 0.5f);
            float sizeX = numOfCards * horizontalSizeOfCards +
                          (numOfCards - 1) * horizontalLayoutGroupOfCardsHolder.spacing;
            resultsHolder.localPosition = new Vector2(sizeX + spacingBetweenCardsAndResult, 0f);
        }

        public INonDraggableCardItemView CreateCardItem()
        {
            return _nonDraggableCardItemViewFactory.Spawn(cardsHolder, nonDraggableCardItemPrefab);
        }
    }

    public interface IResultBlockView
    {
        void Init(NonDraggableCardItemViewFactory nonDraggableCardItemViewFactory, ResultViewFactory resultViewFactory);
        IResultView CreateResult();
        void SetResultHolderLocalPosition(int numOfCards, float horizontalSizeOfCards);
        void SetCardsHolderLocalPosition();
        INonDraggableCardItemView CreateCardItem();
    }
}