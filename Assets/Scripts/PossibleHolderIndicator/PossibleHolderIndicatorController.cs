using UnityEngine;

namespace Scripts
{
    public class PossibleHolderIndicatorController : IPossibleHolderIndicatorController
    {
        private IPossibleHolderIndicatorView _view;
        private bool _status;
        
        public void Initialize(IPossibleHolderIndicatorView view, PossibleHolderIndicatorModel model)
        {
            _view = view;
            _view.Init(model);
        }

        public void SetStatus(bool status)
        {
            _status = status;
            _view.SetStatus(status);
        }

        public bool GetStatus()
        {
            return _status;
        }
    }

    public interface IPossibleHolderIndicatorController
    {
        void Initialize(IPossibleHolderIndicatorView view, PossibleHolderIndicatorModel model);
        void SetStatus(bool status);
        bool GetStatus();
    }

    public class PossibleHolderIndicatorModel
    {
        public string text;
        public Vector3 localPosition;
    }
}