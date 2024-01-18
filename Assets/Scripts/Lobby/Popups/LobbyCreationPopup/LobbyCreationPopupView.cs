using TMPro;
using UnityEngine;

namespace Scripts
{
    public class LobbyCreationPopupView : MonoBehaviour
    {
        [SerializeField] private TMP_InputField lobbyNameInputField;
        [SerializeField] private BaseButtonView closeButtonView;
        [SerializeField] private PlayButtonView publicButton;
        [SerializeField] private PlayButtonView privateButton;
        
        public void Init()
        {
            ICloseButtonController closeButtonController = new CloseButtonController();
            closeButtonController.Initialize(closeButtonView, new BaseButtonModel()
            {
                OnClick = Hide
            });
            publicButton.Init(new BaseButtonModel()
            {
                text = "PUBLIC",
                OnClick = () => PlayerLobby.Instance.CreateLobby(lobbyNameInputField.text, false)
            });
            privateButton.Init(new BaseButtonModel()
            {
                text = "PRIVATE",
                OnClick = () => PlayerLobby.Instance.CreateLobby(lobbyNameInputField.text, true)
            });
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