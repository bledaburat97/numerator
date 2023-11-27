using System;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class ReturnMenuButtonController : IReturnMenuButtonController
    {
        private IReturnMenuButtonView _view;
        public void Initialize(IReturnMenuButtonView view, Action saveGameAction = null)
        {
            _view = view;
            ReturnMenuButtonModel model = new ReturnMenuButtonModel()
            {
                OnClick = () => OnReturnMenuButtonClick(saveGameAction)
            };
            _view.Init(model);
        }

        private void OnReturnMenuButtonClick(Action saveGameAction)
        {
            saveGameAction!.Invoke();
            SceneManager.LoadScene("Menu");
        }
    }

    public interface IReturnMenuButtonController
    {
        void Initialize(IReturnMenuButtonView view, Action saveGameAction = null);
    }
}