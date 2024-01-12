using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class LobbyCreationPopupView : MonoBehaviour
    {
        [SerializeField] private TMP_InputField lobbyNameInputField;
        [SerializeField] private BaseButtonView closeButtonView;
        [SerializeField] private Button publicButton;
        [SerializeField] private Button privateButton;
        
        public void Init()
        {
            ICloseButtonController closeButtonController = new CloseButtonController();
            closeButtonController.Initialize(closeButtonView, new BaseButtonModel()
            {
                OnClick = Hide
            });
            publicButton.onClick.AddListener(
                    () => PlayerLobby.Instance.CreateLobby(lobbyNameInputField.text, false));
            privateButton.onClick.AddListener(
                () => PlayerLobby.Instance.CreateLobby(lobbyNameInputField.text, true));
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
    }
}