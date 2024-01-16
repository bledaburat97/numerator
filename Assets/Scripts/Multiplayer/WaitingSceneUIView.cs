using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace Scripts
{
    public class WaitingSceneUIView : MonoBehaviour, IWaitingSceneUIView
    {
        [SerializeField] private TMP_Text lobbyNameText;
        [SerializeField] private TMP_Text lobbyCodeText;
        [SerializeField] private BaseButtonView menuButton;

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
    }

    public interface IWaitingSceneUIView
    {
        void SetLobbyNameText(string text);
        void SetLobbyCodeText(string text);
        void SetMenuButton(BaseButtonModel model);
    }
}