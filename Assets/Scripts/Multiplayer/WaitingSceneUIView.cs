using TMPro;
using UnityEngine;

namespace Scripts
{
    public class WaitingSceneUIView : MonoBehaviour, IWaitingSceneUIView
    {
        [SerializeField] private TMP_Text lobbyNameText;
        [SerializeField] private TMP_Text lobbyCodeText;
        [SerializeField] private BaseButtonView menuButton;
        [SerializeField] private BaseButtonView readyButton;

        public void SetLobbyNameText(string text)
        {
            lobbyNameText.text = "Lobby Name: " + text;
        }
        
        public void SetLobbyCodeText(string text)
        {
            lobbyCodeText.text = "Lobby Code: " + text;
        }
        
        public IBaseButtonView GetMenuButton()
        {
            return menuButton;
        }

        public IBaseButtonView GetReadyButton()
        {
            return readyButton;
        }
    }

    public interface IWaitingSceneUIView
    {
        void SetLobbyNameText(string text);
        void SetLobbyCodeText(string text);
        IBaseButtonView GetMenuButton();
        IBaseButtonView GetReadyButton();
    }
}