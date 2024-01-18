using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class GameClockView : MonoBehaviour, IGameClockView
    {
        [SerializeField] private Image timerImage;
        [SerializeField] private TMP_Text timeText;
            
        private bool _isTimerStarted = false;
        private float _gamePlayingTimer;
        private const float GAME_PLAYING_TIMER_MAX = 60f;
        private Action _onTimerEnd;
        
        void Update()
        {
            if (_isTimerStarted)
            {
                _gamePlayingTimer -= Time.deltaTime;
                int remainingTime = (int)_gamePlayingTimer;
                if (_gamePlayingTimer < 0f)
                {
                    RemoveTimer();
                    _gamePlayingTimer = GAME_PLAYING_TIMER_MAX;
                    _onTimerEnd.Invoke();
                }
                timerImage.fillAmount = _gamePlayingTimer / GAME_PLAYING_TIMER_MAX;
                timeText.text = remainingTime.ToString();
            }
        }

        public void StartTimer(Action onTimerEnd)
        {
            gameObject.SetActive(true);
            _isTimerStarted = true;
            _gamePlayingTimer = GAME_PLAYING_TIMER_MAX;
            _onTimerEnd = onTimerEnd;
        }

        public void RemoveTimer()
        {
            gameObject.SetActive(false);
            _isTimerStarted = false;
        }
        
    }

    public interface IGameClockView
    {
        void StartTimer(Action onTimerEnd);
        void RemoveTimer();
    }
}