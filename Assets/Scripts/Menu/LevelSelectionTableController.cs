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
        private int _lastSelectedLevelId = -1;
        private int _firstLevelIdOfTable = 0;
        private IActiveLevelIdController _activeLevelIdController;
        private ILevelTracker _levelTracker;
        private readonly int _maxNumOfPageNumber = 20;
        private IDirectionButtonView _backwardButtonView;
        private IDirectionButtonView _forwardButtonView;
        public void Initialize(ILevelSelectionTableView view, IActiveLevelIdController activeLevelIdController, ILevelTracker levelTracker)
        {
            _view = view;
            _activeLevelIdController = activeLevelIdController;
            _levelTracker = levelTracker;
            _localPosListForButtons = new Vector2[rowCount * columnCount];
            SetLocalPosListForButtons();
            _view.Init(new LevelButtonViewFactory(), new DirectionButtonViewFactory());
            _levelButtonList = new List<ILevelButtonView>();
            SetFirstLevelIdOfTable();
            CreateLevelButtons();
            CreateDirectionButtons();
        }

        private void SetFirstLevelIdOfTable()
        {
            int activeLevelId = _activeLevelIdController.GetActiveLevelId();
            _firstLevelIdOfTable = activeLevelId - activeLevelId % (rowCount * columnCount);
        }

        private void CreateLevelButtons()
        {
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
                if (_firstLevelIdOfTable + i == _activeLevelIdController.GetActiveLevelId())
                {
                    levelButtonView.Select(true);
                    _lastSelectedLevelId = _activeLevelIdController.GetActiveLevelId();
                }
                _levelButtonList.Add(levelButtonView);
            }
        }

        private void CreateDirectionButtons()
        {
            _backwardButtonView = _view.CreateDirectionButton();
            _backwardButtonView.Init(new DirectionButtonModel()
            {
                localPosition = new Vector2(-162f, 0),
                onClick = OnClickDirectionButton,
                direction = Direction.Backward
            });

            _forwardButtonView = _view.CreateDirectionButton();
            _forwardButtonView.Init(new DirectionButtonModel()
            {
                localPosition = new Vector2(162f, 0),
                onClick = OnClickDirectionButton,
                direction = Direction.Forward
            });
            SetDirectionButtonsStatus();
        }

        private void SetDirectionButtonsStatus()
        {
            _backwardButtonView.SetButtonStatus(_firstLevelIdOfTable > 0);
            _forwardButtonView.SetButtonStatus(_firstLevelIdOfTable < (_maxNumOfPageNumber - 1) * rowCount * columnCount);
        }

        private void OnClickDirectionButton(Direction direction)
        {
            foreach (ILevelButtonView levelButtonView in _levelButtonList)
            {
                levelButtonView.Destroy();
            }
            _levelButtonList = new List<ILevelButtonView>();
            if (direction == Direction.Forward) _firstLevelIdOfTable += rowCount * columnCount;
            else if (direction == Direction.Backward) _firstLevelIdOfTable -= rowCount * columnCount;
            CreateLevelButtons();
            SetDirectionButtonsStatus();
        }

        private void OnClickLevel(int levelId)
        {
            if (_lastSelectedLevelId >= _firstLevelIdOfTable && _lastSelectedLevelId < _firstLevelIdOfTable + rowCount * columnCount)
            {
                _levelButtonList[_lastSelectedLevelId - _firstLevelIdOfTable].Select(false);
            }
            _levelButtonList[levelId - _firstLevelIdOfTable].Select(true);
            _lastSelectedLevelId = levelId;
            _activeLevelIdController.UpdateActiveLevelId(levelId);
        }

        private void SetLocalPosListForButtons()
        {
            int index = 0;
            for (int j = 0; j < rowCount; j++)
            {
                for (int i = 0; i < columnCount; i++)
                {
                    Vector2 localPos = new Vector2((i - (float)(columnCount - 1) / 2) * 90, (j - (float) (rowCount - 1)/2) * -70);
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