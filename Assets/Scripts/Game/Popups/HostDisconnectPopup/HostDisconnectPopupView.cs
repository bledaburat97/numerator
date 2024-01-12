using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Scripts
{
    public class HostDisconnectPopupView : MonoBehaviour
    {
        [SerializeField] private Button homeButton;
        public void Init()
        {
            Hide();
            homeButton.onClick.AddListener(() => SceneManager.LoadScene("Home"));
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