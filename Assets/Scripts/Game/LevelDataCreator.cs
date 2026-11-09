using System.Collections.Generic;
using Unity.Netcode;
using Zenject;

namespace Scripts
{
    public class LevelDataCreator : NetworkBehaviour, ILevelDataCreator
    {
        private LevelData _levelData;
        [Inject] private ILevelTracker _levelTracker;
        
        public void Awake()
        {

        }
        
        public void SetSinglePlayerLevelData()
        {
            _levelData=DefinitionFor(_levelTracker.GetLevelId());
        }
        public static LevelData DefinitionFor(int levelId)
        {
            if(LevelPlaytestSchedule.TryCreate(levelId,out var definition))return definition;
            var levels=levelId<30?LevelDataGetter.GetStartingLevelsData():LevelDataGetter.GetLoopLevelsData();
            int index=levelId<30?levelId:levels.Count==0?0:levelId%levels.Count;
            definition=levels.Find(level=>level.LevelId==index) ?? GetFallbackLevelData(levels,index);
            return (definition ?? CreateDefaultLevelData(levelId)).Copy();
        }

        public void RestoreDefinition(LevelData definition)
        {
            _levelData = definition.Copy();
            _levelData.Mechanic = LevelMechanicRules.Normalize(_levelData.Mechanic);
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
