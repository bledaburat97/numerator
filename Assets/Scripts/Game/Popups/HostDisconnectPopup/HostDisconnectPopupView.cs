using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Scripts
{
    public class HostDisconnectPopupView : MonoBehaviour
    {
        [SerializeField] private BaseButtonView menuButton;
        public void Init()
        {
            Hide();
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }

        public IBaseButtonView GetMenuButton()
        {
            return menuButton;
        }
    }
    

}