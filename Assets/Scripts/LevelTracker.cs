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
        private int _wildCardCount;
        private int _blueStarCount;
        private IGameSaveService _gameSaveService;
        private List<int> _starCountOfCompletedLevels;
        private int _levelId;
        private GameOption _gameOption;
        private Difficulty _multiplayerLevelDifficulty;
        private LevelSaveData _levelSaveData;
        
        public void Initialize(IGameSaveService gameSaveService)
        {
            _gameOption = (GameOption)PlayerPrefs.GetInt("game_option", 0);
            _gameSaveService = gameSaveService;
            _levelId = PlayerPrefs.GetInt("level_id", 0);
            _starCount = PlayerPrefs.GetInt("star_count", 0);
            _blueStarCount = PlayerPrefs.GetInt("blue_star_count", 0);
            _wildCardCount = PlayerPrefs.GetInt("wild_card_count", 0);
            _starCountOfCompletedLevels = JsonConvert.DeserializeObject<List<int>>(PlayerPrefs.GetString("star_count_of_levels", "")) ?? new List<int>();
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
            PlayerPrefs.DeleteKey("level_id");
            PlayerPrefs.DeleteKey("star_count");
            PlayerPrefs.DeleteKey("wild_card_count");
            PlayerPrefs.DeleteKey("star_count_of_levels");
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
            return _wildCardCount > 0 && PlayerPrefs.GetInt("wild_card_tutorial_completed", 0) == 0;
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

        public void AddStar(int addedStarCount, int addedBlueStarCount)
        {
            if (_blueStarCount % 6 + addedBlueStarCount >= 6) _wildCardCount += 1;
            PlayerPrefs.SetInt("wild_card_count", _wildCardCount);
            _starCount += addedStarCount;
            PlayerPrefs.SetInt("star_count", _starCount);
            _blueStarCount += addedBlueStarCount;
            PlayerPrefs.SetInt("blue_star_count", _blueStarCount);
        }

        public int GetStarCount()
        {
            return _starCount;
        }

        public int GetBlueStarCount()
        {
            return _blueStarCount;
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
        LevelSaveData GetLevelSaveData();
        int GetLevelId();
        void IncrementLevelId(int starCount);
        void AddStar(int addedStarCount, int addedBlueStarCount);
        int GetStarCount();
        int GetBlueStarCount();
        int GetWildCardCount();
        void DecreaseWildCardCount();
        void SetLevelInfo(ITargetNumberCreator targetNumberCreator, ILevelDataCreator levelDataCreator);
        List<int> GetStarCountOfLevels();
        void SetLevelId(int levelId);
        void SetGameOption(GameOption gameOption);
        GameOption GetGameOption();
        void SetMultiplayerLevelDifficulty(Difficulty difficulty);
        bool IsFirstLevelTutorial();
        bool IsCardInfoTutorial();
        bool IsWildCardTutorial();
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