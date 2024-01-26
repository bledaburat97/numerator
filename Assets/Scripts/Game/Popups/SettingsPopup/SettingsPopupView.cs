using UnityEngine;

namespace Scripts
{
    public class SettingsPopupView : MonoBehaviour, ISettingsPopupView
    {
        [SerializeField] private BaseButtonView retryButton;
        [SerializeField] private BaseButtonView menuButton;
        [SerializeField] private BaseButtonView closeButton;
        public void Init()
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
        }

        public void Close()
        {
            Destroy(gameObject);
        }

        public IBaseButtonView GetRetryButton()
        {
            return retryButton;
        }
        
        public IBaseButtonView GetMenuButton()
        {
            return menuButton;
        }
        
        public IBaseButtonView GetCloseButton()
        {
            return closeButton;
        }
    }

    public interface ISettingsPopupView
    {
        void Init();
        void Close();
        IBaseButtonView GetRetryButton();
        IBaseButtonView GetMenuButton();
        IBaseButtonView GetCloseButton();
    }
}