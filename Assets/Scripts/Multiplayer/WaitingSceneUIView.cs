using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace Scripts
{
    public class WaitingSceneUIView : MonoBehaviour, IWaitingSceneUIView
    {
        [SerializeField] private TMP_Text lobbyNameText;
        [SerializeField] private TMP_Text lobbyCodeText;

        public void SetLobbyNameText(string text)
        {
            lobbyNameText.text = "Lobby Name: " + text;
        }
        
        public void SetLobbyCodeText(string text)
        {
            lobbyCodeText.text = "Lobby Code: " + text;
        }
    }

    public interface IWaitingSceneUIView
    {
        void SetLobbyNameText(string text);
        void SetLobbyCodeText(string text);
    }
}