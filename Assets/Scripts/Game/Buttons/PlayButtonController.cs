using System;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class PlayButtonController : IPlayButtonController
    {
        private IBaseButtonView _view;
        public void Initialize(IBaseButtonView view, string text, Action deleteSaveAction)
        {
            _view = view;
            BaseButtonModel model = new BaseButtonModel()
            {
                text = text,
                OnClick = () => OnPlayButtonClick(deleteSaveAction)
            };
            _view.Init(model);
        }

        private void OnPlayButtonClick(Action deleteSaveAction)
        {
            deleteSaveAction?.Invoke();
            SceneManager.LoadScene("Game");
        }
    }

    public interface IPlayButtonController
    {
        void Initialize(IBaseButtonView view, string text, Action deleteSaveAction);
    }
}