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

        public void Initialize(ILevelTracker levelTracker)
        {
            _levelTracker = levelTracker;
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
        
        public void Save(IResultManager resultManager, ITargetNumberCreator targetNumberCreator, IGuessManager guessManager, ICardItemInfoManager cardItemInfoManager)
        {
            if (resultManager.GetTriedCardsList().Count == 0) return;
            if (guessManager.IsGameOver()) return;
            LevelSaveData levelSaveData = new LevelSaveData()
            {
                LevelId = _levelTracker.GetLevelId(),
                TriedCardsList = resultManager.GetTriedCardsList(),
                TargetCards = targetNumberCreator.GetTargetCardsList(),
                RemainingGuessCount = guessManager.GetRemainingGuessCount(),
                CardItemInfoList = cardItemInfoManager.GetCardItemInfoList()
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
        void Save(IResultManager resultManager, ITargetNumberCreator targetNumberCreator, IGuessManager guessManager,
            ICardItemInfoManager cardItemInfoManager);
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