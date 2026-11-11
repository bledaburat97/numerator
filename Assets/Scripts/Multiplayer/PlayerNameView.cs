using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class PlayerNameView : MonoBehaviour, IPlayerNameView
    {
        [SerializeField] private TMP_Text playerName;
        [SerializeField] private Image readyImage;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private BaseButtonView kickButton;
        private TMP_Text _role, _readyLabel;
        private FruitLabGlyph _readyCheck;
        private PlayerAvatarView _avatar;
        private int _index;
        private bool _ready, _canKick, _menuOpen;
        private Button _remove;
        private Tween _entry;
        public string DisplayedName => playerName.text;
        public bool IsReadyVisible => _ready && readyImage.gameObject.activeInHierarchy;
        public void Init(int playerIndex, Action<int> onKickButtonClicked)
        {
            _index = playerIndex;
            _role = FriendUi.Text(transform, "PlayerRole", "", 13);
            _readyLabel = FriendUi.Text(readyImage.transform, "ReadyLabel", "", 15);
            _readyCheck = readyImage.transform.Find("Check")?.GetComponent<FruitLabGlyph>() ?? FruitLabGlyph.Create(readyImage.transform, FruitLabGlyph.Symbol.Check, FruitLabStyle.Mint);
            var legacy = readyImage.transform.Find("Image"); if (legacy != null) legacy.gameObject.SetActive(false);
            readyImage.gameObject.SetActive(true); readyImage.raycastTarget = false;
            FruitLabStyle.AddSurface(gameObject, FruitLabStyle.Tile); FruitLabStyle.Rim(rectTransform, .2f);
            playerName.alignment = TextAlignmentOptions.Midline; playerName.raycastTarget = false; playerName.richText = false;
            playerName.fontSize = 19; playerName.enableAutoSizing = true; playerName.fontSizeMin = 15; playerName.fontSizeMax = 19;
            playerName.color = FruitLabStyle.Text; playerName.overflowMode = TextOverflowModes.Ellipsis;
            _avatar = PlayerAvatarView.Ensure(transform); _avatar.Render(0);
            // The existing BaseButton is the actual removal action, behind a small options menu.
            _remove = FriendUi.Button(transform, "PlayerOptions", "…", () => { _menuOpen = !_menuOpen; ApplyRowLayout(); });
            kickButton.SetOnPointerDownCallBack(null); kickButton.SetOnPointerUpCallBack(null); kickButton.SetOnPointerClickCallBack(null);
            SetReadyStatus(false); ApplyRowLayout();
            if (Application.isPlaying)
            {
                var group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0; _entry = group.DOFade(1, .18f).SetLink(gameObject);
            }
        }
        public void SetAvatar(int avatar) { if (_avatar != null) _avatar.Render(avatar); }
        public void SetRole(bool isHost, bool isLocal, bool canKick)
        {
            _canKick = canKick;
            if (!canKick) _menuOpen = false;
            _role.text = isLocal ? "Sen" : "";
            ButtonAppearance.For(kickButton).SetActionStyle(false); kickButton.SetText("Çıkar");
            ApplyRowLayout();
        }
        public void ApplyRowLayout()
        {
            var parent = (RectTransform)transform.parent;
            int count = parent.GetComponent<PlayerNameAreaView>()?.VisibleSlots ?? 4;
            float width = (parent.rect.width - 10) * .5f;
            float height = count <= 2 ? Mathf.Min(194, parent.rect.height) : (parent.rect.height - 10) * .5f;
            rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.pivot = new Vector2(0, 1);
            rectTransform.localScale = Vector3.one; rectTransform.sizeDelta = new Vector2(width, height);
            rectTransform.anchoredPosition = new Vector2(_index % 2 * (width + 10), -(_index / 2) * (height + 10));
            float avatarSize = height > 160 ? 72 : Mathf.Min(44, Mathf.Max(28, height - 64));
            if (_avatar != null) FruitLabStyle.Place((RectTransform)_avatar.transform, transform, (width - avatarSize) * .5f, height - avatarSize - 8, avatarSize, avatarSize);
            FruitLabStyle.Place(playerName.rectTransform, transform, 5, height > 160 ? 53 : 29, width - 10, 24);
            if (_role != null) { _role.alignment = TextAlignmentOptions.TopLeft; FruitLabStyle.Place(_role.rectTransform, transform, 7, height - 24, 34, 18); }
            FruitLabStyle.Place(readyImage.rectTransform, transform, (width - 94) * .5f, height > 160 ? 16 : 4, 94, 26);
            if (_readyCheck != null) FruitLabStyle.Place(_readyCheck.rectTransform, readyImage.transform, 8, 5, 16, 16);
            if (_readyLabel != null) FruitLabStyle.Place(_readyLabel.rectTransform, readyImage.transform, _ready ? 25 : 3, 0, _ready ? 64 : 88, 26);
            if (_remove != null) { FruitLabStyle.Place((RectTransform)_remove.transform, transform, width - 44, height - 44, 44, 44); _remove.gameObject.SetActive(_canKick); }
            FruitLabStyle.Place(kickButton.GetButtonRectTransform(), transform, width - 94, height - 94, 94, 48);
            kickButton.SetButtonStatus(_canKick && _menuOpen); kickButton.transform.SetAsLastSibling();
        }
        public IBaseButtonView GetKickButton() => kickButton;
        public void Destroy() => Destroy(gameObject);
        public void SetPlayerName(string value) => playerName.text = value;
        public void SetReadyStatus(bool ready)
        {
            _ready = ready;
            FruitLabStyle.Surface(readyImage, ready ? Color.Lerp(FruitLabStyle.Tile, FruitLabStyle.Mint, .16f) : FruitLabStyle.Recess);
            if (_readyLabel != null) { _readyLabel.text = ready ? "Hazır" : "Bekliyor"; _readyLabel.color = ready ? FruitLabStyle.Mint : FruitLabStyle.Muted; }
            if (_readyCheck != null) _readyCheck.gameObject.SetActive(ready);
            ApplyRowLayout();
        }
        private void OnDestroy() => _entry?.Kill();
    }
    public interface IPlayerNameView
    {
        void Init(int index, Action<int> onKick);
        void SetAvatar(int avatar); void SetRole(bool isHost, bool isLocal, bool canKick);
        IBaseButtonView GetKickButton(); void Destroy(); void SetReadyStatus(bool ready); void SetPlayerName(string name);
    }
}
