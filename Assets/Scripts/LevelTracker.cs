using Game;
using System;
using System.Collections.Generic;
using Scripts.Economy;
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
        private int _ultraPowerUpCount;
        private int _levelId;
        private RewardType _currentRewardType;
        private HashSet<RewardType> _introductionGrants = new HashSet<RewardType>();
        private IPlayerProfileRepository _profiles;
        
        private GameOption _gameOption;
        private Difficulty _multiplayerLevelDifficulty;

        private void Awake()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (MultiplayerNetworkProbe.IsRunning) { ConnectProfile();ApplyProfile(_profiles.Snapshot());_gameOption = GameOption.MultiPlayer;return; }
#endif
            ConnectProfile();
            var profile = _profiles.Snapshot();
            ApplyProfile(profile);
            TutorialProgress.RepairIncompleteFirstLevel(_levelId);
            PlayerProfileRuntime.MirrorLegacy(profile);
        }
        
        public void ClearPlayerPrefs()
        {
            _starCount=_giftStarCount=_revealingPowerUpCount=_lifePowerUpCount=_bombPowerUpCount=0;
            _helperPowerUpCount=_pairPowerUpCount=_splitPowerUpCount=_ultraPowerUpCount=0;
            _currentRewardType=RewardType.Revealing;
            SavePlayerPrefs();
        }
        
        public void SavePlayerPrefs()
        {
#if UNITY_EDITOR
            if (SuppressPersistenceForTests) return;
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (MultiplayerNetworkProbe.IsRunning) return;
#endif
            ConnectProfile();
            var wallet=new PlayerWalletData {Coins=_starCount,CrystalProgress=_giftStarCount,NextReward=_currentRewardType,
                IntroductionGrants=new HashSet<RewardType>(_introductionGrants)};
            foreach(RewardType type in Enum.GetValues(typeof(RewardType)))wallet.PowerUps[type]=GetPowerUpCount(type);
            int level=_levelId;
            try { _profiles.Update(profile=>{profile.Progress.LevelId=level;profile.Wallet=wallet;}); }
            catch { ApplyProfile(_profiles.Snapshot());throw; }
        }

        private void ConnectProfile()
        {
            if(_profiles!=null)return;
            _profiles=PlayerProfileRuntime.Current;
            _profiles.Updated+=OnProfileChanged;
        }
        private void OnProfileChanged(ProfileChange change){if(change.Includes(ProfileArea.Progress|ProfileArea.Wallet))ApplyProfile(_profiles.Snapshot());}
        private void OnDestroy() { if(_profiles!=null)_profiles.Updated-=OnProfileChanged; }
        private void ApplyProfile(PlayerProfileData profile)
        {
            _levelId=profile.Progress.LevelId;_starCount=profile.Wallet.Coins;_giftStarCount=profile.Wallet.CrystalProgress;
            _currentRewardType=profile.Wallet.NextReward;
            _revealingPowerUpCount=profile.Wallet.PowerUps[RewardType.Revealing];_lifePowerUpCount=profile.Wallet.PowerUps[RewardType.Life];
            _bombPowerUpCount=profile.Wallet.PowerUps[RewardType.Bomb];_helperPowerUpCount=profile.Wallet.PowerUps[RewardType.Helper];
            _pairPowerUpCount=profile.Wallet.PowerUps[RewardType.Pair];_splitPowerUpCount=profile.Wallet.PowerUps[RewardType.Split];
            _ultraPowerUpCount=profile.Wallet.PowerUps[RewardType.Ultra];
            _introductionGrants=new HashSet<RewardType>(profile.Wallet.IntroductionGrants);
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
            var state=new PlayerProfileData();state.Progress.LevelId=_levelId;
            state.Wallet.Coins=_starCount;state.Wallet.CrystalProgress=_giftStarCount;state.Wallet.NextReward=_currentRewardType;
            state.Wallet.IntroductionGrants=new HashSet<RewardType>(_introductionGrants);
            foreach(RewardType type in Enum.GetValues(typeof(RewardType)))state.Wallet.PowerUps[type]=GetPowerUpCount(type);
            var grants=PlayerProfileRuntime.CreateRewardRules().ApplySingle(state,coinCount,crystalCount);
            ApplyProfile(state);
            SavePlayerPrefs();
            foreach(var type in grants)GameTelemetry.Powerup(type,true,"crystal");
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
        
        
        
        public void IncreaseBombPowerUpCount()
        {
            _bombPowerUpCount++;
            GameTelemetry.Powerup(RewardType.Bomb, true, "duel");
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
                case RewardType.Ultra: return Mathf.Max(0, _ultraPowerUpCount);
                case RewardType.Bomb:
                    return GetBombPowerUpCount();
                default:
                    return 0;
            }
        }

        public bool IsFirstLevelTutorial()
        {
            if(_gameOption!=GameOption.SinglePlayer)return false;
            if(LevelPlaytestSchedule.TryCreate(_levelId,out var level) && level.Lesson!=TutorialLessonId.None)
                return level.Lesson==TutorialLessonId.Basics;
            return _levelId==0;
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
                case RewardType.Ultra: _ultraPowerUpCount++; break;
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
                if (!PowerUpTypeUtility.IsUnlocked(type, _levelId) || _introductionGrants.Contains(type)) continue;
                IncreasePowerUpCount(type, "introduction");
                _introductionGrants.Add(type);
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
        int GetLevelId();
        int GetGiftStarCount();
        int GetCrystalProgressCount();
        int GetStarCount();
        int GetCoinCount();
        int GetRevealingPowerUpCount();
        int GetLifePowerUpCount();
        int GetBombPowerUpCount();
        int GetPowerUpCount(RewardType rewardType);
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
        public RewardType? BonusReward;
        public TutorialLessonId Lesson;
        public bool DisablePowerUps;
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
