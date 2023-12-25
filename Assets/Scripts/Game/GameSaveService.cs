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
        private ICardItemInfoManager _cardItemInfoManager;

        public void Initialize(ILevelTracker levelTracker)
        {
            _levelTracker = levelTracker;
            _resultManager = new ResultManager();
            _levelManager = new LevelManager();
            _cardItemInfoManager = new CardItemInfoManager();
        }
        
        public void Set(IResultManager resultManager, ILevelManager levelManager, ICardItemInfoManager cardItemInfoManager)
        {
            _resultManager = resultManager;
            _levelManager = levelManager;
            _cardItemInfoManager = cardItemInfoManager;
        }
        
        public LevelSaveData GetSavedLevel()
        {
            //DeleteSave();
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
            if (_levelManager.IsGameOver()) return;
            LevelSaveData levelSaveData = new LevelSaveData()
            {
                LevelId = _levelTracker.GetLevelId(),
                TriedCardsList = _resultManager.GetTriedCardsList(),
                TargetCards = _resultManager.GetTargetCards(),
                RemainingGuessCount = _levelManager.GetRemainingGuessCount(),
                CardItemInfoList = _cardItemInfoManager.GetCardItemInfoList()
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
        void Set(IResultManager resultManager, ILevelManager levelManager, ICardItemInfoManager cardItemInfoManager);
        bool HasSavedGame();
    }

    public class LevelSaveData
    {
        public int LevelId;
        public List<List<int>> TriedCardsList;
        public List<int> TargetCards;
        public List<CardItemInfo> CardItemInfoList;
        public int RemainingGuessCount;
    }
    
}