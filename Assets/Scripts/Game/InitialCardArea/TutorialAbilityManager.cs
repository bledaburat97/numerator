using System.Collections.Generic;

namespace Scripts
{
    public class TutorialAbilityManager : ITutorialAbilityManager
    {
        private bool _isTutorialLevel;
        private TutorialAbility _currentTutorialAbility;
        
        public TutorialAbilityManager()
        {
            _currentTutorialAbility = new TutorialAbility();
        }
        public void SetCurrentTutorialAbility(TutorialAbility tutorialAbility)
        {
            _currentTutorialAbility = tutorialAbility;
        }

        public void SetTutorialLevel(bool isTutorialLevel)
        {
            _isTutorialLevel = isTutorialLevel;
        }

        public bool IsTutorialLevel()
        {
            return _isTutorialLevel;
        }

        public bool IsCardSelectable(int cardIndex)
        {
            if (!_isTutorialLevel) return true;
            return _currentTutorialAbility.selectableCardIndex == cardIndex;
        }

        public bool IsCardDraggable(int cardIndex)
        {
            if (!_isTutorialLevel) return true;
            return _currentTutorialAbility.draggableCardIndex == cardIndex;
        }

        public bool IsBoardIndexClickable(int boardIndex)
        {
            if (!_isTutorialLevel) return true;
            return _currentTutorialAbility.clickableBoardIndexes.Contains(boardIndex);
        }

        public bool IsSelectedCardIndexChangeable()
        {
            if (!_isTutorialLevel) return true;
            return _currentTutorialAbility.isSelectedCardIndexChangeable;
        }

        public bool IsBoardIndexDraggable(int boardIndex)
        {
            if (!_isTutorialLevel) return true;
            return _currentTutorialAbility.draggableBoardIndex == boardIndex;
        }
        
        public bool IsHolderIndicatorButtonClickable(int holderIndicatorButtonIndex)
        {
            if (!_isTutorialLevel) return true;
            return _currentTutorialAbility.pressableHolderButtonIndex == holderIndicatorButtonIndex;
        }
        
        public bool IsProbabilityButtonClickable(int probabilityButtonIndex)
        {
            if (!_isTutorialLevel) return true;
            return _currentTutorialAbility.pressableProbabilityButtonIndex == probabilityButtonIndex;
        }
        
        public bool IsCheckButtonClickable()
        {
            if (!_isTutorialLevel) return true;
            return _currentTutorialAbility.isCheckButtonActive;
        }
        
        public bool IsResetButtonClickable()
        {
            if (!_isTutorialLevel) return true;
            return _currentTutorialAbility.isResetButtonActive;
        }
        
        public bool IsCardInfoButtonClickable()
        {
            if (!_isTutorialLevel) return true;
            return _currentTutorialAbility.isCardInfoButtonActive;
        }

        public bool IsSettingsButtonClickable()
        {
            return !_isTutorialLevel;
        }
    }

    public interface ITutorialAbilityManager
    {
        void SetCurrentTutorialAbility(TutorialAbility tutorialAbility);
        void SetTutorialLevel(bool isTutorialLevel);
        bool IsTutorialLevel();
        bool IsCardSelectable(int cardIndex);
        bool IsCardDraggable(int cardIndex);
        bool IsBoardIndexClickable(int boardIndex);
        bool IsSelectedCardIndexChangeable();
        bool IsBoardIndexDraggable(int boardIndex);
        bool IsHolderIndicatorButtonClickable(int holderIndicatorButtonIndex);
        bool IsProbabilityButtonClickable(int probabilityButtonIndex);
        bool IsCheckButtonClickable();
        bool IsResetButtonClickable();
        bool IsCardInfoButtonClickable();
        bool IsSettingsButtonClickable();
    }

    public class TutorialAbility
    {
        public int draggableBoardIndex { get; set; }
        public int draggableCardIndex { get; set; }
        public int selectableCardIndex{ get; set; }
        public bool isCheckButtonActive{ get; set; }
        public bool isResetButtonActive{ get; set; }
        public bool isCardInfoButtonActive{ get; set; }
        public int pressableHolderButtonIndex{ get; set; }
        public int pressableProbabilityButtonIndex{ get; set; }
        public bool isSelectedCardIndexChangeable{ get; set; }
        public List<int> clickableBoardIndexes{ get; set; }

        public TutorialAbility()
        {
            draggableBoardIndex = -1;
            draggableCardIndex = -1;
            selectableCardIndex = -1;
            isCheckButtonActive = false;
            isResetButtonActive = false;
            isCardInfoButtonActive = false;
            pressableHolderButtonIndex = -1;
            pressableProbabilityButtonIndex = -1;
            isSelectedCardIndexChangeable = true;
            clickableBoardIndexes = new List<int>();
        }
        
    }
}