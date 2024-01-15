using UnityEngine;

namespace Scripts
{
    public class WaitingOpponentPopupView : MonoBehaviour, IWaitingOpponentPopupView
    {
        [SerializeField] private BaseButtonView closeButtonView;

        public void Init(BaseButtonModel closeButtonModel)
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            closeButtonView.Init(closeButtonModel);
        }
        
        public void Close()
        {
            Destroy(gameObject);
        }
    }

    public interface IWaitingOpponentPopupView
    {
        void Init(BaseButtonModel closeButtonModel);
        void Close();
    }
}