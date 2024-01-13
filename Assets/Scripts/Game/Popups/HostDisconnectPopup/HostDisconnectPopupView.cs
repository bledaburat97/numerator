using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Scripts
{
    public class HostDisconnectPopupView : MonoBehaviour
    {
        [SerializeField] private Button menuButton;
        public void Init()
        {
            Hide();
            menuButton.onClick.AddListener(() => SceneManager.LoadScene("Menu"));
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }
    }
    

}