using System.Collections.Generic;

namespace Scripts
{
    public class ResultAreaController : IResultAreaController
    {
        private IResultAreaView _view;
        private ResultBlockControllerFactory _resultBlockControllerFactory;
        private List<IResultBlockController> _resultBlockControllerList;
        public void Initialize(IResultAreaView view, IResultManager resultManager)
        {
            _view = view;
            _view.Init(new ResultBlockViewFactory());
            _resultBlockControllerFactory = new ResultBlockControllerFactory();
            _resultBlockControllerList = new List<IResultBlockController>();
            resultManager.ResultBlockAddition += AddResultBlock;
        }
        
        private void AddResultBlock(object sender, ResultBlockModel resultBlockModel)
        {
            IResultBlockController resultBlockController = _resultBlockControllerFactory.Spawn();
            IResultBlockView resultBlockView = _view.CreateResultBlock();
            resultBlockController.Initialize(resultBlockView, resultBlockModel);
            _view.SetScrollPositionToBottom();
            _resultBlockControllerList.Add(resultBlockController);
        }
    }

    public interface IResultAreaController
    {
        void Initialize(IResultAreaView view, IResultManager resultManager);
    }
}