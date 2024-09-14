using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class GameUIController : IGameUIController
    {
        [Inject] private BaseButtonControllerFactory _baseButtonControllerFactory;
        
        private IGameUIView _view;
        private ICardInfoButtonController _cardInfoButtonController;

        private Dictionary<GameUIButtonType, IBaseButtonController> _buttonDictionary;
        
        public GameUIController(IGameUIView view)
        {
            _view = view;
            _buttonDictionary = new Dictionary<GameUIButtonType, IBaseButtonController>();
        }

        public void SetUserText(string text)
        {
            _view.SetUserText(text);
        }

        public void SetOpponentText(string text)
        {
            _view.SetOpponentText(text);
        }

        public void SetOpponentInfoStatus(bool status)
        {
            _view.SetOpponentInfoStatus(status);
        }
        
        public void SetPowerUpImages()
        {
            _view.SetImagesOfPowerUpButtons();
        }

        public void CreateGameUiButtons(Action<GameUIButtonType> buttonClickAction)
        {
            CreateButtonController(_view.GetCheckButton(), GameUIButtonType.Check, buttonClickAction);
            CreateButtonController(_view.GetResetButton(), GameUIButtonType.Reset, buttonClickAction);
            CreateButtonController(_view.GetSettingsButton(), GameUIButtonType.Settings, buttonClickAction);
            CreateButtonController(_view.GetRevealingPowerUpButton(), GameUIButtonType.RevealingPowerUp, buttonClickAction);
            CreateButtonController(_view.GetLifePowerUpButton(), GameUIButtonType.LifePowerUp, buttonClickAction);
            CreateButtonController(_view.GetHintPowerUpButton(), GameUIButtonType.HintPowerUp, buttonClickAction);
        }

        public void CreateCardInfoButton(Action<bool> onClickAction)
        {
            _cardInfoButtonController = new CardInfoButtonController(_view.GetCardInfoButton(), onClickAction);
        }

        public void SetCardInfoButtonStatus(bool status)
        {
            _view.GetCardInfoButton().SetActive(status);
        }

        private void CreateButtonController(IBaseButtonView baseButtonView, GameUIButtonType buttonType, Action<GameUIButtonType> onClickAction)
        {
            IBaseButtonController buttonController = _baseButtonControllerFactory.Create(baseButtonView, () => onClickAction(buttonType));
            _buttonDictionary.Add(buttonType, buttonController);
        }

        public void SetAllButtonsUnclickable()
        {
            foreach (var pair in _buttonDictionary)
            {
                pair.Value.SetButtonClickable(false);
            }
            _cardInfoButtonController.SetButtonClickable(false);
        }

        public void SetButtonClickable(bool isClickable, GameUIButtonType type)
        {
            if (type == GameUIButtonType.CardInfo)
            {
                _cardInfoButtonController.SetButtonClickable(isClickable);
            }
            else
            {
                _buttonDictionary[type].SetButtonClickable(isClickable);
            }
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
    
    public enum GameUIButtonType
    {
        Settings,
        CardInfo,
        Check,
        Reset,
        RevealingPowerUp,
        LifePowerUp,
        HintPowerUp
    }

    public interface IGameUIController
    {
        void SetPowerUpImages();
        void CreateGameUiButtons(Action<GameUIButtonType> buttonClickAction);
        void CreateCardInfoButton(Action<bool> onClickAction);
        void SetCardInfoButtonStatus(bool status);
        RectTransform GetCheckButtonRectTransform();
        RectTransform GetResetButtonRectTransform();
        RectTransform GetCardInfoButtonRectTransform();
        void SetUserText(string text);
        void SetOpponentText(string text);
        void SetAllButtonsUnclickable();
        void SetButtonClickable(bool isClickable, GameUIButtonType type);
        void SetOpponentInfoStatus(bool status);
    }
}