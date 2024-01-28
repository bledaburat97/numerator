namespace Scripts
{
    public class ResultAreaController : IResultAreaController
    {
        private IResultAreaView _view;
        
        public ResultAreaController(IResultAreaView view)
        {
            _view = view;
        }
        
        public void Initialize(IResultManager resultManager, ILevelTracker levelTracker, ITurnOrderDeterminer turnOrderDeterminer)
        {
            _view.Init(new ResultBlockViewFactory(), levelTracker, turnOrderDeterminer);
            resultManager.ResultBlockAddition += _view.AddResultBlock;
        }

    }

    public interface IResultAreaController
    {
        void Initialize(IResultManager resultManager, ILevelTracker levelTracker, ITurnOrderDeterminer turnOrderDeterminer);
    }
}