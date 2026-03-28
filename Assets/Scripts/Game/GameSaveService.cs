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
        
        public void Save(LevelSaveData levelSaveData)
        {
            if (levelSaveData == null) return;
            string data = JsonConvert.SerializeObject(levelSaveData);
            PlayerPrefs.SetString(_saveGameKey, data);
        }
        
    }

    public interface IGameSaveService
    {
        LevelSaveData GetSavedLevel();
        void DeleteSave();
        void Save(LevelSaveData levelSaveData);
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
