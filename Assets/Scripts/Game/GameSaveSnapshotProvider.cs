using Game;
using System.Collections.Generic;
using System.Linq;
using Zenject;

namespace Scripts
{
    public class GameSaveSnapshotProvider : IGameSaveSnapshotProvider
    {
        [InjectOptional] private ILevelSaveDataManager _saveData;
        [InjectOptional] private ILevelDataCreator _levels;
        [InjectOptional] private ILevelTracker _tracker;
        private readonly ITargetNumberCreator _targetNumberCreator;
        private readonly ICardItemInfoManager _cardItemInfoManager;
        private readonly ILevelSession _session;
        private readonly IBoardHolderCountManager _boardHolderCountManager;
        private readonly IRoundStateManager _roundStateManager;

        [Inject]
        public GameSaveSnapshotProvider(
            ITargetNumberCreator targetNumberCreator,
            ICardItemInfoManager cardItemInfoManager,
            ILevelSession session,
            IBoardHolderCountManager boardHolderCountManager,
            IRoundStateManager roundStateManager)
        {
            _targetNumberCreator = targetNumberCreator;
            _cardItemInfoManager = cardItemInfoManager;
            _session = session;
            _boardHolderCountManager = boardHolderCountManager;
            _roundStateManager = roundStateManager;
        }

        public bool TryCreateSnapshot(out LevelSaveData levelSaveData,bool includeUnchanged=false)
        {
            levelSaveData = null;

            var info = _cardItemInfoManager.GetCardItemInfoList();
            bool mutatedBeforeGuess = (_saveData?.GetLevelSaveData().PowerUpUseRevision??0)>0 || _saveData?.GetLevelSaveData().Ice != null || _saveData?.GetLevelSaveData().Dynamite != null || (_saveData?.GetLevelSaveData().GrowingBoard?.FullTarget.Count == GrowingBoardState.MaximumCount) || (_saveData?.GetLevelSaveData().TrialStockPrepared ?? false) || (_saveData?.GetLevelSaveData().BoardSplitApplied ?? false) || (_saveData?.GetLevelSaveData().PairWasApplied ?? false)
                || (_saveData?.GetLevelSaveData().UltraActive ?? false) || (_saveData?.GetLevelSaveData().PairClue != null) || _boardHolderCountManager.GetRemovedBoardHolderCount() > 0
                || (info != null && info.Any(card => card.isLocked || !card.isExisted));
            bool hasNotes = _saveData?.GetLevelSaveData().PositionNotes?.Edited == true;
            if (!includeUnchanged && _roundStateManager.GetTriedCardsList().Count == 0 && !mutatedBeforeGuess && !hasNotes) return false;
            if (_roundStateManager.GetRemainingGuessCount() <= 0 || _session.IsGameOver) return false;
            if (_session.State != LevelFlowState.Idle && _session.State != LevelFlowState.Playing) return false;

            levelSaveData = new LevelSaveData
            {
                RoundId = _saveData?.GetLevelSaveData().RoundId,
                PowerUpUseRevision = _saveData?.GetLevelSaveData().PowerUpUseRevision ?? 0,
                TriedCardsList = CreateTriedCardsSnapshot(),
                TargetCards = new List<int>(_targetNumberCreator.GetTargetCardsList()),
                RemainingGuessCount = _roundStateManager.GetRemainingGuessCount(),
                CardItemInfoList = info.Select(card => new CardItemInfo {
                    isExisted = card.isExisted, isLocked = card.isLocked, probabilityType = card.probabilityType,
                    possibleCardHolderIndicatorIndexes = new List<int>(card.possibleCardHolderIndicatorIndexes)
                }).ToList(),
                RemovedBoardHolderCount = _boardHolderCountManager.GetRemovedBoardHolderCount(),
                LevelId = _tracker?.GetLevelId() ?? -1,
                Definition = _levels?.GetLevelData().Copy(),
                TrialStockPrepared = _saveData?.GetLevelSaveData().TrialStockPrepared ?? false,
                RevealedGuessNumbers = new List<int>(_saveData?.GetLevelSaveData().RevealedGuessNumbers ?? new List<int>()),
                LastEliminationGuessNumber = _saveData?.GetLevelSaveData().LastEliminationGuessNumber ?? 0,
                PairWasApplied = _saveData?.GetLevelSaveData().PairWasApplied ?? false,
                BoardSplitApplied = _saveData?.GetLevelSaveData().BoardSplitApplied ?? false,
                UltraActive = _saveData?.GetLevelSaveData().UltraActive ?? false,
                PairClue = _saveData?.GetLevelSaveData().PairClue,
                GrowingBoard = _saveData?.GetLevelSaveData().GrowingBoard?.Copy(),
                Dynamite = _saveData?.GetLevelSaveData().Dynamite?.Copy(),
                Ice = _saveData?.GetLevelSaveData().Ice?.Copy(),
                ActiveRewardTokens = _roundStateManager.GetLifeBarRewardInfoList().Select(reward => reward.IsActive).ToList(),
                PositionNotes = _saveData?.GetLevelSaveData().PositionNotes?.Copy()
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
        bool TryCreateSnapshot(out LevelSaveData levelSaveData,bool includeUnchanged=false);
    }
}
