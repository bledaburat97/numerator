using System;
using TMPro;
using UnityEngine;

namespace Scripts
{
    public class ConnectingPopupView : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        
        public void Init()
        {
            MultiplayerManager.Instance.OnFailedToJoinGame += OnFailedToJoinGame;
            MultiplayerManager.Instance.OnTryingToJoinGame += OnTryingToJoinGame;
            SetTitle("Connecting...");
            Hide();
        }
        
        private void Show()
        {
            gameObject.SetActive(true);
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnFailedToJoinGame(object sender, EventArgs args)
        {
            Hide();
        }
        
        private void OnTryingToJoinGame(object sender, EventArgs args)
        {
            Show();
        }
        
        private void SetTitle(string text)
        {
            title.SetText(text);
        }

        public void Close()
        {
            Hide();
        }

        private void OnDestroy()
        {
            MultiplayerManager.Instance.OnFailedToJoinGame -= OnFailedToJoinGame;
            MultiplayerManager.Instance.OnTryingToJoinGame -= OnTryingToJoinGame;
        }
    }
}