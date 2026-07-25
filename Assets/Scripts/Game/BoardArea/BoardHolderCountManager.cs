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

        [Inject]
        public BoardHolderCountManager(ILevelSaveDataManager levelSaveDataManager, ILevelDataCreator levelDataCreator)
        {
            _levelSaveDataManager = levelSaveDataManager;
            _levelDataCreator = levelDataCreator;
        }

        public void Initialize()
        {
            _removedBoardHolderCount = _levelSaveDataManager.GetLevelSaveData().RemovedBoardHolderCount;
            _boardHolderCount = _levelDataCreator.GetLevelData().NumOfBoardHolders - _removedBoardHolderCount;
        }

        public void RemoveFirstBoardHolder()
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
        void Initialize();
        void RemoveFirstBoardHolder();
        int GetBoardHolderCount();
        int GetRemovedBoardHolderCount();
    }
}
