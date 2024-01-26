using UnityEngine;

namespace Scripts
{
    public class WaitingOpponentPopupView : MonoBehaviour, IWaitingOpponentPopupView
    {
        [SerializeField] private BaseButtonView closeButton;

        public void Init()
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
        }

        public IBaseButtonView GetCloseButton()
        {
            return closeButton;
        }
        
        public void Close()
        {
            Destroy(gameObject);
        }
    }

    public interface IWaitingOpponentPopupView
    {
        void Init();
        IBaseButtonView GetCloseButton();
        void Close();
    }
}