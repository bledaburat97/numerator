using Game;
using UnityEngine;
namespace Scripts
{
    public class LevelTracker : MonoBehaviour, ILevelTracker
    {
#if UNITY_EDITOR
        public bool SuppressPersistenceForTests { get; set; }
#endif
        private int _starCount;
        private int _giftStarCount;
        private int _revealingPowerUpCount;
        private int _lifePowerUpCount;
        private int _bombPowerUpCount;
        private int _helperPowerUpCount;
        private int _pairPowerUpCount;
        private int _splitPowerUpCount;
        private int _levelId;
        private RewardType _currentRewardType;
        
        private GameOption _gameOption;
        private Difficulty _multiplayerLevelDifficulty;

        private void Awake()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (MultiplayerNetworkProbe.IsRunning) { _levelId = MenuProgression.MultiplayerUnlockLevel - 1; _gameOption = GameOption.MultiPlayer; return; }
#endif
            _levelId = PlayerPrefs.GetInt("level_id", 0);
            TutorialProgress.RepairIncompleteFirstLevel(_levelId);
            _starCount = PlayerPrefs.GetInt("star_count", 0);
            _giftStarCount = PlayerPrefs.GetInt("gift_star_count", 0);
            _revealingPowerUpCount = Mathf.Max(0, PlayerPrefs.GetInt("revealing_power_up_count", 0));
            _lifePowerUpCount = Mathf.Max(0, PlayerPrefs.GetInt("life_power_up_count", 0));
            _bombPowerUpCount = Mathf.Max(0, PlayerPrefs.GetInt("bomb_power_up_count", 0));
            _helperPowerUpCount = Mathf.Max(0, PlayerPrefs.GetInt("helper_power_up_count", 0));
            _pairPowerUpCount = Mathf.Max(0, PlayerPrefs.GetInt("pair_power_up_count", 0));
            _splitPowerUpCount = Mathf.Max(0, PlayerPrefs.GetInt("split_power_up_count", 0));
            _currentRewardType = (RewardType)PlayerPrefs.GetInt("reward_type", 0);

            if (!System.Enum.IsDefined(typeof(RewardType), _currentRewardType)) _currentRewardType = RewardType.Revealing;

            bool shouldSaveSanitizedCounts =
                _revealingPowerUpCount != PlayerPrefs.GetInt("revealing_power_up_count", 0) ||
                _lifePowerUpCount != PlayerPrefs.GetInt("life_power_up_count", 0) ||
                _bombPowerUpCount != PlayerPrefs.GetInt("bomb_power_up_count", 0) ||
                _helperPowerUpCount != PlayerPrefs.GetInt("helper_power_up_count", 0) ||
                _pairPowerUpCount != PlayerPrefs.GetInt("pair_power_up_count", 0) ||
                _splitPowerUpCount != PlayerPrefs.GetInt("split_power_up_count", 0);

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
            PlayerPrefs.DeleteKey("helper_power_up_count");
            PlayerPrefs.DeleteKey("pair_power_up_count");
            PlayerPrefs.DeleteKey("split_power_up_count");
            PlayerPrefs.DeleteKey("reward_type");
        }
        
        public void SavePlayerPrefs()
        {
#if UNITY_EDITOR
            if (SuppressPersistenceForTests) return;
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (MultiplayerNetworkProbe.IsRunning) return;
#endif
            PlayerPrefs.SetInt("level_id", _levelId);
            PlayerPrefs.SetInt("star_count", _starCount);
            PlayerPrefs.SetInt("gift_star_count", _giftStarCount);
            PlayerPrefs.SetInt("revealing_power_up_count", _revealingPowerUpCount);
            PlayerPrefs.SetInt("life_power_up_count", _lifePowerUpCount);
            PlayerPrefs.SetInt("bomb_power_up_count", _bombPowerUpCount);
            PlayerPrefs.SetInt("helper_power_up_count", _helperPowerUpCount);
            PlayerPrefs.SetInt("pair_power_up_count", _pairPowerUpCount);
            PlayerPrefs.SetInt("split_power_up_count", _splitPowerUpCount);
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
            return MultiplayerRules.For(_multiplayerLevelDifficulty).BoardCount;
        }
        
        public void IncrementLevelId(int starCount, int giftStarCount)
        {
            IncrementLevelIdWithRewards(starCount, giftStarCount);
        }

        public void IncrementLevelIdWithRewards(int coinCount, int crystalCount)
        {
            // Normalize legacy locked rewards before advancing across an unlock boundary.
            // The first awarded item must match the preview shown in the completed level.
            _currentRewardType = GetCurrentRewardType();
            _levelId++;
            _starCount += coinCount;
            int totalCrystalCount = _giftStarCount + crystalCount;
            while (totalCrystalCount >= ConstantValues.NUM_OF_STARS_FOR_WILD)
            {
                if (!PowerUpTypeUtility.IsUnlocked(_currentRewardType, _levelId)) _currentRewardType = GetNextRewardType(_currentRewardType);
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
            GameTelemetry.Powerup(RewardType.Revealing, true, "duel");
            SavePlayerPrefs();
        }
        
        public void IncreaseLifePowerUpCount()
        {
            _lifePowerUpCount++;
            GameTelemetry.Powerup(RewardType.Life, true, "duel");
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
            GameTelemetry.Powerup(RewardType.Bomb, true, "duel");
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
            return PowerUpTypeUtility.IsUnlocked(_currentRewardType, _levelId) ? _currentRewardType : GetNextRewardType(_currentRewardType);
        }

        public int GetPowerUpCount(RewardType rewardType)
        {
            switch (rewardType)
            {
                case RewardType.Revealing:
                    return GetRevealingPowerUpCount();
                case RewardType.Life:
                    return GetLifePowerUpCount();
                case RewardType.Pair: return Mathf.Max(0, _pairPowerUpCount);
                case RewardType.Split: return Mathf.Max(0, _splitPowerUpCount);
                case RewardType.Helper: return Mathf.Max(0, _helperPowerUpCount);
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
                case RewardType.Pair:
                    _pairPowerUpCount = Mathf.Max(0, _pairPowerUpCount - 1); SavePlayerPrefs();
                    break;
                case RewardType.Split:
                    _splitPowerUpCount = Mathf.Max(0, _splitPowerUpCount - 1); SavePlayerPrefs();
                    break;
                case RewardType.Helper:
                    _helperPowerUpCount = Mathf.Max(0, _helperPowerUpCount - 1); SavePlayerPrefs();
                    break;
                case RewardType.Bomb:
                    DecreaseBombPowerUpCount();
                    break;
            }

            GameTelemetry.Powerup(rewardType, false, "single");
            return true;
        }

        public bool IsFirstLevelTutorial()
        {
            return _gameOption == GameOption.SinglePlayer && _levelId == 0;
        }

        public bool IsCardInfoTutorial()
        {
            return false; //_levelId == 9 && PlayerPrefs.GetInt("card_info_tutorial_completed", 0) == 0;
        }

        private void IncreasePowerUpCount(RewardType rewardType, string source = "crystal")
        {
            GameTelemetry.Powerup(rewardType, true, source);
            switch (rewardType)
            {
                case RewardType.Revealing:
                    _revealingPowerUpCount++;
                    break;
                case RewardType.Life:
                    _lifePowerUpCount++;
                    break;
                case RewardType.Pair: _pairPowerUpCount++; break;
                case RewardType.Split: _splitPowerUpCount++; break;
                case RewardType.Helper: _helperPowerUpCount++; break;
                case RewardType.Bomb:
                    _bombPowerUpCount++;
                    break;
            }
        }

        public void EnsurePlaytestPowerUp(RewardType type)
        {
            if (_gameOption != GameOption.SinglePlayer || !LevelPlaytestSchedule.IsActive(_levelId) || GetPowerUpCount(type) > 0) return;
            IncreasePowerUpCount(type, "playtest");
            SavePlayerPrefs();
        }

        private RewardType GetNextRewardType(RewardType rewardType)
        {
            return PowerUpTypeUtility.NextReward(rewardType, _levelId);
        }

        public void GrantIntroducedPowerUps()
        {
#if UNITY_EDITOR
            if (SuppressPersistenceForTests) return;
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (MultiplayerNetworkProbe.IsRunning) return;
#endif
            foreach (var type in PowerUpTypeUtility.OrderedRewardTypes)
            {
                string key = "powerup_intro_granted_" + (int)type;
                if (!PowerUpTypeUtility.IsUnlocked(type, _levelId) || PlayerPrefs.GetInt(key, 0) != 0) continue;
                IncreasePowerUpCount(type, "introduction");
                PlayerPrefs.SetInt(key, 1);
            }
            SavePlayerPrefs();
        }
    }

    public interface ILevelTracker
    {
        void GrantIntroducedPowerUps();
        void EnsurePlaytestPowerUp(RewardType type);
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
        public LevelMechanic Mechanic;
        public int PlaytestRevision;
        public int PlaytestStep;
        public int PlaytestStepCount;
        public RewardType? TrialPowerUp;
        [Newtonsoft.Json.JsonIgnore] public bool IsPlaytest => PlaytestRevision > 0;
        // Every definition field is a value type; never mutate a shared resource or save definition.
        public LevelData Copy() => (LevelData)MemberwiseClone();
    }
    
    public enum GameOption
    {
        SinglePlayer = 0,
        MultiPlayer = 1
    }
    
}
