using System;
using Game;

namespace Scripts
{
    public class CheckButtonController : ICheckButtonController
    {
        private IBaseButtonView _view;
        public event EventHandler CheckFinalNumbers;
        public event EventHandler NotAbleToCheck;
        private ITurnOrderDeterminer _turnOrderDeterminer;
        public CheckButtonController(IBaseButtonView view)
        {
            _view = view;
        }
        
        public void Initialize(ITurnOrderDeterminer turnOrderDeterminer)
        {
            _turnOrderDeterminer = turnOrderDeterminer;
            _view.Init(new BaseButtonModel(){OnClick = OnClickCheckButton});
        }

        private void OnClickCheckButton()
        {
            if (_turnOrderDeterminer.IsLocalTurn())
            {
                CheckFinalNumbers?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                NotAbleToCheck?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public interface ICheckButtonController
    {
        void Initialize(ITurnOrderDeterminer turnOrderDeterminer);
        event EventHandler CheckFinalNumbers;
        event EventHandler NotAbleToCheck;
    }
}