using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Random = System.Random;
namespace Scripts
{
    public class LevelTracker : MonoBehaviour, ILevelTracker
    {
        private int _blueCrystalCount;
        private int _redCrystalCount;
        private int _yellowCrystalCount;
        private int _revealingPowerUpCount;
        private int _lifePowerUpCount;
        private int _hintPowerUpCount;
        private IGameSaveService _gameSaveService;
        private int _levelId;
        private GameOption _gameOption;
        private Difficulty _multiplayerLevelDifficulty;
        private LevelSaveData _levelSaveData;
        
        
        public void Initialize(IGameSaveService gameSaveService)
        {
            _gameOption = (GameOption)PlayerPrefs.GetInt("game_option", 0);
            _gameSaveService = gameSaveService;
            _levelId = PlayerPrefs.GetInt("level_id", 0);
            _blueCrystalCount = PlayerPrefs.GetInt("blue_crystal_count", 0);
            _redCrystalCount = PlayerPrefs.GetInt("red_crystal_count", 0);
            _yellowCrystalCount = PlayerPrefs.GetInt("yellow_crystal_count", 0);
            _revealingPowerUpCount = PlayerPrefs.GetInt("revealing_power_up_count", 0);
            _lifePowerUpCount = PlayerPrefs.GetInt("life_power_up_count", 0);
            _hintPowerUpCount = PlayerPrefs.GetInt("hint_power_up_count", 0);
            _multiplayerLevelDifficulty = (Difficulty)PlayerPrefs.GetInt("multiplayer_level_difficulty", 2);
        }

        public void SetGameOption(GameOption gameOption)
        {
            PlayerPrefs.SetInt("game_option", (int)gameOption);
            _gameOption = gameOption;
        }

        public GameOption GetGameOption()
        {
            return _gameOption;
        }

        public void SetMultiplayerLevelDifficulty(Difficulty difficulty)
        {
            PlayerPrefs.SetInt("multiplayer_level_difficulty", (int)difficulty);
            _multiplayerLevelDifficulty = difficulty;
        }

        private int GetNumberOfBoardCardsInMultiplayer()
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

        public void ClearPlayerPrefs()
        {
            PlayerPrefs.DeleteKey("blue_crystal_count");
            PlayerPrefs.DeleteKey("red_crystal_count");
            PlayerPrefs.DeleteKey("yellow_crystal_count");
            PlayerPrefs.DeleteKey("revealing_power_up_count");
            PlayerPrefs.DeleteKey("life_power_up_count");
            PlayerPrefs.DeleteKey("hint_power_up_count");
        }

        public bool IsFirstLevelTutorial()
        {
            return _levelId == 0 && PlayerPrefs.GetInt("first_level_tutorial_completed", 0) == 0;
        }

        public bool IsCardInfoTutorial()
        {
            return _levelId == 9 && PlayerPrefs.GetInt("card_info_tutorial_completed", 0) == 0;
        }

        public bool IsWildCardTutorial()
        {
            return false;
            //return _wildCardCount > 0 && PlayerPrefs.GetInt("wild_card_tutorial_completed", 0) == 0;
        }

        public void SetLevelInfo(ITargetNumberCreator targetNumberCreator, ILevelDataCreator levelDataCreator)
        {
            if (_gameOption == GameOption.SinglePlayer)
            {
                levelDataCreator.SetSinglePlayerLevelData(_levelId);
                LevelData levelData = levelDataCreator.GetLevelData();
                LevelSaveData levelSaveData = _gameSaveService.GetSavedLevel();

                if (IsFirstLevelTutorial())
                {
                    _levelSaveData = CreateDefaultLevelSaveData(levelData);
                    targetNumberCreator.SetSavedTargetCardList(new List<int>(){4,1});
                    return;
                }
                if (IsCardInfoTutorial())
                {
                    _levelSaveData = CreateDefaultLevelSaveData(levelData);
                    targetNumberCreator.SetSavedTargetCardList(new List<int>(){4,6});
                    return;
                }
                
                if (levelSaveData != null)
                {
                    if (_levelId == levelSaveData.LevelId)
                    {
                        _levelSaveData = levelSaveData;
                        targetNumberCreator.SetSavedTargetCardList(_levelSaveData.TargetCards);
                    }
                    else
                    {
                        _levelSaveData = CreateDefaultLevelSaveData(levelData);
                        targetNumberCreator.CreateTargetNumber(levelData.NumOfCards, levelData.NumOfBoardHolders);
                    }
                }
                else
                {
                    _levelSaveData = CreateDefaultLevelSaveData(levelData);
                    targetNumberCreator.CreateTargetNumber(levelData.NumOfCards, levelData.NumOfBoardHolders);
                }
            }

            else
            {
                levelDataCreator.SetMultiplayerLevelData(GetNumberOfBoardCardsInMultiplayer());
                LevelData levelData = levelDataCreator.GetLevelData();
                Debug.Log(levelData.LevelId);
                _levelSaveData = CreateDefaultLevelSaveData(levelData);
                targetNumberCreator.CreateTargetNumber(levelData.NumOfCards, levelData.NumOfBoardHolders);
            }

        }
        
        public LevelSaveData GetLevelSaveData()
        {
            return _levelSaveData;
        }

        private LevelSaveData CreateDefaultLevelSaveData(LevelData levelData)
        {
            LevelSaveData levelSaveData = new LevelSaveData();
            levelSaveData.LevelId = levelData.LevelId;
            levelSaveData.TriedCardsList = new List<List<int>>();
            levelSaveData.RemainingGuessCount = levelData.MaxNumOfTries;
            levelSaveData.CardItemInfoList = new List<CardItemInfo>();
            for (int i = 0; i < levelData.NumOfCards; i++)
            {
                CardItemInfo cardItemInfo = new CardItemInfo()
                {
                    possibleCardHolderIndicatorIndexes = GetAllPossibleCardHolderIndicatorIndexes(levelData.NumOfBoardHolders),
                    probabilityType = ProbabilityType.Probable,
                    isLocked = false
                };
                levelSaveData.CardItemInfoList.Add(cardItemInfo);
            }

            return levelSaveData;
        }
        
        private List<int> GetAllPossibleCardHolderIndicatorIndexes(int numOfBoardCardHolders)
        {
            List<int> possibleCardHolderIndexes = new List<int>();
            for (int i = 0; i < numOfBoardCardHolders; i++)
            {
                possibleCardHolderIndexes.Add(i);
            }

            return possibleCardHolderIndexes;
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

        public void IncrementLevelId()
        {
            _levelId++;
            PlayerPrefs.SetInt("level_id", _levelId);
        }

        public void AddCrystals(int crystalCount)
        {
            if (crystalCount > 2)
            {
                _yellowCrystalCount += 1;
                PlayerPrefs.SetInt("yellow_crystal_count", _yellowCrystalCount);
            }
            if (crystalCount > 1)
            {
                _redCrystalCount += 1;
                PlayerPrefs.SetInt("red_crystal_count", _redCrystalCount);
            }
            if (crystalCount > 0)
            {
                _blueCrystalCount += 1;
                PlayerPrefs.SetInt("blue_crystal_count", _blueCrystalCount);
            }
        }

        public int GetBlueCrystalCount()
        {
            return _blueCrystalCount;
        }
        
        public int GetRedCrystalCount()
        {
            return _redCrystalCount;
        }
        
        public int GetYellowCrystalCount()
        {
            return _yellowCrystalCount;
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

        public void DecreaseRevealingPowerUpCount()
        {
            _revealingPowerUpCount -= 1;
            PlayerPrefs.SetInt("revealing_power_up_count", _revealingPowerUpCount);
        }
        
        public void DecreaseLifePowerUpCount()
        {
            _lifePowerUpCount -= 1;
            PlayerPrefs.SetInt("life_power_up_count", _lifePowerUpCount);
        }
        
        public void DecreaseHintPowerUpCount()
        {
            _hintPowerUpCount -= 1;
            PlayerPrefs.SetInt("hint_power_up_count", _hintPowerUpCount);
        }
    }

    public interface ILevelTracker
    {
        void Initialize(IGameSaveService gameSaveService);
        LevelSaveData GetLevelSaveData();
        int GetLevelId();
        void IncrementLevelId();
        void SetLevelInfo(ITargetNumberCreator targetNumberCreator, ILevelDataCreator levelDataCreator);
        void SetLevelId(int levelId);
        void SetGameOption(GameOption gameOption);
        GameOption GetGameOption();
        void SetMultiplayerLevelDifficulty(Difficulty difficulty);
        bool IsFirstLevelTutorial();
        bool IsCardInfoTutorial();
        bool IsWildCardTutorial();
        void AddCrystals(int crystalCount);
        int GetBlueCrystalCount();
        int GetRedCrystalCount();
        int GetYellowCrystalCount();
        int GetRevealingPowerUpCount();
        int GetLifePowerUpCount();
        int GetHintPowerUpCount();
        void DecreaseRevealingPowerUpCount();
        void DecreaseLifePowerUpCount();
        void DecreaseHintPowerUpCount();
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