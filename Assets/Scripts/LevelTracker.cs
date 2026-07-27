using Game;
using UnityEngine;
namespace Scripts
{
    public class LevelTracker : MonoBehaviour, ILevelTracker
    {
        private int _starCount;
        private int _giftStarCount;
        private int _revealingPowerUpCount;
        private int _lifePowerUpCount;
        private int _bombPowerUpCount;
        private int _levelId;
        private RewardType _currentRewardType;
        
        private GameOption _gameOption;
        private Difficulty _multiplayerLevelDifficulty;

        private void Awake()
        {
            _levelId = PlayerPrefs.GetInt("level_id", 0);
            _starCount = PlayerPrefs.GetInt("star_count", 0);
            _giftStarCount = PlayerPrefs.GetInt("gift_star_count", 0);
            _revealingPowerUpCount = Mathf.Max(0, PlayerPrefs.GetInt("revealing_power_up_count", 0));
            _lifePowerUpCount = Mathf.Max(0, PlayerPrefs.GetInt("life_power_up_count", 0));
            _bombPowerUpCount = Mathf.Max(0, PlayerPrefs.GetInt("bomb_power_up_count", 0));
            _currentRewardType = (RewardType)PlayerPrefs.GetInt("reward_type", 0);

            bool shouldSaveSanitizedCounts =
                _revealingPowerUpCount != PlayerPrefs.GetInt("revealing_power_up_count", 0) ||
                _lifePowerUpCount != PlayerPrefs.GetInt("life_power_up_count", 0) ||
                _bombPowerUpCount != PlayerPrefs.GetInt("bomb_power_up_count", 0);

            if (shouldSaveSanitizedCounts)
            {
                SavePlayerPrefs();
            }
        }
        
        public void ClearPlayerPrefs()
        {
            PlayerPrefs.DeleteKey("star_count");
            PlayerPrefs.DeleteKey("gift_star_count");
            PlayerPrefs.DeleteKey("revealing_power_up_count");
            PlayerPrefs.DeleteKey("life_power_up_count");
            PlayerPrefs.DeleteKey("bomb_power_up_count");
            PlayerPrefs.DeleteKey("hint_power_up_count");
            PlayerPrefs.DeleteKey("reward_type");
        }
        
        public void SavePlayerPrefs()
        {
            PlayerPrefs.SetInt("level_id", _levelId);
            PlayerPrefs.SetInt("star_count", _starCount);
            PlayerPrefs.SetInt("gift_star_count", _giftStarCount);
            PlayerPrefs.SetInt("revealing_power_up_count", _revealingPowerUpCount);
            PlayerPrefs.SetInt("life_power_up_count", _lifePowerUpCount);
            PlayerPrefs.SetInt("bomb_power_up_count", _bombPowerUpCount);
            PlayerPrefs.DeleteKey("hint_power_up_count");
            PlayerPrefs.SetInt("reward_type", (int)_currentRewardType);
            PlayerPrefs.Save();
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
            IncrementLevelIdWithRewards(starCount, giftStarCount);
        }

        public void IncrementLevelIdWithRewards(int coinCount, int crystalCount)
        {
            _levelId++;
            _starCount += coinCount;
            int totalCrystalCount = _giftStarCount + crystalCount;
            while (totalCrystalCount >= ConstantValues.NUM_OF_STARS_FOR_WILD)
            {
                IncreasePowerUpCount(_currentRewardType);
                totalCrystalCount -= ConstantValues.NUM_OF_STARS_FOR_WILD;
                _currentRewardType = GetNextRewardType(_currentRewardType);
            }
            _giftStarCount = totalCrystalCount;
            SavePlayerPrefs();
        }
        
        public void IncreaseRevealingPowerUpCount()
        {
            _revealingPowerUpCount++;
            SavePlayerPrefs();
        }
        
        public void IncreaseLifePowerUpCount()
        {
            _lifePowerUpCount++;
            SavePlayerPrefs();
        }
        
        public void DecreaseRevealingPowerUpCount()
        {
            _revealingPowerUpCount = Mathf.Max(0, _revealingPowerUpCount - 1);
            SavePlayerPrefs();
        }
        
        public void DecreaseLifePowerUpCount()
        {
            _lifePowerUpCount = Mathf.Max(0, _lifePowerUpCount - 1);
            SavePlayerPrefs();
        }
        
        public void IncreaseBombPowerUpCount()
        {
            _bombPowerUpCount++;
            SavePlayerPrefs();
        }

        public void DecreaseBombPowerUpCount()
        {
            _bombPowerUpCount = Mathf.Max(0, _bombPowerUpCount - 1);
            SavePlayerPrefs();
        }

        public int GetLevelId()
        {
            return _levelId;
        }

        public int GetGiftStarCount()
        {
            return _giftStarCount;
        }

        public int GetCrystalProgressCount()
        {
            return GetGiftStarCount();
        }
        
        public int GetStarCount()
        {
            return _starCount;
        }

        public int GetCoinCount()
        {
            return GetStarCount();
        }
        
        public int GetRevealingPowerUpCount()
        {
            return Mathf.Max(0, _revealingPowerUpCount);
        }

        public int GetLifePowerUpCount()
        {
            return Mathf.Max(0, _lifePowerUpCount);
        }

        public int GetBombPowerUpCount()
        {
            return Mathf.Max(0, _bombPowerUpCount);
        }

        public RewardType GetCurrentRewardType()
        {
            return _currentRewardType;
        }

        public int GetPowerUpCount(RewardType rewardType)
        {
            switch (rewardType)
            {
                case RewardType.Revealing:
                    return GetRevealingPowerUpCount();
                case RewardType.Life:
                    return GetLifePowerUpCount();
                case RewardType.Bomb:
                    return GetBombPowerUpCount();
                default:
                    return 0;
            }
        }

        public bool TryConsumePowerUp(RewardType rewardType)
        {
            if (GetPowerUpCount(rewardType) <= 0) return false;

            switch (rewardType)
            {
                case RewardType.Revealing:
                    DecreaseRevealingPowerUpCount();
                    break;
                case RewardType.Life:
                    DecreaseLifePowerUpCount();
                    break;
                case RewardType.Bomb:
                    DecreaseBombPowerUpCount();
                    break;
            }

            return true;
        }

        public bool IsFirstLevelTutorial()
        {
            return false; //_levelId == 0 && PlayerPrefs.GetInt("first_level_tutorial_completed", 0) == 0;
        }

        public bool IsCardInfoTutorial()
        {
            return false; //_levelId == 9 && PlayerPrefs.GetInt("card_info_tutorial_completed", 0) == 0;
        }

        private void IncreasePowerUpCount(RewardType rewardType)
        {
            switch (rewardType)
            {
                case RewardType.Revealing:
                    _revealingPowerUpCount++;
                    break;
                case RewardType.Life:
                    _lifePowerUpCount++;
                    break;
                case RewardType.Bomb:
                    _bombPowerUpCount++;
                    break;
            }
        }

        private static RewardType GetNextRewardType(RewardType rewardType)
        {
            return (RewardType)(((int)rewardType + 1) % 3);
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
        void IncrementLevelIdWithRewards(int coinCount, int crystalCount);
        void IncreaseRevealingPowerUpCount();
        void IncreaseLifePowerUpCount();
        void IncreaseBombPowerUpCount();
        void DecreaseRevealingPowerUpCount();
        void DecreaseLifePowerUpCount();
        void DecreaseBombPowerUpCount();
        int GetLevelId();
        int GetGiftStarCount();
        int GetCrystalProgressCount();
        int GetStarCount();
        int GetCoinCount();
        int GetRevealingPowerUpCount();
        int GetLifePowerUpCount();
        int GetBombPowerUpCount();
        int GetPowerUpCount(RewardType rewardType);
        bool TryConsumePowerUp(RewardType rewardType);
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
