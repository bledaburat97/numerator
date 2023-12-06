using System.Collections.Generic;
using Menu;
using UnityEngine;

namespace Scripts
{
    public class LevelSelectionTableController : ILevelSelectionTableController
    {
        private ILevelSelectionTableView _view;
        private int rowCount = 5;
        private int columnCount = 3;
        private Vector2[] _localPosListForButtons;
        private List<ILevelButtonView> _levelButtonList;
        private int _lastSelectedButtonIndex;
        private int _firstLevelIdOfTable = 0;
        private IActiveLevelIdController _activeLevelIdController;
        private ILevelTracker _levelTracker;
        public void Initialize(ILevelSelectionTableView view, IActiveLevelIdController activeLevelIdController, ILevelTracker levelTracker)
        {
            _view = view;
            _activeLevelIdController = activeLevelIdController;
            _levelTracker = levelTracker;
            _localPosListForButtons = new Vector2[rowCount * columnCount];
            SetLocalPosListForButtons();
            _view.Init(new LevelButtonViewFactory());
            _levelButtonList = new List<ILevelButtonView>();
            CreateLevelButtons();
        }

        private void CreateLevelButtons()
        {
            int activeLevelId = _activeLevelIdController.GetActiveLevelId();
            
            _firstLevelIdOfTable = activeLevelId - activeLevelId % (rowCount * columnCount);
            for (int i = 0; i < _localPosListForButtons.Length; i++)
            {
                ILevelButtonView levelButtonView = _view.CreateLevelButtonView();
                int levelId = _firstLevelIdOfTable + i;
                levelButtonView.Init(new LevelButtonModel()
                {
                    localPosition = _localPosListForButtons[i],
                    levelId = levelId,
                    onSelect = OnClickLevel,
                    starCount = levelId < _levelTracker.GetStarCountOfLevels().Count ? _levelTracker.GetStarCountOfLevels()[levelId] : 0
                });
                if (_firstLevelIdOfTable + i <= _levelTracker.GetStarCountOfLevels().Count) levelButtonView.SetButtonActive();
                if (_firstLevelIdOfTable + i == activeLevelId)
                {
                    levelButtonView.Select(true);
                    _lastSelectedButtonIndex = activeLevelId;
                }
                _levelButtonList.Add(levelButtonView);
            }
        }

        private void OnClickForward()
        {
            
        }

        private void OnClickBackWard()
        {
            
        }

        private void OnClickLevel(int levelId)
        {
            _levelButtonList[_lastSelectedButtonIndex].Select(false);
            _levelButtonList[levelId - _firstLevelIdOfTable].Select(true);
            _lastSelectedButtonIndex = levelId - _firstLevelIdOfTable;
            _activeLevelIdController.UpdateActiveLevelId(levelId);
        }

        private void SetLocalPosListForButtons()
        {
            int index = 0;
            for (int j = 0; j < rowCount; j++)
            {
                for (int i = 0; i < columnCount; i++)
                {
                    Vector2 localPos = new Vector2((i - (float)(columnCount - 1) / 2) * 100, (j - (float) (rowCount - 1)/2) * -70);
                    _localPosListForButtons[index] = localPos;
                    index++;
                }
            }
        }
        
    }

    public interface ILevelSelectionTableController
    {
        void Initialize(ILevelSelectionTableView view, IActiveLevelIdController activeLevelIdController, ILevelTracker levelTracker);
    }
}