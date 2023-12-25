using System;

namespace Scripts
{
    public class SettingsButtonController : ISettingsButtonController
    {
        private IBaseButtonView _view;
        public event EventHandler OpenSettings;

        public SettingsButtonController(IBaseButtonView view)
        {
            _view = view;
        }
        public void Initialize()
        {
            _view.Init(new BaseButtonModel(){OnClick = OnClickSettings});
        }

        private void OnClickSettings()
        {
            OpenSettings?.Invoke(this,  null);
        }
    }

    public interface ISettingsButtonController
    {
        void Initialize();
        event EventHandler OpenSettings;
    }
}