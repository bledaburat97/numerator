using System;
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
        private Canvas _canvas;
        
        public LevelSelectionTableController(ILevelSelectionTableView view, Canvas canvas)
        {
            _view = view;
            _canvas = canvas;
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
                    levelButtonView.CreateSpaceShip();
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
                ILevelButtonView selectedLevelButton = _levelButtonList[levelId - _firstLevelIdOfTable];
                ILevelButtonView lastLevelButton = _levelButtonList[_lastSelectedLevelId - _firstLevelIdOfTable];
                
                RectTransform nextSpaceShipHolder = selectedLevelButton.GetRectTransformOfSpaceShipHolder();
                RectTransform lastSpaceShipHolder = lastLevelButton.GetRectTransformOfSpaceShipHolder();
                ISpaceShipView spaceShip = lastLevelButton.GetSpaceShip();
                RectTransform spaceShipRectTransform = spaceShip.GetRectTransform();

                Vector2 direction = new Vector2((nextSpaceShipHolder.position.x - lastSpaceShipHolder.position.x) / _canvas.scaleFactor,
                    (nextSpaceShipHolder.position.y - lastSpaceShipHolder.position.y) / _canvas.scaleFactor);
                float movementDuration = direction.magnitude * 0.02f;
                
                Quaternion firstRotation = Quaternion.LookRotation(Vector3.forward, direction);
                Quaternion secondRotation = Quaternion.LookRotation(Vector3.forward, Vector3.zero);
                float rotationDuration = Quaternion.Angle(spaceShipRectTransform.localRotation, firstRotation) * 0.003f;
                
                Action setNewParent = () =>
                {
                    spaceShipRectTransform.SetParent(nextSpaceShipHolder);
                    _levelButtonList[levelId - _firstLevelIdOfTable].SetSpaceShip(spaceShip);
                };

                DOTween.Sequence()
                    .OnStart(OnStart)
                    .AppendCallback(_lastSelectedLevelId < levelId ? setNewParent.Invoke : null)
                    .Append(spaceShipRectTransform.DOLocalRotateQuaternion(firstRotation, rotationDuration)
                        .SetEase(Ease.Linear))
                    .Append(spaceShipRectTransform.DOMove(nextSpaceShipHolder.position, movementDuration).SetEase(Ease.Linear))
                    .AppendCallback(_lastSelectedLevelId > levelId ? setNewParent.Invoke : null)
                    .Append(spaceShipRectTransform.DOLocalRotateQuaternion(secondRotation, rotationDuration)
                        .SetEase(Ease.Linear))
                    .OnComplete(OnComplete);
                
                void OnStart()
                {
                    for (int i = 0; i < _levelButtonList.Count; i++)
                    {
                        _levelButtonList[i].SetButtonActiveness(false);
                    }
                    _backwardButtonView.SetButtonActiveness(false);
                    _forwardButtonView.SetButtonActiveness(false);
                    spaceShip.StartFlames();
                }
                
                void OnComplete()
                {
                    spaceShip.StopFlames();
                    for (int i = 0; i < _levelButtonList.Count; i++)
                    {
                        if (_firstLevelIdOfTable + i <= _levelTracker.GetStarCountOfLevels().Count)
                        {
                            _levelButtonList[i].SetButtonActiveness(true);
                        }
                    }
                    _backwardButtonView.SetButtonActiveness(true);
                    _forwardButtonView.SetButtonActiveness(true);
                }
                
            }
            else
            {
                _levelButtonList[levelId - _firstLevelIdOfTable].CreateSpaceShip();
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