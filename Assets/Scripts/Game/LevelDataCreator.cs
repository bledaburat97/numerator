using System.Collections.Generic;
using Unity.Netcode;
using Zenject;

namespace Scripts
{
    public class LevelDataCreator : NetworkBehaviour, ILevelDataCreator
    {
        private LevelData _levelData;
        private List<LevelData> _startingLevelsDataList;
        private List<LevelData> _loopLevelsDataList;
        [Inject] private ILevelTracker _levelTracker;
        
        public void Awake()
        {
            _startingLevelsDataList = LevelDataGetter.GetStartingLevelsData();
            _loopLevelsDataList = LevelDataGetter.GetLoopLevelsData();
        }
        
        public void SetSinglePlayerLevelData()
        {
            int levelId = _levelTracker.GetLevelId();
            if (levelId < 30)
            {
                _levelData = _startingLevelsDataList.Find(level => level.LevelId == levelId);
                if (_levelData == null)
                {
                    _levelData = GetFallbackLevelData(_startingLevelsDataList, levelId);
                }
            }
            else
            {
                int loopLevelId = _loopLevelsDataList.Count == 0 ? 0 : levelId % _loopLevelsDataList.Count;
                _levelData = _loopLevelsDataList.Find(level => level.LevelId == loopLevelId);
                if (_levelData == null)
                {
                    _levelData = GetFallbackLevelData(_loopLevelsDataList, loopLevelId);
                }
            }

            if (_levelData == null)
            {
                _levelData = CreateDefaultLevelData(levelId);
            }
        }

        public LevelData GetLevelData()
        {
            return _levelData;
        }

        public void SetMultiplayerLevelData()
        {
            var settings = MultiplayerRules.ForBoardCount(_levelTracker.GetNumberOfBoardCardsInMultiplayer());
            _levelData = new LevelData { LevelId = -1, NumOfBoardHolders = settings.BoardCount,
                NumOfCards = settings.FruitCount, MaxNumOfTries = 1000 };
        }

        private static LevelData GetFallbackLevelData(IReadOnlyList<LevelData> levelDataList, int preferredIndex)
        {
            if (levelDataList == null || levelDataList.Count == 0) return null;
            if (preferredIndex >= 0 && preferredIndex < levelDataList.Count)
            {
                return levelDataList[preferredIndex];
            }

            return levelDataList[levelDataList.Count - 1];
        }

        private static LevelData CreateDefaultLevelData(int levelId)
        {
            return new LevelData
            {
                LevelId = levelId,
                NumOfBoardHolders = 3,
                NumOfCards = 9,
                MaxNumOfTries = 6
            };
        }
        
    }

    public interface ILevelDataCreator
    {
        void SetSinglePlayerLevelData();
        LevelData GetLevelData();
        void SetMultiplayerLevelData();
    }
}
