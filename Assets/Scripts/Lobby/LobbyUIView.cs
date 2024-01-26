using System;
using TMPro;
using UnityEngine;

namespace Scripts
{
    public class LobbyUIView : MonoBehaviour, ILobbyUIView
    {
        [SerializeField] private BaseButtonView menuButton;
        [SerializeField] private TMP_InputField playerNameInputField;
        [SerializeField] private BaseButtonView createLobbyButton;
        [SerializeField] private BaseButtonView quickJoinButton;
        [SerializeField] private BaseButtonView joinWithCodeButton;
        [SerializeField] private TMP_InputField codeInputField;

        public IBaseButtonView GetMenuButton()
        {
            return menuButton;
        }
        
        public IBaseButtonView GetCreateLobbyButton()
        {
            return createLobbyButton;
        }
        
        public IBaseButtonView GetQuickJoinButton()
        {
            return quickJoinButton;
        }
        
        public IBaseButtonView GetJoinWithCodeButton()
        {
            return joinWithCodeButton;
        }

        public string GetCodeInputField()
        {
            return codeInputField.text;
        }

        public void InitPlayerNameInputField(string playerName, Action<string> onPlayerNameChanged)
        {
            playerNameInputField.text = playerName;
            playerNameInputField.onValueChanged.AddListener(onPlayerNameChanged.Invoke);
        }
    }

    public interface ILobbyUIView
    {
        public IBaseButtonView GetMenuButton();
        public IBaseButtonView GetCreateLobbyButton();
        public IBaseButtonView GetQuickJoinButton();
        public IBaseButtonView GetJoinWithCodeButton();
        string GetCodeInputField();
        void InitPlayerNameInputField(string playerName, Action<string> onPlayerNameChanged);
    }
}