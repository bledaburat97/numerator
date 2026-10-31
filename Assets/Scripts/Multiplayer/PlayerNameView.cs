using System;
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
        private int _index;
        private bool _ready, _canKick;
        public string DisplayedName => playerName.text;
        public bool IsReadyVisible => _ready && readyImage.gameObject.activeInHierarchy;
        public void Init(int playerIndex, Action<int> onKickButtonClicked)
        {
            _index = playerIndex;
            _role = transform.Find("PlayerRole")?.GetComponent<TMP_Text>() ?? FruitLabStyle.CreateText("PlayerRole", transform, 10f, FruitLabStyle.Muted);
            _readyLabel = readyImage.transform.Find("ReadyLabel")?.GetComponent<TMP_Text>() ?? FruitLabStyle.CreateText("ReadyLabel", readyImage.transform, 10f, FruitLabStyle.Muted);
            _readyCheck = readyImage.transform.Find("Check")?.GetComponent<FruitLabGlyph>() ?? FruitLabGlyph.Create(readyImage.transform, FruitLabGlyph.Symbol.Check, FruitLabStyle.Mint);
            var legacyCheck = readyImage.transform.Find("Image");
            if (legacyCheck != null) legacyCheck.gameObject.SetActive(false);
            readyImage.gameObject.SetActive(true); readyImage.raycastTarget = false;
            FruitLabStyle.AddSurface(gameObject, FruitLabStyle.Tile); FruitLabStyle.Rim(rectTransform, .2f);
            playerName.alignment = _role.alignment = TextAlignmentOptions.MidlineLeft;
            playerName.raycastTarget = false; playerName.fontSize = 16; playerName.enableAutoSizing = true;
            playerName.richText = false;
            playerName.fontSizeMin = 11; playerName.fontSizeMax = 16; playerName.color = FruitLabStyle.Text;
            SetReadyStatus(false); ApplyRowLayout();
        }
        public void SetRole(bool isHost, bool isLocal, bool canKick)
        {
            _canKick = canKick;
            kickButton.SetButtonStatus(canKick);
            kickButton.ApplyLabActionStyle(false); kickButton.SetText("×");
            foreach (var graphic in kickButton.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            foreach (var image in kickButton.GetComponentsInChildren<Image>(true))
                if (image.gameObject != kickButton.gameObject) image.enabled = false;
            FruitLabStyle.AddSurface(kickButton.gameObject, FruitLabStyle.Recess).raycastTarget = true;
            foreach (var label in kickButton.GetComponentsInChildren<TMP_Text>(true)) { label.fontSize = 24; label.color = FruitLabStyle.OpponentPlayer; }
            _role.text = (isHost ? "Kurucu" : "Katılımcı") + (isLocal ? " · Sen" : "");
            ApplyRowLayout();
        }
        public void ApplyRowLayout()
        {
            var parent = (RectTransform)transform.parent;
            float width = parent.rect.width, height = Mathf.Min(56f, Mathf.Max(42f, (parent.rect.height - 18f) / MultiplayerRules.MaxPlayers));
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(.5f, 1f);
            rectTransform.pivot = new Vector2(.5f, 1f); rectTransform.localScale = Vector3.one;
            rectTransform.sizeDelta = new Vector2(width, height); rectTransform.anchoredPosition = new Vector2(0, -_index * (height + 6f));
            float readyX = width - (_canKick ? 126f : 86f);
            FruitLabStyle.Place(playerName.rectTransform, transform, 12, height - 25, readyX - 20, 22);
            if (_role != null) FruitLabStyle.Place(_role.rectTransform, transform, 12, 3, readyX - 20, 13);
            FruitLabStyle.Place(readyImage.rectTransform, transform, readyX, (height - 28f) * .5f, 74, 28);
            FruitLabStyle.Place(kickButton.GetButtonRectTransform(), transform, width - 44, (height - 34f) * .5f, 34, 34);
            if (_readyCheck != null) FruitLabStyle.Place(_readyCheck.rectTransform, readyImage.transform, 5, 6, 16, 16);
            if (_readyLabel != null) FruitLabStyle.Place(_readyLabel.rectTransform, readyImage.transform, _ready ? 21 : 3, 0, _ready ? 50 : 68, 28);
        }
        public IBaseButtonView GetKickButton() => kickButton;
        public void SetPlayerName(string value) => playerName.text = value;
        public void Destroy() => Destroy(gameObject);
        public void SetReadyStatus(bool isReady)
        {
            _ready = isReady;
            FruitLabStyle.Surface(readyImage, isReady ? Color.Lerp(FruitLabStyle.Tile, FruitLabStyle.Mint, .16f) : FruitLabStyle.Recess);
            if (_readyLabel != null) { _readyLabel.text = isReady ? "Hazır" : "Bekliyor"; _readyLabel.color = isReady ? FruitLabStyle.Mint : FruitLabStyle.Muted; }
            if (_readyCheck != null) _readyCheck.gameObject.SetActive(isReady);
            ApplyRowLayout();
        }
    }
    public interface IPlayerNameView
    {
        void Init(int playerIndex, Action<int> onKickButtonClicked);
        void SetRole(bool isHost, bool isLocal, bool canKick);
        IBaseButtonView GetKickButton();
        void Destroy(); void SetReadyStatus(bool isReady); void SetPlayerName(string playerName);
    }
}
