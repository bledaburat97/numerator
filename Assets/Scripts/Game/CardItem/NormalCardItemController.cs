using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Scripts
{
    public class NormalCardItemController : DraggableCardItemController, INormalCardItemController
    {
        private ICardMoveHandler _cardMoveHandler;
        private ICardViewHandler _cardViewHandler;
        private ICardClickHandler _cardClickHandler;
        private ICardReleaseHandler _cardReleaseHandler;
        
        public void Initialize(INormalCardItemView cardItemView, Camera cam, IHapticController hapticController, ITutorialAbilityManager tutorialAbilityManager,
            CardItemData cardItemData, ICardItemLocator cardItemLocator, ICardItemInfoManager cardItemInfoManager, IBoardAreaController boardAreaController,
            Action<int> onDragStart)
        {
            _cardViewHandler = new CardViewHandler(cardItemView, cam, cardItemInfoManager, hapticController);
            _cardMoveHandler = new CardMoveHandler(hapticController, tutorialAbilityManager, _cardViewHandler,
                boardAreaController, () => onDragStart(cardItemData.cardItemIndex), cardItemLocator.OnDragContinue);
            _cardClickHandler = new CardClickHandler(hapticController, tutorialAbilityManager, _cardViewHandler,
                cardItemInfoManager, cardItemData.onCardClicked);
            _cardReleaseHandler = new CardReleaseHandler(hapticController, _cardViewHandler, boardAreaController, cardItemLocator.OnDragComplete);

            _cardItemData = cardItemData;

            _cardClickHandler.Initialize(_cardItemData);
            _cardViewHandler.Initialize(_cardMoveHandler, OnPointerUp, _cardItemData);
            _cardMoveHandler.Initialize(_cardItemData);

            cardItemInfoManager.ProbabilityChanged += OnProbabilityChanged;
        }
        
        private void OnProbabilityChanged(object sender, ProbabilityChangedEventArgs args)
        {
            if (_cardItemData.cardItemIndex == args.cardIndex)
            {
                _cardViewHandler.SetProbability(args.probabilityType, args.isLocked);
                _cardClickHandler.SetLockStatus(args.isLocked);
            }
        }
        
        public INormalCardItemView GetView()
        {
            return _cardViewHandler.GetView();
        }

        public void ResetPosition()
        {
            RectTransform parentTransform = _cardItemData.parent;
            _cardViewHandler.SetParent(_cardItemData.tempParent);
            _cardViewHandler.PlaceCard(parentTransform.position, parentTransform);
        }
        
        private void OnPointerUp(PointerEventData data)
        {
            if (!_cardMoveHandler.IsDragStarted())
            {
                _cardClickHandler.TryClickCard();
            }
            else
            {
                _cardReleaseHandler.Release(_cardItemData.cardItemIndex, _cardItemData.parent, _cardItemData.tempParent);
            }
        }

        public void MoveCardByClick(int boardCardHolderIndex)
        {
            _cardMoveHandler.MoveCardToBoard(boardCardHolderIndex, _cardItemData.tempParent);
        }


        public void DeselectCard()
        {
            _cardClickHandler.DeselectCard();
        }

        public void SetCardAnimation(bool status)
        {
            _cardViewHandler.SetCardAnimation(status);
        }

        public void BackFlipAnimation(float delayDuration, bool isGuessRight, string correctNumber)
        {
            _cardViewHandler.BackFlipAnimation(delayDuration, isGuessRight, correctNumber);
        }
    }
    public interface INormalCardItemController
    {
        void Initialize(INormalCardItemView cardItemView, Camera cam, IHapticController hapticController, ITutorialAbilityManager tutorialAbilityManager,
            CardItemData cardItemData, ICardItemLocator cardItemLocator, ICardItemInfoManager cardItemInfoManager, IBoardAreaController boardAreaController,
            Action<int> onDragStart);
        void ResetPosition();
        INormalCardItemView GetView();
        void MoveCardByClick(int boardCardHolderIndex);
        void BackFlipAnimation(float delayDuration, bool isGuessRight, string correctNumber);
        void DeselectCard();
        void SetCardAnimation(bool status);
    }
}