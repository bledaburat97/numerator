﻿using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Random = System.Random;
namespace Scripts
{
    public class LevelTracker : MonoBehaviour, ILevelTracker
    {
        private List<LevelData> _levelDataList = new List<LevelData>();

        private int _starCount;
        private int _wildCardCount;
        private LevelInfo _levelInfo;
        private IGameSaveService _gameSaveService;
        private List<int> _starCountOfCompletedLevels;
        private int _levelId;
        
        public void Initialize(IGameSaveService gameSaveService)
        {
            //ClearPlayerPrefs();
            _gameSaveService = gameSaveService;
            _levelId = PlayerPrefs.GetInt("level_id", 0);
            _starCount = PlayerPrefs.GetInt("star_count", 0);
            _wildCardCount = PlayerPrefs.GetInt("wild_card_count", 0);
            _starCountOfCompletedLevels = JsonConvert.DeserializeObject<List<int>>(PlayerPrefs.GetString("star_count_of_levels", "")) ?? new List<int>();
        }

        public void ClearPlayerPrefs()
        {
            PlayerPrefs.DeleteKey("level_id");
            PlayerPrefs.DeleteKey("star_count");
            PlayerPrefs.DeleteKey("wild_card_count");
            PlayerPrefs.DeleteKey("star_count_of_levels");
        }

        public void SetLevelInfo()
        {
            List<LevelData> levelDataList = LevelDataGetter.GetLevelDataFromJson();
            LevelData levelData = levelDataList.Find(level => level.LevelId == _levelId % 30);
            _levelInfo = new LevelInfo();
            LevelSaveData levelSaveData = _gameSaveService.GetSavedLevel();

            if (levelSaveData != null)
            {
                if (_levelId == levelSaveData.LevelId)
                {
                    _levelInfo.levelSaveData = levelSaveData;
                    _levelInfo.levelData = levelData;
                }
                else
                {
                    _levelInfo = CreateDefaultLevelInfo(_levelId, levelData);
                }
            }
            else
            {
                _levelInfo = CreateDefaultLevelInfo(_levelId, levelData);
            }
        }
        
        public LevelInfo GetLevelInfo()
        {
            return _levelInfo;
        }

        private LevelInfo CreateDefaultLevelInfo(int levelId, LevelData levelData)
        {
            LevelSaveData levelSaveData = new LevelSaveData();
            levelSaveData.LevelId = levelId;
            levelSaveData.TriedCardsList = new List<List<int>>();
            levelSaveData.TargetCards = CreateTargetCards(levelData.NumOfCards, levelData.NumOfBoardHolders);
            levelSaveData.ProbabilityTypes = new List<ProbabilityType>();
            for (int i = 0; i < levelData.NumOfCards; i++)
            {
                levelSaveData.ProbabilityTypes.Add(ProbabilityType.Probable);
            }

            levelSaveData.ActiveHolderIndicatorIndexesList = new List<List<int>>();
            for (int i = 0; i < levelData.NumOfCards; i++)
            {
                List<int> holderIndexes = new List<int>();
                for (int j = 0; j < levelData.NumOfBoardHolders; j++)
                {
                    holderIndexes.Add(j);
                }
                levelSaveData.ActiveHolderIndicatorIndexesList.Add(holderIndexes);
            }

            levelSaveData.LockedCardIndexes = new List<int>();
            levelSaveData.RemainingGuessCount = levelData.MaxNumOfTries;

            return new LevelInfo()
            {
                levelSaveData = levelSaveData,
                levelData = levelData
            };
        }
        
        private List<int> CreateTargetCards(int numOfCards, int numOfBoardHolders)
        {
            List<int> cards = Enumerable.Range(1, numOfCards).ToList();
            Random random = new Random();
            for (int i = numOfCards - 1; i > 0; i--)
            {
                int j = random.Next(0, i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }

            for (int i = 0; i < numOfBoardHolders; i++)
            {
                Debug.Log(cards[i]);
            }
            
            return cards.Take(numOfBoardHolders).ToList();
        }

        public int GetLevelId()
        {
            return _levelId;
        }

        public void SetLevelId(int levelId)
        {
            PlayerPrefs.SetInt("level_id", levelId);
        }

        public List<int> GetStarCountOfLevels()
        {
            return _starCountOfCompletedLevels;
        }

        public void IncrementLevelId(int starCount)
        {
            if (_levelId == _starCountOfCompletedLevels.Count)
            {
                _starCountOfCompletedLevels.Add(starCount);
                _levelId++;
            }
            else
            {
                if (starCount > _starCountOfCompletedLevels[_levelId])
                {
                    _starCountOfCompletedLevels[_levelId] = starCount;
                }
            }
            
            PlayerPrefs.SetInt("level_id", _levelId);
            string data = JsonConvert.SerializeObject(_starCountOfCompletedLevels);
            PlayerPrefs.SetString("star_count_of_levels", data);
        }

        public void AddStar(int addedStarCount)
        {
            if (_starCount % 6 + addedStarCount >= 6) _wildCardCount += 1;
            PlayerPrefs.SetInt("wild_card_count", _wildCardCount);
            _starCount += addedStarCount;
            PlayerPrefs.SetInt("star_count", _starCount);
        }

        public int GetStarCount()
        {
            return _starCount;
        }

        public int GetWildCardCount()
        {
            return _wildCardCount;
        }

        public void DecreaseWildCardCount()
        {
            PlayerPrefs.SetInt("wild_card_count", _wildCardCount - 1);
            _wildCardCount -= 1;
        }
    }

    public interface ILevelTracker
    {
        void Initialize(IGameSaveService gameSaveService);
        LevelInfo GetLevelInfo();
        int GetLevelId();
        void IncrementLevelId(int starCount);
        void AddStar(int addedStarCount);
        int GetStarCount();
        int GetWildCardCount();
        void DecreaseWildCardCount();
        void SetLevelInfo();
        List<int> GetStarCountOfLevels();
        void SetLevelId(int levelId);
    }

    public class LevelData
    {
        public int LevelId;
        public int NumOfBoardHolders;
        public int NumOfCards;
        public int MaxNumOfTries;
        public int NumOfGainedStars;
    }

    public class LevelInfo
    {
        public LevelSaveData levelSaveData;
        public LevelData levelData;
    }
    
}