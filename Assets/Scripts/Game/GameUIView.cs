using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class GameUIView : MonoBehaviour, IGameUIView
    {
        [SerializeField] private TMP_Text userText;
        [SerializeField] private TMP_Text opponentText;
        [SerializeField] private RectTransform scrollAreaRectTransform;
        [SerializeField] private BaseButtonView checkButton;
        [SerializeField] private BaseButtonView resetButton;
        [SerializeField] private BaseButtonView settingsButton;
        [SerializeField] private CardInfoButtonView cardInfoButton;
        [SerializeField] private GameObject opponentInfo;
        [SerializeField] private CanvasGroup topButtonsCanvasGroup;
        [SerializeField] private CanvasGroup middleButtonsCanvasGroup;
        [SerializeField] private Image coinImage;
        [SerializeField] private TextHolderAdjustment coinCountHolder;

        private bool _multiplayerMode;
        public bool IsMultiplayer => _multiplayerMode;
        private RectTransform _matchHud;
        private TMP_Text _turnLabel, _timeLabel, _opponentNameLabel;
        private RectTransform _timerChip;
        public string MultiplayerStatusText => _turnLabel != null ? _turnLabel.text : "";
        public string MultiplayerTimerText => _timeLabel != null ? _timeLabel.text : "";
        private double _deadline;
        public void SetMultiplayerStatus(string value, double deadline)
        {
            EnsureMultiplayerHud(); _turnLabel.text = value; _deadline = deadline; Update();
        }
        public void SetMultiplayerTurn(string value, double deadline, bool localTurn)
        {
            SetMultiplayerStatus(value, deadline);
            _turnLabel.color = deadline <= 0 ? FruitLabStyle.Muted : localTurn ? FruitLabStyle.LocalPlayer : FruitLabStyle.OpponentPlayer;
        }
        private void EnsureMultiplayerHud()
        {
            if (_matchHud != null) return;
            _matchHud = new GameObject("MultiplayerStatus", typeof(RectTransform)).GetComponent<RectTransform>();
            _matchHud.SetParent(transform, false); FruitLabStyle.AddSurface(_matchHud.gameObject, FruitLabStyle.Panel);
            _turnLabel = FruitLabStyle.CreateText("Turn", _matchHud, 13f, FruitLabStyle.Mint);
            _turnLabel.enableAutoSizing = true; _turnLabel.fontSizeMin = 10; _turnLabel.fontSizeMax = 13;
            _timerChip = new GameObject("Timer", typeof(RectTransform)).GetComponent<RectTransform>();
            _timerChip.SetParent(_matchHud, false); FruitLabStyle.AddSurface(_timerChip.gameObject, FruitLabStyle.Recess);
            _timeLabel = FruitLabStyle.CreateText("Time", _timerChip, 16f, FruitLabStyle.Text);
            _opponentNameLabel = FruitLabStyle.CreateText("MultiplayerOpponent", transform, 15f, FruitLabStyle.Muted);
        }
        private void Update()
        {
            if (!_multiplayerMode || _timeLabel == null) return;
            double now = Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening
                ? Unity.Netcode.NetworkManager.Singleton.ServerTime.Time : 0;
            _timeLabel.text = _deadline > 0 ? Mathf.CeilToInt((float)System.Math.Max(0, _deadline - now)) + " sn" : "";
            _timeLabel.color = _deadline > 0 && _deadline - now <= 10 ? FruitLabStyle.OpponentPlayer : FruitLabStyle.Text;
            _timerChip.gameObject.SetActive(_deadline > 0);

        }

        public void ApplyLabLayout(RectTransform header, RectTransform actionArea, float width, float headerHeight, float actionWidth, float actionHeight)
        {
            opponentInfo.SetActive(false);
            if (_multiplayerMode)
            {
                EnsureMultiplayerHud();
                FruitLabStyle.Place(_matchHud, header, 0f, 0f, width, 32f);
                FruitLabStyle.Place(_turnLabel.rectTransform, _matchHud, 10f, 0f, width - 92f, 32f);
                FruitLabStyle.Place(_timerChip, _matchHud, width - 78f, 3f, 74f, 26f);
                FruitLabStyle.Stretch(_timeLabel.rectTransform, 1f);
                FruitLabStyle.Place(_opponentNameLabel.rectTransform, header, width * .5f - 4f, headerHeight - 34f, width * .5f - 40f, 30f);
                _opponentNameLabel.text = opponentText.text;
                _opponentNameLabel.enableAutoSizing = true; _opponentNameLabel.fontSizeMin = 10; _opponentNameLabel.fontSizeMax = 15;
            }
            if (_matchHud != null) _matchHud.gameObject.SetActive(_multiplayerMode);
            if (_opponentNameLabel != null) _opponentNameLabel.gameObject.SetActive(_multiplayerMode);
            FruitLabStyle.Place(userText.rectTransform, header, 0f, headerHeight - 34f, _multiplayerMode ? width * .5f - 20f : width - 180f, 30f);
            userText.fontSize = _multiplayerMode ? 15f : 19f;
            userText.enableAutoSizing = false;
            userText.fontStyle = FontStyles.Bold;
            userText.color = FruitLabStyle.Text;
            userText.alignment = TextAlignmentOptions.MidlineLeft;
            FruitLabStyle.Place(settingsButton.GetButtonRectTransform(), header, width - 32f, headerHeight - 35f, 32f, 32f);
            settingsButton.ApplyLabStyle();
            settingsButton.SetLabIconScale(1.5f);
            if (coinCountHolder != null)
            {
                RectTransform counter = (RectTransform)coinCountHolder.transform;
                FruitLabStyle.Place(counter, header, width - 169f, headerHeight - 37f, 124f, 36f);
                FruitLabStyle.AddSurface(counter.gameObject, FruitLabStyle.Panel);
                coinCountHolder.SetFixedWidth(124f);
                foreach (Image background in counter.GetComponentsInChildren<Image>(true))
                    if (background != coinImage) FruitLabStyle.Surface(background, FruitLabStyle.Panel);
                foreach (TMP_Text label in counter.GetComponentsInChildren<TMP_Text>(true))
                {
                    label.fontSize = 24f;
                    label.enableAutoSizing = false;
                    label.color = FruitLabStyle.Text;
                    label.alignment = TextAlignmentOptions.MidlineLeft;
                    FruitLabStyle.Place(label.rectTransform, counter, 42f, 0f, 78f, 36f);
                }
                RectTransform coinWidget = coinImage.rectTransform;
                while (coinWidget.parent != null && coinWidget.parent != header && coinWidget.parent != counter)
                    coinWidget = (RectTransform)coinWidget.parent;
                FruitLabStyle.Place(coinWidget, counter, 6f, 3f, 30f, 30f);
                if (coinWidget != coinImage.rectTransform) FruitLabStyle.Stretch(coinImage.rectTransform);
            }
            FruitLabStyle.SetIcon(coinImage, "Coin");
            if (_multiplayerMode) SetCoinCounterStatus(false);
            FruitLabStyle.Place(checkButton.GetButtonRectTransform(), actionArea, width - actionWidth, 0f, actionWidth, actionHeight);
            checkButton.ApplyLabStyle(true);
            FruitLabStyle.Place(resetButton.GetButtonRectTransform(), actionArea, 0f, 0f, actionWidth, actionHeight);
            resetButton.ApplyLabStyle();
        }

        public CanvasGroup GetTopButtonsCanvasGroup() => topButtonsCanvasGroup;
        public CanvasGroup GetMiddleButtonsCanvasGroup() => middleButtonsCanvasGroup;
        
        public void SetUserText(string text)
        {
            userText.gameObject.SetActive(true);
            userText.SetText(text);
        }
        
        public void SetOpponentText(string text)
        {
            opponentText.gameObject.SetActive(true);
            opponentText.SetText(text);
        }

        public void SetOpponentInfoStatus(bool status)
        {
            _multiplayerMode = status;
            opponentInfo.SetActive(false);
            GetComponentInParent<Canvas>()?.GetComponent<FruitLabGameLayout>()?.ApplyLayout();
        }

        public IBaseButtonView GetCheckButton()
        {
            return checkButton;
        }
        
        public IBaseButtonView GetResetButton()
        {
            return resetButton;
        }
        
        public IBaseButtonView GetSettingsButton()
        {
            return settingsButton;
        }

        public ICardInfoButtonView GetCardInfoButton()
        {
            return cardInfoButton;
        }
        
        public TMP_Text GetUserText()
        {
            return userText;
        }

        public void SetCoinCounterStatus(bool status)
        {
            if (coinImage != null)
            {
                coinImage.gameObject.SetActive(status);
            }

            if (coinCountHolder != null)
            {
                coinCountHolder.gameObject.SetActive(status);
            }
        }

        public void SetCoinCount(int count)
        {
            if (coinCountHolder == null) return;

            coinCountHolder.SetText(count.ToString());
            coinCountHolder.SetPosition();
        }

        public RectTransform GetCoinImageRectTransform()
        {
            return coinImage != null ? coinImage.rectTransform : null;
        }

        public Sequence PunchCoinCounter(float duration)
        {
            Sequence sequence = DOTween.Sequence();
            if (coinImage != null)
            {
                sequence.Join(coinImage.rectTransform.DOPunchScale(Vector3.one * 0.08f, duration, 5, 0.65f));
            }

            if (coinCountHolder != null)
            {
                sequence.Join(coinCountHolder.transform.DOPunchScale(Vector3.one * 0.06f, duration, 5, 0.65f));
            }

            return sequence;
        }
    }

    public interface IGameUIView
    {
        void SetUserText(string text);
        void SetOpponentText(string text);
        IBaseButtonView GetCheckButton();
        IBaseButtonView GetResetButton();
        IBaseButtonView GetSettingsButton();
        ICardInfoButtonView GetCardInfoButton();
        void SetOpponentInfoStatus(bool status);
        CanvasGroup GetTopButtonsCanvasGroup();
        CanvasGroup GetMiddleButtonsCanvasGroup();
        TMP_Text GetUserText();
        void SetCoinCounterStatus(bool status);
        void SetCoinCount(int count);
        RectTransform GetCoinImageRectTransform();
        Sequence PunchCoinCounter(float duration);
    }
}
