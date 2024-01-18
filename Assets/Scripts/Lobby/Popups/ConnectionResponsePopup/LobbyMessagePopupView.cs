using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Scripts
{
    public class LobbyMessagePopupView : MonoBehaviour
    {
        [SerializeField] private BaseButtonView closeButtonView;
        [SerializeField] private TMP_Text title;
        
        public void Init()
        {
            closeButtonView.Init(new BaseButtonModel()
            {
                OnClick = Hide
            });
            Hide();
        }

        public void Show(string text)
        {
            gameObject.SetActive(true);
            SetTitle(text);
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }
        
        private void SetTitle(string text)
        {
            title.SetText(text);
        }
    }
}