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
            _correctCount.text = correct.ToString();
            _misplacedCount.text = misplaced.ToString();
            ArrangeCards();
        }

        public void SetCardResult(int index, CardResultState state) => _cards[index].SetResultOutline(state);
        public void SetPlayerIdentity(bool isLocal)
        {
            IsLocalGuess = isLocal;
            Color color = isLocal ? FruitLabStyle.LocalPlayer : FruitLabStyle.OpponentPlayer;
            if (playerStripe == null) playerStripe = FruitLabStyle.Detail(transform, "PlayerStripe", color);
            playerStripe.gameObject.SetActive(true);
            playerStripe.color = color;
            FruitLabStyle.Surface(cardsHolderBgImageRectTransform.GetComponent<Image>(), Color.Lerp(FruitLabStyle.Tile, color, .12f));
            _playerLabel = transform.Find("PlayerLabel")?.GetComponent<TMP_Text>()
                ?? FruitLabStyle.CreateText("PlayerLabel", transform, 10f, FruitLabStyle.Muted);
            _playerLabel.text = isLocal ? "Sen" : "Rakip";
            _playerLabel.color = color;
            Resize(_width, _height);
        }

        public void Resize(float width, float height)
        {
            _width = width;
            _height = height;
            rectTransform.sizeDelta = new Vector2(width, height);
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
            float size = FruitLabLayoutMetrics.GetHistoryCardSize(_width, _height, _cards.Count);
            float left = -cardsHolder.rect.width * 0.5f + size * 0.5f;
            for (int i = 0; i < _cards.Count; i++)
            {
                _cards[i].SetSize(new Vector2(size, size));
                _cards[i].SetLocalPosition(new Vector3(left + i * (size + FruitLabLayoutMetrics.HistoryCardGap), 0f, 0f));
            }
        }

        public void Destroy() => Destroy(gameObject);
        public RectTransform GetRectTransform() => rectTransform;
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
