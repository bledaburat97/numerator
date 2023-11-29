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
        private ILevelManager _levelManager;

        public void Initialize(ILevelTracker levelTracker)
        {
            _levelTracker = levelTracker;
            _resultManager = new ResultManager();
            _initialCardAreaController = new InitialCardAreaController();
            _levelManager = new LevelManager();
        }
        
        public void Set(IResultManager resultManager, IInitialCardAreaController initialCardAreaController, ILevelManager levelManager)
        {
            _resultManager = resultManager;
            _initialCardAreaController = initialCardAreaController;
            _levelManager = levelManager;
        }
        
        public LevelSaveData GetSavedLevel()
        {
            if (!HasSavedGame()) return null;
            LevelSaveData levelSaveData = JsonConvert.DeserializeObject<LevelSaveData>(PlayerPrefs.GetString(_saveGameKey));
            return levelSaveData;
        }

        public bool HasSavedGame()
        {
            return PlayerPrefs.HasKey(_saveGameKey);
        }

        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey(_saveGameKey);
        }
        
        public void Save()
        {
            if (_resultManager.GetTriedCardsList().Count == 0) return; 
            LevelSaveData levelSaveData = new LevelSaveData()
            {
                LevelId = _levelTracker.GetLevelId(),
                TriedCardsList = _resultManager.GetTriedCardsList(),
                TargetCards = _resultManager.GetTargetCards(),
                ProbabilityTypes = _initialCardAreaController.GetProbabilityTypes(),
                ActiveHolderIndicatorIndexesList = _initialCardAreaController.GetActiveHolderIndicatorIndexesList(),
                LockedCardIndexes = _initialCardAreaController.GetLockedCardIndexes(),
                RemainingGuessCount = _levelManager.GetRemainingGuessCount()
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
        void Set(IResultManager resultManager, IInitialCardAreaController initialCardAreaController, ILevelManager levelManager);
        bool HasSavedGame();
    }

    public class LevelSaveData
    {
        public int LevelId;
        public List<List<int>> TriedCardsList;
        public List<int> TargetCards;
        public List<ProbabilityType> ProbabilityTypes;
        public List<List<int>> ActiveHolderIndicatorIndexesList;
        public List<int> LockedCardIndexes;
        public int RemainingGuessCount;
    }
    
}