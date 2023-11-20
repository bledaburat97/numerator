using System;

namespace Scripts
{
    public class SettingsButtonController : ISettingsButtonController
    {
        private ISettingsButtonView _view;
        public event EventHandler OpenSettings;
        public void Initialize(ISettingsButtonView view)
        {
            _view = view;
            _view.Init(new SettingsButtonModel(){OnClick = OnClickSettings});
        }

        private void OnClickSettings()
        {
            OpenSettings?.Invoke(this,  null);
        }
    }

    public interface ISettingsButtonController
    {
        void Initialize(ISettingsButtonView view);
        event EventHandler OpenSettings;
    }

    public class SettingsButtonModel
    {
        public Action OnClick;
    }
}