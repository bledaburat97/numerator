using UnityEngine;

namespace Scripts
{
    public class SettingsPopupView : MonoBehaviour, ISettingsPopupView
    {
        [SerializeField] private PlayButtonView retryButtonView;
        [SerializeField] private PlayButtonView menuButtonView;
        [SerializeField] private BaseButtonView closeButtonView;
        public void Init(BaseButtonModel closeButtonModel)
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            retryButtonView.gameObject.SetActive(false);
            menuButtonView.gameObject.SetActive(false);
            closeButtonView.Init(closeButtonModel);
        }
        
        public void CreateRetryButton(BaseButtonModel model)
        {
            retryButtonView.gameObject.SetActive(true);
            retryButtonView.Init(model);
            retryButtonView.InitPosition(model.localPosition);
        }
        
        public void CreateMenuButton(BaseButtonModel model)
        {
            menuButtonView.gameObject.SetActive(true);
            menuButtonView.Init(model);
            menuButtonView.InitPosition(model.localPosition);
        }

        public void Close()
        {
            Destroy(gameObject);
        }
    }

    public interface ISettingsPopupView
    {
        void Init(BaseButtonModel closeButtonModel);
        void CreateRetryButton(BaseButtonModel model);
        void CreateMenuButton(BaseButtonModel model);
        void Close();
    }
}