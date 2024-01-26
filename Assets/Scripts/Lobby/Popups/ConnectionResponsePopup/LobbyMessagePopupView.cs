using TMPro;
using UnityEngine;

namespace Scripts
{
    public class LobbyMessagePopupView : MonoBehaviour
    {
        [SerializeField] private BaseButtonView closeButton;
        [SerializeField] private TMP_Text title;
        
        public void Init()
        {
            Hide();
        }

        public IBaseButtonView GetCloseButton()
        {
            return closeButton;
        }

        public void Show(string text)
        {
            gameObject.SetActive(true);
            SetTitle(text);
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }
        
        private void SetTitle(string text)
        {
            title.SetText(text);
        }
    }
}