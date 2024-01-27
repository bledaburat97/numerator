
using Zenject;

namespace Scripts
{
    public class ResultAreaController : IResultAreaController
    {
        [Inject] private IHapticController _hapticController;
        private IResultAreaView _view;
        
        public ResultAreaController(IResultAreaView view)
        {
            _view = view;
        }
        
        public void Initialize(IResultManager resultManager, ILevelTracker levelTracker, ITurnOrderDeterminer turnOrderDeterminer)
        {
            _view.Init(new ResultBlockViewFactory(), levelTracker, turnOrderDeterminer, _hapticController);
            resultManager.ResultBlockAddition += _view.AddResultBlock;
        }

    }

    public interface IResultAreaController
    {
        void Initialize(IResultManager resultManager, ILevelTracker levelTracker, ITurnOrderDeterminer turnOrderDeterminer);
    }
}