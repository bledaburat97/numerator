using System.Collections.Generic;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class BoardLayoutManager : IBoardLayoutManager
    {
        private const float GardenSpacingToHolderWidthRatio = 3f / 70f;

        private readonly IBoardAreaView _view;
        private readonly ISizeManager _sizeManager;
        private List<Vector2> _boardHolderSceneLocalPositionList;

        [Inject]
        public BoardLayoutManager(IBoardAreaView view, ISizeManager sizeManager)
        {
            _view = view;
            _sizeManager = sizeManager;
            _boardHolderSceneLocalPositionList = new List<Vector2>();
            _sizeManager.SetSizeRatio(
                new Vector2(_view.GetRectTransform().rect.width, _view.GetRectTransform().rect.height),
                _view.GetSizeOfBoardHolder(),
                GardenSpacingToHolderWidthRatio);
        }

        public void Initialize(int numOfBoardHolders)
        {
            _boardHolderSceneLocalPositionList.Clear();
            Vector2 cardHolderSize = GetSizeOfBoardHolder();
            float spacing = cardHolderSize.x * GardenSpacingToHolderWidthRatio;
            _boardHolderSceneLocalPositionList = _boardHolderSceneLocalPositionList.GetLocalPositionList(
                numOfBoardHolders,
                spacing,
                cardHolderSize,
                0f);
        }

        public Vector2 GetSizeOfBoardHolder()
        {
            return _view.GetSizeOfBoardHolder() * _sizeManager.GetSizeRatio();
        }

        public List<Vector2> GetBoardHolderSceneLocalPositionList()
        {
            return _boardHolderSceneLocalPositionList;
        }
    }

    public interface IBoardLayoutManager
    {
        void Initialize(int numOfBoardHolders);
        Vector2 GetSizeOfBoardHolder();
        List<Vector2> GetBoardHolderSceneLocalPositionList();
    }
}
