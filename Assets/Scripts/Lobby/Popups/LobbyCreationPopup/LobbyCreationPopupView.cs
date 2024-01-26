using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Scripts
{
    public class LobbyCreationPopupView : MonoBehaviour, ILobbyCreationPopupView
    {
        [SerializeField] private TMP_InputField lobbyNameInputField;
        [SerializeField] private BaseButtonView[] difficultyButtonList = new BaseButtonView[ConstantValues.NUM_OF_DIFFICULTY_BUTTONS];
        [SerializeField] private BaseButtonView closeButton;
        [SerializeField] private BaseButtonView publicButton;
        [SerializeField] private BaseButtonView privateButton;
        
        public void Init()
        {
            Hide();
        }
        
        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public IBaseButtonView GetDifficultyButtonViewByIndex(int index)
        {
            return difficultyButtonList[index];
        }
        
        public IBaseButtonView GetCloseButton()
        {
            return closeButton;
        }
        
        public IBaseButtonView GetPublicButton()
        {
            return publicButton;
        }
        
        public IBaseButtonView GetPrivateButton()
        {
            return privateButton;
        }

        public string GetLobbyName()
        {
            return lobbyNameInputField.text;
        }
    }

    public interface ILobbyCreationPopupView
    {
        void Init();
        void Show();
        void Hide();
        IBaseButtonView GetDifficultyButtonViewByIndex(int index);
        public IBaseButtonView GetCloseButton();
        public IBaseButtonView GetPublicButton();
        public IBaseButtonView GetPrivateButton();
        string GetLobbyName();
    }

    public enum Difficulty
    {
        Easy, 
        Medium,
        Hard
    }
}