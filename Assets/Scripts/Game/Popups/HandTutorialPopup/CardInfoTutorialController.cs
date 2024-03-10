using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Scripts
{
    public class CardInfoTutorialController : ICardInfoTutorialController
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
        private ICardItemInfoPopupController _cardItemInfoPopupController;
        private ICardItemInfoManager _cardItemInfoManager;
        public void Initialize(IInitialCardAreaController initialCardAreaController, ICardItemLocator cardItemLocator,
            IHandTutorialView handTutorialView, IUnmaskServiceAreaView unmaskServiceAreaView,
            ITutorialMessagePopupView tutorialMessagePopupView, ICardHolderModelCreator cardHolderModelCreator, IGameUIController gameUIController, IResultAreaController resultAreaController, ICardItemInfoPopupController cardItemInfoPopupController, ICardItemInfoManager cardItemInfoManager)
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
            _unmaskServiceAreaView.InstantiateTutorialFade();
            _handTutorialView.Init();
            _tutorialMessagePopupView.Init();
            InitializeTutorialAnimationActions();
        }

        private void InitializeTutorialAnimationActions()
        {
            _tutorialAnimationActions = new Queue<Action>();
            Vector2 sizeOfInitialHolder = _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Initial)[0].size;
            Vector2 sizeOfBoardHolder = _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Board)[0].size;
            
            AddTutorialAction(() => StartDragAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_initialCardAreaController.GetNormalCardHolderPositionAtIndex(0), _cardItemLocator.GetBoardCardHolderPositionAtIndex(0)},
                sizeList = new List<Vector2>(){sizeOfInitialHolder, sizeOfBoardHolder},
                allowedBoardHolderIndex = 0,
                draggableCardIndex = 0,
                text = "Drag the card to the board."
            }));
            AddTutorialAction(() => StartDragAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_initialCardAreaController.GetNormalCardHolderPositionAtIndex(1), _cardItemLocator.GetBoardCardHolderPositionAtIndex(1)},
                sizeList = new List<Vector2>(){sizeOfInitialHolder, sizeOfBoardHolder},
                allowedBoardHolderIndex = 1,
                draggableCardIndex = 1,
                text = "Drag the card to the board."
            }));
            AddTutorialAction(() => StartCheckButtonClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_gameUIController.GetCheckButtonRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_gameUIController.GetCheckButtonRectTransform().rect.width, _gameUIController.GetCheckButtonRectTransform().rect.height)},
                text = "Click the check button."
            }));
            ResultAreaInfo resultAreaInfo = _resultAreaController.GetResultAreaInfo();
            AddTutorialAction(() => ShowResultBlock(new TutorialAnimation()
            {
                posList = new List<Vector2>(){resultAreaInfo.topPoint},
                sizeList = new List<Vector2>(){resultAreaInfo.resultBlockSize},
                changeInLocalPos = new Vector2(0,- resultAreaInfo.resultBlockSize.y / 2),
                text = "Any number does not located on the board."
            }));
            AddTutorialAction(() => ExecuteNextTutorialActionWithDelay(5));
            AddTutorialAction(() => StartCardInfoButtonClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_gameUIController.GetCardInfoButtonRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_gameUIController.GetCardInfoButtonRectTransform().rect.width, _gameUIController.GetCardInfoButtonRectTransform().rect.height)},
                text = "Click the card info button."
            }));
            AddTutorialAction(() => StartClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemLocator.GetBoardCardHolderPositionAtIndex(0)},
                sizeList = new List<Vector2>(){sizeOfBoardHolder},
                selectableCardIndex = 0,
                text = "Click the card."
            }));
                //CLICK RED.
            AddTutorialAction(() => StartProbabilityButtonClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().rect.width, _cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().rect.height)},
                text = "When the card does not exist on the board, turn the color to red."
            }));
            AddTutorialAction(() => StartClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemLocator.GetBoardCardHolderPositionAtIndex(1)},
                sizeList = new List<Vector2>(){sizeOfBoardHolder},
                selectableCardIndex = 1,
                text = "Click the card."
            }));
                //CLICK RED.
            AddTutorialAction(() => StartProbabilityButtonClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().rect.width, _cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().rect.height)},
                text = "When the card does not exist on the board, turn the color to red."
            }));
            AddTutorialAction(() => StartResetButtonClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_gameUIController.GetResetButtonRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_gameUIController.GetResetButtonRectTransform().rect.width, _gameUIController.GetResetButtonRectTransform().rect.height)},
                text = "Click the reset button."
            }));
            AddTutorialAction(() => StartDragAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_initialCardAreaController.GetNormalCardHolderPositionAtIndex(2), _cardItemLocator.GetBoardCardHolderPositionAtIndex(0)},
                sizeList = new List<Vector2>(){sizeOfInitialHolder, sizeOfBoardHolder},
                allowedBoardHolderIndex = 0,
                draggableCardIndex = 2,
                text = "Drag the card to the board."
            }));
            AddTutorialAction(() => StartDragAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_initialCardAreaController.GetNormalCardHolderPositionAtIndex(3), _cardItemLocator.GetBoardCardHolderPositionAtIndex(1)},
                sizeList = new List<Vector2>(){sizeOfInitialHolder, sizeOfBoardHolder},
                allowedBoardHolderIndex = 1,
                draggableCardIndex = 3,
                text = "Drag the card to the board."
            }));
            AddTutorialAction(() => StartCheckButtonClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_gameUIController.GetCheckButtonRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_gameUIController.GetCheckButtonRectTransform().rect.width, _gameUIController.GetCheckButtonRectTransform().rect.height)},
                text = "Click the check button."
            }));
            AddTutorialAction(() => ShowResultBlock(new TutorialAnimation()
            {
                posList = new List<Vector2>(){resultAreaInfo.topPoint},
                sizeList = new List<Vector2>(){resultAreaInfo.resultBlockSize},
                changeInLocalPos = new Vector2(0, - resultAreaInfo.resultBlockSize.y - resultAreaInfo.spacing - resultAreaInfo.resultBlockSize.y / 2),
                text = "Any number is not at correct position. One number is at wrong position."
            }));
            AddTutorialAction(() => ExecuteNextTutorialActionWithDelay(5));
            AddTutorialAction(() => StartResetButtonClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_gameUIController.GetResetButtonRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_gameUIController.GetResetButtonRectTransform().rect.width, _gameUIController.GetResetButtonRectTransform().rect.height)},
                text = "Click the reset button."
            }));
            AddTutorialAction(() => StartClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_initialCardAreaController.GetNormalCardHolderPositionAtIndex(2)},
                sizeList = new List<Vector2>(){sizeOfInitialHolder},
                selectableCardIndex = 2,
                text = "Click the card."
            }));
            //CLICK A
            AddTutorialAction(() => StartHolderIndicatorButtonClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemInfoPopupController.GetHolderIndicatorButton(0).GetView().GetRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_cardItemInfoPopupController.GetHolderIndicatorButton(0).GetView().GetRectTransform().rect.width, _cardItemInfoPopupController.GetHolderIndicatorButton(0).GetView().GetRectTransform().rect.height)},
                text = "If the card does not exist on a holder on the board, put a cross for that holder."
            }));
            AddTutorialAction(() => StartClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_initialCardAreaController.GetNormalCardHolderPositionAtIndex(3)},
                sizeList = new List<Vector2>(){sizeOfInitialHolder},
                selectableCardIndex = 3,
                text = "Click the card."
            }));
            //CLICK B
            AddTutorialAction(() => StartHolderIndicatorButtonClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemInfoPopupController.GetHolderIndicatorButton(1).GetView().GetRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_cardItemInfoPopupController.GetHolderIndicatorButton(1).GetView().GetRectTransform().rect.width, _cardItemInfoPopupController.GetHolderIndicatorButton(1).GetView().GetRectTransform().rect.height)},
                text = "If the card does not exist on a holder on the board, put a cross for that holder."
            }));
            AddTutorialAction(() => StartBoardClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemLocator.GetBoardCardHolderPositionAtIndex(0)},
                sizeList = new List<Vector2>(){sizeOfBoardHolder},
                text = "Click the board."
            }));
            AddTutorialAction(() => StartDragAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_initialCardAreaController.GetNormalCardHolderPositionAtIndex(4), _cardItemLocator.GetBoardCardHolderPositionAtIndex(1)},
                sizeList = new List<Vector2>(){sizeOfInitialHolder, sizeOfBoardHolder},
                allowedBoardHolderIndex = 1,
                draggableCardIndex = 4,
                text = "Drag the card to the board."
            }));
            AddTutorialAction(() => StartCheckButtonClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_gameUIController.GetCheckButtonRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_gameUIController.GetCheckButtonRectTransform().rect.width, _gameUIController.GetCheckButtonRectTransform().rect.height)},
                text = "Click the check button."
            }));
            AddTutorialAction(() => ShowResultBlock(new TutorialAnimation()
            {
                posList = new List<Vector2>(){resultAreaInfo.topPoint},
                sizeList = new List<Vector2>(){resultAreaInfo.resultBlockSize},
                changeInLocalPos = new Vector2(0, -2* (resultAreaInfo.resultBlockSize.y + resultAreaInfo.spacing) - resultAreaInfo.resultBlockSize.y / 2),
                text = "Only one number is at correct position."
            }));
            AddTutorialAction(() => ExecuteNextTutorialActionWithDelay(5));
            AddTutorialAction(() => StartClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemLocator.GetBoardCardHolderPositionAtIndex(0)},
                sizeList = new List<Vector2>(){sizeOfBoardHolder},
                selectableCardIndex = 3,
                text = "Click the card."
            }));
            //CLICK GREEN.
            AddTutorialAction(() => StartProbabilityButtonClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.Certain).GetView().GetRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.Certain).GetView().GetRectTransform().rect.width, _cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.Certain).GetView().GetRectTransform().rect.height)},
                text = "When the card certainly exists on the board, turn the color to green."
            }));
            AddTutorialAction(() => StartClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemLocator.GetBoardCardHolderPositionAtIndex(1)},
                sizeList = new List<Vector2>(){sizeOfBoardHolder},
                selectableCardIndex = 4,
                text = "Click the card."
            }));
            //CLICK RED
            AddTutorialAction(() => StartProbabilityButtonClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().rect.width, _cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().rect.height)},
                text = "When the card does not exist on the board, turn the color to red."
            }));
            AddTutorialAction(() => StartClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_initialCardAreaController.GetNormalCardHolderPositionAtIndex(2)},
                sizeList = new List<Vector2>(){sizeOfInitialHolder},
                selectableCardIndex = 2,
                text = "Click the card."
            }));
            //CLICK RED
            AddTutorialAction(() => StartProbabilityButtonClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().rect.width, _cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().rect.height)},
                text = "When the card does not exist on the board, turn the color to red."
            }));
            AddTutorialAction(() => StartDragAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemLocator.GetBoardCardHolderPositionAtIndex(1), _initialCardAreaController.GetNormalCardHolderPositionAtIndex(4)},
                sizeList = new List<Vector2>(){sizeOfBoardHolder, sizeOfInitialHolder},
                allowedBoardHolderIndex = 1,
                draggableCardIndex = 4,
                text = "Drag the card to the initial area back."
            }, true));
            AddTutorialAction(() => StartDragAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_initialCardAreaController.GetNormalCardHolderPositionAtIndex(5), _cardItemLocator.GetBoardCardHolderPositionAtIndex(1)},
                sizeList = new List<Vector2>(){sizeOfInitialHolder, sizeOfBoardHolder},
                allowedBoardHolderIndex = 1,
                draggableCardIndex = 5,
                text = "Drag the card to the board."
            }));
            AddTutorialAction(() => StartCheckButtonClickAnimation(new TutorialAnimation()
            {
                posList = new List<Vector2>(){_gameUIController.GetCheckButtonRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_gameUIController.GetCheckButtonRectTransform().rect.width, _gameUIController.GetCheckButtonRectTransform().rect.height)},
                text = "Click the check button."
            }));
            AddTutorialAction(() => ShowResultBlock(new TutorialAnimation()
            {
                posList = new List<Vector2>(){resultAreaInfo.topPoint},
                sizeList = new List<Vector2>(){resultAreaInfo.resultBlockSize},
                changeInLocalPos = new Vector2(0, - 3 * (resultAreaInfo.resultBlockSize.y + resultAreaInfo.spacing) - resultAreaInfo.resultBlockSize.y / 2),
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
                _unmaskServiceAreaView.ClearUnmaskCardItems();
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
                _unmaskServiceAreaView.ClearUnmaskCardItems();
                _initialCardAreaController.CardClicked -= CloseClickAnimation;
                _handTutorialView.StopActiveAnimation();
                ExecuteNextTutorialActionWithDelay(0.3f);
            }
        }
        
        private void StartProbabilityButtonClickAnimation(TutorialAnimation tutorialAnimation)
        {
            DisableUI(tutorialAnimation);
            _unmaskServiceAreaView.CreateUnmaskCardItem(tutorialAnimation.posList[0], tutorialAnimation.sizeList[0]);
            _handTutorialView.StartClickAnimation(tutorialAnimation.posList[0]);
            _tutorialMessagePopupView.SetText(tutorialAnimation.text);
            _cardItemInfoManager.ProbabilityChanged += CloseClickAnimation;
            
            void CloseClickAnimation(object sender, EventArgs args)
            {
                _unmaskServiceAreaView.ClearUnmaskCardItems();
                _cardItemInfoManager.ProbabilityChanged -= CloseClickAnimation;
                _handTutorialView.StopActiveAnimation();
                ExecuteNextTutorialActionWithDelay(0.3f);
            }
        }
        
        private void StartHolderIndicatorButtonClickAnimation(TutorialAnimation tutorialAnimation)
        {
            DisableUI(tutorialAnimation);
            _unmaskServiceAreaView.CreateUnmaskCardItem(tutorialAnimation.posList[0], tutorialAnimation.sizeList[0]);
            _handTutorialView.StartClickAnimation(tutorialAnimation.posList[0]);
            _tutorialMessagePopupView.SetText(tutorialAnimation.text);
            _cardItemInfoManager.HolderIndicatorListChanged += CloseClickAnimation;
            
            void CloseClickAnimation(object sender, EventArgs args)
            {
                _unmaskServiceAreaView.ClearUnmaskCardItems();
                _cardItemInfoManager.HolderIndicatorListChanged -= CloseClickAnimation;
                _handTutorialView.StopActiveAnimation();
                ExecuteNextTutorialActionWithDelay(0.3f);
            }
        }
        
        private void StartBoardClickAnimation(TutorialAnimation tutorialAnimation)
        {
            DisableUI(tutorialAnimation);
            _unmaskServiceAreaView.CreateUnmaskCardItem(tutorialAnimation.posList[0], tutorialAnimation.sizeList[0]);
            _handTutorialView.StartClickAnimation(tutorialAnimation.posList[0]);
            _tutorialMessagePopupView.SetText(tutorialAnimation.text);
            _initialCardAreaController.OpenCardItemInfoPopup += CloseClickAnimation;
            
            void CloseClickAnimation(object sender, (bool,int) args)
            {
                _unmaskServiceAreaView.ClearUnmaskCardItems();
                _initialCardAreaController.OpenCardItemInfoPopup -= CloseClickAnimation;
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
                _unmaskServiceAreaView.ClearUnmaskCardItems();
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
                _unmaskServiceAreaView.ClearUnmaskCardItems();
                _gameUIController.ResetNumbers -= CloseClickButtonAnimation;
                _handTutorialView.StopActiveAnimation();
                ExecuteNextTutorialActionWithDelay(0.3f);
            }
        }
        
        private void StartCardInfoButtonClickAnimation(TutorialAnimation tutorialAnimation)
        {
            DisableUI(tutorialAnimation);
            _unmaskServiceAreaView.CreateUnmaskCardItem(tutorialAnimation.posList[0], tutorialAnimation.sizeList[0]);
            _handTutorialView.StartClickAnimation(tutorialAnimation.posList[0]);
            _tutorialMessagePopupView.SetText(tutorialAnimation.text);
            _gameUIController.CardInfoToggleChanged += CloseClickButtonAnimation;
            
            void CloseClickButtonAnimation(object sender, bool args)
            {
                _unmaskServiceAreaView.ClearUnmaskCardItems();
                _gameUIController.CardInfoToggleChanged -= CloseClickButtonAnimation;
                _handTutorialView.StopActiveAnimation();
                ExecuteNextTutorialActionWithDelay(0.3f);
            }
        }

        private void DisableUI(TutorialAnimation tutorialAnimation)
        {
            _cardItemLocator.SetDisallowedCardHolderIndexes(tutorialAnimation.allowedBoardHolderIndex);
            _initialCardAreaController.SetCardsAsUndraggable(tutorialAnimation.draggableCardIndex);
            _initialCardAreaController.SetCardsAsUnselectable(tutorialAnimation.selectableCardIndex);
        }

        private void ExecuteNextTutorialActionWithDelay(float duration)
        {
            DOTween.Sequence().AppendInterval(duration).OnComplete(ExecuteNextTutorialAction);
        }

    }

    public interface ICardInfoTutorialController
    {
        void Initialize(IInitialCardAreaController initialCardAreaController, ICardItemLocator cardItemLocator,
            IHandTutorialView handTutorialView, IUnmaskServiceAreaView unmaskServiceAreaView,
            ITutorialMessagePopupView tutorialMessagePopupView, ICardHolderModelCreator cardHolderModelCreator, IGameUIController gameUIController, IResultAreaController resultAreaController, ICardItemInfoPopupController cardItemInfoPopupController, ICardItemInfoManager cardItemInfoManager);
    }
    
}