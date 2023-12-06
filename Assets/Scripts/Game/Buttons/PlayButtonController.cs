using System;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class PlayButtonController : IPlayButtonController
    {
        private IBaseButtonView _view;
        public void Initialize(IBaseButtonView view, BaseButtonModel model)
        {
            _view = view;
            _view.Init(model);
        }
    }

    public interface IPlayButtonController
    {
        void Initialize(IBaseButtonView view, BaseButtonModel model);
    }
}