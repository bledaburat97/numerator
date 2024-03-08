using System;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class GameUIController : IGameUIController
    {
        [Inject] private BaseButtonControllerFactory _baseButtonControllerFactory;

        private IGameUIView _view;
        private ILevelTracker _levelTracker;
        private ITurnOrderDeterminer _turnOrderDeterminer;
        private bool _isCardInfoToggleOn;

        public event EventHandler CheckFinalNumbers;
        public event EventHandler NotAbleToCheck;
        public event EventHandler ResetNumbers;
        public event EventHandler OpenSettings;
        public event EventHandler<bool> CardInfoToggleChanged;
        
        public GameUIController(IGameUIView view)
        {
            _view = view;
        }

        public void Initialize(ILevelTracker levelTracker, ITurnOrderDeterminer turnOrderDeterminer)
        {
            _levelTracker = levelTracker;
            _turnOrderDeterminer = turnOrderDeterminer;

            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                _view.SetLevelId("Level " + (_levelTracker.GetLevelId() + 1));
            }
            else
            {
                _view.DisableLevelId();
                _view.IncreaseSizeAndPositionOfScrollArea(44f);
            }

            IBaseButtonController checkButtonController = _baseButtonControllerFactory.Create(_view.GetCheckButton());
            checkButtonController.Initialize(OnClickCheckButton);

            IBaseButtonController resetButtonController = _baseButtonControllerFactory.Create(_view.GetResetButton());
            resetButtonController.Initialize(OnClickResetButton);
            resetButtonController.SetText("C");

            IBaseButtonController settingsButtonController =
                _baseButtonControllerFactory.Create(_view.GetSettingsButton());
            settingsButtonController.Initialize(OnClickSettings);

            IBaseButtonController cardInfoButtonController =
                _baseButtonControllerFactory.Create(_view.GetCardItemInfoPopupToggleButton());
            cardInfoButtonController.Initialize(OnCardInfoButton);

        }
        
        private void OnClickCheckButton()
        {
            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer || _turnOrderDeterminer.IsLocalTurn())
            {
                CheckFinalNumbers?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                NotAbleToCheck?.Invoke(this, EventArgs.Empty);
            }
        }
        
        private void OnClickResetButton()
        {
            ResetNumbers?.Invoke(this,  null);
        }
        
        private void OnClickSettings()
        {
            OpenSettings?.Invoke(this,  null);
        }

        private void OnCardInfoButton()
        {
            _isCardInfoToggleOn = !_isCardInfoToggleOn;
            CardInfoToggleChanged?.Invoke(this, _isCardInfoToggleOn);
        }

        public RectTransform GetCheckButtonRectTransform()
        {
            return _view.GetCheckButton().GetRectTransform();
        }
        
        public RectTransform GetResetButtonRectTransform()
        {
            return _view.GetResetButton().GetRectTransform();
        }
        
    }

    public interface IGameUIController
    {
        void Initialize(ILevelTracker levelTracker, ITurnOrderDeterminer turnOrderDeterminer);
        event EventHandler CheckFinalNumbers;
        event EventHandler NotAbleToCheck;
        event EventHandler ResetNumbers;
        event EventHandler OpenSettings;
        RectTransform GetCheckButtonRectTransform();
        RectTransform GetResetButtonRectTransform();
        event EventHandler<bool> CardInfoToggleChanged;
    }
}