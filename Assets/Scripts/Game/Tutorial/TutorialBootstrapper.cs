using System;
using System.Linq;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public sealed class TutorialBootstrapper : ITutorialBootstrapper
    {
        [InjectOptional] private ILevelDataCreator _levels;
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
            else if (_tracker.GetPowerUpCount(RewardType.Ultra)>0 && !TutorialProgress.Seen(TutorialProgress.Ultra)
                && _levels?.GetLevelData()?.Mechanic==LevelMechanic.Normal && ExplainPowerUp(GameUIButtonType.UltraPowerUp)) { }
            else if (_levels?.GetLevelData()?.TrialPowerUp is RewardType trial && ExplainPowerUp(PowerUpTypeUtility.ToGameButtonType(trial))) { }
            else if (_levels != null && ExplainLevelMechanic()) { }
            else if (!TutorialProgress.Seen(TutorialProgress.CrystalHints) && _round.GetLifeBarRewardInfoList().Any(token => token.IsCrystal))
            {
                ShowTip("Kristal İpuçları", "Kazanamadığın kristaller oyun içinde sana yardım eder.",
                    TutorialProgress.CrystalHints, Find<LifeBarView>()?.GetRectTransform());
            }
        }
        private bool ExplainLevelMechanic()
        {
            var mechanic = LevelMechanicRules.Normalize(_levels.GetLevelData().Mechanic);
            if (mechanic == LevelMechanic.Normal) return false;
            if (mechanic == LevelMechanic.Dynamite)
            {
                if (TutorialProgress.Seen(TutorialProgress.DynamiteLevel)) return false;
                ShowTip("Dinamitli Tahta", "İlk 4 hamlede dinamitli alan için doğru meyveyi bulmalısın yoksa oyun kaybedilir.",
                    TutorialProgress.DynamiteLevel,Find<BoardAreaView>()?.GetRectTransform());
                return true;
            }
            if (mechanic == LevelMechanic.Growing)
            {
                if (TutorialProgress.Seen(TutorialProgress.GrowingLevel)) return false;
                ShowTip("Büyüyen Tahta", "2 alanla başlarsın. Her 2 yanlışta bir alan açılır. Ek 4 hakkın başlangıçta verilir.",
                    TutorialProgress.GrowingLevel,Find<BoardAreaView>()?.GetRectTransform());
                return true;
            }
            if (mechanic == LevelMechanic.Split)
            {
                if (TutorialProgress.Seen(TutorialProgress.SplitLevel)) return false;
                ShowTip("Bölünmüş Tahta", "İki grubun sonuçları ayrı hesaplanır, yerleştirilen meyveler kendi grubunda değerlendirilir.",
                    TutorialProgress.SplitLevel,Find<BoardAreaView>()?.GetRectTransform());
                return true;
            }
            if (mechanic == LevelMechanic.Pair)
            {
                if (TutorialProgress.Seen(TutorialProgress.PairLevel)) return false;
                ShowTip("Ardışık Bağlı Meyveler", "Cevapta bulunan yan yana iki meyveyi gösterir.",
                    TutorialProgress.PairLevel, PairTargets());
                return true;
            }
            if (TutorialProgress.Seen(TutorialProgress.HelperLevel)) return false;
            ShowTip("Bonus İpucu", "Her 3 yanlış tahminde bir tahmindeki meyvelerin detaylı bilgisi verilir",
                TutorialProgress.HelperLevel, LatestResult());
            return true;
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
            ShowTip("Coin ve güçlendiriciler", "Coinlerle tohum al.\nKristallerle güçlendirici kazan.",
                TutorialProgress.Currency, _ui.GetCoinImageRectTransform(), bar);
        }
        private bool ExplainPowerUp(GameUIButtonType type)
        {
            if (!_ready || _roundEnded || _message != null || _tracker.GetGameOption() != GameOption.SinglePlayer) return false;
            if (type != GameUIButtonType.BombPowerUp && type != GameUIButtonType.RevealingPowerUp
                && type != GameUIButtonType.LifePowerUp && type != GameUIButtonType.HelperPowerUp
                && type != GameUIButtonType.PairPowerUp && type != GameUIButtonType.SplitPowerUp && type != GameUIButtonType.UltraPowerUp) return false;
            var reward=PowerUpTypeUtility.ToRewardType(type);
            string key=PowerUpTypeUtility.TutorialKey(reward);
            if (TutorialProgress.Seen(key) || _tracker.GetPowerUpCount(reward) <= 0) return false;
            var button = Find<PowerUpListHolderView>()?.GetPowerUpButton(reward)?.GetButtonRectTransform();
            if(button!=null && !button.gameObject.activeInHierarchy)button=null;
            ShowTip(PowerUpTypeUtility.Name(reward),PowerUpTypeUtility.Description(reward),key,button);
            return true;
        }
        private void ShowTip(string title, string text, string key, params RectTransform[] targets)
        {
            if (_message != null) return;
            GameTelemetry.Tutorial("tutorial_started", key);
            CreatePresentation(); _gate.Block(); LockCards(-1);
            foreach (var target in targets) _mask.Highlight(target);
            _message.ShowAt(title, text, true, TutorialMessagePlacement.Center, "Tamam");
            var presentation=_message;
            bool closed=false;
            void Dismiss()
            {
                if(closed || _message==null || _message!=presentation)return;
                closed=true;
                GameTelemetry.Tutorial("tutorial_completed", key);
                TutorialProgress.Mark(key); ClosePresentation(!_roundEnded);
                // Acknowledging an introduction never activates or spends a powerup.
            }
            _message.Bind(_buttons,Dismiss);
            _mask.SetDismiss(Dismiss);
        }
        private RectTransform[] PairTargets()
        {
            var clue = Find<PairClueView>()?.Clue;
            if (clue == null) return Array.Empty<RectTransform>();
            return new[] { _initial.GetRectTransformOfCardItem(clue.First - 1), _initial.GetRectTransformOfCardItem(clue.Second - 1) };
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
                if (_initial.TryGetCardItem(i, out var card)) card.GetCardMoveHandler().SetMovementLocked(info != null && i < info.Count && (info[i].isLocked || !info[i].isExisted));
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
