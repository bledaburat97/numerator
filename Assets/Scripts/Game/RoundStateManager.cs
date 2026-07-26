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
        private List<LifeBarStarInfo> _lifeBarStarInfoList;

        [Inject]
        public RoundStateManager(ILevelDataCreator levelDataCreator, ILevelSaveDataManager levelSaveDataManager)
        {
            _levelDataCreator = levelDataCreator;
            _levelSaveDataManager = levelSaveDataManager;
            _triedCardsList = new List<List<int>>();
            _lifeBarStarInfoList = new List<LifeBarStarInfo>();
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

            int rewardStarCount = levelData.NumOfBoardHolders - 2;
            _lifeBarStarInfoList = CreateLifeBarStarInfoList(
                _maxGuessCount,
                _remainingGuessCount,
                _triedCardsList.Count,
                rewardStarCount);
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

        public IReadOnlyList<LifeBarStarInfo> GetLifeBarStarInfoList()
        {
            return _lifeBarStarInfoList;
        }

        public void SetLifeBarStarStatus(int lifeBarStarInfoIndex, bool isActive)
        {
            _lifeBarStarInfoList[lifeBarStarInfoIndex].SetIsActive(isActive);
        }

        public void GetActiveStarCounts(out int activeTotalStarCount, out int activeRewardStarCount)
        {
            activeTotalStarCount = 0;
            activeRewardStarCount = 0;
            foreach (LifeBarStarInfo lifeBarStarInfo in _lifeBarStarInfoList)
            {
                if (!lifeBarStarInfo.IsActive) continue;

                activeTotalStarCount++;
                if (!lifeBarStarInfo.IsOriginal)
                {
                    activeRewardStarCount++;
                }
            }
        }

        private static List<LifeBarStarInfo> CreateLifeBarStarInfoList(
            int maxGuessCount,
            int remainingGuessCount,
            int triedCardsCount,
            int rewardStarCount)
        {
            List<LifeBarStarInfo> lifeBarStarInfoList = new List<LifeBarStarInfo>();
            List<int> lifeBarStarIndexes = new List<int> { 0, (maxGuessCount - 2) / 4, (maxGuessCount - 2) / 2 };
            for (int i = 0; i < lifeBarStarIndexes.Count; i++)
            {
                int boundaryIndex = lifeBarStarIndexes[i];
                bool hasLifePastBoundary = remainingGuessCount > boundaryIndex;
                bool hasNeverCrossedBoundary = maxGuessCount - triedCardsCount > boundaryIndex;
                lifeBarStarInfoList.Add(new LifeBarStarInfo(
                    boundaryIndex,
                    rewardStarCount < 3 - i,
                    hasLifePastBoundary && hasNeverCrossedBoundary));
            }

            return lifeBarStarInfoList;
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
        IReadOnlyList<LifeBarStarInfo> GetLifeBarStarInfoList();
        void SetLifeBarStarStatus(int lifeBarStarInfoIndex, bool isActive);
        void GetActiveStarCounts(out int activeTotalStarCount, out int activeRewardStarCount);
    }
}
