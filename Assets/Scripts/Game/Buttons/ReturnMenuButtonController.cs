using System;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class ReturnMenuButtonController : IReturnMenuButtonController
    {
        private IBaseButtonView _view;
        public void Initialize(IBaseButtonView view, Action saveGameAction = null)
        {
            _view = view;
            BaseButtonModel model = new BaseButtonModel()
            {
                OnClick = () => OnReturnMenuButtonClick(saveGameAction),
                text = "Menu"
            };
            _view.Init(model);
        }

        private void OnReturnMenuButtonClick(Action saveGameAction)
        {
            saveGameAction?.Invoke();
            SceneManager.LoadScene("Menu");
        }
    }

    public interface IReturnMenuButtonController
    {
        void Initialize(IBaseButtonView view, Action saveGameAction = null);
    }
}