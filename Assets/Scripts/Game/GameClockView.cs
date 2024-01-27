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
        private bool isColorChanged = false;
        private int _vibrationCount = 0;
        private Action _vibrationAction;
        public void Init(Action vibrationAction)
        {
            _vibrationAction = vibrationAction;
        }
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

                if (_gamePlayingTimer < GAME_PLAYING_TIMER_MAX / 4 && !isColorChanged)
                {
                    timerImage.color = ConstantValues.NOT_ABLE_TO_MOVE_TEXT_COLOR;
                    isColorChanged = true;
                }

                if (_gamePlayingTimer < 5f && _gamePlayingTimer > 0f)
                {
                    if (5 - _vibrationCount > _gamePlayingTimer)
                    {
                        _vibrationAction?.Invoke();
                        _vibrationCount++;
                        Debug.Log("a");
                    }
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
            timerImage.color = ConstantValues.ABLE_TO_MOVE_TEXT_COLOR;
            isColorChanged = false;
            _vibrationCount = 0;
        }

        public void RemoveTimer()
        {
            gameObject.SetActive(false);
            _isTimerStarted = false;
        }
        
    }

    public interface IGameClockView
    {
        void Init(Action vibrationAction);
        void StartTimer(Action onTimerEnd);
        void RemoveTimer();
    }
}