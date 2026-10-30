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
        [SerializeField] private BaseButtonView[] difficultyButtons = new BaseButtonView[3];
        private bool _createOpen, _joinOpen;
        public bool IsJoinInputOpen => _joinOpen;
        public TMP_InputField CodeInput => codeInputField;
        public TMP_InputField NameInput => playerNameInputField;
        public IBaseButtonView GetDifficultyButton(int index) { EnsureDifficultyButtons(); return difficultyButtons[index]; }
        private void EnsureDifficultyButtons()
        {
            if (difficultyButtons == null || difficultyButtons.Length != 3) difficultyButtons = new BaseButtonView[3];
            for (int i = 0; i < difficultyButtons.Length; i++)
                if (difficultyButtons[i] == null)
                {
                    difficultyButtons[i] = Instantiate(createLobbyButton, transform);
                    difficultyButtons[i].name = "Difficulty" + (Difficulty)i;
                }
        }
        public void SetSelectedDifficulty(Difficulty difficulty)
        {
            EnsureDifficultyButtons();
            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                difficultyButtons[i].ApplyLabActionStyle(i == (int)difficulty);
                difficultyButtons[i].SetText(MultiplayerRules.For((Difficulty)i).Name);
                difficultyButtons[i].gameObject.SetActive(_createOpen);
            }
        }
        public void ShowCreateOptions()
        {
            _createOpen = true; _joinOpen = false;
            ApplySectionVisibility();
        }
        public void ShowJoinCodeInput()
        {
            _joinOpen = true; _createOpen = false;
            ApplySectionVisibility();
            codeInputField.Select(); codeInputField.ActivateInputField();
        }
        private void ApplySectionVisibility()
        {
            EnsureDifficultyButtons();
            foreach (var button in difficultyButtons) button.gameObject.SetActive(_createOpen);
            codeInputField.gameObject.SetActive(_joinOpen);
        }
        public bool ValidatePlayerName()
        {
            if (!string.IsNullOrWhiteSpace(playerNameInputField.text)) return true;
            playerNameInputField.Select(); playerNameInputField.ActivateInputField(); return false;
        }
        public IBaseButtonView GetMenuButton() => menuButton;
        public IBaseButtonView GetCreateLobbyButton() => createLobbyButton;
        public IBaseButtonView GetQuickJoinButton() => quickJoinButton;
        public IBaseButtonView GetJoinWithCodeButton() => joinWithCodeButton;
        public string GetCodeInputField() => codeInputField.text;
        public void SetCodeSubmit(Action submit) => codeInputField.onSubmit.AddListener(_ => submit());
        public void ApplyOnlineLayout(float width, float height)
        {
            quickJoinButton.gameObject.SetActive(false);
            FruitLabOnlineLayout.Input(playerNameInputField, transform, height - 130, width, "Kullanıcı adı giriniz.");
            FruitLabOnlineLayout.Button(createLobbyButton, transform, height - 200, width, true);
            EnsureDifficultyButtons();
            float buttonWidth = (width - 56) / 3;
            for (int i = 0; i < difficultyButtons.Length; i++)
                FruitLabStyle.Place(difficultyButtons[i].GetButtonRectTransform(), transform, 20 + i * (buttonWidth + 8), height - 252, buttonWidth, 40);
            SetSelectedDifficulty(Difficulty.Easy);
            FruitLabOnlineLayout.Button(joinWithCodeButton, transform, height - 322, width, false);
            FruitLabOnlineLayout.Input(codeInputField, transform, height - 382, width, "Kod giriniz.");
            FruitLabOnlineLayout.Button(menuButton, transform, 24, width, false);
            createLobbyButton.SetText("Düello yarat"); joinWithCodeButton.SetText("Düelloya katıl"); menuButton.SetText("Menü");
            if (!Application.isPlaying) { playerNameInputField.SetTextWithoutNotify(""); codeInputField.SetTextWithoutNotify(""); }
            ApplySectionVisibility();
        }
        public void InitPlayerNameInputField(string playerName, Action<string> onPlayerNameChanged)
        {
            playerNameInputField.characterLimit = 16; codeInputField.characterLimit = 12;
            codeInputField.SetTextWithoutNotify(""); playerNameInputField.SetTextWithoutNotify(playerName);
            codeInputField.lineType = TMP_InputField.LineType.SingleLine;
            codeInputField.keyboardType = TouchScreenKeyboardType.ASCIICapable;
            playerNameInputField.onValueChanged.AddListener(onPlayerNameChanged.Invoke);
        }
    }
    public interface ILobbyUIView
    {
        IBaseButtonView GetDifficultyButton(int index);
        void SetSelectedDifficulty(Difficulty difficulty);
        void ShowCreateOptions(); void ShowJoinCodeInput();
        bool IsJoinInputOpen { get; }
        bool ValidatePlayerName();
        void SetCodeSubmit(Action submit);
        IBaseButtonView GetMenuButton();
        IBaseButtonView GetCreateLobbyButton();
        IBaseButtonView GetQuickJoinButton();
        IBaseButtonView GetJoinWithCodeButton();
        string GetCodeInputField();
        void InitPlayerNameInputField(string playerName, Action<string> onPlayerNameChanged);
    }
}
