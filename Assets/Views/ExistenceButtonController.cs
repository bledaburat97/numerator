using UnityEngine;

namespace Views
{
    public class ExistenceButtonController : IExistenceButtonController
    {
        private IExistenceButtonView _view;
        
        public void Initialize(IExistenceButtonView existenceButtonView)
        {
            _view = existenceButtonView;
            _view.Init();
        }
        
        public void SetStatus(bool status)
        {
            _view.SetStatus(status);
        }
        
        public void SetParent(RectTransform parent)
        {
            _view.SetParent(parent);
        }
    }
    
    public interface IExistenceButtonController
    {
        void Initialize(IExistenceButtonView existenceButtonView);
        void SetStatus(bool status);
        void SetParent(RectTransform parent);
    }
}