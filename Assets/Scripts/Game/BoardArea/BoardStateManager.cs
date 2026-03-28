using Game;
using Zenject;

namespace Scripts
{
    public class BoardStateManager : IBoardStateManager
    {
        private readonly ILevelSaveDataManager _levelSaveDataManager;
        private readonly ILevelDataCreator _levelDataCreator;

        private int _numOfBoardHolders;
        private int _removedBoardHolderCount;

        [Inject]
        public BoardStateManager(ILevelSaveDataManager levelSaveDataManager, ILevelDataCreator levelDataCreator)
        {
            _levelSaveDataManager = levelSaveDataManager;
            _levelDataCreator = levelDataCreator;
        }

        public void Initialize()
        {
            _removedBoardHolderCount = _levelSaveDataManager.GetLevelSaveData().RemovedBoardHolderCount;
            _numOfBoardHolders = _levelDataCreator.GetLevelData().NumOfBoardHolders - _removedBoardHolderCount;
        }

        public void RemoveFirstBoardHolder()
        {
            _removedBoardHolderCount++;
            _numOfBoardHolders--;
        }

        public int GetNumOfBoardHolders()
        {
            return _numOfBoardHolders;
        }

        public int GetRemovedBoardHolderCount()
        {
            return _removedBoardHolderCount;
        }
    }

    public interface IBoardStateManager
    {
        void Initialize();
        void RemoveFirstBoardHolder();
        int GetNumOfBoardHolders();
        int GetRemovedBoardHolderCount();
    }
}
