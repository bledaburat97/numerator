using Game;
using System.Collections.Generic;
using System.Linq;
using Zenject;

namespace Scripts
{
    public class GameSaveSnapshotProvider : IGameSaveSnapshotProvider
    {
        private readonly ITargetNumberCreator _targetNumberCreator;
        private readonly ICardItemInfoManager _cardItemInfoManager;
        private readonly ILevelFlowOrchestrator _levelFlowOrchestrator;
        private readonly IBoardHolderCountManager _boardHolderCountManager;
        private readonly IRoundStateManager _roundStateManager;

        [Inject]
        public GameSaveSnapshotProvider(
            ITargetNumberCreator targetNumberCreator,
            ICardItemInfoManager cardItemInfoManager,
            ILevelFlowOrchestrator levelFlowOrchestrator,
            IBoardHolderCountManager boardHolderCountManager,
            IRoundStateManager roundStateManager)
        {
            _targetNumberCreator = targetNumberCreator;
            _cardItemInfoManager = cardItemInfoManager;
            _levelFlowOrchestrator = levelFlowOrchestrator;
            _boardHolderCountManager = boardHolderCountManager;
            _roundStateManager = roundStateManager;
        }

        public bool TryCreateSnapshot(out LevelSaveData levelSaveData)
        {
            levelSaveData = null;

            if (_roundStateManager.GetTriedCardsList().Count == 0) return false;
            if (_levelFlowOrchestrator.IsGameOver()) return false;
            if (_levelFlowOrchestrator.IsTransitioning()) return false;

            levelSaveData = new LevelSaveData
            {
                TriedCardsList = CreateTriedCardsSnapshot(),
                TargetCards = _targetNumberCreator.GetTargetCardsList(),
                RemainingGuessCount = _roundStateManager.GetRemainingGuessCount(),
                CardItemInfoList = _cardItemInfoManager.GetCardItemInfoList(),
                RemovedBoardHolderCount = _boardHolderCountManager.GetRemovedBoardHolderCount()
            };

            return true;
        }

        private List<List<int>> CreateTriedCardsSnapshot()
        {
            return _roundStateManager.GetTriedCardsList()
                .Select(triedCards => new List<int>(triedCards))
                .ToList();
        }
    }

    public interface IGameSaveSnapshotProvider
    {
        bool TryCreateSnapshot(out LevelSaveData levelSaveData);
    }
}
