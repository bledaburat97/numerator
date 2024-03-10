using System;

namespace Scripts
{
    public class CardInfoButtonController : ICardInfoButtonController
    {
        private ICardInfoButtonView _view;
        private bool _isCardInfoToggleOn;
        private Action<bool> _cardInfoTogglePressed;
        public CardInfoButtonController(ICardInfoButtonView view)
        {
            _view = view;
        }
        
        public void Initialize(Action<bool> cardInfoTogglePressed)
        {
            _isCardInfoToggleOn = false;
            _cardInfoTogglePressed = cardInfoTogglePressed;
            _view.Init(ChangeCardInfoToggle);
            _view.SetCardInfoToggleStatus(_isCardInfoToggleOn, 0f);
        }

        private void ChangeCardInfoToggle()
        {
            _isCardInfoToggleOn = !_isCardInfoToggleOn;
            _view.SetCardInfoToggleStatus(_isCardInfoToggleOn, 0.2f);
            _cardInfoTogglePressed?.Invoke(_isCardInfoToggleOn);
        }
    }

    public interface ICardInfoButtonController
    {
        void Initialize(Action<bool> cardInfoTogglePressed);
    }
}