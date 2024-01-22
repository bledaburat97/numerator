using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Scripts
{
    public class LobbyCreationPopupView : MonoBehaviour, ILobbyCreationPopupView
    {
        [SerializeField] private TMP_InputField lobbyNameInputField;
        [SerializeField] private BaseButtonView closeButtonView;
        [SerializeField] private PlayButtonView publicButton;
        [SerializeField] private PlayButtonView privateButton;
        [SerializeField] private OptionButtonView[] difficultyButtonList = new OptionButtonView[ConstantValues.NUM_OF_DIFFICULTY_BUTTONS];
        

        public void Init()
        {
            closeButtonView.Init(new BaseButtonModel()
            {
                OnClick = Hide
            });
            
            publicButton.Init(new BaseButtonModel()
            {
                text = "PUBLIC",
                OnClick = () => PlayerLobby.Instance.CreateLobby(lobbyNameInputField.text, false)
            });
            privateButton.Init(new BaseButtonModel()
            {
                text = "PRIVATE",
                OnClick = () => PlayerLobby.Instance.CreateLobby(lobbyNameInputField.text, true)
            });
            Hide();
        }
        
        public IOptionButtonView GetDifficultyButtonViewByIndex(int index)
        {
            return difficultyButtonList[index];
        }

        private void OnDifficultyButtonClicked(int difficultyIndex)
        {
            
        }
        
        public void Show()
        {
            gameObject.SetActive(true);
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }
    }

    public interface ILobbyCreationPopupView
    {
        void Init();
        IOptionButtonView GetDifficultyButtonViewByIndex(int index);
        void Show();
    }

    public enum Difficulty
    {
        Easy, 
        Medium,
        Hard
    }
}