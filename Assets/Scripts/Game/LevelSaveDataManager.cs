using System.Collections.Generic;
using Scripts;

namespace Game
{
    public class LevelSaveDataManager : ILevelSaveDataManager
    {
        private LevelSaveData _levelSaveData;
        
        public void SetLevelSaveData(LevelSaveData savedData, ITargetNumberCreator targetNumberCreator, ILevelDataCreator levelDataCreator, ILevelTracker levelTracker)
        {
            if (levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                levelDataCreator.SetSinglePlayerLevelData(levelTracker.GetLevelId());
                LevelData levelData = levelDataCreator.GetLevelData();

                if (levelTracker.IsFirstLevelTutorial())
                {
                    _levelSaveData = CreateDefaultLevelSaveData(levelData);
                    targetNumberCreator.SetSavedTargetCardList(new List<int>(){4,1});
                    return;
                }
                if (levelTracker.IsCardInfoTutorial())
                {
                    _levelSaveData = CreateDefaultLevelSaveData(levelData);
                    targetNumberCreator.SetSavedTargetCardList(new List<int>(){4,6});
                    return;
                }
                
                if (savedData != null)
                {
                    _levelSaveData = savedData;
                    targetNumberCreator.SetSavedTargetCardList(_levelSaveData.TargetCards);
                }
                else
                {
                    _levelSaveData = CreateDefaultLevelSaveData(levelData);
                    targetNumberCreator.CreateTargetNumber(levelData.NumOfCards, levelData.NumOfBoardHolders);
                }
            }

            else
            {
                levelDataCreator.SetMultiplayerLevelData(levelTracker.GetNumberOfBoardCardsInMultiplayer());
                LevelData levelData = levelDataCreator.GetLevelData();
                _levelSaveData = CreateDefaultLevelSaveData(levelData);
                targetNumberCreator.CreateMultiplayerTargetNumber(levelData.NumOfCards, levelData.NumOfBoardHolders);
            }

        }
        
        public LevelSaveData GetLevelSaveData()
        {
            return _levelSaveData;
        }
        
        private LevelSaveData CreateDefaultLevelSaveData(LevelData levelData)
        {
            LevelSaveData levelSaveData = new LevelSaveData();
            levelSaveData.TriedCardsList = new List<List<int>>();
            levelSaveData.RemainingGuessCount = levelData.MaxNumOfTries;
            levelSaveData.CardItemInfoList = new List<CardItemInfo>();
            for (int i = 0; i < levelData.NumOfCards; i++)
            {
                CardItemInfo cardItemInfo = new CardItemInfo()
                {
                    possibleCardHolderIndicatorIndexes = GetAllPossibleCardHolderIndicatorIndexes(levelData.NumOfBoardHolders),
                    probabilityType = ProbabilityType.Probable,
                    isLocked = false
                };
                levelSaveData.CardItemInfoList.Add(cardItemInfo);
            }

            return levelSaveData;
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
        void SetLevelSaveData(LevelSaveData savedData, ITargetNumberCreator targetNumberCreator,
            ILevelDataCreator levelDataCreator, ILevelTracker levelTracker);

        LevelSaveData GetLevelSaveData();
    }
}