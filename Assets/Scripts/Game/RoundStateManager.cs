using System.Collections.Generic;
using System.Linq;
using Scripts;
using Zenject;

namespace Game
{
    public class RoundStateManager : IRoundStateManager
    {
        private readonly ILevelDataCreator _levelDataCreator;
        private readonly ILevelSaveDataManager _levelSaveDataManager;

        private int _maxGuessCount;
        private int _remainingGuessCount;
        private List<List<int>> _triedCardsList;
        private List<LifeBarRewardInfo> _lifeBarRewardInfoList;

        [Inject]
        public RoundStateManager(ILevelDataCreator levelDataCreator, ILevelSaveDataManager levelSaveDataManager)
        {
            _levelDataCreator = levelDataCreator;
            _levelSaveDataManager = levelSaveDataManager;
            _triedCardsList = new List<List<int>>();
            _lifeBarRewardInfoList = new List<LifeBarRewardInfo>();
        }

        public void Initialize()
        {
            LevelData levelData = _levelDataCreator.GetLevelData();
            LevelSaveData levelSaveData = _levelSaveDataManager.GetLevelSaveData();

            _maxGuessCount = levelData.MaxNumOfTries;
            _remainingGuessCount = levelSaveData.RemainingGuessCount;
            _triedCardsList = (levelSaveData.TriedCardsList ?? new List<List<int>>())
                .Select(triedCards => new List<int>(triedCards))
                .ToList();

            int crystalTokenCount = levelData.NumOfBoardHolders - 2;
            _lifeBarRewardInfoList = CreateLifeBarRewardInfoList(
                _maxGuessCount,
                _remainingGuessCount,
                _triedCardsList.Count,
                crystalTokenCount);
        }

        public int GetMaxGuessCount()
        {
            return _maxGuessCount;
        }

        public int GetRemainingGuessCount()
        {
            return _remainingGuessCount;
        }

        public void DecreaseRemainingGuessCount()
        {
            _remainingGuessCount--;
        }

        public void IncreaseRemainingGuessCount(int amount)
        {
            _remainingGuessCount += amount;
        }

        public IReadOnlyList<List<int>> GetTriedCardsList()
        {
            return _triedCardsList;
        }

        public void AddTriedCards(List<int> triedCards)
        {
            _triedCardsList.Add(new List<int>(triedCards));
        }

        public IReadOnlyList<LifeBarRewardInfo> GetLifeBarRewardInfoList()
        {
            return _lifeBarRewardInfoList;
        }

        public void SetLifeBarRewardStatus(int lifeBarRewardInfoIndex, bool isActive)
        {
            _lifeBarRewardInfoList[lifeBarRewardInfoIndex].SetIsActive(isActive);
        }

        public void GetActiveRewardCounts(out int activeCoinCount, out int activeCrystalCount)
        {
            activeCoinCount = 0;
            activeCrystalCount = 0;
            foreach (LifeBarRewardInfo lifeBarRewardInfo in _lifeBarRewardInfoList)
            {
                if (!lifeBarRewardInfo.IsActive) continue;

                if (lifeBarRewardInfo.IsCrystal)
                {
                    activeCrystalCount++;
                    continue;
                }

                activeCoinCount++;
            }
        }

        private static List<LifeBarRewardInfo> CreateLifeBarRewardInfoList(
            int maxGuessCount,
            int remainingGuessCount,
            int triedCardsCount,
            int crystalTokenCount)
        {
            List<LifeBarRewardInfo> lifeBarRewardInfoList = new List<LifeBarRewardInfo>();
            List<int> lifeBarRewardIndexes = new List<int> { 0, (maxGuessCount - 2) / 4, (maxGuessCount - 2) / 2 };
            for (int i = 0; i < lifeBarRewardIndexes.Count; i++)
            {
                int boundaryIndex = lifeBarRewardIndexes[i];
                bool hasLifePastBoundary = remainingGuessCount > boundaryIndex;
                bool hasNeverCrossedBoundary = maxGuessCount - triedCardsCount > boundaryIndex;
                bool isCoin = crystalTokenCount < 3 - i;
                lifeBarRewardInfoList.Add(new LifeBarRewardInfo(
                    boundaryIndex,
                    isCoin ? LifeBarRewardType.Coin : LifeBarRewardType.Crystal,
                    hasLifePastBoundary && hasNeverCrossedBoundary));
            }

            return lifeBarRewardInfoList;
        }
    }

    public interface IRoundStateManager
    {
        void Initialize();
        int GetMaxGuessCount();
        int GetRemainingGuessCount();
        void DecreaseRemainingGuessCount();
        void IncreaseRemainingGuessCount(int amount);
        IReadOnlyList<List<int>> GetTriedCardsList();
        void AddTriedCards(List<int> triedCards);
        IReadOnlyList<LifeBarRewardInfo> GetLifeBarRewardInfoList();
        void SetLifeBarRewardStatus(int lifeBarRewardInfoIndex, bool isActive);
        void GetActiveRewardCounts(out int activeCoinCount, out int activeCrystalCount);
    }
}
