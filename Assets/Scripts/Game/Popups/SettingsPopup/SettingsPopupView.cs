using UnityEngine;

namespace Scripts
{
    public class SettingsPopupView : MonoBehaviour, ISettingsPopupView
    {
        [SerializeField] private BaseButtonView playButtonView;
        [SerializeField] private BaseButtonView returnMenuButtonView;
        [SerializeField] private BaseButtonView closeButtonView;
        public void Init()
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
        }
        
        public IBaseButtonView GetCloseButtonView()
        {
            return closeButtonView;
        }
        
        public IBaseButtonView GetPlayButtonView()
        {
            return playButtonView;
        }

        public IBaseButtonView GetReturnMenuButtonView()
        {
            return returnMenuButtonView;
        }

        public void Close()
        {
            Destroy(gameObject);
        }
    }

    public interface ISettingsPopupView
    {
        void Init();
        IBaseButtonView GetCloseButtonView();
        IBaseButtonView GetPlayButtonView();
        IBaseButtonView GetReturnMenuButtonView();
        void Close();
    }
}