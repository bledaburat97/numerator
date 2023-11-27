using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;
namespace Scripts
{
    public class LevelTracker : MonoBehaviour, ILevelTracker
    {
        private List<LevelData> _levelDataList = new List<LevelData>();
        private int _levelId;
        private int _starCount;
        private int _wildCardCount;
        private LevelInfo _levelInfo;
        
        public void Initialize(IGameSaveService gameSaveService)
        {
            _levelDataList = LevelDataGetter.GetLevelDataFromJson();
            LevelSaveData levelSaveData = gameSaveService.GetSavedLevel();
            _levelInfo = new LevelInfo();
            if (levelSaveData == null)
            {
                _levelId = PlayerPrefs.GetInt("level_id", 1);
                CreateDefaultLevelInfo(_levelDataList.Find(level => level.LevelId == _levelId));
            }
            else
            {
                _levelId = levelSaveData.LevelId;
                _levelInfo.levelSaveData = levelSaveData;
                _levelInfo.levelData = GetLevelDataOfLevelId(_levelId);
            }
            PlayerPrefs.SetInt("level_id", _levelId);
            
            //PlayerPrefs.SetInt("star_count", 0);
            _starCount = PlayerPrefs.GetInt("star_count", 0);
            _wildCardCount = PlayerPrefs.GetInt("wild_card_count", 0);
        }
        
        public LevelInfo GetLevelInfo()
        {
            return _levelInfo;
        }

        private void CreateDefaultLevelInfo(LevelData levelData)
        {
            LevelSaveData levelSaveData = new LevelSaveData();
            levelSaveData.LevelId = levelData.LevelId;
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

            _levelInfo = new LevelInfo()
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
            return _levelId;
        }

        private LevelData GetLevelDataOfLevelId(int levelId)
        {
            return _levelDataList.Find(level => level.LevelId == levelId);
        }

        public void SetLevelId(int levelId)
        {
            PlayerPrefs.SetInt("level_id", levelId);
            _levelId = levelId;
        }

        public void IncrementLevelId()
        {
            PlayerPrefs.SetInt("level_id", _levelId + 1);
            _levelId += 1;
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
        void SetLevelId(int levelId);
        void IncrementLevelId();
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
    }

    public class LevelInfo
    {
        public LevelSaveData levelSaveData;
        public LevelData levelData;
    }
}