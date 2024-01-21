using System.Collections.Generic;
using DG.Tweening;
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
        
        public const float SPACESHIP_INITIAL_LOCAL_VERTICAL_POS = -10f;

        public LevelSelectionTableController(ILevelSelectionTableView view)
        {
            _view = view;
        }
        
        public void Initialize(IActiveLevelIdController activeLevelIdController, ILevelTracker levelTracker)
        {
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
                    levelButtonView.CreateSpaceShip(new Vector2(0,SPACESHIP_INITIAL_LOCAL_VERTICAL_POS));
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
                RectTransform nextSpaceShipHolder =
                    _levelButtonList[levelId - _firstLevelIdOfTable].GetRectTransformOfSpaceShipHolder();

                ISpaceShipView spaceShip = _levelButtonList[_lastSelectedLevelId - _firstLevelIdOfTable].GetSpaceShip();
                RectTransform spaceShipRectTransform = spaceShip.GetRectTransform();
                
                spaceShipRectTransform.SetParent(nextSpaceShipHolder);
                _levelButtonList[levelId - _firstLevelIdOfTable].SetSpaceShip(spaceShip);
                
                Vector3 direction = -spaceShipRectTransform.localPosition;
                Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, direction);
                Quaternion reverseRotation = Quaternion.LookRotation(Vector3.forward, Vector3.zero);
                DOTween.Sequence()
                    .Append(spaceShipRectTransform.DOLocalMove(new Vector2(0, 0), 0.5f).SetEase(Ease.Linear))
                    .Join(spaceShipRectTransform.DOLocalRotateQuaternion(targetRotation, 0.5f).SetEase(Ease.Linear))
                    .Append(spaceShipRectTransform.DOLocalMove(new Vector2(0,SPACESHIP_INITIAL_LOCAL_VERTICAL_POS), 0.2f)).SetEase(Ease.Linear)
                    .Join(spaceShipRectTransform.DOLocalRotateQuaternion(reverseRotation, 0.2f).SetEase(Ease.Linear));
            }
            else
            {
                _levelButtonList[levelId - _firstLevelIdOfTable].CreateSpaceShip(new Vector2(0,SPACESHIP_INITIAL_LOCAL_VERTICAL_POS));
            }
            
            _lastSelectedLevelId = levelId;
            _activeLevelIdController.UpdateActiveLevelId(levelId);
        }

        private void SetLocalPosListForButtons()
        {
            _localPosListForButtons = new[]
            {
                new Vector2(-90,0), 
                new Vector2(-110,70),
                new Vector2(-130,140),
                new Vector2(-90,210),
                new Vector2(-10,210),
                new Vector2(40,140),
                new Vector2(20,70),
                new Vector2(0,0),
                new Vector2(-20,-70),
                new Vector2(-40,-140),
                new Vector2(10,-210),
                new Vector2(90,-210),
                new Vector2(130,-140),
                new Vector2(110,-70),
                new Vector2(90,0)
            };
        }
        
    }

    public interface ILevelSelectionTableController
    {
        void Initialize(IActiveLevelIdController activeLevelIdController, ILevelTracker levelTracker);
    }
}