using UnityEngine.SceneManagement;

namespace Scripts
{
    public class ReturnMenuButtonController : IReturnMenuButtonController
    {
        private IReturnMenuButtonView _view;
        
        public void Initialize(IReturnMenuButtonView view)
        {
            _view = view;
            ReturnMenuButtonModel model = new ReturnMenuButtonModel()
            {
                OnClick = OnReturnMenuButtonClick
            };
            _view.Init(model);
        }

        private void OnReturnMenuButtonClick()
        {
            SceneManager.LoadScene("Menu");
        }
    }

    public interface IReturnMenuButtonController
    {
        void Initialize(IReturnMenuButtonView view);
    }
}