using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Scripts
{
    public class ConnectionResponsePopupView : MonoBehaviour
    {
        [SerializeField] private BaseButtonView closeButtonView;
        [SerializeField] private TMP_Text title;
        
        public void Init()
        {
            MultiplayerManager.Instance.OnFailedToJoinGame += OnFailedToJoinGame;
            ICloseButtonController closeButtonController = new CloseButtonController();
            closeButtonController.Initialize(closeButtonView, new BaseButtonModel()
            {
                OnClick = Hide
            });
            Hide();
        }

        private void Show()
        {
            gameObject.SetActive(true);
            SetTitle(NetworkManager.Singleton.DisconnectReason);
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnFailedToJoinGame(object sender, EventArgs args)
        {
            Show();
        }
        
        private void SetTitle(string text)
        {
            title.SetText(text);
        }

        private void OnDestroy()
        {
            MultiplayerManager.Instance.OnFailedToJoinGame -= OnFailedToJoinGame;
        }
    }
}