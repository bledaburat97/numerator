﻿using System;
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

            if (_levelTracker.GetLevelId() > 8)
            {
                ICardInfoButtonController cardInfoButtonController = new CardInfoButtonController(_view.GetCardInfoButton());
                cardInfoButtonController.Initialize(OnCardInfoButton);
            }
            else
            {
                _view.GetCardInfoButton().SetActive(false);
            }
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

        private void OnCardInfoButton(bool isCardInfoToggleOn)
        {
            CardInfoToggleChanged?.Invoke(this, isCardInfoToggleOn);
        }

        public RectTransform GetCheckButtonRectTransform()
        {
            return _view.GetCheckButton().GetRectTransform();
        }
        
        public RectTransform GetResetButtonRectTransform()
        {
            return _view.GetResetButton().GetRectTransform();
        }
        
        public RectTransform GetCardInfoButtonRectTransform()
        {
            return _view.GetCardInfoButton().GetRectTransform();
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
        RectTransform GetCardInfoButtonRectTransform();

        event EventHandler<bool> CardInfoToggleChanged;
    }
}