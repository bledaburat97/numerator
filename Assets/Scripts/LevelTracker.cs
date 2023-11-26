using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class LevelTracker : MonoBehaviour, ILevelTracker
    {
        private List<LevelData> _levelDataList = new List<LevelData>();
        private int _levelId;
        private int _starCount;
        private int _wildCardCount;
        
        public void Initialize()
        {
            SetLevelId(1);
            _levelId = PlayerPrefs.GetInt("level_id", 1);
            PlayerPrefs.SetInt("level_id", _levelId);
            _levelDataList = LevelDataGetter.GetLevelDataFromJson();
            //PlayerPrefs.SetInt("star_count", 0);
            _starCount = PlayerPrefs.GetInt("star_count", 0);
            _wildCardCount = PlayerPrefs.GetInt("wild_card_count", 0);
        }

        public int GetLevelId()
        {
            return _levelId;
        }

        public LevelData GetLevelData()
        {
            return _levelDataList.Find(level => level.LevelId == _levelId);
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
        void Initialize();
        LevelData GetLevelData();
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
        //public List<int> TargetNumbers; //TODO: set by random.
    }
}