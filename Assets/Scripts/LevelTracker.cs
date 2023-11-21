using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class LevelTracker : MonoBehaviour, ILevelTracker
    {
        private List<LevelData> _levelDataList = new List<LevelData>();
        private int _levelId;
        
        public void Initialize()
        {
            //_levelId = PlayerPrefs.GetInt("level_id", 1);
            _levelId = 1;
            _levelDataList = LevelDataGetter.GetLevelDataFromJson();
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
    }

    public interface ILevelTracker
    {
        void Initialize();
        LevelData GetLevelData();
        int GetLevelId();
        void SetLevelId(int levelId);
        void IncrementLevelId();
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