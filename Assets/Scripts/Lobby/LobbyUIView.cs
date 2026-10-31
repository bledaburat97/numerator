using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private RectTransform dialogOverlay, createPopup, joinPopup;
        [SerializeField] private BaseButtonView createConfirmButton, joinConfirmButton, createCloseButton, joinCloseButton;
        [SerializeField] private TMP_Text joinError;
        [SerializeField] private Difficulty selectedDifficulty;
        [SerializeField] private RectTransform profileSection, gameSection;
        private bool _createOpen, _joinOpen;
        public bool IsJoinInputOpen => _joinOpen;
        public TMP_InputField CodeInput => codeInputField;
        public TMP_InputField NameInput => playerNameInputField;
        public RectTransform CreatePopup => createPopup;
        public RectTransform JoinPopup => joinPopup;
        public IBaseButtonView GetDifficultyButton(int index) { EnsureDialogs(); return difficultyButtons[index]; }
        private void EnsureDialogs()
        {
            if (dialogOverlay == null)
            {
                dialogOverlay = FruitLabOnlineLayout.Rect("EntryDialogs", transform);
                var blocker = dialogOverlay.gameObject.AddComponent<Image>();
                blocker.color = new Color(0, 0, 0, .66f); blocker.raycastTarget = true;
            }
            if (createPopup == null) createPopup = FruitLabOnlineLayout.Panel("CreateGamePopup", dialogOverlay);
            if (joinPopup == null) joinPopup = FruitLabOnlineLayout.Panel("JoinGamePopup", dialogOverlay);
            if (difficultyButtons == null || difficultyButtons.Length != 3) difficultyButtons = new BaseButtonView[3];
            for (int i = 0; i < 3; i++)
                EnsureButton(ref difficultyButtons[i], "Difficulty" + (Difficulty)i, createPopup);
            EnsureButton(ref createConfirmButton, "CreateConfirmButton", createPopup);
            EnsureButton(ref joinConfirmButton, "JoinConfirmButton", joinPopup);
            EnsureButton(ref createCloseButton, "CreateCloseButton", createPopup);
            EnsureButton(ref joinCloseButton, "JoinCloseButton", joinPopup);
            if (joinError == null) joinError = FruitLabStyle.CreateText("JoinError", joinPopup, 12, FruitLabStyle.OpponentPlayer);
        }
        private void EnsureButton(ref BaseButtonView button, string name, Transform parent)
        {
            if (button == null) { button = Instantiate(createLobbyButton, parent); button.name = name; }
            button.transform.SetParent(parent, false);
        }
        public void SetSelectedDifficulty(Difficulty difficulty)
        {
            selectedDifficulty = difficulty; EnsureDialogs();
            for (int i = 0; i < 3; i++)
            {
                difficultyButtons[i].ApplyLabActionStyle(i == (int)difficulty);
                difficultyButtons[i].SetText(MultiplayerRules.For((Difficulty)i).Name);
                difficultyButtons[i].gameObject.SetActive(true);
            }
        }
        public void ShowCreateOptions() { EnsureDialogs(); _createOpen = true; _joinOpen = false; ApplySectionVisibility(); }
        public void ShowJoinCodeInput()
        {
            EnsureDialogs(); _joinOpen = true; _createOpen = false; SetJoinError(""); ApplySectionVisibility();
            if (Application.isPlaying) { codeInputField.Select(); codeInputField.ActivateInputField(); }
        }
        public void HideDialogs() { _createOpen = _joinOpen = false; ApplySectionVisibility(); }
        private void ApplySectionVisibility()
        {
            dialogOverlay.gameObject.SetActive(_createOpen || _joinOpen);
            createPopup.gameObject.SetActive(_createOpen); joinPopup.gameObject.SetActive(_joinOpen);
            codeInputField.gameObject.SetActive(_joinOpen);
            dialogOverlay.SetAsLastSibling();
        }
        public void SetJoinError(string message) { EnsureDialogs(); joinError.text = message; }
        public bool ValidatePlayerName()
        {
            if (!string.IsNullOrWhiteSpace(playerNameInputField.text)) return true;
            HideDialogs(); playerNameInputField.Select(); playerNameInputField.ActivateInputField(); return false;
        }
        public IBaseButtonView GetMenuButton() => menuButton;
        public IBaseButtonView GetCreateLobbyButton() => createLobbyButton;
        public IBaseButtonView GetQuickJoinButton() => quickJoinButton;
        public IBaseButtonView GetJoinWithCodeButton() => joinWithCodeButton;
        public IBaseButtonView GetCreateConfirmButton() { EnsureDialogs(); return createConfirmButton; }
        public IBaseButtonView GetJoinConfirmButton() { EnsureDialogs(); return joinConfirmButton; }
        public IBaseButtonView GetCreateCloseButton() { EnsureDialogs(); return createCloseButton; }
        public IBaseButtonView GetJoinCloseButton() { EnsureDialogs(); return joinCloseButton; }
        public string GetCodeInputField() => codeInputField.text;
        public void SetCodeSubmit(Action submit) => codeInputField.onSubmit.AddListener(_ => submit());
        public void ApplyOnlineLayout(float width, float height)
        {
            EnsureDialogs();
            if (profileSection == null) profileSection = FruitLabOnlineLayout.Panel("ProfileSection", transform);
            if (gameSection == null) gameSection = FruitLabOnlineLayout.Panel("GameSection", transform);
            FruitLabStyle.Place(profileSection, transform, 0, height - 140, width, 140);
            FruitLabStyle.Place(gameSection, transform, 0, 0, width, height - 156);
            FruitLabOnlineLayout.Title(profileSection, "Profil Bilgisi", width, 140);
            FruitLabOnlineLayout.Title(gameSection, "Oyun Bilgisi", width, height - 156);
            FruitLabOnlineLayout.Input(playerNameInputField, profileSection, 22, width, "Kullanıcı adı giriniz");
            FruitLabOnlineLayout.Button(createLobbyButton, gameSection, 144, width, true);
            FruitLabOnlineLayout.Button(joinWithCodeButton, gameSection, 84, width, false);
            FruitLabOnlineLayout.Button(quickJoinButton, gameSection, 24, width, false);
            createLobbyButton.SetText("Oyun Oluştur"); joinWithCodeButton.SetText("Oyuna Katıl"); quickJoinButton.SetText("Hemen Oyna");
            quickJoinButton.gameObject.SetActive(true);
            var disabled = quickJoinButton.GetComponent<CanvasGroup>();
            if (disabled == null) disabled = quickJoinButton.gameObject.AddComponent<CanvasGroup>();
            disabled.alpha = .4f; disabled.interactable = disabled.blocksRaycasts = false;
            var canvas = (RectTransform)GetComponentInParent<Canvas>().transform;
            dialogOverlay.anchorMin = dialogOverlay.anchorMax = dialogOverlay.pivot = Vector2.one * .5f;
            dialogOverlay.sizeDelta = canvas.rect.size; dialogOverlay.position = canvas.TransformPoint(canvas.rect.center);
            float popupWidth = Mathf.Min(319, width);
            FruitLabOnlineLayout.Center(createPopup, popupWidth, 252);
            FruitLabOnlineLayout.Center(joinPopup, popupWidth, 260);
            FruitLabOnlineLayout.Title(createPopup, "Oyun Oluştur", popupWidth, 252);
            FruitLabOnlineLayout.Title(joinPopup, "Oyuna Katıl", popupWidth, 260);
            float optionWidth = (popupWidth - 56) / 3;
            for (int i = 0; i < 3; i++)
                FruitLabStyle.Place(difficultyButtons[i].GetButtonRectTransform(), createPopup, 20 + i * (optionWidth + 8), 116, optionWidth, 46);
            SetSelectedDifficulty(selectedDifficulty);
            FruitLabOnlineLayout.Button(createConfirmButton, createPopup, 28, popupWidth, true); createConfirmButton.SetText("Oluştur");
            FruitLabOnlineLayout.Button(joinConfirmButton, joinPopup, 24, popupWidth, true); joinConfirmButton.SetText("Giriş");
            FruitLabOnlineLayout.Input(codeInputField, joinPopup, 128, popupWidth, "Kod giriniz.");
            codeInputField.characterLimit = 6;
            codeInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            codeInputField.characterValidation = TMP_InputField.CharacterValidation.Digit;
            codeInputField.keyboardType = TouchScreenKeyboardType.NumberPad;
            FruitLabStyle.Place(joinError.rectTransform, joinPopup, 20, 78, popupWidth - 40, 42);
            joinError.textWrappingMode = TextWrappingModes.Normal;
            foreach (var pair in new[] { (createCloseButton, createPopup), (joinCloseButton, joinPopup) })
            {
                FruitLabStyle.Place(pair.Item1.GetButtonRectTransform(), pair.Item2, popupWidth - 52, pair.Item2.rect.height - 52, 32, 32);
                pair.Item1.ApplyLabActionStyle(false); pair.Item1.SetText("×");
            }
            FruitLabOnlineLayout.Settings(menuButton, canvas);
            if (!Application.isPlaying) { playerNameInputField.SetTextWithoutNotify(""); codeInputField.SetTextWithoutNotify(""); }
            ApplySectionVisibility();
        }
        public void InitPlayerNameInputField(string playerName, Action<string> onPlayerNameChanged)
        {
            playerNameInputField.characterLimit = 16; codeInputField.characterLimit = 6;
            codeInputField.SetTextWithoutNotify(""); playerNameInputField.SetTextWithoutNotify(playerName);
            codeInputField.lineType = TMP_InputField.LineType.SingleLine;
            codeInputField.keyboardType = TouchScreenKeyboardType.NumberPad;
            playerNameInputField.onValueChanged.AddListener(onPlayerNameChanged.Invoke);
        }
    }
    public interface ILobbyUIView
    {
        IBaseButtonView GetDifficultyButton(int index);
        void SetSelectedDifficulty(Difficulty difficulty);
        void ShowCreateOptions(); void ShowJoinCodeInput(); void HideDialogs(); void SetJoinError(string message);
        bool IsJoinInputOpen { get; }
        bool ValidatePlayerName();
        void SetCodeSubmit(Action submit);
        IBaseButtonView GetMenuButton(); IBaseButtonView GetCreateLobbyButton(); IBaseButtonView GetQuickJoinButton(); IBaseButtonView GetJoinWithCodeButton();
        IBaseButtonView GetCreateConfirmButton(); IBaseButtonView GetJoinConfirmButton(); IBaseButtonView GetCreateCloseButton(); IBaseButtonView GetJoinCloseButton();
        string GetCodeInputField();
        void InitPlayerNameInputField(string playerName, Action<string> onPlayerNameChanged);
    }
}
