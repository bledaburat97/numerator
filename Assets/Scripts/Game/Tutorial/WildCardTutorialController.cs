using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Scripts
{
    public class WildCardTutorialController : IWildCardTutorialController
    {
        private ICardItemLocator _cardItemLocator;
        private ICardHolderModelCreator _cardHolderModelCreator;
        private IInitialCardAreaController _initialCardAreaController;
        private IUnmaskServiceAreaView _unmaskServiceAreaView;
        private ITutorialMessagePopupView _tutorialMessagePopupView;
        private Queue<Action> _tutorialAnimationActions;

        public void Initialize(IInitialCardAreaController initialCardAreaController, ICardItemLocator cardItemLocator,
            IUnmaskServiceAreaView unmaskServiceAreaView,
            ITutorialMessagePopupView tutorialMessagePopupView, ICardHolderModelCreator cardHolderModelCreator)
        {
            _cardItemLocator = cardItemLocator;
            _cardHolderModelCreator = cardHolderModelCreator;
            _initialCardAreaController = initialCardAreaController;
            _unmaskServiceAreaView = unmaskServiceAreaView;
            _tutorialMessagePopupView = tutorialMessagePopupView;
            _unmaskServiceAreaView.InstantiateTutorialFade();
            _tutorialMessagePopupView.Init();
            InitializeTutorialAnimationActions();
        }

        private void InitializeTutorialAnimationActions()
        {
            _tutorialAnimationActions = new Queue<Action>();
            _unmaskServiceAreaView.CreateUnmaskCardItem(_initialCardAreaController.GetWildCardHolderPosition(), _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Initial)[0].size);
            _tutorialMessagePopupView.SetText("You can drag the wild card to see correct number at any position.");
            AddTutorialAction(() => ExecuteNextTutorialActionWithDelay(5));

            /*
            Vector2 sizeOfInitialHolder =
                _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Initial)[0].size;
            Vector2 sizeOfBoardHolder = _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Board)[0].size;
            AddTutorialAction(() => StartDragAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>()
                {
                    _initialCardAreaController.GetWildCardHolderPosition(),
                    _cardItemLocator.GetBoardCardHolderPositionAtIndex(0)
                },
                sizeList = new List<Vector2>() { sizeOfInitialHolder, sizeOfBoardHolder },
                allowedBoardHolderIndex = 0,
                text = "Drag the wild card to see correct number."
            }));
            */
            ExecuteNextTutorialAction();
        }
/*
        private void StartDragAnimation(TutorialAnimation tutorialAnimation)
        {
            DisableUI(tutorialAnimation);
            _unmaskServiceAreaView.CreateUnmaskCardItem(tutorialAnimation.posList[1], tutorialAnimation.sizeList[1]);
            _unmaskServiceAreaView.CreateUnmaskCardItem(tutorialAnimation.posList[0], tutorialAnimation.sizeList[0]);
            _handTutorialView.StartDragAnimation(tutorialAnimation.posList[0], tutorialAnimation.posList[1]);
            _tutorialMessagePopupView.SetText(tutorialAnimation.text);
            _cardItemLocator.OnCardDragStarted += StopDragAnimation;
            _cardItemLocator.OnCardReturnedToInitial += RestartDragAnimation;
            _cardItemLocator.OnCardPlacedBoard += CloseDragAnimation;

            void StopDragAnimation(object sender, EventArgs args)
            {
                _handTutorialView.StopActiveAnimation();
                _unmaskServiceAreaView.ClearUnmaskCardItem(1);
            }

            void RestartDragAnimation(object sender, EventArgs args)
            {
                _unmaskServiceAreaView.CreateUnmaskCardItem(tutorialAnimation.posList[0], tutorialAnimation.sizeList[0]);
                _handTutorialView.StartDragAnimation(tutorialAnimation.posList[0], tutorialAnimation.posList[1]);
            }

            void CloseDragAnimation(object sender, EventArgs args)
            {
                _unmaskServiceAreaView.ClearAllUnmaskCardItems();
                _cardItemLocator.OnCardDragStarted -= StopDragAnimation;

                _cardItemLocator.OnCardReturnedToInitial -= RestartDragAnimation;
                _cardItemLocator.OnCardPlacedBoard -= CloseDragAnimation;

                ExecuteNextTutorialActionWithDelay(0.3f);
            }
        }
        private void DisableUI(TutorialAnimation tutorialAnimation)
        {
            _cardItemLocator.SetDisallowedCardHolderIndexes(tutorialAnimation.allowedBoardHolderIndex);
            _initialCardAreaController.SetCardsAsUndraggable(tutorialAnimation.draggableCardIndex);
            _initialCardAreaController.SetCardsAsUnselectable(tutorialAnimation.selectableCardIndex);
        }
        */

        private void ExecuteNextTutorialActionWithDelay(float duration)
        {
            DOTween.Sequence().AppendInterval(duration).OnComplete(ExecuteNextTutorialAction);
        }
        
        private void ExecuteNextTutorialAction()
        {
            if (_tutorialAnimationActions.Count > 0)
            {
                _tutorialAnimationActions.Dequeue()?.Invoke();
            }
            else
            {
                _tutorialMessagePopupView.Destroy();
                _unmaskServiceAreaView.CloseTutorialFade();
                PlayerPrefs.SetInt("wild_card_tutorial_completed", 1);
            }
        }
        
        private void AddTutorialAction(Action action)
        {
            _tutorialAnimationActions.Enqueue(action);
        }
    }

    public interface IWildCardTutorialController
    {
        void Initialize(IInitialCardAreaController initialCardAreaController, ICardItemLocator cardItemLocator,
            IUnmaskServiceAreaView unmaskServiceAreaView,
            ITutorialMessagePopupView tutorialMessagePopupView, ICardHolderModelCreator cardHolderModelCreator);
    }
}