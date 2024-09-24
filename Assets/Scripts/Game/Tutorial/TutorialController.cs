using System;
using System.Collections.Generic;
using DG.Tweening;
using Game;
using UnityEngine;

namespace Scripts
{
    public class TutorialController : ITutorialController
    {
        protected IUnmaskServiceAreaView _unmaskServiceAreaView;
        protected IHandTutorialView _handTutorialView;
        protected ITutorialMessagePopupView _tutorialMessagePopupView;
        protected ICardItemLocator _cardItemLocator;
        protected IInitialCardAreaController _initialCardAreaController;
        protected IGameUIController _gameUIController;
        protected Queue<Action> _tutorialAnimationActions;
        protected ICardHolderModelCreator _cardHolderModelCreator;
        protected IResultAreaController _resultAreaController;
        protected ICardItemInfoPopupController _cardItemInfoPopupController;
        protected ICardItemInfoManager _cardItemInfoManager;
        protected ITutorialAbilityManager _tutorialAbilityManager;
        protected ICardInteractionManager _cardInteractionManager;
        protected IBoardAreaController _boardAreaController;
        protected Vector2 _sizeOfInitialHolder;
        protected Vector2 _sizeOfBoardHolder;
        
        public void Initialize(IInitialCardAreaController initialCardAreaController, ICardItemLocator cardItemLocator,
            IHandTutorialView handTutorialView, IUnmaskServiceAreaView unmaskServiceAreaView,
            ITutorialMessagePopupView tutorialMessagePopupView, ICardHolderModelCreator cardHolderModelCreator, IGameUIController gameUIController, IResultAreaController resultAreaController, ICardItemInfoPopupController cardItemInfoPopupController, ICardItemInfoManager cardItemInfoManager, ITutorialAbilityManager tutorialAbilityManager, ICardInteractionManager cardInteractionManager, IBoardAreaController boardAreaController)
        {
            _unmaskServiceAreaView = unmaskServiceAreaView;
            _handTutorialView = handTutorialView;
            _tutorialMessagePopupView = tutorialMessagePopupView;
            _cardItemLocator = cardItemLocator;
            _initialCardAreaController = initialCardAreaController;
            _gameUIController = gameUIController;
            _cardHolderModelCreator = cardHolderModelCreator;
            _resultAreaController = resultAreaController;
            _cardItemInfoPopupController = cardItemInfoPopupController;
            _cardItemInfoManager = cardItemInfoManager;
            _tutorialAbilityManager = tutorialAbilityManager;
            _cardInteractionManager = cardInteractionManager;
            _boardAreaController = boardAreaController;
            _unmaskServiceAreaView.InstantiateTutorialFade();
            _tutorialMessagePopupView.Init();
            _tutorialAnimationActions = new Queue<Action>();
            _sizeOfInitialHolder = _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Initial)[0].size + Vector2.one;
            _sizeOfBoardHolder = _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Board)[0].size + Vector2.one;
            InitializeTutorialAnimationActions();
        }

        protected virtual void InitializeTutorialAnimationActions()
        {
            
        }
        
        protected void AddTutorialAction(Action action)
        {
            _tutorialAnimationActions.Enqueue(action);
        }

        protected void ExecuteNextTutorialAction()
        {
            if (_tutorialAnimationActions.Count > 0)
            {
                _tutorialAnimationActions.Dequeue()?.Invoke();
            }
            else
            {
                _tutorialMessagePopupView.Destroy();
                _handTutorialView.Destroy();
                _unmaskServiceAreaView.CloseTutorialFade();
                PlayerPrefs.SetInt("first_level_tutorial_completed", 1);
            }
        }

        protected void StartDragAnimation(int cardIndex, int boardIndex, bool isReversed = false)
        {
            Vector2 cardHolderPosition = _initialCardAreaController.GetNormalCardHolderPositionAtIndex(cardIndex);
            Vector2 boardHolderPosition = _boardAreaController.GetBoardHolderPositionAtIndex(boardIndex);
            _tutorialAbilityManager.SetCurrentTutorialAbility(new TutorialAbility()
            {
                draggableBoardIndex = boardIndex,
                draggableCardIndex = cardIndex
            });
            _unmaskServiceAreaView.CreateUnmaskCardItem(cardHolderPosition, _sizeOfInitialHolder);
            _unmaskServiceAreaView.CreateUnmaskCardItem(boardHolderPosition, _sizeOfBoardHolder);
            _initialCardAreaController.OnCardDragStartedEvent += StopDragAnimation;

            if (!isReversed)
            {
                _handTutorialView.StartDragAnimation(cardHolderPosition, boardHolderPosition);
                _tutorialMessagePopupView.SetText("You can drag the number to the board.");
                _cardItemLocator.CardReturnedToInitialEvent += RestartDragAnimation;
                _cardItemLocator.CardPlacedBoardEvent += CloseDragAnimation;
            }
            else
            {
                _handTutorialView.StartDragAnimation(boardHolderPosition, cardHolderPosition);
                _tutorialMessagePopupView.SetText("You can drag the number to initial area back.");
                _cardItemLocator.CardReturnedToInitialEvent += CloseDragAnimation;
                _cardItemLocator.CardPlacedBoardEvent += RestartDragAnimation;
            }

            void StopDragAnimation(object sender, EventArgs args)
            {
                _handTutorialView.StopActiveAnimation();
            }

            void RestartDragAnimation(object sender, EventArgs args)
            {
                _handTutorialView.StartDragAnimation(cardHolderPosition, boardHolderPosition);
            }

            void CloseDragAnimation(object sender, EventArgs args)
            {
                _unmaskServiceAreaView.ClearAllUnmaskCardItems();
                _initialCardAreaController.OnCardDragStartedEvent -= StopDragAnimation;
                if (!isReversed)
                {
                    _cardItemLocator.CardReturnedToInitialEvent -= RestartDragAnimation;
                    _cardItemLocator.CardPlacedBoardEvent -= CloseDragAnimation;
                }
                else
                {
                    _cardItemLocator.CardReturnedToInitialEvent -= CloseDragAnimation;
                    _cardItemLocator.CardPlacedBoardEvent -= RestartDragAnimation;
                }
                ExecuteNextTutorialActionWithDelay(0.3f);
            }
        }

        protected void StartCardClickAnimation(int cardIndex, Vector2 position, Vector2 size)
        {
            _tutorialAbilityManager.SetCurrentTutorialAbility(new TutorialAbility()
            {
                selectableCardIndex = cardIndex
            });
            _unmaskServiceAreaView.CreateUnmaskCardItem(position, size);
            _handTutorialView.StartClickAnimation(position);
            _tutorialMessagePopupView.SetText("You can click the card.");
            _initialCardAreaController.OnCardClickedEvent += CloseClickAnimation;
            
            void CloseClickAnimation(object sender, CardClickedEventArgs args)
            {
                _unmaskServiceAreaView.ClearAllUnmaskCardItems();
                _initialCardAreaController.OnCardClickedEvent -= CloseClickAnimation;
                _handTutorialView.StopActiveAnimation();
                ExecuteNextTutorialActionWithDelay(0.3f);
            }
        }

        protected void ShowResultBlock(int resultBlockIndex, string text)
        {
            ResultAreaInfo resultAreaInfo = _resultAreaController.GetResultAreaInfo();
            float changeInLocalPosY = - resultBlockIndex * (resultAreaInfo.resultBlockSize.y + resultAreaInfo.spacing) -
                                      resultAreaInfo.resultBlockSize.y / 2;
            _tutorialAbilityManager.SetCurrentTutorialAbility(new TutorialAbility());
            _unmaskServiceAreaView.CreateUnmaskCardItem(resultAreaInfo.topPoint, resultAreaInfo.resultBlockSize, changeInLocalPosY);
            _tutorialMessagePopupView.SetText(text);
            ExecuteNextTutorialActionWithDelay(0.3f);
        }
        
        protected void StartCheckButtonClickAnimation()
        {
            RectTransform checkButtonRectTransform = _gameUIController.GetCheckButtonRectTransform();
            Vector2 position = checkButtonRectTransform.position;
            Vector2 size = new Vector2(checkButtonRectTransform.rect.width, checkButtonRectTransform.rect.height);
            _tutorialAbilityManager.SetCurrentTutorialAbility(new TutorialAbility());
            _unmaskServiceAreaView.CreateUnmaskCardItem(position, size);
            _handTutorialView.StartClickAnimation(position);
            _tutorialMessagePopupView.SetText("You can click the check button.");
            _gameUIController.CheckFinalNumbers += CloseClickButtonAnimation;
            _gameUIController.SetAllButtonsUnclickable();
            _gameUIController.SetButtonClickable(true, GameUIButtonType.Check);
            void CloseClickButtonAnimation(object sender, EventArgs args)
            {
                _unmaskServiceAreaView.ClearAllUnmaskCardItems();
                _gameUIController.CheckFinalNumbers -= CloseClickButtonAnimation;
                _handTutorialView.StopActiveAnimation();
                ExecuteNextTutorialActionWithDelay(0.3f);
            }
        }
        
        protected void StartResetButtonClickAnimation()
        {
            RectTransform resetButtonRectTransform = _gameUIController.GetResetButtonRectTransform();
            Vector2 position = resetButtonRectTransform.position;
            Vector2 size = new Vector2(resetButtonRectTransform.rect.width, resetButtonRectTransform.rect.height);
            _tutorialAbilityManager.SetCurrentTutorialAbility(new TutorialAbility());
            _unmaskServiceAreaView.CreateUnmaskCardItem(position, size);
            _handTutorialView.StartClickAnimation(position);
            _tutorialMessagePopupView.SetText("You can click the reset button.");
            _gameUIController.ResetNumbers += CloseClickButtonAnimation;
            _gameUIController.SetAllButtonsUnclickable();
            _gameUIController.SetButtonClickable(true, GameUIButtonType.Reset);
            void CloseClickButtonAnimation(object sender, EventArgs args)
            {
                _unmaskServiceAreaView.ClearAllUnmaskCardItems();
                _gameUIController.ResetNumbers -= CloseClickButtonAnimation;
                _handTutorialView.StopActiveAnimation();
                ExecuteNextTutorialActionWithDelay(0.3f);
            }
        }
        
        protected void WaitForATime(float duration)
        {
            DOTween.Sequence().AppendInterval(duration).OnComplete(() =>
            {
                _unmaskServiceAreaView.ClearAllUnmaskCardItems();
                ExecuteNextTutorialAction();
            });
        }

        protected void ExecuteNextTutorialActionWithDelay(float duration)
        {
            DOTween.Sequence().AppendInterval(duration).OnComplete(ExecuteNextTutorialAction);
        }
    }
    
    public interface ITutorialController
    {
        void Initialize(IInitialCardAreaController initialCardAreaController, ICardItemLocator cardItemLocator,
            IHandTutorialView handTutorialView, IUnmaskServiceAreaView unmaskServiceAreaView,
            ITutorialMessagePopupView tutorialMessagePopupView, ICardHolderModelCreator cardHolderModelCreator, IGameUIController gameUIController, IResultAreaController resultAreaController, ICardItemInfoPopupController cardItemInfoPopupController, ICardItemInfoManager cardItemInfoManager, ITutorialAbilityManager tutorialAbilityManager, ICardInteractionManager cardInteractionManager, IBoardAreaController boardAreaController);
    }
}