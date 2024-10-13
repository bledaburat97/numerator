using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class CardInfoTutorialController : TutorialController
    {
        protected override void InitializeTutorialAnimationActions()
        {
            AddTutorialAction(() => StartDragAnimation(0, 0));
            AddTutorialAction(() => StartCardClickAnimation(1, _initialCardAreaController.GetNormalCardHolderPositionAtIndex(1), _sizeOfInitialHolder));
            AddTutorialAction(StartCheckButtonClickAnimation);
            AddTutorialAction(() => ShowResultBlock(0, "Any number does not exist on the board. Now you can specify these numbers."));
            AddTutorialAction(()=> WaitForATime(5));
            AddTutorialAction(StartCardInfoButtonClickAnimation);
            AddTutorialAction(() => StartCardClickAnimation(0, _boardAreaController.GetBoardHolderPositionAtIndex(0), _sizeOfBoardHolder));
            AddTutorialAction(() => StartProbabilityButtonClickAnimation(ProbabilityType.NotExisted, "When the number does not exist on the board, turn the color of number to red."));
            AddTutorialAction(() => StartCardClickAnimation(1, _boardAreaController.GetBoardHolderPositionAtIndex(1), _sizeOfBoardHolder));
            AddTutorialAction(() => StartProbabilityButtonClickAnimation(ProbabilityType.NotExisted, "When the number does not exist on the board, turn the color of number to red."));
            AddTutorialAction(StartResetButtonClickAnimation);
            AddTutorialAction(() => StartDragAnimation(2,0));
            AddTutorialAction(() => StartDragAnimation(3,1));
            AddTutorialAction(StartCheckButtonClickAnimation);
            AddTutorialAction(() => ShowResultBlock(1, "Any number is not at correct position. One number is at wrong position."));
            AddTutorialAction(()=> WaitForATime(5));
            AddTutorialAction(StartResetButtonClickAnimation);
            AddTutorialAction(() => StartCardClickAnimation(2, _initialCardAreaController.GetNormalCardHolderPositionAtIndex(2), _sizeOfInitialHolder));
            AddTutorialAction(() => StartHolderIndicatorButtonClickAnimation(0));
            AddTutorialAction(() => StartCardClickAnimation(3, _initialCardAreaController.GetNormalCardHolderPositionAtIndex(3), _sizeOfInitialHolder));
            AddTutorialAction(() => StartHolderIndicatorButtonClickAnimation(1));
            AddTutorialAction(() => StartBoardClickAnimation(0));
            AddTutorialAction(() => StartDragAnimation(4, 1));
            AddTutorialAction(StartCheckButtonClickAnimation);
            AddTutorialAction(() => ShowResultBlock(2, "Only one number is at correct position, the other one doesn't exist."));
            AddTutorialAction(()=> WaitForATime(5));
            AddTutorialAction(() => StartCardClickAnimation(3, _boardAreaController.GetBoardHolderPositionAtIndex(0), _sizeOfBoardHolder));
            AddTutorialAction(() => StartProbabilityButtonClickAnimation(ProbabilityType.Certain, "When the number certainly exists somewhere on the board, turn the color to green."));
            AddTutorialAction(() => StartCardClickAnimation(4, _boardAreaController.GetBoardHolderPositionAtIndex(1), _sizeOfBoardHolder));
            AddTutorialAction(() => StartProbabilityButtonClickAnimation(ProbabilityType.NotExisted, "When the number does not exist on the board, turn the color of number to red."));
            AddTutorialAction(() => StartCardClickAnimation(2, _initialCardAreaController.GetNormalCardHolderPositionAtIndex(2), _sizeOfInitialHolder));
            AddTutorialAction(() => StartProbabilityButtonClickAnimation(ProbabilityType.NotExisted, "When the number does not exist on the board, turn the color of number to red."));
            AddTutorialAction(() => StartDragAnimation(4, 1, true));
            AddTutorialAction(() => StartDragAnimation(5, 1));
            AddTutorialAction(StartCheckButtonClickAnimation);
            AddTutorialAction(() => ShowResultBlock(3, "Both of the numbers are at correct position. Congratulations!!!"));
            ExecuteNextTutorialAction();
        }
        
        private void StartProbabilityButtonClickAnimation(ProbabilityType probabilityType, string text)
        {
            RectTransform probabilityButtonRectTransform = _cardItemInfoPopupController.GetProbabilityButton(probabilityType).GetView().GetRectTransform();
            Vector2 position = probabilityButtonRectTransform.position;
            Vector2 size = new Vector2(probabilityButtonRectTransform.rect.width,
                probabilityButtonRectTransform.rect.height);
            _unmaskServiceAreaView.CreateUnmaskCardItem(position, size, 0f, 1f);
            _handTutorialView.StartClickAnimation(position);
            _tutorialMessagePopupView.SetText(text);
            _cardItemInfoPopupController.ProbabilityButtonClickedEvent += CloseClickAnimation;
            
            void CloseClickAnimation(object sender, EventArgs args)
            {
                _unmaskServiceAreaView.ClearAllUnmaskCardItems();
                _cardItemInfoPopupController.ProbabilityButtonClickedEvent -= CloseClickAnimation;
                _handTutorialView.StopActiveAnimation();
                ExecuteNextTutorialActionWithDelay(0.3f);
            }
        }
        
        private void StartHolderIndicatorButtonClickAnimation(int holderIndicatorIndex)
        {
            RectTransform holderIndicatorButtonRectTransform = _cardItemInfoPopupController
                .GetHolderIndicatorButton(holderIndicatorIndex).GetView().GetRectTransform();

            Vector2 position = holderIndicatorButtonRectTransform.position;
            Vector2 size = new Vector2(holderIndicatorButtonRectTransform.rect.width,
                holderIndicatorButtonRectTransform.rect.height);
            _unmaskServiceAreaView.CreateUnmaskCardItem(position, size);
            _handTutorialView.StartClickAnimation(size);
            _tutorialMessagePopupView.SetText("When the number does not exist on a specific place, put a cross for that place.");
            _cardItemInfoPopupController.HolderIndicatorClickedEvent += CloseClickAnimation;
            
            void CloseClickAnimation(object sender, EventArgs args)
            {
                _unmaskServiceAreaView.ClearAllUnmaskCardItems();
                _cardItemInfoPopupController.HolderIndicatorClickedEvent -= CloseClickAnimation;
                _handTutorialView.StopActiveAnimation();
                ExecuteNextTutorialActionWithDelay(0.3f);
            }
        }
        
        private void StartBoardClickAnimation(int boardHolderIndex)
        {
            Vector2 position = _boardAreaController.GetBoardHolderPositionAtIndex(boardHolderIndex);
            _unmaskServiceAreaView.CreateUnmaskCardItem(position, _sizeOfBoardHolder);
            _handTutorialView.StartClickAnimation(position);
            _tutorialMessagePopupView.SetText("You can click the board.");
            _cardInteractionManager.OpenCardItemInfoPopupEvent += CloseClickAnimation;
            
            void CloseClickAnimation(object sender, (bool,int) args)
            {
                _unmaskServiceAreaView.ClearAllUnmaskCardItems();
                _cardInteractionManager.OpenCardItemInfoPopupEvent -= CloseClickAnimation;
                _handTutorialView.StopActiveAnimation();
                ExecuteNextTutorialActionWithDelay(0.3f);
            }
        }
        
        private void StartCardInfoButtonClickAnimation()
        {
            RectTransform cardInfoButtonRectTransform = _gameUIController.GetCardInfoButtonRectTransform();
            Vector2 position = cardInfoButtonRectTransform.position;
            Vector2 size = new Vector2(cardInfoButtonRectTransform.rect.width, cardInfoButtonRectTransform.rect.height);
            _unmaskServiceAreaView.CreateUnmaskCardItem(position, size);
            _handTutorialView.StartClickAnimation(position);
            _tutorialMessagePopupView.SetText("You can click the number info button. It will help you to play.");
            _gameUIController.CardInfoToggleChanged += CloseClickButtonAnimation;
            _gameUIController.SetAllButtonsUnclickable();
            _gameUIController.SetButtonClickable(true, GameUIButtonType.CardInfo);
            void CloseClickButtonAnimation(object sender, bool args)
            {
                _unmaskServiceAreaView.ClearAllUnmaskCardItems();
                _gameUIController.CardInfoToggleChanged -= CloseClickButtonAnimation;
                _handTutorialView.StopActiveAnimation();
                ExecuteNextTutorialActionWithDelay(0.3f);
            }
        }
    }
}