using System;

namespace Scripts
{
    public class SettingsButtonController : ISettingsButtonController
    {
        private IBaseButtonView _view;
        public event EventHandler OpenSettings;
        public void Initialize(IBaseButtonView view)
        {
            _view = view;
            _view.Init(new BaseButtonModel(){OnClick = OnClickSettings});
        }

        private void OnClickSettings()
        {
            OpenSettings?.Invoke(this,  null);
        }
    }

    public interface ISettingsButtonController
    {
        void Initialize(IBaseButtonView view);
        event EventHandler OpenSettings;
    }
}