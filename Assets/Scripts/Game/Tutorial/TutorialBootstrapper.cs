using System;
using System.Linq;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public sealed class TutorialBootstrapper : ITutorialBootstrapper
    {
        private readonly ILevelTracker _tracker;
        private readonly IInitialCardAreaController _initial;
        private readonly IBoardAreaController _board;
        private readonly ICardPlacementCoordinator _placement;
        private readonly IResultManager _results;
        private readonly IRoundStateManager _round;
        private readonly ICardItemInfoManager _info;
        private readonly IGameUIController _ui;
        private readonly IUnmaskServiceAreaView _mask;
        private readonly IFadePanelController _fade;
        private readonly TutorialInputGate _gate;
        private readonly BaseButtonControllerFactory _buttons;
        private Transform _parent;
        private RectTransform _canvas;
        private HandTutorialView _handPrefab, _hand;
        private TutorialMessagePopupView _messagePrefab, _message;
        private FirstLevelTutorialController _first;
        private bool _ready, _roundEnded;

        [Inject]
        public TutorialBootstrapper(ILevelTracker tracker, IInitialCardAreaController initial, IBoardAreaController board,
            ICardPlacementCoordinator placement, IResultManager results, IRoundStateManager round, ICardItemInfoManager info,
            IGameUIController ui, IUnmaskServiceAreaView mask, IFadePanelController fade,
            TutorialInputGate gate, BaseButtonControllerFactory buttons)
        {
            _tracker = tracker; _initial = initial; _board = board; _placement = placement; _results = results;
            _round = round; _info = info; _ui = ui; _mask = mask; _fade = fade; _gate = gate; _buttons = buttons;
        }
        public bool IsFirstActive => _first != null && _first.IsActive;
        public void Configure(Transform parent, RectTransform safeArea, RectTransform canvas,
            HandTutorialView handPrefab, TutorialMessagePopupView messagePrefab)
        {
            _parent = parent; _canvas = canvas; _handPrefab = handPrefab; _messagePrefab = messagePrefab;
            _mask.Initialize(_fade);
            _gate.FirstUseHandler = ExplainPowerUp;
        }
        public void PrepareRound()
        {
            Cancel(); _ready = false; _roundEnded = false; _gate.Block();
        }
        public void StartRound()
        {
            Cancel(); _ready = true; _roundEnded = false; _gate.Clear();
            if (_tracker.GetGameOption() != GameOption.SinglePlayer) return;
            if (_tracker.IsFirstLevelTutorial())
            {
                CreatePresentation();
                _first = new FirstLevelTutorialController(_initial, _board, _placement, _results, _round, _info, _ui,
                    _gate, _mask, _hand, _message, _buttons, LatestResult, () =>
                    {
                        _first = null; ClosePresentation(false);
                    });
                _first.Start();
            }
            else if (!TutorialProgress.Seen(TutorialProgress.CrystalHints) && _round.GetLifeBarRewardInfoList().Any(token => token.IsCrystal))
            {
                ShowTip("Kristal ipuçları", "Oyunda kazanamadığın ilk pembe kristal yanlış bir meyveyi eleme ipucusu verir, ikincisi ise bir doğru meyveyi yerine koyarak başka bir ipucu daha verir.",
                    TutorialProgress.CrystalHints, true, null, Find<LifeBarView>()?.GetRectTransform());
            }
        }
        public void RoundEnded(bool success)
        {
            _roundEnded = true; _ready = false;
            if (_first != null)
            {
                if (success) _first.Complete();
                else { _first.Dispose(); _first = null; ClosePresentation(false); }
            }
            else ClosePresentation(false);
        }
        public void ShowRewards(int coins, int crystals)
        {
            if ((coins <= 0 && crystals <= 0) || TutorialProgress.Seen(TutorialProgress.Currency)) return;
            RectTransform bar = Find<VerticalCrystalProgressView>()?.GetRectTransform();
            if (bar != null && !bar.gameObject.activeInHierarchy) bar = null;
            ShowTip("Coin ve kristal enerjisi", "Sarı coin'leri biriktirebilir, pembe kristaller ile powerup kazanabilirsin.",
                TutorialProgress.Currency, false, null, _ui.GetCoinImageRectTransform(), bar);
        }
        private bool ExplainPowerUp(GameUIButtonType type, Action use)
        {
            if (!_ready || _roundEnded || _message != null || _tracker.GetGameOption() != GameOption.SinglePlayer) return false;
            string key, title, body;
            RewardType reward;
            switch (type)
            {
                case GameUIButtonType.BombPowerUp:
                    key = TutorialProgress.Bomb; title = "Bomba"; reward = RewardType.Bomb;
                    body = "Bir alan eksiltmek için kullanıp tahminini kolaylaştırabilirsin. Hiç tahmin yapmadan kullanman gerekir.";
                    break;
                case GameUIButtonType.RevealingPowerUp:
                    key = TutorialProgress.Reveal; title = "Göster"; reward = RewardType.Revealing;
                    body = "Hangi alandaki meyveyi öğrenmek istiyorsan o alana dokun. Doğru meyve o alana yerleşir ve kilitlenir.";
                    break;
                case GameUIButtonType.LifePowerUp:
                    key = TutorialProgress.Life; title = "Can"; reward = RewardType.Life;
                    body = "3 ek tahmin hakkı verir. En az 3 hak harcadığında kullanabilirsin.";
                    break;
                default: return false;
            }
            if (TutorialProgress.Seen(key) || _tracker.GetPowerUpCount(reward) <= 0) return false;
            var button = Find<PowerUpListHolderView>()?.GetPowerUpButton(reward)?.GetButtonRectTransform();
            ShowTip(title, body, key, false, use, button);
            return true;
        }
        private void ShowTip(string title, string text, string key, bool bottom, Action use, params RectTransform[] targets)
        {
            if (_message != null) return;
            GameTelemetry.Tutorial("tutorial_started", key);
            CreatePresentation(); _gate.Block(); LockCards(-1);
            foreach (var target in targets) _mask.Highlight(target);
            _message.Show(title, text, true, bottom, use == null ? "Anladım" : "Devam et");
            void End(bool proceed)
            {
                GameTelemetry.Tutorial(proceed ? "tutorial_completed" : "tutorial_dismissed", key);
                TutorialProgress.Mark(key); ClosePresentation(!_roundEnded);
                if (proceed) use?.Invoke();
            }
            _message.Bind(_buttons, () => End(true), () => End(false));
            _message.SetSecondaryText(use == null ? "Kapat" : "Şimdi değil");
        }
        private void CreatePresentation()
        {
            _mask.Init(1f, _canvas.rect.height); _mask.InstantiateTutorialFade();
            _message = UnityEngine.Object.Instantiate(_messagePrefab, _parent, false); _message.Init();
            _hand = UnityEngine.Object.Instantiate(_handPrefab, _parent, false); _hand.Init(1f, _canvas.rect.height);
            foreach (Transform child in _message.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = _parent.gameObject.layer;
            foreach (Transform child in _hand.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = _parent.gameObject.layer;
        }
        private RectTransform LatestResult()
        {
            var area = Find<ResultAreaView>();
            var row = area != null ? area.GetComponentsInChildren<ResultBlockView>(true).LastOrDefault() : null;
            return row != null ? (RectTransform)row.transform : null;
        }
        private T Find<T>() where T : Component => _canvas != null ? _canvas.GetComponentInChildren<T>(true) : null;
        private void LockCards(int allowed)
        {
            if (_initial == null) return;
            for (int i = 0; i < _initial.GetCardCount(); i++)
                if (_initial.TryGetCardItem(i, out var card)) card.GetCardMoveHandler().SetMovementLocked(i != allowed);
        }
        private void ClosePresentation(bool restoreCards)
        {
            if (_hand != null) _hand.Destroy(); _hand = null;
            if (_message != null) _message.Destroy(); _message = null;
            _mask.CloseTutorialFade(); _gate.Clear();
            if (!restoreCards) return;
            var info = _info.GetCardItemInfoList();
            for (int i = 0; i < _initial.GetCardCount(); i++)
                if (_initial.TryGetCardItem(i, out var card)) card.GetCardMoveHandler().SetMovementLocked(info != null && i < info.Count && info[i].isLocked);
        }
        public void Cancel()
        {
            _first?.Dispose(); _first = null; ClosePresentation(!_roundEnded); _ready = false;
        }
        public void Dispose() { Cancel(); _gate.FirstUseHandler = null; }
    }
    public interface ITutorialBootstrapper : IDisposable
    {
        void Configure(Transform parent, RectTransform safeArea, RectTransform canvas, HandTutorialView handPrefab, TutorialMessagePopupView messagePrefab);
        void PrepareRound();
        void StartRound();
        void RoundEnded(bool success);
        void ShowRewards(int coins, int crystals);
        void Cancel();
    }
}
