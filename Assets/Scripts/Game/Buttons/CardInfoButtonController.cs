using System;

namespace Scripts
{
    public class CardInfoButtonController : ICardInfoButtonController
    {
        private ICardInfoButtonView _view;
        private bool _isCardInfoToggleOn;
        private Action<bool> _cardInfoTogglePressed;
        private ITutorialAbilityManager _tutorialAbilityManager;
        
        public CardInfoButtonController(ICardInfoButtonView view)
        {
            _view = view;
        }
        
        public void Initialize(ITutorialAbilityManager tutorialAbilityManager, Action<bool> cardInfoTogglePressed)
        {
            _tutorialAbilityManager = tutorialAbilityManager;
            _isCardInfoToggleOn = false;
            _cardInfoTogglePressed = cardInfoTogglePressed;
            _view.Init(ChangeCardInfoToggle);
            _view.SetCardInfoToggleStatus(_isCardInfoToggleOn, 0f);
        }

        private void ChangeCardInfoToggle()
        {
            if (_tutorialAbilityManager.IsCardInfoButtonClickable())
            {
                _isCardInfoToggleOn = !_isCardInfoToggleOn;
                _view.SetCardInfoToggleStatus(_isCardInfoToggleOn, 0.2f);
                _cardInfoTogglePressed?.Invoke(_isCardInfoToggleOn);
            }
        }
    }

    public interface ICardInfoButtonController
    {
        void Initialize(ITutorialAbilityManager tutorialAbilityManager, Action<bool> cardInfoTogglePressed);
    }
}