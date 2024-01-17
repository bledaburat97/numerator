using System;
using TMPro;
using UnityEngine;

namespace Scripts
{
    public class LobbyUIView : MonoBehaviour, ILobbyUIView
    {
        [SerializeField] private PlayButtonView menuButton;
        [SerializeField] private TMP_InputField playerNameInputField;
        [SerializeField] private PlayButtonView createLobbyButton;
        [SerializeField] private PlayButtonView quickJoinButton;
        [SerializeField] private PlayButtonView joinWithCodeButton;
        [SerializeField] private TMP_InputField codeInputField;

        public void SetCreateLobbyButton(BaseButtonModel model)
        {
            createLobbyButton.Init(model);
        }
        
        public void SetQuickJoinButton(BaseButtonModel model)
        {
            quickJoinButton.Init(model);
        }
        
        public void SetJoinWithCodeButton(BaseButtonModel model)
        {
            joinWithCodeButton.Init(model);
        }

        public string GetCodeInputField()
        {
            return codeInputField.text;
        }

        public void SetMenuButton(BaseButtonModel model)
        {
            menuButton.Init(model);
        }

        public void InitPlayerNameInputField(string playerName, Action<string> onPlayerNameChanged)
        {
            playerNameInputField.text = playerName;
            playerNameInputField.onValueChanged.AddListener(onPlayerNameChanged.Invoke);
        }
    }

    public interface ILobbyUIView
    {
        void SetCreateLobbyButton(BaseButtonModel model);
        void SetQuickJoinButton(BaseButtonModel model);
        void SetJoinWithCodeButton(BaseButtonModel model);
        string GetCodeInputField();
        void InitPlayerNameInputField(string playerName, Action<string> onPlayerNameChanged);
        void SetMenuButton(BaseButtonModel model);
    }
}