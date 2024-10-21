using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class GameUIController : IGameUIController
    {
        private BaseButtonControllerFactory _baseButtonControllerFactory;
        private ILevelTracker _levelTracker;
        private ITurnOrderDeterminer _turnOrderDeterminer;
        private IHapticController _hapticController;
        private IGameUIView _view;
        private ICardInfoButtonController _cardInfoButtonController;

        private Dictionary<GameUIButtonType, IBaseButtonController> _buttonDictionary;
        
        public event EventHandler CheckFinalNumbers;
        public event EventHandler NotAbleToCheck;
        public event EventHandler ResetNumbers;
        public event EventHandler OpenSettings;
        public event EventHandler<GameUIButtonType> PowerUpClickedEvent;
        public event EventHandler<bool> CardInfoToggleChanged;
        
        [Inject]
        public GameUIController(BaseButtonControllerFactory baseButtonControllerFactory, ILevelTracker levelTracker, ITurnOrderDeterminer turnOrderDeterminer, IHapticController hapticController, IGameUIView view)
        {
            _view = view;
            _buttonDictionary = new Dictionary<GameUIButtonType, IBaseButtonController>();
            _baseButtonControllerFactory = baseButtonControllerFactory;
            _levelTracker = levelTracker;
            _turnOrderDeterminer = turnOrderDeterminer;
            _hapticController = hapticController;
            CreateGameUiButtons(OnButtonClick);
        }

        public void Initialize(bool isNewGame)
        {
            SetUserText("Level " + (_levelTracker.GetLevelId() + 1));
            SetOpponentInfoStatus(false);
            if (_levelTracker.GetLevelId() > 8)
            {
                SetCardInfoButtonStatus(true);
                InitializeCardInfoButton(OnCardInfoButtonClick);
            }
            else
            {
                SetCardInfoButtonStatus(false);
            }
            _view.GetTopButtonsCanvasGroup().alpha = isNewGame ? 0 : 1;
            _view.GetMiddleButtonsCanvasGroup().alpha = isNewGame ? 0 : 1;
        }

        public Sequence FadeInTopButtons(float duration)
        {
            return DOTween.Sequence().Append(_view.GetTopButtonsCanvasGroup().DOFade(1f, duration));
        }
        
        public Sequence FadeInMiddleButtons(float duration)
        {
            return DOTween.Sequence().Append(_view.GetMiddleButtonsCanvasGroup().DOFade(1f, duration));
        }

        public void InitializeForMultiplayer()
        {
            SetUserText(MultiplayerManager.Instance.GetPlayerDataFromPlayerIndex(0).playerName.ToString());
            SetOpponentText(MultiplayerManager.Instance.GetPlayerDataFromPlayerIndex(1).playerName.ToString());
            SetOpponentInfoStatus(true);
            SetCardInfoButtonStatus(true);
            InitializeCardInfoButton(OnCardInfoButtonClick);
        }

        private void SetUserText(string text)
        {
            _view.SetUserText(text);
        }

        private void SetOpponentText(string text)
        {
            _view.SetOpponentText(text);
        }

        private void SetOpponentInfoStatus(bool status)
        {
            _view.SetOpponentInfoStatus(status);
        }

        private void CreateGameUiButtons(Action<GameUIButtonType> buttonClickAction)
        {
            CreateButtonController(_view.GetCheckButton(), GameUIButtonType.Check, buttonClickAction);
            CreateButtonController(_view.GetResetButton(), GameUIButtonType.Reset, buttonClickAction);
            CreateButtonController(_view.GetSettingsButton(), GameUIButtonType.Settings, buttonClickAction);
            CreateButtonController(_view.GetRevealingPowerUpButton(), GameUIButtonType.RevealingPowerUp, buttonClickAction);
            CreateButtonController(_view.GetLifePowerUpButton(), GameUIButtonType.LifePowerUp, buttonClickAction);
            CreateButtonController(_view.GetHintPowerUpButton(), GameUIButtonType.BombPowerUp, buttonClickAction);
        }

        private void InitializeCardInfoButton(Action<bool> onClickAction)
        {
            if (_cardInfoButtonController != null)
            {
                _cardInfoButtonController.Initialize();
            }
            _cardInfoButtonController = new CardInfoButtonController(_view.GetCardInfoButton(), onClickAction);
        }

        private void SetCardInfoButtonStatus(bool status)
        {
            _view.GetCardInfoButton().SetActive(status);
        }

        private void CreateButtonController(IBaseButtonView baseButtonView, GameUIButtonType buttonType, Action<GameUIButtonType> onClickAction)
        {
            if (_buttonDictionary.ContainsKey(buttonType)) return;
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
        
        public IBaseButtonController GetButton(GameUIButtonType buttonType)
        {
            return _buttonDictionary[buttonType];
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
        
        private void OnButtonClick(GameUIButtonType buttonType)
        {
            switch (buttonType)
            {
                case GameUIButtonType.Check:
                    if (_levelTracker.GetGameOption() == GameOption.SinglePlayer || _turnOrderDeterminer.IsLocalTurn())
                    {
                        CheckFinalNumbers?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        NotAbleToCheck?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case GameUIButtonType.Reset:
                    ResetNumbers?.Invoke(this,  EventArgs.Empty);
                    break;
                case GameUIButtonType.Settings:
                    OpenSettings?.Invoke(this,  EventArgs.Empty);
                    break;
                case GameUIButtonType.RevealingPowerUp:
                    PowerUpClickedEvent?.Invoke(this, GameUIButtonType.RevealingPowerUp);
                    break;
                case GameUIButtonType.LifePowerUp:
                    PowerUpClickedEvent?.Invoke(this, GameUIButtonType.LifePowerUp);
                    break;
                case GameUIButtonType.BombPowerUp:
                    PowerUpClickedEvent?.Invoke(this, GameUIButtonType.BombPowerUp);
                    break;
                default:
                    break;
            }
        }

        private void OnCardInfoButtonClick(bool isCardInfoToggleOn)
        {
            _hapticController.Vibrate(HapticType.ButtonClick);
            CardInfoToggleChanged?.Invoke(this, isCardInfoToggleOn);
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
        BombPowerUp,
        Default
    }

    public interface IGameUIController
    {
        void Initialize(bool isNewGame);
        void InitializeForMultiplayer();
        RectTransform GetCheckButtonRectTransform();
        RectTransform GetResetButtonRectTransform();
        RectTransform GetCardInfoButtonRectTransform();
        void SetAllButtonsUnclickable();
        void SetButtonClickable(bool isClickable, GameUIButtonType type);
        Sequence FadeInTopButtons(float duration);
        Sequence FadeInMiddleButtons(float duration);
        IBaseButtonController GetButton(GameUIButtonType buttonType);
        event EventHandler CheckFinalNumbers;
        event EventHandler NotAbleToCheck;
        event EventHandler ResetNumbers;
        event EventHandler OpenSettings;
        event EventHandler<GameUIButtonType> PowerUpClickedEvent;
        event EventHandler<bool> CardInfoToggleChanged;
    }
}