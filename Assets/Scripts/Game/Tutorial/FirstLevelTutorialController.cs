using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Scripts
{
    public class FirstLevelTutorialController : IFirstLevelTutorialController
    {
        private IUnmaskServiceAreaView _unmaskServiceAreaView;
        private IHandTutorialView _handTutorialView;
        private ITutorialMessagePopupView _tutorialMessagePopupView;
        private ICardItemLocator _cardItemLocator;
        private IInitialCardAreaController _initialCardAreaController;
        private IGameUIController _gameUIController;
        private Queue<Action> _tutorialAnimationActions;
        private ICardHolderModelCreator _cardHolderModelCreator;
        private IResultAreaController _resultAreaController;
        public void Initialize(IInitialCardAreaController initialCardAreaController, ICardItemLocator cardItemLocator,
            IHandTutorialView handTutorialView, IUnmaskServiceAreaView unmaskServiceAreaView,
            ITutorialMessagePopupView tutorialMessagePopupView, ICardHolderModelCreator cardHolderModelCreator, IGameUIController gameUIController, IResultAreaController resultAreaController)
        {
            _unmaskServiceAreaView = unmaskServiceAreaView;
            _handTutorialView = handTutorialView;
            _tutorialMessagePopupView = tutorialMessagePopupView;
            _cardItemLocator = cardItemLocator;
            _initialCardAreaController = initialCardAreaController;
            _gameUIController = gameUIController;
            _cardHolderModelCreator = cardHolderModelCreator;
            _resultAreaController = resultAreaController;
            _unmaskServiceAreaView.InstantiateTutorialFade();
            _handTutorialView.Init();
            _tutorialMessagePopupView.Init();
            InitializeTutorialAnimationActions();
        }

        private void InitializeTutorialAnimationActions()
        {
            _tutorialAnimationActions = new Queue<Action>();
            Vector2 sizeOfInitialHolder = _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Initial)[0].size + Vector2.one;
            Vector2 sizeOfBoardHolder = _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Board)[0].size + Vector2.one;
            
            AddTutorialAction(() => StartDragAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_initialCardAreaController.GetNormalCardHolderPositionAtIndex(0), _cardItemLocator.GetBoardCardHolderPositionAtIndex(0)},
                sizeList = new List<Vector2>(){sizeOfInitialHolder, sizeOfBoardHolder},
                allowedBoardHolderIndex = 0,
                draggableCardIndex = 0,
                text = "You can drag the number to the board."
            }));
            AddTutorialAction(() => StartClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_initialCardAreaController.GetNormalCardHolderPositionAtIndex(1)},
                sizeList = new List<Vector2>(){sizeOfInitialHolder},
                selectableCardIndex = 1,
                text = "Or you can click the number."
            }));
            AddTutorialAction(() => StartCheckButtonClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_gameUIController.GetCheckButtonRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_gameUIController.GetCheckButtonRectTransform().rect.width, _gameUIController.GetCheckButtonRectTransform().rect.height)},
                isCheckButtonActive = true,
                text = "You can click the check button."
            }));
            ResultAreaInfo resultAreaInfo = _resultAreaController.GetResultAreaInfo();
            AddTutorialAction(() => ShowResultBlock(new TutorialAnimation()
            {
                posList = new List<Vector2>(){resultAreaInfo.topPoint},
                sizeList = new List<Vector2>(){resultAreaInfo.resultBlockSize},
                changeInLocalPos = new Vector2(0,- resultAreaInfo.resultBlockSize.y / 2),
                text = "Any number is not at correct position. One number is at wrong position."
            }));
            AddTutorialAction(() => ExecuteNextTutorialActionWithDelay(5));
            AddTutorialAction(() => StartResetButtonClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_gameUIController.GetResetButtonRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_gameUIController.GetResetButtonRectTransform().rect.width, _gameUIController.GetResetButtonRectTransform().rect.height)},
                isResetButtonActive = true,
                text = "You can click the reset button."
            }));
            AddTutorialAction(() => StartDragAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_initialCardAreaController.GetNormalCardHolderPositionAtIndex(2), _cardItemLocator.GetBoardCardHolderPositionAtIndex(0)},
                sizeList = new List<Vector2>(){sizeOfInitialHolder, sizeOfBoardHolder},
                allowedBoardHolderIndex = 0,
                draggableCardIndex = 2,
                text = "You can drag the number to the board."
            }));
            AddTutorialAction(() => StartDragAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_initialCardAreaController.GetNormalCardHolderPositionAtIndex(0), _cardItemLocator.GetBoardCardHolderPositionAtIndex(1)},
                sizeList = new List<Vector2>(){sizeOfInitialHolder, sizeOfBoardHolder},
                allowedBoardHolderIndex = 1,
                draggableCardIndex = 0,
                text = "You can drag the number to the board."
            }));
            AddTutorialAction(() => StartCheckButtonClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_gameUIController.GetCheckButtonRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_gameUIController.GetCheckButtonRectTransform().rect.width, _gameUIController.GetCheckButtonRectTransform().rect.height)},
                isCheckButtonActive = true,
                text = "You can click the check button."
            }));
            AddTutorialAction(() => ShowResultBlock(new TutorialAnimation()
            {
                posList = new List<Vector2>(){resultAreaInfo.topPoint},
                sizeList = new List<Vector2>(){resultAreaInfo.resultBlockSize},
                changeInLocalPos = new Vector2(0, - (resultAreaInfo.resultBlockSize.y + resultAreaInfo.spacing) - resultAreaInfo.resultBlockSize.y / 2),
                text = "Only one number is at correct position, the other one doesn't exist."
            }));
            AddTutorialAction(() => ExecuteNextTutorialActionWithDelay(5));
            AddTutorialAction(() => StartDragAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemLocator.GetBoardCardHolderPositionAtIndex(0), _initialCardAreaController.GetNormalCardHolderPositionAtIndex(2)},
                sizeList = new List<Vector2>(){sizeOfBoardHolder, sizeOfInitialHolder},
                allowedBoardHolderIndex = 0,
                draggableCardIndex = 2,
                text = "You can drag the number to the initial area back."
            }, true));
            AddTutorialAction(() => StartDragAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_initialCardAreaController.GetNormalCardHolderPositionAtIndex(3), _cardItemLocator.GetBoardCardHolderPositionAtIndex(0)},
                sizeList = new List<Vector2>(){sizeOfInitialHolder, sizeOfBoardHolder},
                allowedBoardHolderIndex = 0,
                draggableCardIndex = 3,
                text = "You can drag the number to the board."
            }));
            AddTutorialAction(() => StartCheckButtonClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_gameUIController.GetCheckButtonRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_gameUIController.GetCheckButtonRectTransform().rect.width, _gameUIController.GetCheckButtonRectTransform().rect.height)},
                isCheckButtonActive = true,
                text = "You can click the check button."
            }));
            AddTutorialAction(() => ShowResultBlock(new TutorialAnimation()
            {
                posList = new List<Vector2>(){resultAreaInfo.topPoint},
                sizeList = new List<Vector2>(){resultAreaInfo.resultBlockSize},
                changeInLocalPos = new Vector2(0, - 2 * (resultAreaInfo.resultBlockSize.y + resultAreaInfo.spacing) - resultAreaInfo.resultBlockSize.y / 2),
                text = "Both of the numbers are at correct position. Congratulations!!!"
            }));
            ExecuteNextTutorialAction();
        }
        
        private void AddTutorialAction(Action action)
        {
            _tutorialAnimationActions.Enqueue(action);
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
                _handTutorialView.Destroy();
                _unmaskServiceAreaView.CloseTutorialFade();
                PlayerPrefs.SetInt("first_level_tutorial_completed", 1);
            }
        }

        private void StartDragAnimation(TutorialAnimation tutorialAnimation, bool isReversed = false)
        {
            DisableUI(tutorialAnimation);
            _unmaskServiceAreaView.CreateUnmaskCardItem(tutorialAnimation.posList[0], tutorialAnimation.sizeList[0]);
            _unmaskServiceAreaView.CreateUnmaskCardItem(tutorialAnimation.posList[1], tutorialAnimation.sizeList[1]);
            _handTutorialView.StartDragAnimation(tutorialAnimation.posList[0], tutorialAnimation.posList[1]);
            _tutorialMessagePopupView.SetText(tutorialAnimation.text);
            _cardItemLocator.OnCardDragStarted += StopDragAnimation;

            if (!isReversed)
            {
                _cardItemLocator.OnCardReturnedToInitial += RestartDragAnimation;
                _cardItemLocator.OnCardPlacedBoard += CloseDragAnimation;
            }
            else
            {
                _cardItemLocator.OnCardReturnedToInitial += CloseDragAnimation;
                _cardItemLocator.OnCardPlacedBoard += RestartDragAnimation;
            }

            void StopDragAnimation(object sender, EventArgs args)
            {
                _handTutorialView.StopActiveAnimation();
            }

            void RestartDragAnimation(object sender, EventArgs args)
            {
                _handTutorialView.StartDragAnimation(tutorialAnimation.posList[0], tutorialAnimation.posList[1]);
            }

            void CloseDragAnimation(object sender, EventArgs args)
            {
                _unmaskServiceAreaView.ClearAllUnmaskCardItems();
                _cardItemLocator.OnCardDragStarted -= StopDragAnimation;
                if (!isReversed)
                {
                    _cardItemLocator.OnCardReturnedToInitial -= RestartDragAnimation;
                    _cardItemLocator.OnCardPlacedBoard -= CloseDragAnimation;
                }
                else
                {
                    _cardItemLocator.OnCardReturnedToInitial -= CloseDragAnimation;
                    _cardItemLocator.OnCardPlacedBoard -= RestartDragAnimation;
                }
                ExecuteNextTutorialActionWithDelay(0.3f);
            }
        }

        private void StartClickAnimation(TutorialAnimation tutorialAnimation)
        {
            DisableUI(tutorialAnimation);
            _unmaskServiceAreaView.CreateUnmaskCardItem(tutorialAnimation.posList[0], tutorialAnimation.sizeList[0]);
            _handTutorialView.StartClickAnimation(tutorialAnimation.posList[0]);
            _tutorialMessagePopupView.SetText(tutorialAnimation.text);
            _initialCardAreaController.CardClicked += CloseClickAnimation;
            
            void CloseClickAnimation(object sender, EventArgs args)
            {
                _unmaskServiceAreaView.ClearAllUnmaskCardItems();
                _initialCardAreaController.CardClicked -= CloseClickAnimation;
                _handTutorialView.StopActiveAnimation();
                ExecuteNextTutorialActionWithDelay(0.3f);
            }
        }

        private void ShowResultBlock(TutorialAnimation tutorialAnimation)
        {
            DisableUI(tutorialAnimation);
            _unmaskServiceAreaView.CreateUnmaskCardItem(tutorialAnimation.posList[0], tutorialAnimation.sizeList[0]);
            _unmaskServiceAreaView.ChangeLocalPositionOfUnmaskCardItem(tutorialAnimation.changeInLocalPos);
            _tutorialMessagePopupView.SetText(tutorialAnimation.text);
            ExecuteNextTutorialActionWithDelay(0.3f);
        }
        
        private void StartCheckButtonClickAnimation(TutorialAnimation tutorialAnimation)
        {
            DisableUI(tutorialAnimation);
            _unmaskServiceAreaView.CreateUnmaskCardItem(tutorialAnimation.posList[0], tutorialAnimation.sizeList[0]);
            _handTutorialView.StartClickAnimation(tutorialAnimation.posList[0]);
            _tutorialMessagePopupView.SetText(tutorialAnimation.text);
            _gameUIController.CheckFinalNumbers += CloseClickButtonAnimation;
            
            void CloseClickButtonAnimation(object sender, EventArgs args)
            {
                _unmaskServiceAreaView.ClearAllUnmaskCardItems();
                _gameUIController.CheckFinalNumbers -= CloseClickButtonAnimation;
                _handTutorialView.StopActiveAnimation();
                ExecuteNextTutorialActionWithDelay(0.3f);
            }
        }
        
        private void StartResetButtonClickAnimation(TutorialAnimation tutorialAnimation)
        {
            DisableUI(tutorialAnimation);
            _unmaskServiceAreaView.CreateUnmaskCardItem(tutorialAnimation.posList[0], tutorialAnimation.sizeList[0]);
            _handTutorialView.StartClickAnimation(tutorialAnimation.posList[0]);
            _tutorialMessagePopupView.SetText(tutorialAnimation.text);
            _gameUIController.ResetNumbers += CloseClickButtonAnimation;
            
            void CloseClickButtonAnimation(object sender, EventArgs args)
            {
                _unmaskServiceAreaView.ClearAllUnmaskCardItems();
                _gameUIController.ResetNumbers -= CloseClickButtonAnimation;
                _handTutorialView.StopActiveAnimation();
                ExecuteNextTutorialActionWithDelay(0.3f);
            }
        }

        private void DisableUI(TutorialAnimation tutorialAnimation)
        {
            _cardItemLocator.SetDisallowedCardHolderIndexes(tutorialAnimation.allowedBoardHolderIndex);
            _initialCardAreaController.SetCardsAsUndraggable(tutorialAnimation.draggableCardIndex);
            _initialCardAreaController.SetCardsAsUnselectable(tutorialAnimation.selectableCardIndex);
            _gameUIController.SetCheckButtonClickable(tutorialAnimation.isCheckButtonActive);
            _gameUIController.SetResetButtonClickable(tutorialAnimation.isResetButtonActive);
            _gameUIController.SetCardInfoButtonClickable(tutorialAnimation.isCardInfoButtonActive);
        }

        private void ExecuteNextTutorialActionWithDelay(float duration)
        {
            DOTween.Sequence().AppendInterval(duration).OnComplete(ExecuteNextTutorialAction);
        }

    }

    public interface IFirstLevelTutorialController
    {
        void Initialize(IInitialCardAreaController initialCardAreaController, ICardItemLocator cardItemLocator,
            IHandTutorialView handTutorialView, IUnmaskServiceAreaView unmaskServiceAreaView,
            ITutorialMessagePopupView tutorialMessagePopupView, ICardHolderModelCreator cardHolderModelCreator, IGameUIController gameUIController, IResultAreaController resultAreaController);
    }

    public class TutorialAnimation
    {
        public List<Vector2> posList;
        public List<Vector2> sizeList;
        public int allowedBoardHolderIndex = -1;
        public int draggableCardIndex = -1;
        public int selectableCardIndex = -1;
        public bool isCheckButtonActive = false;
        public bool isResetButtonActive = false;
        public bool isCardInfoButtonActive = false;
        public string text;
        public Vector2 changeInLocalPos;
    }
}