using System;

namespace Scripts
{
    public class CheckButtonController : ICheckButtonController
    {
        private ICheckButtonView _view;
        public event EventHandler CheckFinalNumbers;
        
        public void Initialize(ICheckButtonView view)
        {
            _view = view;
            _view.Init(new CheckButtonModel(){OnClick = OnClickCheckButton});
        }

        private void OnClickCheckButton()
        {
            CheckFinalNumbers?.Invoke(this,  null);
        }
    }

    public interface ICheckButtonController
    {
        void Initialize(ICheckButtonView view);
        event EventHandler CheckFinalNumbers;
    }
}