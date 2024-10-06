using System.Collections.Generic;
using Scripts;
using Zenject;

namespace Game
{
    public class LevelSaveDataManager : ILevelSaveDataManager
    {
        private ILevelDataCreator _levelDataCreator;
        private LevelSaveData _levelSaveData;

        [Inject]
        public LevelSaveDataManager(ILevelDataCreator levelDataCreator)
        {
            _levelDataCreator = levelDataCreator;
            _levelSaveData = new LevelSaveData();
        }
        
        public LevelSaveData GetLevelSaveData()
        {
            return _levelSaveData;
        }
        
        public void CreateDefaultLevelSaveData()
        {
            LevelData levelData = _levelDataCreator.GetLevelData();
            _levelSaveData.TriedCardsList = new List<List<int>>();
            _levelSaveData.RemainingGuessCount = levelData.MaxNumOfTries;
            _levelSaveData.CardItemInfoList = new List<CardItemInfo>();
            for (int i = 0; i < levelData.NumOfCards; i++)
            {
                CardItemInfo cardItemInfo = new CardItemInfo()
                {
                    possibleCardHolderIndicatorIndexes = GetAllPossibleCardHolderIndicatorIndexes(levelData.NumOfBoardHolders),
                    probabilityType = ProbabilityType.Probable,
                    isLocked = false
                };
                _levelSaveData.CardItemInfoList.Add(cardItemInfo);
            }
        }

        public void SetLevelSaveDataAsSaved(LevelSaveData savedData)
        {
            _levelSaveData = savedData;
        }
        
        private List<int> GetAllPossibleCardHolderIndicatorIndexes(int numOfBoardCardHolders)
        {
            List<int> possibleCardHolderIndexes = new List<int>();
            for (int i = 0; i < numOfBoardCardHolders; i++)
            {
                possibleCardHolderIndexes.Add(i);
            }

            return possibleCardHolderIndexes;
        }
    }

    public interface ILevelSaveDataManager
    {
        LevelSaveData GetLevelSaveData();
        void CreateDefaultLevelSaveData();
        void SetLevelSaveDataAsSaved(LevelSaveData savedData);
    }
}