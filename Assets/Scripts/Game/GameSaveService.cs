using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Scripts
{
    public class GameSaveService : IGameSaveService
    {
        private readonly string _saveGameKey = "saved_game_key";
        private ILevelTracker _levelTracker;
        private IResultManager _resultManager;
        private IInitialCardAreaController _initialCardAreaController;

        public void Initialize(ILevelTracker levelTracker)
        {
            _levelTracker = levelTracker;
            _resultManager = new ResultManager();
            _initialCardAreaController = new InitialCardAreaController();
        }
        
        public void Set(IResultManager resultManager, IInitialCardAreaController initialCardAreaController)
        {
            _resultManager = resultManager;
            _initialCardAreaController = initialCardAreaController;
        }
        
        public LevelSaveData GetSavedLevel()
        {
            if (!PlayerPrefs.HasKey(_saveGameKey)) return null;
            LevelSaveData levelSaveData = JsonConvert.DeserializeObject<LevelSaveData>(PlayerPrefs.GetString(_saveGameKey));
            return levelSaveData;
        }

        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey(_saveGameKey);
        }
        
        public void Save()
        {
            LevelSaveData levelSaveData = new LevelSaveData()
            {
                LevelId = _levelTracker.GetLevelId(),
                TriedCardsList = _resultManager.GetTriedCardsList(),
                TargetCards = _resultManager.GetTargetCards(),
                ProbabilityTypes = _initialCardAreaController.GetProbabilityTypes(),
                ActiveHolderIndicatorIndexesList = _initialCardAreaController.GetActiveHolderIndicatorIndexesList(),
                LockedCardIndexes = _initialCardAreaController.GetLockedCardIndexes()
            };
            
            string data = JsonConvert.SerializeObject(levelSaveData);
            PlayerPrefs.SetString(_saveGameKey, data);
        }
        
    }

    public interface IGameSaveService
    {
        void Initialize(ILevelTracker levelTracker);
        LevelSaveData GetSavedLevel();
        void DeleteSave();
        void Save();
        void Set(IResultManager resultManager, IInitialCardAreaController initialCardAreaController);
    }

    public class LevelSaveData
    {
        public int LevelId;
        public List<List<int>> TriedCardsList;
        public List<int> TargetCards;
        public List<ProbabilityType> ProbabilityTypes;
        public List<List<int>> ActiveHolderIndicatorIndexesList;
        public List<int> LockedCardIndexes;
    }
    
}