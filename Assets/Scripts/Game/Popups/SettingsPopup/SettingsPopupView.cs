using UnityEngine;

namespace Scripts
{
    public class SettingsPopupView : MonoBehaviour, ISettingsPopupView
    {
        [SerializeField] private PlayButtonView retryButtonView;
        [SerializeField] private PlayButtonView menuButtonView;
        [SerializeField] private BaseButtonView closeButtonView;
        public void Init(BaseButtonModel retryButtonModel, BaseButtonModel menuButtonModel, BaseButtonModel closeButtonModel)
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            retryButtonView.Init(retryButtonModel);
            menuButtonView.Init(menuButtonModel);
            closeButtonView.Init(closeButtonModel);
        }

        public void DestroyRetryButton()
        {
            Destroy(retryButtonView.gameObject);
        }

        public void Close()
        {
            Destroy(gameObject);
        }
    }

    public interface ISettingsPopupView
    {
        void Init(BaseButtonModel retryButtonModel, BaseButtonModel menuButtonModel, BaseButtonModel closeButtonModel);
        void DestroyRetryButton();
        void Close();
    }
}