﻿using System;
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
            _tutorialMessagePopupView.Init();
            InitializeTutorialAnimationActions();
        }

        private void InitializeTutorialAnimationActions()
        {
            _tutorialAnimationActions = new Queue<Action>();
            Vector2 sizeOfInitialHolder = _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Initial)[0].size;
            Vector2 sizeOfBoardHolder = _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Board)[0].size;
            
            AddTutorialAction(() => StartDragAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_initialCardAreaController.GetNormalCardHolderPositionAtIndex(0), _cardItemLocator.GetBoardCardHolderPositionAtIndex(0)},
                sizeList = new List<Vector2>(){sizeOfInitialHolder, sizeOfBoardHolder},
                allowedBoardHolderIndex = 0,
                draggableCardIndex = 0,
                text = "You can drag the number to the board."
            }));
            AddTutorialAction(() => StartClickAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_initialCardAreaController.GetNormalCardHolderPositionAtIndex(1)},
                sizeList = new List<Vector2>(){sizeOfInitialHolder},
                selectableCardIndex = 1,
                text = "You can click the number."
            }));
            AddTutorialAction(() => StartCheckButtonClickAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_gameUIController.GetCheckButtonRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_gameUIController.GetCheckButtonRectTransform().rect.width, _gameUIController.GetCheckButtonRectTransform().rect.height)},
                isCheckButtonActive = true,
                text = "You can click the check button."
            }));
            ResultAreaInfo resultAreaInfo = _resultAreaController.GetResultAreaInfo();
            AddTutorialAction(() => ShowResultBlock(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){resultAreaInfo.topPoint},
                sizeList = new List<Vector2>(){resultAreaInfo.resultBlockSize},
                changeInLocalPosY = - resultAreaInfo.resultBlockSize.y / 2,
                text = "Any number does not exist on the board. Now you can specify these numbers."
            }));
            AddTutorialAction(()=> WaitForATime(5));
            AddTutorialAction(() => StartCardInfoButtonClickAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_gameUIController.GetCardInfoButtonRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_gameUIController.GetCardInfoButtonRectTransform().rect.width, _gameUIController.GetCardInfoButtonRectTransform().rect.height)},
                isCardInfoButtonActive = true,
                text = "You can click the number info button. It will help you to play."
            }));
            AddTutorialAction(() => StartClickAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemLocator.GetBoardCardHolderPositionAtIndex(0)},
                sizeList = new List<Vector2>(){sizeOfBoardHolder},
                selectableCardIndex = 0,
                text = "You can click the number."
            }));
            AddTutorialAction(() => StartProbabilityButtonClickAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().rect.width, _cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().rect.height)},
                pressableProbabilityButtonIndex = (int) ProbabilityType.NotExisted,
                text = "When the number does not exist on the board, turn the color of number to red."
            }));
            AddTutorialAction(() => StartClickAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemLocator.GetBoardCardHolderPositionAtIndex(1)},
                sizeList = new List<Vector2>(){sizeOfBoardHolder},
                selectableCardIndex = 1,
                text = "You can click the number."
            }));
            AddTutorialAction(() => StartProbabilityButtonClickAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().rect.width, _cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().rect.height)},
                pressableProbabilityButtonIndex = (int) ProbabilityType.NotExisted,
                text = "When the number does not exist on the board, turn the color of number to red."
            }));
            AddTutorialAction(() => StartResetButtonClickAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_gameUIController.GetResetButtonRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_gameUIController.GetResetButtonRectTransform().rect.width, _gameUIController.GetResetButtonRectTransform().rect.height)},
                isResetButtonActive = true,
                text = "You can click the reset button."
            }));
            AddTutorialAction(() => StartDragAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_initialCardAreaController.GetNormalCardHolderPositionAtIndex(2), _cardItemLocator.GetBoardCardHolderPositionAtIndex(0)},
                sizeList = new List<Vector2>(){sizeOfInitialHolder, sizeOfBoardHolder},
                allowedBoardHolderIndex = 0,
                draggableCardIndex = 2,
                text = "You can drag the number to the board."
            }));
            AddTutorialAction(() => StartDragAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_initialCardAreaController.GetNormalCardHolderPositionAtIndex(3), _cardItemLocator.GetBoardCardHolderPositionAtIndex(1)},
                sizeList = new List<Vector2>(){sizeOfInitialHolder, sizeOfBoardHolder},
                allowedBoardHolderIndex = 1,
                draggableCardIndex = 3,
                text = "You can drag the number to the board."
            }));
            AddTutorialAction(() => StartCheckButtonClickAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_gameUIController.GetCheckButtonRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_gameUIController.GetCheckButtonRectTransform().rect.width, _gameUIController.GetCheckButtonRectTransform().rect.height)},
                isCheckButtonActive = true,
                text = "You can click the check button."
            }));
            AddTutorialAction(() => ShowResultBlock(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){resultAreaInfo.topPoint},
                sizeList = new List<Vector2>(){resultAreaInfo.resultBlockSize},
                changeInLocalPosY = - resultAreaInfo.resultBlockSize.y - resultAreaInfo.spacing - resultAreaInfo.resultBlockSize.y / 2,
                text = "Any number is not at correct position. One number is at wrong position."
            }));
            AddTutorialAction(()=> WaitForATime(5));
            AddTutorialAction(() => StartResetButtonClickAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_gameUIController.GetResetButtonRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_gameUIController.GetResetButtonRectTransform().rect.width, _gameUIController.GetResetButtonRectTransform().rect.height)},
                isResetButtonActive = true,
                text = "You can click the reset button."
            }));
            AddTutorialAction(() => StartClickAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_initialCardAreaController.GetNormalCardHolderPositionAtIndex(2)},
                sizeList = new List<Vector2>(){sizeOfInitialHolder},
                selectableCardIndex = 2,
                text = "You can click the number."
            }));
            AddTutorialAction(() => StartHolderIndicatorButtonClickAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemInfoPopupController.GetHolderIndicatorButton(0).GetView().GetRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_cardItemInfoPopupController.GetHolderIndicatorButton(0).GetView().GetRectTransform().rect.width, _cardItemInfoPopupController.GetHolderIndicatorButton(0).GetView().GetRectTransform().rect.height)},
                pressableHolderButtonIndex = 0,
                text = "When the number does not exist on a specific place, put a cross for that place."
            }));
            AddTutorialAction(() => StartClickAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_initialCardAreaController.GetNormalCardHolderPositionAtIndex(3)},
                sizeList = new List<Vector2>(){sizeOfInitialHolder},
                selectableCardIndex = 3,
                text = "You can click the number."
            }));
            AddTutorialAction(() => StartHolderIndicatorButtonClickAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemInfoPopupController.GetHolderIndicatorButton(1).GetView().GetRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_cardItemInfoPopupController.GetHolderIndicatorButton(1).GetView().GetRectTransform().rect.width, _cardItemInfoPopupController.GetHolderIndicatorButton(1).GetView().GetRectTransform().rect.height)},
                pressableHolderButtonIndex = 1,
                text = "When the number does not exist on a specific place, put a cross for that place."
            }));
            AddTutorialAction(() => StartBoardClickAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemLocator.GetBoardCardHolderPositionAtIndex(0)},
                sizeList = new List<Vector2>(){sizeOfBoardHolder},
                nonForbiddenBoardIndex = 0,
                text = "You can click the board."
            }));
            AddTutorialAction(() => StartDragAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_initialCardAreaController.GetNormalCardHolderPositionAtIndex(4), _cardItemLocator.GetBoardCardHolderPositionAtIndex(1)},
                sizeList = new List<Vector2>(){sizeOfInitialHolder, sizeOfBoardHolder},
                allowedBoardHolderIndex = 1,
                draggableCardIndex = 4,
                text = "You can drag the number to the board."
            }));
            AddTutorialAction(() => StartCheckButtonClickAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_gameUIController.GetCheckButtonRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_gameUIController.GetCheckButtonRectTransform().rect.width, _gameUIController.GetCheckButtonRectTransform().rect.height)},
                isCheckButtonActive = true,
                text = "You can click the check button."
            }));
            AddTutorialAction(() => ShowResultBlock(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){resultAreaInfo.topPoint},
                sizeList = new List<Vector2>(){resultAreaInfo.resultBlockSize},
                changeInLocalPosY = -2 * (resultAreaInfo.resultBlockSize.y + resultAreaInfo.spacing) - resultAreaInfo.resultBlockSize.y / 2,
                text = "Only one number is at correct position, the other one doesn't exist."
            }));
            AddTutorialAction(()=> WaitForATime(5));
            AddTutorialAction(() => StartClickAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemLocator.GetBoardCardHolderPositionAtIndex(0)},
                sizeList = new List<Vector2>(){sizeOfBoardHolder},
                selectableCardIndex = 3,
                text = "You can click the number."
            }));
            AddTutorialAction(() => StartProbabilityButtonClickAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.Certain).GetView().GetRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.Certain).GetView().GetRectTransform().rect.width, _cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.Certain).GetView().GetRectTransform().rect.height)},
                pressableProbabilityButtonIndex = (int) ProbabilityType.Certain,
                text = "When the number certainly exists somewhere on the board, turn the color to green."
            }));
            AddTutorialAction(() => StartClickAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemLocator.GetBoardCardHolderPositionAtIndex(1)},
                sizeList = new List<Vector2>(){sizeOfBoardHolder},
                selectableCardIndex = 4,
                text = "You can click the number."
            }));
            AddTutorialAction(() => StartProbabilityButtonClickAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().rect.width, _cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().rect.height)},
                pressableProbabilityButtonIndex = (int) ProbabilityType.NotExisted,
                text = "When the number does not exist on the board, turn the color of number to red."            }));
            AddTutorialAction(() => StartClickAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_initialCardAreaController.GetNormalCardHolderPositionAtIndex(2)},
                sizeList = new List<Vector2>(){sizeOfInitialHolder},
                selectableCardIndex = 2,
                text = "You can click the number."
            }));
            AddTutorialAction(() => StartProbabilityButtonClickAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().rect.width, _cardItemInfoPopupController.GetProbabilityButton(ProbabilityType.NotExisted).GetView().GetRectTransform().rect.height)},
                pressableProbabilityButtonIndex = (int) ProbabilityType.NotExisted,
                text = "When the number does not exist on the board, turn the color of number to red."
            }));
            AddTutorialAction(() => StartDragAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_cardItemLocator.GetBoardCardHolderPositionAtIndex(1), _initialCardAreaController.GetNormalCardHolderPositionAtIndex(4)},
                sizeList = new List<Vector2>(){sizeOfBoardHolder, sizeOfInitialHolder},
                allowedBoardHolderIndex = 1,
                draggableCardIndex = 4,
                text = "You can drag the number to the initial area back."
            }, true));
            AddTutorialAction(() => StartDragAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_initialCardAreaController.GetNormalCardHolderPositionAtIndex(5), _cardItemLocator.GetBoardCardHolderPositionAtIndex(1)},
                sizeList = new List<Vector2>(){sizeOfInitialHolder, sizeOfBoardHolder},
                allowedBoardHolderIndex = 1,
                draggableCardIndex = 5,
                text = "You can drag the number to the board."
            }));
            AddTutorialAction(() => StartCheckButtonClickAnimation(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){_gameUIController.GetCheckButtonRectTransform().position},
                sizeList = new List<Vector2>(){new Vector2(_gameUIController.GetCheckButtonRectTransform().rect.width, _gameUIController.GetCheckButtonRectTransform().rect.height)},
                isCheckButtonActive = true,
                text = "You can click the check button."
            }));
            AddTutorialAction(() => ShowResultBlock(new CardInfoTutorialAnimation()
            {
                posList = new List<Vector2>(){resultAreaInfo.topPoint},
                sizeList = new List<Vector2>(){resultAreaInfo.resultBlockSize}, 
                changeInLocalPosY = - 3 * (resultAreaInfo.resultBlockSize.y + resultAreaInfo.spacing) - resultAreaInfo.resultBlockSize.y / 2,
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
                PlayerPrefs.SetInt("card_info_tutorial_completed", 1);
            }
        }

        private void StartDragAnimation(CardInfoTutorialAnimation tutorialAnimation, bool isReversed = false)
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

        private void StartClickAnimation(CardInfoTutorialAnimation tutorialAnimation)
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
        
        private void StartProbabilityButtonClickAnimation(CardInfoTutorialAnimation tutorialAnimation)
        {
            tutorialAnimation.selectedCardIndexChangeable = false;
            DisableUI(tutorialAnimation);
            _unmaskServiceAreaView.CreateUnmaskCardItem(tutorialAnimation.posList[0], tutorialAnimation.sizeList[0], 0f, 1f);
            _handTutorialView.StartClickAnimation(tutorialAnimation.posList[0]);
            _tutorialMessagePopupView.SetText(tutorialAnimation.text);
            _cardItemInfoManager.ProbabilityChanged += CloseClickAnimation;
            
            void CloseClickAnimation(object sender, EventArgs args)
            {
                _unmaskServiceAreaView.ClearAllUnmaskCardItems();
                _cardItemInfoManager.ProbabilityChanged -= CloseClickAnimation;
                _handTutorialView.StopActiveAnimation();
                ExecuteNextTutorialActionWithDelay(0.3f);
            }
        }
        
        private void StartHolderIndicatorButtonClickAnimation(CardInfoTutorialAnimation tutorialAnimation)
        {
            tutorialAnimation.selectedCardIndexChangeable = false;
            DisableUI(tutorialAnimation);
            _unmaskServiceAreaView.CreateUnmaskCardItem(tutorialAnimation.posList[0], tutorialAnimation.sizeList[0]);
            _handTutorialView.StartClickAnimation(tutorialAnimation.posList[0]);
            _tutorialMessagePopupView.SetText(tutorialAnimation.text);
            _cardItemInfoManager.HolderIndicatorListChanged += CloseClickAnimation;
            
            void CloseClickAnimation(object sender, EventArgs args)
            {
                _unmaskServiceAreaView.ClearAllUnmaskCardItems();
                _cardItemInfoManager.HolderIndicatorListChanged -= CloseClickAnimation;
                _handTutorialView.StopActiveAnimation();
                ExecuteNextTutorialActionWithDelay(0.3f);
            }
        }
        
        private void StartBoardClickAnimation(CardInfoTutorialAnimation tutorialAnimation)
        {
            DisableUI(tutorialAnimation);
            _unmaskServiceAreaView.CreateUnmaskCardItem(tutorialAnimation.posList[0], tutorialAnimation.sizeList[0]);
            _handTutorialView.StartClickAnimation(tutorialAnimation.posList[0]);
            _tutorialMessagePopupView.SetText(tutorialAnimation.text);
            _initialCardAreaController.OpenCardItemInfoPopup += CloseClickAnimation;
            
            void CloseClickAnimation(object sender, (bool,int) args)
            {
                _unmaskServiceAreaView.ClearAllUnmaskCardItems();
                _initialCardAreaController.OpenCardItemInfoPopup -= CloseClickAnimation;
                _handTutorialView.StopActiveAnimation();
                ExecuteNextTutorialActionWithDelay(0.3f);
            }
        }

        private void ShowResultBlock(CardInfoTutorialAnimation tutorialAnimation)
        {
            DisableUI(tutorialAnimation);
            _unmaskServiceAreaView.CreateUnmaskCardItem(tutorialAnimation.posList[0], tutorialAnimation.sizeList[0], tutorialAnimation.changeInLocalPosY);
            _tutorialMessagePopupView.SetText(tutorialAnimation.text);
            ExecuteNextTutorialActionWithDelay(0.3f);
        }
        
        private void StartCheckButtonClickAnimation(CardInfoTutorialAnimation tutorialAnimation)
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
        
        private void StartResetButtonClickAnimation(CardInfoTutorialAnimation tutorialAnimation)
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
        
        private void StartCardInfoButtonClickAnimation(CardInfoTutorialAnimation tutorialAnimation)
        {
            DisableUI(tutorialAnimation);
            _unmaskServiceAreaView.CreateUnmaskCardItem(tutorialAnimation.posList[0], tutorialAnimation.sizeList[0]);
            _handTutorialView.StartClickAnimation(tutorialAnimation.posList[0]);
            _tutorialMessagePopupView.SetText(tutorialAnimation.text);
            _gameUIController.CardInfoToggleChanged += CloseClickButtonAnimation;
            
            void CloseClickButtonAnimation(object sender, bool args)
            {
                _unmaskServiceAreaView.ClearAllUnmaskCardItems();
                _gameUIController.CardInfoToggleChanged -= CloseClickButtonAnimation;
                _handTutorialView.StopActiveAnimation();
                ExecuteNextTutorialActionWithDelay(0.3f);
            }
        }

        private void WaitForATime(float duration)
        {
            DOTween.Sequence().AppendInterval(duration).OnComplete(() =>
            {
                _unmaskServiceAreaView.ClearAllUnmaskCardItems();
                ExecuteNextTutorialAction();
            });
        }

        private void DisableUI(CardInfoTutorialAnimation tutorialAnimation)
        {
            List<int> forbiddenBoardIndexes = new List<int>() { 0, 1 };
            if (forbiddenBoardIndexes.Contains(tutorialAnimation.nonForbiddenBoardIndex))
                forbiddenBoardIndexes.Remove(tutorialAnimation.nonForbiddenBoardIndex);
            _cardItemLocator.SetDisallowedCardHolderIndexes(tutorialAnimation.allowedBoardHolderIndex);
            _initialCardAreaController.SetCardsAsUndraggable(tutorialAnimation.draggableCardIndex);
            _initialCardAreaController.SetCardsAsUnselectable(tutorialAnimation.selectableCardIndex);
            _initialCardAreaController.SetSelectedCardIndexChangeable(tutorialAnimation.selectedCardIndexChangeable);
            _initialCardAreaController.SetBoardIndexesAsForbidden(forbiddenBoardIndexes);
            _cardItemInfoPopupController.SetPressableHolderIndicatorButtonIndex(tutorialAnimation.pressableHolderButtonIndex);
            _cardItemInfoPopupController.SetPressableProbabilityButtonIndex(tutorialAnimation.pressableProbabilityButtonIndex);
            _gameUIController.SetCheckButtonClickable(tutorialAnimation.isCheckButtonActive);
            _gameUIController.SetResetButtonClickable(tutorialAnimation.isResetButtonActive);
            _gameUIController.SetCardInfoButtonClickable(tutorialAnimation.isCardInfoButtonActive);
            _gameUIController.SetSettingsButtonClickable(false);
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

    public class CardInfoTutorialAnimation : TutorialAnimation
    {
        public int pressableHolderButtonIndex = -1;
        public int pressableProbabilityButtonIndex = -1;
        public bool selectedCardIndexChangeable = true;
        public int nonForbiddenBoardIndex = -1;
    }
    
}