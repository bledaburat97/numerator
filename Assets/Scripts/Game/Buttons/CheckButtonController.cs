using System;

namespace Scripts
{
    public class CheckButtonController : ICheckButtonController
    {
        private IBaseButtonView _view;
        public event EventHandler CheckFinalNumbers;

        public CheckButtonController(IBaseButtonView view)
        {
            _view = view;
        }
        
        public void Initialize()
        {
            _view.Init(new BaseButtonModel(){OnClick = OnClickCheckButton});
        }

        private void OnClickCheckButton()
        {
            CheckFinalNumbers?.Invoke(this,  null);
        }
    }

    public interface ICheckButtonController
    {
        void Initialize();
        event EventHandler CheckFinalNumbers;
    }
}