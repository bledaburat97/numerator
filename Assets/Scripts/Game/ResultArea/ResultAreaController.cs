using Zenject;

namespace Scripts
{
    public class ResultAreaController : IResultAreaController
    {
        private IResultAreaView _view;
        
        public ResultAreaController(IResultAreaView view)
        {
            _view = view;
        }
        
        public void Initialize(IResultManager resultManager, ILevelTracker levelTracker)
        {
            _view.Init(new ResultBlockViewFactory(), levelTracker);
            resultManager.ResultBlockAddition += _view.AddResultBlock;
        }

    }

    public interface IResultAreaController
    {
        void Initialize(IResultManager resultManager, ILevelTracker levelTracker);
    }
}