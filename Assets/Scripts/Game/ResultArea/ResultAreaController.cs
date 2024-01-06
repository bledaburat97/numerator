namespace Scripts
{
    public class ResultAreaController : IResultAreaController
    {
        private IResultAreaView _view;

        public ResultAreaController(IResultAreaView view)
        {
            _view = view;
        }
        
        public void Initialize(IResultManager resultManager)
        {
            _view.Init(new ResultBlockViewFactory());
            resultManager.ResultBlockAddition += _view.AddResultBlock;
        }
        /*
        private void AddResultBlock(object sender, ResultBlockModel resultBlockModel)
        {
            IResultBlockController resultBlockController = _resultBlockControllerFactory.Spawn();
            IResultBlockView resultBlockView = _view.CreateResultBlockServerRpc();
            resultBlockController.Initialize(resultBlockView, resultBlockModel);
            _view.SetScrollPositionToBottom();
        }
        */
    }

    public interface IResultAreaController
    {
        void Initialize(IResultManager resultManager);
    }
}