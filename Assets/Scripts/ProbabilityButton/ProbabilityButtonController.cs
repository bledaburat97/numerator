using System;

namespace Scripts
{
    public class ProbabilityButtonController : IProbabilityButtonController
    {
        private IProbabilityButtonView _view;
        private ProbabilityButtonModel _model;
        public void Initialize(IProbabilityButtonView view, ProbabilityButtonModel model)
        {
            _view = view;
            _model = model;
            _view.Init(_model);
        }

        public void SetFrameStatus(bool status)
        {
            _view.SetFrameStatus(status);
        }

    }
    
    public interface IProbabilityButtonController
    {
        void Initialize(IProbabilityButtonView view, ProbabilityButtonModel model);
        void SetFrameStatus(bool status);
    }
    
    public class ProbabilityButtonModel
    {
        public ProbabilityType probabilityType;
        public Action onClickAction;
    }
}