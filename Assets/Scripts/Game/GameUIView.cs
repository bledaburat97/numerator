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

        [SerializeField] private PairClueView pairClue;
        private Button _informationButton;
        public Button GetInformationButton()
        {
            if(_informationButton!=null)return _informationButton;
            var old=settingsButton.transform.parent.Find("InformationButton")??transform.Find("InformationButton");
            var item=old!=null?old.gameObject:new GameObject("InformationButton",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button));
            if(old==null)item.transform.SetParent(transform,false);
            _informationButton=item.GetComponent<Button>();FruitLabStyle.AddSurface(item,FruitLabStyle.Tile).raycastTarget=true;
            var glyph=item.GetComponentInChildren<FruitLabGlyph>(true)??FruitLabGlyph.Create(item.transform,FruitLabGlyph.Symbol.Information,FruitLabStyle.Text);
            glyph.Type=FruitLabGlyph.Symbol.Information;glyph.color=Color.white;glyph.SetAllDirty();
            FruitLabStyle.Stretch(glyph.rectTransform,6);return _informationButton;
        }
        public PairClueView GetPairView() => pairClue;
        public bool HasPairClue => pairClue != null && pairClue.gameObject.activeSelf && !_multiplayerMode;
        public void SetPairClue(PairClue clue)
        {
            if (pairClue == null) return;
            pairClue.Show(_multiplayerMode ? null : clue);
            pairClue.RefreshRope(0f);
        }
        public bool HasPositionIndicators { get; private set; }
        public void SetPositionIndicators(bool visible)
        {
            if (HasPositionIndicators == visible) return;
            HasPositionIndicators = visible;
            GetComponentInParent<Canvas>()?.GetComponent<FruitLabGameLayout>()?.ApplyLayout();
        }

        private bool _dynamiteFooter;
        public bool HasDynamiteFooter => _dynamiteFooter && !_multiplayerMode;
        public bool SetDynamiteLayout(bool visible)
        {
            if (_dynamiteFooter == visible) return false;
            _dynamiteFooter = visible;
            GetComponentInParent<Canvas>()?.GetComponent<FruitLabGameLayout>()?.ApplyLayout();
            return true;
        }

        private TMP_Text _mechanicLabel;
        public bool HasMechanicStatus => _mechanicLabel != null && _mechanicLabel.gameObject.activeSelf;
        public void SetMechanicStatus(string text)
        {
            bool wasVisible = HasMechanicStatus;
            bool visible = !string.IsNullOrEmpty(text) && !_multiplayerMode;
            if (_mechanicLabel != null && _mechanicLabel.text == text && wasVisible == visible) return;
            if (_mechanicLabel == null) _mechanicLabel = userText.transform.parent.Find("MechanicStatus")?.GetComponent<TMP_Text>()
                ?? FruitLabStyle.CreateText("MechanicStatus", userText.transform.parent, 14f, FruitLabStyle.Mint);
            _mechanicLabel.text = text;
            _mechanicLabel.raycastTarget = false;
            _mechanicLabel.enableAutoSizing = true; _mechanicLabel.fontSizeMin = 12; _mechanicLabel.fontSizeMax = 14;
            _mechanicLabel.gameObject.SetActive(visible);
            PlaceMechanicStatus();
            if (wasVisible != visible) GetComponentInParent<Canvas>()?.GetComponent<FruitLabGameLayout>()?.ApplyLayout();
        }
        public RectTransform MechanicStatusRect=>_mechanicLabel?.rectTransform;
        private void PlaceMechanicStatus()
        {
            if (_mechanicLabel == null) return;
            var header = (RectTransform)userText.transform.parent;
            FruitLabStyle.Place(_mechanicLabel.rectTransform, header, 0, 34f, Mathf.Max(1,header.rect.width-106), 14f);
        }

        private bool _multiplayerMode;
        public bool IsMultiplayer => _multiplayerMode;
        private PlayerAvatarView _turnAvatar;
        private Sequence _turnFeedback,_timerFeedback;
        private bool _localTurn;
        private string _lastTurnStatus;
        private int _lastTimerPulse=-1;
        public void SetMultiplayerAvatar(int avatar)
        {
            EnsureMultiplayerHud();
            if (_turnAvatar == null) _turnAvatar = PlayerAvatarView.Ensure(_matchHud, "TurnAvatar");
            _turnAvatar.Render(avatar);
            FruitLabStyle.Place((RectTransform)_turnAvatar.transform, _matchHud, 3, 2, 28, 28);
            FruitLabStyle.Place(_turnLabel.rectTransform, _matchHud, 36, 0, _matchHud.rect.width - 118, 32);
        }
        private RectTransform _matchHud;
        private TMP_Text _turnLabel, _timeLabel;
        private RectTransform _timerChip;
        public string MultiplayerStatusText => _turnLabel != null ? _turnLabel.text : "";
        public string MultiplayerTimerText => _timeLabel != null ? _timeLabel.text : "";
        public void SetMultiplayerStatus(string value)
        {
            EnsureMultiplayerHud();
            if (_turnLabel.text != value) _turnLabel.text = value;
        }
        public void SetMultiplayerTurn(string value, bool timed, bool localTurn)
        {
            bool changed=_lastTurnStatus!=value || _localTurn!=localTurn;
            _lastTurnStatus=value;_localTurn=localTurn;
            SetMultiplayerStatus(value);
            _turnLabel.color = !timed ? FruitLabStyle.Muted : localTurn ? FruitLabStyle.LocalPlayer : FruitLabStyle.OpponentPlayer;
            if(changed && timed)
            {
                _turnFeedback?.Kill();_turnLabel.rectTransform.localScale=Vector3.one;
                _turnFeedback=DOTween.Sequence().Append(_turnLabel.rectTransform.DOPunchScale(Vector3.one*.065f,.28f,3,.6f));
                if(_turnAvatar!=null)
                {
                    _turnAvatar.transform.localScale=Vector3.one;
                    _turnFeedback.Join(_turnAvatar.transform.DOPunchScale(Vector3.one*.13f,.28f,3,.6f));
                }
                _turnFeedback.SetLink(gameObject,LinkBehaviour.KillOnDisable);
            }
        }
        public void SetMultiplayerTimer(int seconds, bool visible)
        {
            EnsureMultiplayerHud();
            _timeLabel.text = visible ? seconds + " sn" : "";
            _timeLabel.color = visible && seconds <= 10 ? FruitLabStyle.OpponentPlayer : FruitLabStyle.Text;
            if (_timerChip.gameObject.activeSelf != visible) _timerChip.gameObject.SetActive(visible);
            if(!visible || !_localTurn || seconds>5 || seconds<=0)
            {
                _timerFeedback?.Kill();_timerChip.localScale=Vector3.one;_lastTimerPulse=-1;
            }
            else if(_lastTimerPulse!=seconds)
            {
                _lastTimerPulse=seconds;_timerFeedback?.Kill();_timerChip.localScale=Vector3.one;
                _timerFeedback=DOTween.Sequence().Append(_timerChip.DOPunchScale(Vector3.one*.07f,.22f,2,.5f))
                    .SetLink(gameObject,LinkBehaviour.KillOnDisable);
            }
        }
        private void EnsureMultiplayerHud()
        {
            if (_matchHud != null) return;
            _matchHud = new GameObject("MultiplayerStatus", typeof(RectTransform)).GetComponent<RectTransform>();
            _matchHud.SetParent(transform, false); FruitLabStyle.AddSurface(_matchHud.gameObject, FruitLabStyle.Panel);
            _turnLabel = FruitLabStyle.CreateText("Turn", _matchHud, 16f, FruitLabStyle.Mint);
            _turnLabel.richText = false;
            _turnLabel.enableAutoSizing = true; _turnLabel.fontSizeMin = 14; _turnLabel.fontSizeMax = 16;
            _timerChip = new GameObject("Timer", typeof(RectTransform)).GetComponent<RectTransform>();
            _timerChip.SetParent(_matchHud, false); FruitLabStyle.AddSurface(_timerChip.gameObject, FruitLabStyle.Recess);
            _timeLabel = FruitLabStyle.CreateText("Time", _timerChip, 16f, FruitLabStyle.Text);
        }
        public void ApplyLabLayout(RectTransform header, RectTransform actionArea, float width, float headerHeight, float actionWidth, float actionHeight)
        {
            opponentInfo.SetActive(false);
            PlaceMechanicStatus();
            if (_mechanicLabel != null && _multiplayerMode) _mechanicLabel.gameObject.SetActive(false);
            if (_multiplayerMode)
            {
                EnsureMultiplayerHud();
                FruitLabStyle.Place(_matchHud, header, 0f, 0f, width, 32f);
                FruitLabStyle.Place(_turnLabel.rectTransform, _matchHud, _turnAvatar != null ? 36 : 10, 0f, width - (_turnAvatar != null ? 118 : 92), 32f);
                FruitLabStyle.Place(_timerChip, _matchHud, width - 78f, 3f, 74f, 26f);
                FruitLabStyle.Stretch(_timeLabel.rectTransform, 1f);
            }
            if (_matchHud != null) _matchHud.gameObject.SetActive(_multiplayerMode);
            FruitLabStyle.Place(userText.rectTransform, header, 0f, headerHeight - 34f, _multiplayerMode ? width - 102f : width - 227f, 30f);
            userText.fontSize = _multiplayerMode ? 15f : width<330?16f:19f;
            userText.textWrappingMode=TextWrappingModes.NoWrap;
            userText.enableAutoSizing = !_multiplayerMode;userText.fontSizeMin=14;userText.fontSizeMax=width<330?16:19;
            userText.fontStyle = FontStyles.Bold;
            userText.color = FruitLabStyle.Text;
            userText.alignment = TextAlignmentOptions.MidlineLeft;
            FruitLabStyle.Place(settingsButton.GetButtonRectTransform(), header, width - 48f, headerHeight - 48f, 48f, 48f);
            var information=GetInformationButton();
            FruitLabStyle.Place((RectTransform)information.transform,header,width-100f,headerHeight-48f,48f,48f);
            settingsButton.ApplyLabStyle();
            settingsButton.SetLabIconInset(6f);
            if (coinCountHolder != null)
            {
                RectTransform counter = (RectTransform)coinCountHolder.transform;
                FruitLabStyle.Place(counter, header, width - 219f, headerHeight - 39f, 111f, 36f);
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
            if (status && pairClue != null) pairClue.Show(null);
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
        void SetMultiplayerAvatar(int avatar);
        void SetMultiplayerStatus(string text);
        void SetMultiplayerTurn(string text, bool showTimer, bool localTurn);
        void SetMultiplayerTimer(int remainingSeconds, bool visible);
        void SetUserText(string text);
        void SetOpponentText(string text);
        IBaseButtonView GetCheckButton();
        IBaseButtonView GetResetButton();
        IBaseButtonView GetSettingsButton();
        Button GetInformationButton();
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
