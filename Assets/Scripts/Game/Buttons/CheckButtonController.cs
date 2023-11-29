using System;

namespace Scripts
{
    public class CheckButtonController : ICheckButtonController
    {
        private IBaseButtonView _view;
        public event EventHandler CheckFinalNumbers;
        
        public void Initialize(IBaseButtonView view)
        {
            _view = view;
            _view.Init(new BaseButtonModel(){OnClick = OnClickCheckButton});
        }

        private void OnClickCheckButton()
        {
            CheckFinalNumbers?.Invoke(this,  null);
        }
    }

    public interface ICheckButtonController
    {
        void Initialize(IBaseButtonView view);
        event EventHandler CheckFinalNumbers;
    }
}