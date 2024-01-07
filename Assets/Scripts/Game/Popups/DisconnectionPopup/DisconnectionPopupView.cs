using UnityEngine;

namespace Scripts
{
    public class DisconnectionPopupView : MonoBehaviour, IDisconnectionPopupView
    {
        [SerializeField] private BaseButtonView returnMenuButtonView;

        public void Init()
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
        }
        
        public IBaseButtonView GetReturnMenuButtonView()
        {
            return returnMenuButtonView;
        }
    }

    public interface IDisconnectionPopupView
    {
        void Init();
        IBaseButtonView GetReturnMenuButtonView();
    }
}