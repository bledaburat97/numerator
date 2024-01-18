using TMPro;
using UnityEngine;

namespace Scripts
{
    public class WaitingSceneUIView : MonoBehaviour, IWaitingSceneUIView
    {
        [SerializeField] private TMP_Text lobbyNameText;
        [SerializeField] private TMP_Text lobbyCodeText;
        [SerializeField] private PlayButtonView menuButton;
        [SerializeField] private PlayButtonView readyButton;

        public void SetLobbyNameText(string text)
        {
            lobbyNameText.text = "Lobby Name: " + text;
        }
        
        public void SetLobbyCodeText(string text)
        {
            lobbyCodeText.text = "Lobby Code: " + text;
        }
        
        public void SetMenuButton(BaseButtonModel model)
        {
            menuButton.Init(model);
        }

        public void SetReadyButton(BaseButtonModel model)
        {
            readyButton.Init(model);
        }
    }

    public interface IWaitingSceneUIView
    {
        void SetLobbyNameText(string text);
        void SetLobbyCodeText(string text);
        void SetMenuButton(BaseButtonModel model);
        void SetReadyButton(BaseButtonModel model);
    }
}