using Game;
using Zenject;

namespace Scripts
{
    public class GameSaveSnapshotProvider : IGameSaveSnapshotProvider
    {
        private readonly IResultManager _resultManager;
        private readonly ITargetNumberCreator _targetNumberCreator;
        private readonly IGuessManager _guessManager;
        private readonly ICardItemInfoManager _cardItemInfoManager;
        private readonly ILevelEndManager _levelEndManager;
        private readonly IBoardStateReader _boardStateReader;

        [Inject]
        public GameSaveSnapshotProvider(
            IResultManager resultManager,
            ITargetNumberCreator targetNumberCreator,
            IGuessManager guessManager,
            ICardItemInfoManager cardItemInfoManager,
            ILevelEndManager levelEndManager,
            IBoardStateReader boardStateReader)
        {
            _resultManager = resultManager;
            _targetNumberCreator = targetNumberCreator;
            _guessManager = guessManager;
            _cardItemInfoManager = cardItemInfoManager;
            _levelEndManager = levelEndManager;
            _boardStateReader = boardStateReader;
        }

        public bool TryCreateSnapshot(out LevelSaveData levelSaveData)
        {
            levelSaveData = null;

            if (_resultManager.GetTriedCardsList().Count == 0) return false;
            if (_levelEndManager.IsGameOver()) return false;

            levelSaveData = new LevelSaveData
            {
                TriedCardsList = _resultManager.GetTriedCardsList(),
                TargetCards = _targetNumberCreator.GetTargetCardsList(),
                RemainingGuessCount = _guessManager.GetRemainingGuessCount(),
                CardItemInfoList = _cardItemInfoManager.GetCardItemInfoList(),
                RemovedBoardHolderCount = _boardStateReader.GetRemovedBoardHolderCount()
            };

            return true;
        }
    }

    public interface IGameSaveSnapshotProvider
    {
        bool TryCreateSnapshot(out LevelSaveData levelSaveData);
    }
}
