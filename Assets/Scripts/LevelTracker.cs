using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Random = System.Random;
namespace Scripts
{
    public class LevelTracker : MonoBehaviour, ILevelTracker
    {
        private List<LevelData> _levelDataList = new List<LevelData>();
        private int _lastLevelId;
        private int _selectedLevelId;
        private int _starCount;
        private int _wildCardCount;
        private LevelInfo _levelInfo;
        private IGameSaveService _gameSaveService;
        private List<int> _starCountOfCompletedLevels;
        
        public void Initialize(IGameSaveService gameSaveService)
        {
            ClearPlayerPrefs();
            _gameSaveService = gameSaveService;
            _levelDataList = LevelDataGetter.GetLevelDataFromJson();
            _levelInfo = new LevelInfo();
            _lastLevelId = PlayerPrefs.GetInt("last_level_id", 0);
            _selectedLevelId = _lastLevelId;
            SetSelectedLevelInfo(_selectedLevelId);
            _starCount = PlayerPrefs.GetInt("star_count", 0);
            _wildCardCount = PlayerPrefs.GetInt("wild_card_count", 0);
            _starCountOfCompletedLevels = JsonConvert.DeserializeObject<List<int>>(PlayerPrefs.GetString("star_count_of_levels", "")) ?? new List<int>();
        }

        public void ClearPlayerPrefs()
        {
            PlayerPrefs.DeleteKey("last_level_id");
            PlayerPrefs.DeleteKey("star_count");
            PlayerPrefs.DeleteKey("wild_card_count");
            PlayerPrefs.DeleteKey("star_count_of_levels");
        }

        public void SetSelectedLevelInfo(int levelId)
        {
            LevelSaveData levelSaveData = _gameSaveService.GetSavedLevel();
            _selectedLevelId = levelId;

            if (levelSaveData != null)
            {
                if (levelId == levelSaveData.LevelId)
                {
                    _levelInfo.levelSaveData = levelSaveData;
                    _levelInfo.levelData = GetLevelDataOfLevelId(_selectedLevelId);
                }
                else
                {
                    _levelInfo = CreateDefaultLevelInfo(levelId);
                }
            }
            else
            {
                _levelInfo = CreateDefaultLevelInfo(levelId);
            }
        }
        
        public LevelInfo GetLevelInfo()
        {
            return _levelInfo;
        }

        private LevelInfo CreateDefaultLevelInfo(int levelId)
        {
            LevelData levelData = _levelDataList.Find(level => level.LevelId == levelId % 30);
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

            return cards.Take(numOfBoardHolders).ToList();
        }

        public int GetLevelId()
        {
            return _selectedLevelId;
        }

        private LevelData GetLevelDataOfLevelId(int levelId)
        {
            return _levelDataList.Find(level => level.LevelId == levelId % 30);
        }

        public void IncrementLevelId(int starCount)
        {
            if (_selectedLevelId == _lastLevelId)
            {
                _lastLevelId ++;
                _starCountOfCompletedLevels.Add(starCount);
            }
            else
            {
                if (starCount > _starCountOfCompletedLevels[_selectedLevelId])
                {
                    _starCountOfCompletedLevels[_selectedLevelId] = starCount;
                }
            }
            
            PlayerPrefs.SetInt("last_level_id", _lastLevelId);
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