using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Scripts
{
    public class NormalCardItemController : DraggableCardItemController, INormalCardItemController
    {
        private ICardMoveHandler _cardMoveHandler;
        private ICardViewHandler _cardViewHandler;
        private ICardReleaseHandler _cardReleaseHandler;
        
        public void Initialize(INormalCardItemView cardItemView, Camera cam, IHapticController hapticController, ITutorialAbilityManager tutorialAbilityManager,
            CardItemData cardItemData, ICardItemLocator cardItemLocator, IBoardAreaController boardAreaController,
            Action<int> onDragStart)
        {
            _cardViewHandler = new CardViewHandler(cardItemView, cam, hapticController);
            _cardMoveHandler = new CardMoveHandler(hapticController, tutorialAbilityManager, _cardViewHandler,
                boardAreaController, () => onDragStart(cardItemData.CardItemIndex), cardItemLocator.OnDragContinue);
            _cardReleaseHandler = new CardReleaseHandler(hapticController, _cardViewHandler, boardAreaController, cardItemLocator.OnDragComplete);

            _cardItemData = cardItemData;

            _cardViewHandler.Initialize(_cardMoveHandler, OnPointerUp, _cardItemData);
            _cardMoveHandler.Initialize(_cardItemData);
        }
        
        public INormalCardItemView GetView()
        {
            return _cardViewHandler.GetView();
        }

        public void ResetPosition()
        {
            RectTransform parentTransform = _cardItemData.Parent;
            _cardViewHandler.SetParent(_cardItemData.TempParent);
            _cardViewHandler.PlaceCard(parentTransform.position, parentTransform);
        }
        
        private void OnPointerUp(PointerEventData data)
        {
            if (!_cardMoveHandler.IsDragStarted())
            {
                _cardItemData.OnCardClicked(_cardItemData.CardItemIndex);
            }
            else
            {
                _cardReleaseHandler.Release(_cardItemData.CardItemIndex, _cardItemData.Parent, _cardItemData.TempParent);
            }
        }

        public void MoveCardByClick(int boardCardHolderIndex)
        {
            _cardMoveHandler.MoveCardToBoard(boardCardHolderIndex, _cardItemData.TempParent);
        }
        
        public void SetCardAnimation(bool status)
        {
            _cardViewHandler.SetCardAnimation(status);
        }

        public void BackFlipAnimation(float delayDuration, bool isGuessRight, string correctNumber)
        {
            _cardViewHandler.BackFlipAnimation(delayDuration, isGuessRight, correctNumber);
        }

        public void DestroyObject()
        {
            _cardViewHandler.DestroyObject();
        }

        public void AnimateProbabilityChange(float duration, ProbabilityType probabilityType, bool isLocked)
        {
            _cardViewHandler.AnimateProbabilityChange(duration, probabilityType, isLocked);
        }
        
        public void SetProbability(ProbabilityType probabilityType, bool isLocked)
        {
            _cardViewHandler.SetProbability(probabilityType, isLocked);
        }
        
        public RectTransform GetRectTransform()
        {
            return _cardViewHandler.GetRectTransform();
        }
        
    }
    public interface INormalCardItemController
    {
        void Initialize(INormalCardItemView cardItemView, Camera cam, IHapticController hapticController, ITutorialAbilityManager tutorialAbilityManager,
            CardItemData cardItemData, ICardItemLocator cardItemLocator, IBoardAreaController boardAreaController,
            Action<int> onDragStart);
        void ResetPosition();
        INormalCardItemView GetView();
        void MoveCardByClick(int boardCardHolderIndex);
        void BackFlipAnimation(float delayDuration, bool isGuessRight, string correctNumber);
        void SetCardAnimation(bool status);
        void DestroyObject();
        void SetProbability(ProbabilityType probabilityType, bool isLocked);
        void AnimateProbabilityChange(float duration, ProbabilityType probabilityType, bool isLocked);
        RectTransform GetRectTransform();
    }
}