using UnityEngine;

namespace Scripts
{
    public class GameOptionTracker : MonoBehaviour, IGameOptionTracker
    {
        private GameOption _gameOption;

        public void Initialize()
        {
            _gameOption = (GameOption)PlayerPrefs.GetInt("game_option", 0);
        }

        public void SetGameOption(GameOption gameOption)
        {
            PlayerPrefs.SetInt("game_option", (int)gameOption);
        }

        public GameOption GetGameOption()
        {
            return _gameOption;
        }
    }

    public interface IGameOptionTracker
    {
        void Initialize();
        void SetGameOption(GameOption gameOption);
        GameOption GetGameOption();
    }
}