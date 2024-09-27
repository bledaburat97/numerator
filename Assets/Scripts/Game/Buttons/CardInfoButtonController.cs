using System;

namespace Scripts
{
    public class CardInfoButtonController : ICardInfoButtonController
    {
        private ICardInfoButtonView _view;
        private bool _isCardInfoToggleOn;
        private Action<bool> _cardInfoTogglePressed;
        private bool _isButtonClickable;
        
        public CardInfoButtonController(ICardInfoButtonView view, Action<bool> cardInfoTogglePressed)
        {
            _view = view;
            _isCardInfoToggleOn = false;
            _cardInfoTogglePressed = cardInfoTogglePressed;
            _isButtonClickable = true;
            _view.Init(ChangeCardInfoToggle);
            _view.SetCardInfoToggleStatus(_isCardInfoToggleOn, 0f);
        }

        public void Initialize()
        {
            _isCardInfoToggleOn = false;
            _isButtonClickable = true;
            _view.SetCardInfoToggleStatus(_isCardInfoToggleOn, 0f);
        }
        
        public void SetButtonClickable(bool isClickable)
        {
            _isButtonClickable = isClickable;
        }

        private void ChangeCardInfoToggle()
        {
            if (_isButtonClickable)
            {
                _isCardInfoToggleOn = !_isCardInfoToggleOn;
                _view.SetCardInfoToggleStatus(_isCardInfoToggleOn, 0.2f);
                _cardInfoTogglePressed?.Invoke(_isCardInfoToggleOn);
            }
        }
    }

    public interface ICardInfoButtonController
    {
        void SetButtonClickable(bool isClickable);
        void Initialize();
    }
}