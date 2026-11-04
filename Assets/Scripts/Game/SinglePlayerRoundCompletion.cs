using Scripts;

namespace Game
{
    // Owns the durable outcome. Presenters only consume the resulting snapshot.
    public sealed class SinglePlayerRoundCompletion
    {
        private readonly ILevelTracker _tracker;
        private readonly IRoundStateManager _round;
        private readonly IGameSaveService _saves;
        private SinglePlayerRoundResult _completed;

        public SinglePlayerRoundCompletion(ILevelTracker tracker, IRoundStateManager round, IGameSaveService saves)
        {
            _tracker = tracker;
            _round = round;
            _saves = saves;
        }

        public void BeginRound() => _completed = null;

        public SinglePlayerRoundResult Complete(bool won)
        {
            if (_completed != null) return _completed;

            int coins = 0, crystals = 0;
            int previousCoins = _tracker.GetCoinCount();
            int previousProgress = _tracker.GetCrystalProgressCount();
            RewardType previousReward = _tracker.GetCurrentRewardType();
            if (won)
            {
                _round.GetActiveRewardCounts(out coins, out crystals);
                // Persist progress before deleting the old checkpoint. A stale checkpoint
                // is rejected on resume by its level id if deletion is interrupted.
                _tracker.IncrementLevelIdWithRewards(coins, crystals);
            }

            _completed = new SinglePlayerRoundResult(won, _round.GetTriedCardsList().Count,
                coins, crystals, previousCoins, previousProgress, previousReward, _tracker.GetLevelId() + 1);
            _saves.DeleteSave();
            return _completed;
        }
    }

    public sealed class SinglePlayerRoundResult
    {
        public bool Won { get; }
        public int GuessCount { get; }
        public int EarnedCoins { get; }
        public int EarnedCrystals { get; }
        public int PreviousCoins { get; }
        public int PreviousCrystalProgress { get; }
        public RewardType PreviousReward { get; }
        public int NextLevelNumber { get; }

        public SinglePlayerRoundResult(bool won, int guessCount, int earnedCoins, int earnedCrystals,
            int previousCoins, int previousCrystalProgress, RewardType previousReward, int nextLevelNumber)
        {
            Won = won;
            GuessCount = guessCount;
            EarnedCoins = earnedCoins;
            EarnedCrystals = earnedCrystals;
            PreviousCoins = previousCoins;
            PreviousCrystalProgress = previousCrystalProgress;
            PreviousReward = previousReward;
            NextLevelNumber = nextLevelNumber;
        }
    }
}
