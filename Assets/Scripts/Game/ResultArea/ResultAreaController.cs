﻿namespace Scripts
{
    public class ResultAreaController : IResultAreaController
    {
        private IResultAreaView _view;
        private ResultBlockControllerFactory _resultBlockControllerFactory;

        public ResultAreaController(IResultAreaView view)
        {
            _view = view;
        }
        
        public void Initialize(IResultManager resultManager)
        {
            _view.Init(new ResultBlockViewFactory());
            _resultBlockControllerFactory = new ResultBlockControllerFactory();
            resultManager.ResultBlockAddition += AddResultBlock;
        }
        
        private void AddResultBlock(object sender, ResultBlockModel resultBlockModel)
        {
            IResultBlockController resultBlockController = _resultBlockControllerFactory.Spawn();
            IResultBlockView resultBlockView = _view.CreateResultBlock();
            resultBlockController.Initialize(resultBlockView, resultBlockModel);
            _view.SetScrollPositionToBottom();
        }
    }

    public interface IResultAreaController
    {
        void Initialize(IResultManager resultManager);
    }
}