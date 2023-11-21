using UnityEngine;

namespace Scripts
{
    public class SettingsPopupView : MonoBehaviour, ISettingsPopupView
    {
        [SerializeField] private PlayButtonView playButtonView;
        [SerializeField] private ReturnMenuButtonView returnMenuButtonView;
        [SerializeField] private CloseButtonView closeButtonView;
        public void Init()
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
        }
        
        public ICloseButtonView GetCloseButtonView()
        {
            return closeButtonView;
        }
        
        public IPlayButtonView GetPlayButtonView()
        {
            return playButtonView;
        }

        public IReturnMenuButtonView GetReturnMenuButtonView()
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
        ICloseButtonView GetCloseButtonView();
        IPlayButtonView GetPlayButtonView();
        IReturnMenuButtonView GetReturnMenuButtonView();
        void Close();
    }
}