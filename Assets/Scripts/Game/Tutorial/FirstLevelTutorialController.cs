using System;
using System.Linq;
using DG.Tweening;
using Game;
using UnityEngine;

namespace Scripts
{
    public sealed class FirstLevelTutorialController : IDisposable
    {
        private readonly IInitialCardAreaController _initial;
        private readonly IBoardAreaController _board;
        private readonly ICardPlacementCoordinator _placement;
        private readonly IResultManager _results;
        private readonly IRoundStateManager _round;
        private readonly ICardItemInfoManager _info;
        private readonly IGameUIController _ui;
        private readonly TutorialInputGate _gate;
        private readonly IUnmaskServiceAreaView _mask;
        private readonly IHandTutorialView _hand;
        private readonly TutorialMessagePopupView _message;
        private readonly Func<RectTransform> _latestResult;
        private readonly Action _ended;
        private Sequence _pending;
        private int _step;
        private bool _disposed;
        private bool _completed;
        public int Step => _step;
        public bool IsActive => !_disposed;

        public FirstLevelTutorialController(IInitialCardAreaController initial, IBoardAreaController board,
            ICardPlacementCoordinator placement, IResultManager results, IRoundStateManager round,
            ICardItemInfoManager info, IGameUIController ui, TutorialInputGate gate, IUnmaskServiceAreaView mask,
            IHandTutorialView hand, TutorialMessagePopupView message, BaseButtonControllerFactory buttons,
            Func<RectTransform> latestResult, Action ended)
        {
            _initial = initial; _board = board; _placement = placement; _results = results; _round = round;
            _info = info; _ui = ui; _gate = gate; _mask = mask; _hand = hand; _message = message;
            _latestResult = latestResult; _ended = ended;
            _message.Bind(buttons, Next);
        }
        public static bool CanResume(LevelSaveData save, int maxGuesses)
        {
            if (save == null || save.TargetCards == null || !save.TargetCards.SequenceEqual(new[] { 4, 1 })
                || save.RemovedBoardHolderCount != 0 || save.CardItemInfoList == null || save.CardItemInfoList.Count != 5
                || save.CardItemInfoList.Any(card => card == null || !card.isExisted || card.isLocked)) return false;
            var guesses = save.TriedCardsList;
            int count = guesses?.Count ?? 0;
            if (count > 2 || save.RemainingGuessCount != maxGuesses - count) return false;
            return (count == 0 || (guesses[0] != null && guesses[0].SequenceEqual(new[] { 1, 2 })))
                && (count < 2 || (guesses[1] != null && guesses[1].SequenceEqual(new[] { 3, 1 })));
        }
        public void Start()
        {
            GameTelemetry.Tutorial("tutorial_started", "first_level");
            _step = _round.GetTriedCardsList().Count >= 2 ? 7 : _round.GetTriedCardsList().Count == 1 ? 3 : 0;
            _placement.CardPlacedEvent += OnPlaced;
            _results.WrongGuessEvent += OnWrong;
            ShowStep();
        }
        private bool PlacementStep(out int card, out int slot)
        {
            card = slot = -1;
            switch (_step)
            {
                case 0: card = 0; slot = 0; return true;
                case 1: card = 1; slot = 1; return true;
                case 4: card = 2; slot = 0; return true;
                case 5: card = 0; slot = 1; return true;
                case 8: card = 3; slot = 0; return true;
                case 9: card = 0; slot = 1; return true;
                default: return false;
            }
        }
        private void ShowStep()
        {
            if (_disposed) return;
            GameTelemetry.Tutorial("tutorial_step", "first_level", _step);
            _mask.ClearAllUnmaskCardItems(); _hand.StopActiveAnimation(); _gate.Block(); SetCardLocks(-1);
            if (PlacementStep(out int card, out int slot))
            {
                string[] names = { "Elmayı", "Muzu", "Çileği", "Portakalı" };
                _gate.AllowPlacement(card, slot); SetCardLocks(card);
                RectTransform source = _initial.GetRectTransformOfCardItem(card);
                RectTransform target = _board.GetRectTransformOfGarden(slot);
                _mask.Highlight(source); _mask.Highlight(target);
                _hand.StartDragAnimation(source.position, target.position);
                _message.Show("Meyvelerin yerini bul", names[card] + (slot == 0 ? " soldaki" : " sağdaki")
                    + " alana yerleştir. Sürükleyebilir veya meyveye dokunabilirsin.", false);
            }
            else if (_step == 3 || _step == 7)
            {
                _mask.Highlight(_latestResult());
                _message.Show(_step == 3 ? "Sonuçları okuyalım" : "Bir meyve yerinde!",
                    _step == 3 ? "Doğru yer: 0. Yanlış yer: 1. Bir meyve cevapta var, ama başka bir alanda olmalı. Her tahmin bir hak kullanır."
                        : "Doğru yer: 1. Yanlış yer: 0. Sağdaki elma doğru yerde! Kartlar yeni tahmin için yuvalarına döndü.", true, true, "Devam et");
            }
            else
            {
                _gate.AllowGuess();
                RectTransform check = _ui.GetCheckButtonRectTransform();
                _mask.Highlight(check); _hand.StartClickAnimation(check.position);
                _message.Show("Tahminini gönder", _step == 10 ? "İki meyve de hazır. Son tahminini gönderelim!" : "Tahmin et butonuna dokun. Sonuçlar meyvelerin yerleri hakkında ipucu verecek.", false);
            }
            _gate.AllowSettings();
            _mask.Highlight(_ui.GetButton(GameUIButtonType.Settings).GetView().GetButtonRectTransform());
        }
        private void OnPlaced(object sender, (int cardIndex, int boardIndex) args)
        {
            if (_disposed || !PlacementStep(out int card, out int slot) || args.cardIndex != card || args.boardIndex != slot) return;
            _step++; ShowStep();
        }
        private void OnWrong(object sender, EventArgs args)
        {
            if (_disposed || (_step != 2 && _step != 6)) return;
            _gate.Block(); _gate.AllowSettings(); SetCardLocks(-1); _hand.StopActiveAnimation();
            _pending = DOTween.Sequence().AppendInterval(.35f).AppendCallback(() => { _step++; ShowStep(); })
                .SetLink(_message.gameObject);
        }
        public void Next()
        {
            if (_disposed || (_step != 3 && _step != 7)) return;
            _step++; ShowStep();
        }
        public void Complete()
        {
            if (_disposed) return;
            _completed = true; GameTelemetry.Tutorial("tutorial_completed", "first_level", _step);
            TutorialProgress.CompleteFirst(); Dispose(); _ended();
        }
        private void SetCardLocks(int allowed)
        {
            var info = _info.GetCardItemInfoList();
            for (int i = 0; i < _initial.GetCardCount(); i++)
                if (_initial.TryGetCardItem(i, out var item))
                    item.GetCardMoveHandler().SetMovementLocked(i != allowed || (info != null && info[i].isLocked));
        }
        public void Dispose()
        {
            if (_disposed) return;
            if (!_completed) GameTelemetry.Tutorial("tutorial_abandoned", "first_level", _step);
            _disposed = true;
            _placement.CardPlacedEvent -= OnPlaced; _results.WrongGuessEvent -= OnWrong;
            if (_pending != null && _pending.IsActive()) _pending.Kill();
            _pending = null; _hand.StopActiveAnimation();
        }
    }
}
