using System.Collections.Generic;
using Game;
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
        private IGuessManager _guessManager;
        private ICardItemInfoManager _cardItemInfoManager;

        public void Initialize(ILevelTracker levelTracker)
        {
            _levelTracker = levelTracker;
            _resultManager = new ResultManager();
            _guessManager = new GuessManager();
            _cardItemInfoManager = new CardItemInfoManager();
        }
        
        public void Set(IResultManager resultManager, IGuessManager guessManager, ICardItemInfoManager cardItemInfoManager)
        {
            _resultManager = resultManager;
            _guessManager = guessManager;
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
            if (_guessManager.IsGameOver()) return;
            LevelSaveData levelSaveData = new LevelSaveData()
            {
                LevelId = _levelTracker.GetLevelId(),
                TriedCardsList = _resultManager.GetTriedCardsList(),
                TargetCards = _resultManager.GetTargetCards(),
                RemainingGuessCount = _guessManager.GetRemainingGuessCount(),
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
        void Set(IResultManager resultManager, IGuessManager guessManager, ICardItemInfoManager cardItemInfoManager);
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