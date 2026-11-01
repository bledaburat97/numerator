using Game;
using Zenject;

namespace Scripts
{
    public class BoardHolderCountManager : IBoardHolderCountManager
    {
        private readonly ILevelSaveDataManager _levelSaveDataManager;
        private readonly ILevelDataCreator _levelDataCreator;

        private int _boardHolderCount;
        private int _removedBoardHolderCount;
        private bool _canShrink = true;

        [Inject]
        public BoardHolderCountManager(ILevelSaveDataManager levelSaveDataManager, ILevelDataCreator levelDataCreator)
        {
            _levelSaveDataManager = levelSaveDataManager;
            _levelDataCreator = levelDataCreator;
        }

        public void Initialize()
        {
            _canShrink = _levelDataCreator.GetLevelData().Mechanic != LevelMechanic.Growing;
            _removedBoardHolderCount = _levelSaveDataManager.GetLevelSaveData().RemovedBoardHolderCount;
            _boardHolderCount = (_levelSaveDataManager.GetLevelSaveData().GrowingBoard?.ActiveCount
                ?? _levelDataCreator.GetLevelData().NumOfBoardHolders) - _removedBoardHolderCount;
        }

        public bool CanShrink => _canShrink;
        public void AddBoardHolder() => _boardHolderCount++;

        public void RemoveLastBoardHolder()
        {
            _removedBoardHolderCount++;
            _boardHolderCount--;
        }

        public int GetBoardHolderCount()
        {
            return _boardHolderCount;
        }

        public int GetRemovedBoardHolderCount()
        {
            return _removedBoardHolderCount;
        }
    }

    public interface IBoardHolderCountManager
    {
        bool CanShrink { get; }
        void AddBoardHolder();
        void Initialize();
        void RemoveLastBoardHolder();
        int GetBoardHolderCount();
        int GetRemovedBoardHolderCount();
    }
}
