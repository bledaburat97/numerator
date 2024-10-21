using System.Collections.Generic;
using Game;
using Newtonsoft.Json;
using UnityEngine;

namespace Scripts
{
    public class GameSaveService : IGameSaveService
    {
        private readonly string _saveGameKey = "saved_game_key";
        
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
        
        public void Save(IResultManager resultManager, ITargetNumberCreator targetNumberCreator, IGuessManager guessManager, ICardItemInfoManager cardItemInfoManager, ILevelSuccessManager levelSuccessManager, IBoardAreaController boardAreaController)
        {
            if (resultManager.GetTriedCardsList().Count == 0) return;
            if (levelSuccessManager.IsGameOver()) return;
            LevelSaveData levelSaveData = new LevelSaveData()
            {
                TriedCardsList = resultManager.GetTriedCardsList(),
                TargetCards = targetNumberCreator.GetTargetCardsList(),
                RemainingGuessCount = guessManager.GetRemainingGuessCount(),
                CardItemInfoList = cardItemInfoManager.GetCardItemInfoList(),
                RemovedBoardHolderCount = boardAreaController.GetRemovedBoardHolderCount()
            };
            
            string data = JsonConvert.SerializeObject(levelSaveData);
            PlayerPrefs.SetString(_saveGameKey, data);
        }
        
    }

    public interface IGameSaveService
    {
        LevelSaveData GetSavedLevel();
        void DeleteSave();
        void Save(IResultManager resultManager, ITargetNumberCreator targetNumberCreator, IGuessManager guessManager,
            ICardItemInfoManager cardItemInfoManager, ILevelSuccessManager levelSuccessManager, IBoardAreaController boardAreaController);
        bool HasSavedGame();
    }

    public class LevelSaveData
    {
        public List<List<int>> TriedCardsList;
        public List<int> TargetCards;
        public List<CardItemInfo> CardItemInfoList;
        public int RemainingGuessCount;
        public int RemovedBoardHolderCount;
    }
    
}