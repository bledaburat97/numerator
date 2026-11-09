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
        [SerializeField] private RectTransform dialogOverlay, joinPopup;
        [SerializeField] private BaseButtonView joinConfirmButton, joinCloseButton;
        [SerializeField] private TMP_Text joinError;
        [SerializeField] private Difficulty selectedDifficulty;
        [SerializeField] private RectTransform profileSection, gameSection;
        private bool _joinOpen;
        private RectTransform _profileOverlay, _profilePopup, _inviteOverlay, _invitePopup;
        private string _profileName;
        private int _profileAvatar, _editingAvatar;
        private Action<string, int> _saveProfile;
        private Action _acceptInvite, _dismissInvite, _retryInvite;
        public void ConfigureProfile(string name, int avatar, Action<string, int> save)
        {
            _profileName = name; _profileAvatar = avatar; _saveProfile = save;
            RefreshProfile();
        }
        private void RefreshProfile()
        {
            if (profileSection == null) return;
            var button = FriendUi.Button(profileSection, "EditProfile", "", OpenProfile);
            FruitLabStyle.Place((RectTransform)button.transform, profileSection, 10, 10, profileSection.rect.width - 20, 80);
            var avatar = PlayerAvatarView.Ensure(button.transform); avatar.Render(_profileAvatar);
            FruitLabStyle.Place((RectTransform)avatar.transform, button.transform, 8, 8, 64, 64);
            var name = FriendUi.Text(button.transform, "ProfileName", _profileName ?? "Misafir", 21);
            name.alignment = TextAlignmentOptions.MidlineLeft; name.enableAutoSizing = true;
            name.fontSizeMin = 17; name.fontSizeMax = 21; name.overflowMode = TextOverflowModes.Ellipsis;
            FruitLabStyle.Place(name.rectTransform, button.transform, 86, 34, profileSection.rect.width - 120, 32);
            var edit = FriendUi.Text(button.transform, "EditHint", "Profili düzenle", 15); edit.color = FruitLabStyle.Muted;
            edit.alignment = TextAlignmentOptions.MidlineLeft;
            FruitLabStyle.Place(edit.rectTransform, button.transform, 86, 12, profileSection.rect.width - 120, 24);
        }
        public void OpenProfile()
        {
            var canvas = GetComponentInParent<Canvas>().transform;
            _profileOverlay = FriendUi.Overlay(canvas, "FriendProfileOverlay");
            _profilePopup = _profileOverlay.Find("ProfilePopup") as RectTransform ?? FruitLabOnlineLayout.Panel("ProfilePopup", _profileOverlay);
            float width = Mathf.Min(330, ((RectTransform)canvas).rect.width - 24);
            FruitLabOnlineLayout.Center(_profilePopup, width, 340);
            FriendUi.Text(_profilePopup, "Title", "Profilin", 24).rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            FruitLabStyle.Place((RectTransform)_profilePopup.Find("Title"), _profilePopup, 12, 292, width - 24, 36);
            FruitLabOnlineLayout.Input(playerNameInputField, _profilePopup, 230, width, "Kullanıcı adı giriniz.");
            playerNameInputField.gameObject.SetActive(true); playerNameInputField.SetTextWithoutNotify(_profileName);
            _editingAvatar = _profileAvatar;
            DrawAvatars();
            var save = FriendUi.Button(_profilePopup, "SaveProfile", "Tamam", () => {
                _saveProfile?.Invoke(playerNameInputField.text, _editingAvatar);
                var identity = PlayerIdentityStore.Current;
                _profileName = identity.name; _profileAvatar = identity.avatar;
                _profileOverlay.gameObject.SetActive(false); RefreshProfile();
            }, true);
            FruitLabStyle.Place((RectTransform)save.transform, _profilePopup, 20, 16, width - 40, 48);
            _profileOverlay.gameObject.SetActive(true);
        }
        private void DrawAvatars()
        {
            float width = _profilePopup.rect.width, cell = 52, gap = (width - 40 - cell * 3) * .5f;
            for (int i = 0; i < PlayerIdentityStore.AvatarCount; i++)
            {
                int index = i;
                var button = FriendUi.Button(_profilePopup, "AvatarChoice" + i, "", () => { _editingAvatar = index; DrawAvatars(); });
                FruitLabStyle.Place((RectTransform)button.transform, _profilePopup, 20 + i % 3 * (cell + gap), 156 - i / 3 * 66, cell, cell);
                var portrait = PlayerAvatarView.Ensure(button.transform); portrait.Render(i, i == _editingAvatar);
                FruitLabStyle.Stretch((RectTransform)portrait.transform);
            }
        }
        public void ConfigureInvitation(Action accept, Action dismiss, Action retry)
        { _acceptInvite = accept; _dismissInvite = dismiss; _retryInvite = retry; }
        public void ShowInvitation(FriendInvitePreview preview, string error, bool loading)
        {
            var canvas = GetComponentInParent<Canvas>().transform;
            _inviteOverlay = FriendUi.Overlay(canvas, "ReceivedFriendInvite");
            _invitePopup = _inviteOverlay.Find("Invitation") as RectTransform ?? FruitLabOnlineLayout.Panel("Invitation", _inviteOverlay);
            float width = Mathf.Min(330, ((RectTransform)canvas).rect.width - 24);
            FruitLabOnlineLayout.Center(_invitePopup, width, 346);
            var avatar = PlayerAvatarView.Ensure(_invitePopup); avatar.Render(preview?.Avatar ?? 0);
            FruitLabStyle.Place((RectTransform)avatar.transform, _invitePopup, (width - 82) * .5f, 234, 82, 82);
            var title = FriendUi.Text(_invitePopup, "Title", loading ? "Davet açılıyor…" : preview != null ? preview.HostName + " seni oyuna davet ediyor" : "Davet açılamadı", 23);
            title.enableAutoSizing = true; title.fontSizeMin = 20; title.fontSizeMax = 23;
            FruitLabStyle.Place(title.rectTransform, _invitePopup, 20, 164, width - 40, 64);
            var detail = FriendUi.Text(_invitePopup, "Detail", error ?? (preview != null ? MultiplayerRules.For(preview.Difficulty).Name : "Biraz bekle…"), 17);
            detail.color = error != null ? FruitLabStyle.OpponentPlayer : FruitLabStyle.Muted;
            FruitLabStyle.Place(detail.rectTransform, _invitePopup, 20, 104, width - 40, 56);
            var accept = FriendUi.Button(_invitePopup, "Accept", preview != null ? "Katıl ve oyna" : "Tekrar dene",
                preview != null ? _acceptInvite : _retryInvite, true); accept.interactable = !loading;
            FruitLabStyle.Place((RectTransform)accept.transform, _invitePopup, 20, 60, width - 40, 48);
            var close = FriendUi.Button(_invitePopup, "Close", "Kapat", _dismissInvite);
            FruitLabStyle.Place((RectTransform)close.transform, _invitePopup, 20, 8, width - 40, 44);
        }
        public void HideInvitation() { if (_inviteOverlay != null) _inviteOverlay.gameObject.SetActive(false); }
        public void SetResumeAction(bool available, Action resume)
        {
            var button = FriendUi.Button(gameSection, "ResumeInvitation", "Davetine dön", resume);
            FruitLabStyle.Place((RectTransform)button.transform, gameSection, 20, 12, gameSection.rect.width - 40, 44);
            button.gameObject.SetActive(available);
        }
        public bool IsJoinInputOpen => _joinOpen;
        public TMP_InputField CodeInput => codeInputField;
        public TMP_InputField NameInput => playerNameInputField;
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
            var oldCreate = dialogOverlay.Find("CreateGamePopup"); if (oldCreate != null) oldCreate.gameObject.SetActive(false);
            if (joinPopup == null) joinPopup = FruitLabOnlineLayout.Panel("JoinGamePopup", dialogOverlay);
            if (difficultyButtons == null || difficultyButtons.Length != 3) difficultyButtons = new BaseButtonView[3];
            for (int i = 0; i < 3; i++)
                EnsureButton(ref difficultyButtons[i], "Difficulty" + (Difficulty)i, gameSection != null ? gameSection : transform);
            EnsureButton(ref joinConfirmButton, "JoinConfirmButton", joinPopup);
            EnsureButton(ref joinCloseButton, "JoinCloseButton", joinPopup);
            if (joinError == null) joinError = FruitLabStyle.CreateText("JoinError", joinPopup, 12, FruitLabStyle.OpponentPlayer);
        }
        private void EnsureButton(ref BaseButtonView button, string name, Transform parent)
        {
            if (button == null) { button = Instantiate(createLobbyButton, parent); button.name = name; }

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
        public void ShowJoinCodeInput()
        {
            EnsureDialogs(); _joinOpen = true; SetJoinError(""); ApplySectionVisibility();
            if (Application.isPlaying) { codeInputField.Select(); codeInputField.ActivateInputField(); }
        }
        public void HideDialogs() { _joinOpen = false; ApplySectionVisibility(); }
        private void ApplySectionVisibility()
        {
            dialogOverlay.gameObject.SetActive(_joinOpen);
            joinPopup.gameObject.SetActive(_joinOpen);
            codeInputField.gameObject.SetActive(_joinOpen);
            dialogOverlay.SetAsLastSibling();
        }
        public void SetJoinError(string message) { EnsureDialogs(); joinError.text = message; }
        public IBaseButtonView GetMenuButton() => menuButton;
        public IBaseButtonView GetCreateLobbyButton() => createLobbyButton;
        public IBaseButtonView GetQuickJoinButton() => quickJoinButton;
        public IBaseButtonView GetJoinWithCodeButton() => joinWithCodeButton;
        public IBaseButtonView GetJoinConfirmButton() { EnsureDialogs(); return joinConfirmButton; }
        public IBaseButtonView GetJoinCloseButton() { EnsureDialogs(); return joinCloseButton; }
        public string GetCodeInputField() => codeInputField.text;
        public void SetCodeSubmit(Action submit) => codeInputField.onSubmit.AddListener(_ => submit());
        public void ApplyOnlineLayout(float width, float height)
        {
            EnsureDialogs();
            if (profileSection == null) profileSection = FruitLabOnlineLayout.Panel("ProfileSection", transform);
            if (gameSection == null) gameSection = FruitLabOnlineLayout.Panel("GameSection", transform);
            FruitLabStyle.Place(profileSection, transform, 0, height - 100, width, 100);
            FruitLabStyle.Place(gameSection, transform, 0, 0, width, height - 116);
            foreach (var section in new[] { profileSection, gameSection })
            { var old = section.Find("OnlineTitle"); if (old != null) old.gameObject.SetActive(false); }
            if (_profilePopup == null) playerNameInputField.gameObject.SetActive(false);
            RefreshProfile();
            var heading = FriendUi.Text(gameSection, "InviteHeading", "Birlikte çözün", 24);
            FruitLabStyle.Place(heading.rectTransform, gameSection, 16, gameSection.rect.height - 54, width - 32, 38);
            FruitLabOnlineLayout.Button(createLobbyButton, gameSection, 112, width, true);
            FruitLabOnlineLayout.Button(joinWithCodeButton, gameSection, 60, width, false);
            createLobbyButton.SetText("Davet et"); joinWithCodeButton.SetText("Kodla katıl");
            quickJoinButton.gameObject.SetActive(false);
            var canvas = (RectTransform)GetComponentInParent<Canvas>().transform;
            dialogOverlay.anchorMin = dialogOverlay.anchorMax = dialogOverlay.pivot = Vector2.one * .5f;
            dialogOverlay.sizeDelta = canvas.rect.size; dialogOverlay.position = canvas.TransformPoint(canvas.rect.center);
            float popupWidth = Mathf.Min(319, width);
            FruitLabOnlineLayout.Center(joinPopup, popupWidth, 260);
            FruitLabOnlineLayout.Title(joinPopup, "Oyuna Katıl", popupWidth, 260);
            float optionWidth = (popupWidth - 56) / 3;
            for (int i = 0; i < 3; i++)
                FruitLabStyle.Place(difficultyButtons[i].GetButtonRectTransform(), gameSection, 20 + i * (optionWidth + 8), 178, optionWidth, 46);
            SetSelectedDifficulty(selectedDifficulty);
            FruitLabOnlineLayout.Button(joinConfirmButton, joinPopup, 24, popupWidth, true); joinConfirmButton.SetText("Giriş");
            FruitLabOnlineLayout.Input(codeInputField, joinPopup, 128, popupWidth, "Kod giriniz.");
            codeInputField.characterLimit = 6;
            codeInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            codeInputField.characterValidation = TMP_InputField.CharacterValidation.Digit;
            codeInputField.keyboardType = TouchScreenKeyboardType.NumberPad;
            FruitLabStyle.Place(joinError.rectTransform, joinPopup, 20, 78, popupWidth - 40, 42);
            joinError.textWrappingMode = TextWrappingModes.Normal;
            FruitLabStyle.Place(joinCloseButton.GetButtonRectTransform(), joinPopup, popupWidth - 56, joinPopup.rect.height - 56, 44, 44);
            joinCloseButton.ApplyLabActionStyle(false); joinCloseButton.SetText("×");
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
            playerNameInputField.onValueChanged.RemoveAllListeners();
        }
    }
    public interface ILobbyUIView
    {
        void ConfigureProfile(string name, int avatar, Action<string, int> save);
        void ConfigureInvitation(Action accept, Action dismiss, Action retry);
        void ShowInvitation(FriendInvitePreview preview, string error, bool loading); void HideInvitation();
        void SetResumeAction(bool available, Action resume);
        IBaseButtonView GetDifficultyButton(int index);
        void SetSelectedDifficulty(Difficulty difficulty);
        void ShowJoinCodeInput(); void HideDialogs(); void SetJoinError(string message);
        bool IsJoinInputOpen { get; }
        void SetCodeSubmit(Action submit);
        IBaseButtonView GetMenuButton(); IBaseButtonView GetCreateLobbyButton(); IBaseButtonView GetQuickJoinButton(); IBaseButtonView GetJoinWithCodeButton();
        IBaseButtonView GetJoinConfirmButton(); IBaseButtonView GetJoinCloseButton();
        string GetCodeInputField();
        void InitPlayerNameInputField(string playerName, Action<string> onPlayerNameChanged);
    }
}
