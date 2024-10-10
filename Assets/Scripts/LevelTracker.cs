using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Random = System.Random;
namespace Scripts
{
    public class LevelTracker : MonoBehaviour, ILevelTracker
    {
        private int _starCount;
        private int _giftStarCount;
        private int _revealingPowerUpCount;
        private int _lifePowerUpCount;
        private int _hintPowerUpCount;
        private int _levelId;
        private RewardType _currentRewardType;
        
        private GameOption _gameOption;
        private Difficulty _multiplayerLevelDifficulty;

        private void Awake()
        {
            _levelId = PlayerPrefs.GetInt("level_id", 0);
            _starCount = PlayerPrefs.GetInt("star_count", 0);
            _giftStarCount = PlayerPrefs.GetInt("gift_star_count", 0);
            _revealingPowerUpCount = PlayerPrefs.GetInt("revealing_power_up_count", 0);
            _lifePowerUpCount = PlayerPrefs.GetInt("life_power_up_count", 0);
            _hintPowerUpCount = PlayerPrefs.GetInt("hint_power_up_count", 0);
            _currentRewardType = (RewardType)PlayerPrefs.GetInt("reward_type", 0);
        }
        
        public void ClearPlayerPrefs()
        {
            PlayerPrefs.DeleteKey("star_count");
            PlayerPrefs.DeleteKey("gift_star_count");
            PlayerPrefs.DeleteKey("revealing_power_up_count");
            PlayerPrefs.DeleteKey("life_power_up_count");
            PlayerPrefs.DeleteKey("hint_power_up_count");
        }
        
        public void SavePlayerPrefs()
        {
            PlayerPrefs.SetInt("level_id", _levelId);
            PlayerPrefs.SetInt("star_count", _starCount);
            PlayerPrefs.SetInt("gift_star_count", _giftStarCount);
            PlayerPrefs.SetInt("revealing_power_up_count", _revealingPowerUpCount);
            PlayerPrefs.SetInt("life_power_up_count", _lifePowerUpCount);
            PlayerPrefs.SetInt("hint_power_up_count", _hintPowerUpCount);
            PlayerPrefs.SetInt("reward_type", (int)_currentRewardType);
        }

        public void SetGameOption(GameOption gameOption)
        {
            _gameOption = gameOption;
        }

        public GameOption GetGameOption()
        {
            return _gameOption;
        }

        public void SetMultiplayerLevelDifficulty(Difficulty difficulty)
        {
            _multiplayerLevelDifficulty = difficulty;
        }
        
        public int GetNumberOfBoardCardsInMultiplayer()
        {
            switch (_multiplayerLevelDifficulty)
            {
                case Difficulty.Easy:
                    return 3;
                case Difficulty.Medium:
                    return 4;
                case Difficulty.Hard:
                    return 5;
                default:
                    return 5;
            } 
        }
        
        public void IncrementLevelId(int starCount, int giftStarCount)
        {
            _levelId++;
            _starCount += starCount;
            if (_giftStarCount + giftStarCount >= ConstantValues.NUM_OF_STARS_FOR_WILD)
            {
                int newRewardType = ((int)_currentRewardType + 1) % 3;
                _currentRewardType = (RewardType)newRewardType;
            }
            _giftStarCount = (_giftStarCount + giftStarCount) % ConstantValues.NUM_OF_STARS_FOR_WILD;
        }
        
        public void IncreaseRevealingPowerUpCount()
        {
            _revealingPowerUpCount++;
        }
        
        public void IncreaseLifePowerUpCount()
        {
            _lifePowerUpCount++;
        }
        
        public void IncreaseHintPowerUpCount()
        {
            _hintPowerUpCount++;
        }
        
        public void DecreaseRevealingPowerUpCount()
        {
            _revealingPowerUpCount -= 1;
        }
        
        public void DecreaseLifePowerUpCount()
        {
            _lifePowerUpCount -= 1;
        }
        
        public void DecreaseHintPowerUpCount()
        {
            _hintPowerUpCount -= 1;
        }

        public int GetLevelId()
        {
            return _levelId;
        }

        public int GetGiftStarCount()
        {
            return _giftStarCount;
        }
        
        public int GetStarCount()
        {
            return _starCount;
        }
        
        public int GetRevealingPowerUpCount()
        {
            return _revealingPowerUpCount;
        }

        public int GetLifePowerUpCount()
        {
            return _lifePowerUpCount;
        }

        public int GetHintPowerUpCount()
        {
            return _hintPowerUpCount;
        }

        public RewardType GetCurrentRewardType()
        {
            return _currentRewardType;
        }

        public bool IsFirstLevelTutorial()
        {
            return false; //_levelId == 0 && PlayerPrefs.GetInt("first_level_tutorial_completed", 0) == 0;
        }

        public bool IsCardInfoTutorial()
        {
            return false; //_levelId == 9 && PlayerPrefs.GetInt("card_info_tutorial_completed", 0) == 0;
        }
    }

    public interface ILevelTracker
    {
        void ClearPlayerPrefs();
        void SavePlayerPrefs();
        void SetGameOption(GameOption gameOption);
        GameOption GetGameOption();
        void SetMultiplayerLevelDifficulty(Difficulty difficulty);
        int GetNumberOfBoardCardsInMultiplayer();
        bool IsFirstLevelTutorial();
        bool IsCardInfoTutorial();
        void IncrementLevelId(int starCount, int giftStarCount);
        void IncreaseRevealingPowerUpCount();
        void IncreaseLifePowerUpCount();
        void IncreaseHintPowerUpCount();
        void DecreaseRevealingPowerUpCount();
        void DecreaseLifePowerUpCount();
        void DecreaseHintPowerUpCount();
        int GetLevelId();
        int GetGiftStarCount();
        int GetStarCount();
        int GetRevealingPowerUpCount();
        int GetLifePowerUpCount();
        int GetHintPowerUpCount();
        RewardType GetCurrentRewardType();
    }

    public class LevelData
    {
        public int LevelId;
        public int NumOfBoardHolders;
        public int NumOfCards;
        public int MaxNumOfTries;
    }
    
    public enum GameOption
    {
        SinglePlayer = 0,
        MultiPlayer = 1
    }
    
}