using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class ResultBlockView : MonoBehaviour, IResultBlockView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private NonDraggableCardItemView nonDraggableCardItemPrefab;
        [SerializeField] private RectTransform cardsHolder;
        [SerializeField] private RectTransform topResultHolder;
        [SerializeField] private RectTransform cardsHolderBgImageRectTransform;
        [SerializeField] private Image playerStripe;
        public bool? IsLocalGuess { get; private set; }

        public int HistoryIndex { get; set; } = -1;
        private readonly List<INonDraggableCardItemView> _cardPool = new List<INonDraggableCardItemView>();
        private readonly List<FruitLabGlyph> _absentMarks = new List<FruitLabGlyph>();
        private readonly List<INonDraggableCardItemView> _cards = new List<INonDraggableCardItemView>();
        private NonDraggableCardItemViewFactory _cardFactory;
        private TMP_Text _correctCount, _misplacedCount;
        private Sequence _feedbackIntro,_hintFeedback;
        private Image _hintGlow;
        public void PlayHintFeedback()
        {
            _hintFeedback?.Kill();
            if(_hintGlow==null)_hintGlow=FruitLabStyle.Detail(transform,"HintGlow",FruitLabStyle.Gold,true);
            FruitLabStyle.Stretch(_hintGlow.rectTransform,1);_hintGlow.sprite=FruitLabStyle.ResultFrame;
            _hintGlow.color=new Color(FruitLabStyle.Gold.r,FruitLabStyle.Gold.g,FruitLabStyle.Gold.b,0);
            _hintFeedback=DOTween.Sequence().Append(_hintGlow.DOFade(.9f,.12f)).Append(_hintGlow.DOFade(0,.50f))
                .SetLink(gameObject,LinkBehaviour.KillOnDisable);
        }
        public bool IsFeedbackAnimating=>_feedbackIntro!=null && _feedbackIntro.IsActive() && _feedbackIntro.IsPlaying();
        public void PlayFeedbackIntro()
        {
            StopFeedbackIntro();
            _feedbackIntro=DOTween.Sequence().SetLink(gameObject,LinkBehaviour.KillOnDisable);
            if(IsSplit)for(int i=0;i<_groupViews.Length;i++)_groupViews[i].JoinIntro(_feedbackIntro,i*.06f);
            else {JoinCountIntro(_feedbackIntro,_correctCount,_correctIcon,FruitLabStyle.Mint,0);JoinCountIntro(_feedbackIntro,_misplacedCount,_misplacedIcon,FruitLabStyle.Gold,.12f);}
        }
        private static void JoinCountIntro(Sequence sequence,TMP_Text label,Graphic icon,Color color,float delay)
        {
            if(label==null)return;
            label.alpha=0;
            if(icon!=null){var tint=icon.color;tint.a=0;icon.color=tint;sequence.Insert(delay,icon.DOFade(1,.10f));}
            sequence.Insert(delay,label.rectTransform.DOScale(1.10f,.09f).SetEase(Ease.OutQuad));
            sequence.Insert(delay,label.DOColor(color,.09f));
            sequence.Insert(delay+.09f,label.rectTransform.DOScale(1f,.13f).SetEase(Ease.OutQuad));
            sequence.Insert(delay+.09f,label.DOColor(FruitLabStyle.Text,.13f));
        }
        private static void ResetIcon(Graphic icon){if(icon!=null){var tint=icon.color;tint.a=1;icon.color=tint;}}
        private void StopFeedbackIntro()
        {
            _feedbackIntro?.Kill();_feedbackIntro=null;
            _hintFeedback?.Kill();_hintFeedback=null;if(_hintGlow!=null){var tint=_hintGlow.color;tint.a=0;_hintGlow.color=tint;}
            ResetCount(_correctCount);ResetCount(_misplacedCount);ResetIcon(_correctIcon);ResetIcon(_misplacedIcon);
            if(_groupViews!=null)foreach(var group in _groupViews)group.ResetIntro();
        }
        private static void ResetCount(TMP_Text label){if(label!=null){label.rectTransform.localScale=Vector3.one;label.color=FruitLabStyle.Text;}}
        private void OnDisable()=>StopFeedbackIntro();
        private TMP_Text _playerLabel;
        private Image _evidenceFrame;
        public void SetEvidenceHighlight(bool visible)
        {
            if (_evidenceFrame == null && !visible) return;
            if (_evidenceFrame == null) _evidenceFrame = FruitLabStyle.Detail(transform, "DeductionEvidence", FruitLabStyle.Gold, true);
            FruitLabStyle.Stretch(_evidenceFrame.rectTransform, 1);
            _evidenceFrame.raycastTarget = false; _evidenceFrame.gameObject.SetActive(visible);
            if (visible) _evidenceFrame.transform.SetAsLastSibling();
        }
        private RectTransform _feedback;
        private FruitLabGlyph _correctIcon, _misplacedIcon;
        private float _width, _height;
        private IReadOnlyList<GuessGroupFeedback> _groups;
        private GroupFeedbackView[] _groupViews;
        public bool IsSplit => _groups != null && _groups.Count == 2;
        public void ConfigureGroups(IReadOnlyList<GuessGroupFeedback> groups) => _groups = groups;

        public void Init(NonDraggableCardItemViewFactory cardFactory, float width, float height)
        {
            _cardFactory = cardFactory;
            rectTransform.localScale = Vector3.one;
            // Only aggregate counts are visible during play.
            if (topResultHolder != null) topResultHolder.parent.gameObject.SetActive(false);
            foreach (LayoutGroup group in cardsHolder.GetComponents<LayoutGroup>()) group.enabled = false;
            ContentSizeFitter fitter = cardsHolder.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.enabled = false;
            FruitLabStyle.Stretch(cardsHolderBgImageRectTransform, 1f);
            FruitLabStyle.Surface(cardsHolderBgImageRectTransform.GetComponent<Image>(), FruitLabStyle.Tile);
            cardsHolderBgImageRectTransform.gameObject.SetActive(!IsSplit);

            if (_feedback == null)
            {
                _feedback = new GameObject("Feedback", typeof(RectTransform)).GetComponent<RectTransform>();
                _feedback.SetParent(transform, false);
                _correctIcon = FruitLabGlyph.Create(_feedback, FruitLabGlyph.Symbol.Check, FruitLabStyle.Mint);
                _misplacedIcon = FruitLabGlyph.Create(_feedback, FruitLabGlyph.Symbol.Swap, FruitLabStyle.Gold);
                _correctCount = FruitLabStyle.CreateText("CorrectCount", _feedback, 14f, FruitLabStyle.Text);
                _misplacedCount = FruitLabStyle.CreateText("MisplacedCount", _feedback, 14f, FruitLabStyle.Text);
            }
            if (IsSplit && _groupViews == null)
                _groupViews = new[] {
                    new GroupFeedbackView(transform,"SplitGroupA",FruitLabStyle.SplitFirstGroup),
                    new GroupFeedbackView(transform,"SplitGroupB",FruitLabStyle.SplitSecondGroup)
                };
            _feedback.gameObject.SetActive(!IsSplit);
            if (_groupViews != null) foreach (var group in _groupViews) group.SetActive(IsSplit);
            Resize(width, height);
        }

        public void Bind(ResultBlockModel model, float width, float height)
        {
            ResetForReuse();
            ConfigureGroups(model.groups);
            Init(_cardFactory ?? new NonDraggableCardItemViewFactory(), width, height);
            foreach (int number in model.finalNumbers)
            {
                var card = CreateCardItem(); if(number==GuessSubmission.FrozenMarker)card.InitFrozen();else card.Init(number); card.InitLocalScale(); card.SetAlpha(1);
            }
            DisplayFeedback(model.correctPosCount, model.wrongPosCount);
            if (model.isLocalGuess.HasValue) SetPlayerIdentity(model.isLocalGuess.Value, model.playerName);
            if (model.hintStates != null) RevealDetails(model.hintStates);
            if (model.reviewStates != null)
                for (int i = 0; i < _cards.Count && i < model.reviewStates.Count; i++) SetCardResult(i, model.reviewStates[i]);
        }

        public void ResetForReuse()
        {
            StopFeedbackIntro();
            rectTransform.DOKill();
            SetEvidenceHighlight(false);
            for (int i = 0; i < _cardPool.Count; i++)
            {
                var card = _cardPool[i];
                ((Component)card).transform.DOKill();
                card.SetResultOutline(CardResultState.Unrevealed);
                card.SetAlpha(1); card.InitLocalScale();
                ((Component)card).gameObject.SetActive(false);
                if (_absentMarks[i] != null) _absentMarks[i].gameObject.SetActive(false);
            }
            _cards.Clear();
            IsLocalGuess = null; HistoryIndex = -1;
            FruitLabStyle.Surface(cardsHolderBgImageRectTransform.GetComponent<Image>(), FruitLabStyle.Tile);
            if (playerStripe != null) playerStripe.gameObject.SetActive(false);
            if (_playerLabel != null) { _playerLabel.text = ""; _playerLabel.gameObject.SetActive(false); }
        }

        public INonDraggableCardItemView CreateCardItem()
        {
            int index = _cards.Count;
            if (index == _cardPool.Count)
            {
                _cardPool.Add(_cardFactory.Spawn(cardsHolder, nonDraggableCardItemPrefab));
                _absentMarks.Add(null);
            }
            var card = _cardPool[index];
            ((Component)card).gameObject.SetActive(true);
            _cards.Add(card); ArrangeCards();
            return card;
        }

        public void DisplayFeedback(int correct, int misplaced)
        {
            if (IsSplit)
            {
                for (int index = 0; index < 2; index++) _groupViews[index].Display(_groups[index]);
            }
            else
            {
                _correctCount.text = correct.ToString();
                _misplacedCount.text = misplaced.ToString();
            }
            ArrangeCards();
        }

        public void RevealDetails(IReadOnlyList<CardResultState> states)
        {
            for (int i = 0; i < _cards.Count && i < states.Count; i++)
            {
                _cards[i].SetResultOutline(states[i]);
                // A cross means absent, amber means present in a different slot.
                if (states[i] != CardResultState.Absent) continue;
                var mark = _absentMarks[i];
                if (mark == null)
                {
                    mark = _absentMarks[i] = FruitLabGlyph.Create(((Component)_cards[i]).transform, FruitLabGlyph.Symbol.Cross, new Color(.30f,.025f,.045f,1f));
                    mark.name = "HelperAbsent"; FruitLabStyle.StyleAbsentMark(mark);
                }
                mark.gameObject.SetActive(true);
            }
        }

        public void SetCardResult(int index, CardResultState state)
        {
            _cards[index].SetResultOutline(state);
            // Keep verified local-absence marks when completion still excludes the fruit from this group.
            if (state != CardResultState.Absent && _absentMarks[index] != null)
                _absentMarks[index].gameObject.SetActive(false);
        }
        public void SetPlayerIdentity(bool isLocal, string playerName = null)
        {
            IsLocalGuess = isLocal;
            Color color = isLocal ? FruitLabStyle.LocalPlayer : FruitLabStyle.OpponentPlayer;
            if (playerStripe == null) playerStripe = FruitLabStyle.Detail(transform, "PlayerStripe", color);
            playerStripe.gameObject.SetActive(true);
            playerStripe.color = color;
            FruitLabStyle.Surface(cardsHolderBgImageRectTransform.GetComponent<Image>(), Color.Lerp(FruitLabStyle.Tile, color, .12f));
            _playerLabel = transform.Find("PlayerLabel")?.GetComponent<TMP_Text>()
                ?? FruitLabStyle.CreateText("PlayerLabel", transform, 10f, FruitLabStyle.Muted);
            _playerLabel.gameObject.SetActive(true);
            _playerLabel.richText = false;
            _playerLabel.enableAutoSizing = true; _playerLabel.fontSizeMin = 7; _playerLabel.fontSizeMax = 10;
            _playerLabel.overflowMode = TextOverflowModes.Ellipsis;
            _playerLabel.text = isLocal ? "Sen" : string.IsNullOrWhiteSpace(playerName) ? "Rakip" : playerName;
            _playerLabel.color = color;
            Resize(_width, _height);
        }

        public void Resize(float width, float height)
        {
            _width = width;
            _height = height;
            rectTransform.sizeDelta = new Vector2(width, height);
            if (IsSplit)
            {
                FruitLabStyle.Place(cardsHolder,transform,0f,0f,width,height);
                float groupWidth = (width-12f)*.5f;
                _groupViews[0].Resize(2f,groupWidth,height);
                _groupViews[1].Resize(groupWidth+10f,groupWidth,height);
                ArrangeCards(); return;
            }
            if (playerStripe != null && IsLocalGuess.HasValue) FruitLabStyle.Place(playerStripe.rectTransform, transform, 0f, 6f, 4f, Mathf.Max(4f, height - 12f));
            FruitLabStyle.Place(cardsHolder, transform, 6f, 0f, width - FruitLabLayoutMetrics.HistoryFeedbackReservation, height);
            float feedbackHeight = height - (IsLocalGuess.HasValue ? 14f : 0f);
            if (_playerLabel != null && IsLocalGuess.HasValue) FruitLabStyle.Place(_playerLabel.rectTransform, transform, width - 96f, height - 14f, 92f, 12f);
            FruitLabStyle.Place(_feedback, transform, width - 96f, 0f, 92f, feedbackHeight);
            float iconSize = Mathf.Min(24f, height - 4f);
            FruitLabStyle.Place(_correctIcon.rectTransform, _feedback, 0f, (feedbackHeight - iconSize) * 0.5f, iconSize, iconSize);
            FruitLabStyle.Place(_misplacedIcon.rectTransform, _feedback, 48f, (feedbackHeight - iconSize) * 0.5f, iconSize, iconSize);
            FruitLabStyle.Place(_correctCount.rectTransform, _feedback, 25f, 0f, 18f, feedbackHeight);
            FruitLabStyle.Place(_misplacedCount.rectTransform, _feedback, 73f, 0f, 18f, feedbackHeight);
            _correctCount.fontSize = _misplacedCount.fontSize = Mathf.Max(16f,Mathf.Min(18f, height * 0.55f));
            ArrangeCards();
        }

        private void ArrangeCards()
        {
            if (IsSplit)
            {
                float groupWidth = (_width-12f)*.5f;
                int maximum = Mathf.Max(_groups[0].Count,_groups[1].Count);
                float cardArea = groupWidth-GroupFeedbackView.Reservation-8f;
                float side = Mathf.Max(1f,Mathf.Min(_height-6f,(cardArea-(maximum-1)*FruitLabLayoutMetrics.HistoryCardGap)/maximum));
                for (int group = 0; group < 2; group++)
                {
                    var feedback = _groups[group];
                    float groupX = group == 0 ? 2f : groupWidth+10f;
                    float start = groupX+4f+(cardArea-feedback.Count*side-(feedback.Count-1)*FruitLabLayoutMetrics.HistoryCardGap)*.5f;
                    for (int offset = 0; offset < feedback.Count && feedback.Start+offset < _cards.Count; offset++)
                    {
                        var card = _cards[feedback.Start+offset]; card.SetSize(Vector2.one*side);
                        card.SetLocalPosition(new Vector3(start+side*.5f+offset*(side+FruitLabLayoutMetrics.HistoryCardGap)-_width*.5f,0f,0f));
                    }
                }
                return;
            }
            float size = FruitLabLayoutMetrics.GetHistoryCardSize(_width, _height, _cards.Count);
            float left = -cardsHolder.rect.width * 0.5f + size * 0.5f;
            for (int i = 0; i < _cards.Count; i++)
            {
                _cards[i].SetSize(new Vector2(size, size));
                _cards[i].SetLocalPosition(new Vector3(left + i * (size + FruitLabLayoutMetrics.HistoryCardGap), 0f, 0f));
            }
        }

        public void Destroy() { if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject); }
        public RectTransform GetRectTransform() => rectTransform;

        private sealed class GroupFeedbackView
        {
            public const float Reservation = 36f;
            private readonly Image _panel;
            private readonly FruitLabGlyph _correct, _misplaced;
            private readonly TMP_Text _correctCount, _misplacedCount;
            public GroupFeedbackView(Transform parent,string name,Color color)
            {
                _panel = FruitLabStyle.Detail(parent,name,Color.Lerp(FruitLabStyle.Recess,color,.1f));
                FruitLabStyle.Rim(_panel.rectTransform,.45f);
                _panel.transform.Find("LabRim").GetComponent<Image>().color = new Color(color.r,color.g,color.b,.5f);
                _panel.transform.SetAsFirstSibling();
                _correct = FruitLabGlyph.Create(_panel.transform,FruitLabGlyph.Symbol.Check,FruitLabStyle.Mint);
                _misplaced = FruitLabGlyph.Create(_panel.transform,FruitLabGlyph.Symbol.Swap,FruitLabStyle.Gold);
                _correctCount = FruitLabStyle.CreateText("CorrectCount",_panel.transform,16f,FruitLabStyle.Text);
                _misplacedCount = FruitLabStyle.CreateText("MisplacedCount",_panel.transform,16f,FruitLabStyle.Text);
            }
            public void SetActive(bool active) => _panel.gameObject.SetActive(active);
            public void Display(GuessGroupFeedback result)
            { _correctCount.text = result.Correct.ToString(); _misplacedCount.text = result.Misplaced.ToString(); }
            public void JoinIntro(Sequence sequence,float delay){JoinCountIntro(sequence,_correctCount,_correct,FruitLabStyle.Mint,delay);JoinCountIntro(sequence,_misplacedCount,_misplaced,FruitLabStyle.Gold,delay+.12f);}
            public void ResetIntro(){ResetCount(_correctCount);ResetCount(_misplacedCount);ResetIcon(_correct);ResetIcon(_misplaced);}
            public void Resize(float x,float width,float height)
            {
                FruitLabStyle.Place(_panel.rectTransform,_panel.transform.parent,x,0f,width,height);
                float line = height*.5f, icon = Mathf.Min(19f,line-1f), left = width-Reservation;
                FruitLabStyle.Place(_correct.rectTransform,_panel.transform,left,line+(line-icon)*.5f,icon,icon);
                FruitLabStyle.Place(_misplaced.rectTransform,_panel.transform,left,(line-icon)*.5f,icon,icon);
                float fontSize = Mathf.Clamp(line*.85f,14f,18f);
                // TMP line metrics include descenders even for a single digit.
                // Keep the same baselines while allowing that invisible padding.
                float textHeight = Mathf.Max(line,fontSize*1.2f), padding=(line-textHeight)*.5f;
                FruitLabStyle.Place(_correctCount.rectTransform,_panel.transform,left+20f,line+padding,15f,textHeight);
                FruitLabStyle.Place(_misplacedCount.rectTransform,_panel.transform,left+20f,padding,15f,textHeight);
                FruitLabStyle.TextStyle(_correctCount,fontSize);FruitLabStyle.TextStyle(_misplacedCount,fontSize);
            }
        }
    }

    public interface IResultBlockView
    {
        void Init(NonDraggableCardItemViewFactory cardFactory, float width, float height);
        INonDraggableCardItemView CreateCardItem();
        void DisplayFeedback(int correct, int misplaced);
        void SetCardResult(int index, CardResultState state);
        void Destroy();
    }
}
