using System.Collections.Generic;
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

        private readonly List<INonDraggableCardItemView> _cards = new List<INonDraggableCardItemView>();
        private NonDraggableCardItemViewFactory _cardFactory;
        private TMP_Text _correctCount, _misplacedCount;
        private TMP_Text _playerLabel;
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

            if (IsSplit)
            {
                _groupViews = new[] {
                    new GroupFeedbackView(transform,"SplitGroupA",FruitLabStyle.SplitFirstGroup),
                    new GroupFeedbackView(transform,"SplitGroupB",FruitLabStyle.SplitSecondGroup)
                };
                Resize(width,height); return;
            }
            _feedback = new GameObject("Feedback", typeof(RectTransform)).GetComponent<RectTransform>();
            _feedback.SetParent(transform, false);
            _correctIcon = FruitLabGlyph.Create(_feedback, FruitLabGlyph.Symbol.Check, FruitLabStyle.Mint);
            _misplacedIcon = FruitLabGlyph.Create(_feedback, FruitLabGlyph.Symbol.Swap, FruitLabStyle.Gold);
            _correctCount = FruitLabStyle.CreateText("CorrectCount", _feedback, 14f, FruitLabStyle.Text);
            _misplacedCount = FruitLabStyle.CreateText("MisplacedCount", _feedback, 14f, FruitLabStyle.Text);
            Resize(width, height);
        }

        public INonDraggableCardItemView CreateCardItem()
        {
            INonDraggableCardItemView card = _cardFactory.Spawn(cardsHolder, nonDraggableCardItemPrefab);
            _cards.Add(card);
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
                var card = _cards[i] as Component;
                if (card == null || card.transform.Find("HelperAbsent") != null) continue;
                var cross = FruitLabGlyph.Create(card.transform, FruitLabGlyph.Symbol.Cross, new Color(.30f, .025f, .045f, 1f));
                cross.name = "HelperAbsent";
                FruitLabStyle.Stretch(cross.rectTransform, 4f);
            }
        }

        public void SetCardResult(int index, CardResultState state)
        {
            _cards[index].SetResultOutline(state);
            // A group-local absent hint can become a global misplaced result at completion.
            if (state != CardResultState.Absent && _cards[index] is Component card)
            {
                var cross = card.transform.Find("HelperAbsent");
                if (cross != null) TutorialObjectUtility.Destroy(cross.gameObject);
            }
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
            if (playerStripe != null) FruitLabStyle.Place(playerStripe.rectTransform, transform, 0f, 6f, 4f, Mathf.Max(4f, height - 12f));
            FruitLabStyle.Place(cardsHolder, transform, 6f, 0f, width - FruitLabLayoutMetrics.HistoryFeedbackReservation, height);
            float feedbackHeight = height - (_playerLabel != null ? 14f : 0f);
            if (_playerLabel != null) FruitLabStyle.Place(_playerLabel.rectTransform, transform, width - 96f, height - 14f, 92f, 12f);
            FruitLabStyle.Place(_feedback, transform, width - 96f, 0f, 92f, feedbackHeight);
            float iconSize = Mathf.Min(24f, height - 4f);
            FruitLabStyle.Place(_correctIcon.rectTransform, _feedback, 0f, (feedbackHeight - iconSize) * 0.5f, iconSize, iconSize);
            FruitLabStyle.Place(_misplacedIcon.rectTransform, _feedback, 48f, (feedbackHeight - iconSize) * 0.5f, iconSize, iconSize);
            FruitLabStyle.Place(_correctCount.rectTransform, _feedback, 25f, 0f, 18f, feedbackHeight);
            FruitLabStyle.Place(_misplacedCount.rectTransform, _feedback, 73f, 0f, 18f, feedbackHeight);
            _correctCount.fontSize = _misplacedCount.fontSize = Mathf.Min(18f, height * 0.55f);
            ArrangeCards();
        }

        private void ArrangeCards()
        {
            if (IsSplit)
            {
                float groupWidth = (_width-12f)*.5f;
                int maximum = Mathf.Max(_groups[0].Count,_groups[1].Count);
                float cardArea = groupWidth-GroupFeedbackView.Reservation-8f;
                float side = Mathf.Max(1f,Mathf.Min(_height-8f,(cardArea-(maximum-1)*FruitLabLayoutMetrics.HistoryCardGap)/maximum));
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
            public void Display(GuessGroupFeedback result)
            { _correctCount.text = result.Correct.ToString(); _misplacedCount.text = result.Misplaced.ToString(); }
            public void Resize(float x,float width,float height)
            {
                FruitLabStyle.Place(_panel.rectTransform,_panel.transform.parent,x,2f,width,height-4f);
                float line = (height-4f)*.5f, icon = Mathf.Min(19f,line-1f), left = width-Reservation;
                FruitLabStyle.Place(_correct.rectTransform,_panel.transform,left,line+(line-icon)*.5f,icon,icon);
                FruitLabStyle.Place(_misplaced.rectTransform,_panel.transform,left,(line-icon)*.5f,icon,icon);
                FruitLabStyle.Place(_correctCount.rectTransform,_panel.transform,left+20f,line,15f,line);
                FruitLabStyle.Place(_misplacedCount.rectTransform,_panel.transform,left+20f,0f,15f,line);
                _correctCount.fontSize = _misplacedCount.fontSize = Mathf.Clamp(line*.84f,12f,18f);
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
