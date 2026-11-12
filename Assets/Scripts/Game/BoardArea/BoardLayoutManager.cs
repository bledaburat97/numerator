using System.Collections.Generic;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class BoardLayoutManager : IBoardLayoutManager
    {
        private const float GardenSpacingToHolderWidthRatio = 3f / 70f;
        [InjectOptional] private BoardSplitService _split;
        [InjectOptional] private ILevelDataCreator _levels;

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
            var partition = _split?.PartitionFor(numOfBoardHolders);
            Vector2 area = _view.GetRectTransform().rect.size;
            float footer = (_view as BoardAreaView)?.FooterHeight ?? 0f;
            area.y = Mathf.Max(1f,area.y-footer);
            float groupGap = partition != null ? Mathf.Min(12f,area.x*.04f) : 0f;
            _sizeManager.SetSizeRatio(new Vector2(area.x-groupGap,area.y),
                _view.GetSizeOfBoardHolder(), GardenSpacingToHolderWidthRatio);
            _boardHolderSceneLocalPositionList.Clear();
            Vector2 cardHolderSize = GetSizeOfBoardHolder();
            float spacing = cardHolderSize.x * GardenSpacingToHolderWidthRatio;
            _boardHolderSceneLocalPositionList = _boardHolderSceneLocalPositionList.GetLocalPositionList(
                _levels?.GetLevelData()?.Mechanic==LevelMechanic.Growing?GrowingBoardState.MaximumCount:numOfBoardHolders,
                spacing,
                cardHolderSize,
                footer*.5f);
            if (partition != null)
                for (int index = 0; index < numOfBoardHolders; index++)
                    _boardHolderSceneLocalPositionList[index] += Vector2.right * (index < partition.FirstCount ? -groupGap*.5f : groupGap*.5f);
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
