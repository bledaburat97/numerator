using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class PlayerNameView : MonoBehaviour, IPlayerNameView
    {
        [SerializeField] private TMP_Text playerName;
        [SerializeField] private Image readyImage;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Button kickButton;
        public void Init(int playerIndex, Action<int> onKickButtonClicked)
        {
            readyImage.gameObject.SetActive(false);
            rectTransform.localScale = Vector3.one;
            kickButton.onClick.AddListener(() => onKickButtonClicked.Invoke(playerIndex));
            kickButton.gameObject.SetActive(NetworkManager.Singleton.IsServer);
        }

        public void SetPlayerName(string playerName)
        {
            this.playerName.text = playerName;
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }

        public void SetReadyStatus(bool isReady)
        {
            readyImage.gameObject.SetActive(isReady);
        }
    }

    public interface IPlayerNameView
    {
        void Init(int playerIndex, Action<int> onKickButtonClicked);
        void Destroy();
        void SetReadyStatus(bool isReady);
        void SetPlayerName(string playerName);
    }
}