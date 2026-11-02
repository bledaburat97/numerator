using System;
using Game;
using Zenject;

namespace Scripts
{
    // Coordinates supported level rules. Crystal hints keep their own presentation.
    public sealed class LevelMechanicsController : IDisposable
    {
        [InjectOptional] private ILevelDataCreator _levels;
        private readonly IResultManager _results;
        private readonly HelperEffect _helper;
        private readonly PairEffect _pair;
        private readonly BoardSplitService _split;
        private readonly GrowingBoardService _growing;
        private readonly DynamiteService _dynamite;
        private readonly IBoardAreaController _board;
        private readonly IRoundStateManager _round;
        private readonly ILevelSaveDataManager _save;
        private readonly ITargetNumberCreator _target;
        private readonly IResultAreaController _area;
        private readonly IGamePowerUpAreaController _powers;
        private readonly ILevelTracker _tracker;
        private LevelMechanic _mechanic;
        private ILevelMechanicsPresentation _presentation;
        private Action _checkpoint;

        public LevelMechanicsController(IResultManager results, HelperEffect helper, IRoundStateManager round,
            ILevelSaveDataManager save, ITargetNumberCreator target, IResultAreaController area,
            IGamePowerUpAreaController powers, ILevelTracker tracker, PairEffect pair = null, BoardSplitService split = null, IBoardAreaController board = null, GrowingBoardService growing = null, DynamiteService dynamite = null)
        {
            _results = results; _helper = helper; _round = round; _save = save;
            _target = target; _area = area; _powers = powers; _tracker = tracker; _pair = pair;
            _split = split; _board = board; _growing = growing; _dynamite = dynamite;
            _results.GuessResolved += OnGuess;
            _helper.Applied += OnHelper;
            if (_pair != null) _pair.Changed += OnPair;
            if (_split != null) _split.Applied += OnSplit;
            if (_dynamite != null) _dynamite.Changed += OnDynamite;
        }

        public void Attach(ILevelMechanicsPresentation presentation, Action checkpoint) { _presentation = presentation; _checkpoint = checkpoint; }

        public void Begin(LevelMechanic mechanic)
        {
            _mechanic = LevelMechanicRules.Normalize(mechanic);
            _dynamite?.Prepare();
            UpdateDynamiteView();
            RestoreDetails();
            UpdateStatus();
            if (_mechanic != LevelMechanic.Normal) GameTelemetry.Mechanic("mechanic_started", _mechanic);
            if (_pair != null && _pair.Prepare(_mechanic == LevelMechanic.Pair)) GameTelemetry.Mechanic("mechanic_triggered", LevelMechanic.Pair);
        }

        private void RestoreDetails()
        {
            var history = _round.GetTriedCardsList();
            foreach (int number in _save.GetLevelSaveData().RevealedGuessNumbers)
                if (number > 0 && number <= history.Count)
                    _area.RevealGuess(_helper.EvaluateGuess(number));
        }

        private void OnSplit(object sender,EventArgs args)
        {
            _board?.RefreshLayout();
            _area.RemoveResultBlocks(); _results.TryAddTriedCards(); RestoreDetails();
            UpdateStatus(); _powers.Refresh();
            GameTelemetry.Mechanic("mechanic_triggered",LevelMechanic.Split);
            _checkpoint?.Invoke();
        }

        private void OnGuess(object sender, GuessEvaluation guess)
        {
            if (_tracker.GetGameOption() != GameOption.SinglePlayer) return;
            if (LevelMechanicRules.ShouldTrigger(_mechanic, guess, _round.GetRemainingGuessCount()) && _helper.TryApply())
                GameTelemetry.Mechanic("mechanic_triggered", _mechanic);
            if (_growing?.TryAdvance(guess) == true)
                GameTelemetry.Mechanic("mechanic_triggered",LevelMechanic.Growing);
            if (_dynamite?.Resolve(guess) == true) GameTelemetry.Mechanic("mechanic_triggered",LevelMechanic.Dynamite);
            UpdateStatus(); _powers.Refresh();
            if (!guess.Won && _round.GetRemainingGuessCount() > 0) _checkpoint?.Invoke();
        }

        private void OnDynamite(object sender,DynamiteUpdateReason reason)
        {
            UpdateDynamiteView();
            if (reason == DynamiteUpdateReason.Lock) _checkpoint?.Invoke();
        }

        private void UpdateDynamiteView()
        {
            _presentation?.ShowDynamite(_mechanic, _dynamite?.Current);
        }

        private void OnPair(object sender, PairClue clue)
        {
            _presentation?.ShowPair(clue); _powers.Refresh();
            if (clue != null) _checkpoint?.Invoke();
        }

        private void OnHelper(object sender, GuessEvaluation guess)
        {
            _area.RevealGuess(guess); _powers.Refresh(); _checkpoint?.Invoke();
        }

        private void UpdateStatus()
        {
            _presentation?.ShowStatus(_mechanic, _levels?.GetLevelData(), _save.GetLevelSaveData().GrowingBoard?.ActiveCount ?? 2,
                _round.GetTriedCardsList().Count, _split?.IsActive ?? false);
        }

        public void Dispose()
        {
            _results.GuessResolved -= OnGuess; _helper.Applied -= OnHelper;
            if (_pair != null) _pair.Changed -= OnPair;
            if (_split != null) _split.Applied -= OnSplit;
            if (_dynamite != null) { _dynamite.Changed -= OnDynamite; _dynamite.Dispose(); }
            _checkpoint = null; _presentation = null;
        }
    }
    // Called once a submitted guess has been evaluated and its life consumed.
    // No scene teardown: board holders and locked fruit keep their identities.
    public sealed class GrowingBoardService
    {
        private readonly ILevelDataCreator _levels;
        private readonly ILevelTracker _tracker;
        private readonly ILevelSaveDataManager _save;
        private readonly ITargetNumberCreator _target;
        private readonly IBoardAreaController _board;
        private readonly ICardItemInfoManager _info;
        private readonly IRoundStateManager _round;
        private readonly IGuessManager _guesses;

        public GrowingBoardService(ILevelDataCreator levels,ILevelTracker tracker,ILevelSaveDataManager save,
            ITargetNumberCreator target,IBoardAreaController board,ICardItemInfoManager info,
            IRoundStateManager round,IGuessManager guesses)
        { _levels = levels; _tracker = tracker; _save = save; _target = target; _board = board; _info = info; _round = round; _guesses = guesses; }

        public bool TryAdvance(GuessEvaluation guess)
        {
            var state = _save.GetLevelSaveData().GrowingBoard;
            if (_tracker.GetGameOption() != GameOption.SinglePlayer || _levels.GetLevelData().Mechanic != LevelMechanic.Growing
                || state == null || guess.Won || guess.Number <= state.LastProcessedGuess) return false;
            state.LastProcessedGuess = guess.Number;
            if (state.ActiveCount >= GrowingBoardState.MaximumCount) return false;
            state.WrongSinceGrowth++;
            if (state.WrongSinceGrowth < GrowingBoardState.GuessesPerGrowth) return false;
            if (_round.GetRemainingGuessCount() <= 0 && state.ActiveCount+1 < GrowingBoardState.MaximumCount) return false;
            if (_target.GetTargetCardsList().Count != state.ActiveCount
                || !_target.TryAppendTargetCard(state.FullTarget[state.ActiveCount])) return false;
            int newIndex = state.ActiveCount++;
            state.WrongSinceGrowth = 0;
            foreach (var card in _info.GetCardItemInfoList())
                if (!card.isLocked && card.isExisted && !card.possibleCardHolderIndicatorIndexes.Contains(newIndex))
                    card.possibleCardHolderIndicatorIndexes.Add(newIndex);
            _board.AddBoardHolder();
            if (state.ActiveCount == GrowingBoardState.MaximumCount && !state.BonusGranted)
            {
                state.BonusGranted = true;
                _guesses.GrantExtraLives(GrowingBoardState.BonusLives,true);
            }
            return true;
        }
    }
    public enum DynamiteUpdateReason { Prepared, Guess, Lock }

    // A submitted guess or an authoritative card lock is the only way to resolve danger.
    public sealed class DynamiteService : IDisposable
    {
        private readonly ILevelDataCreator _levels;
        private readonly ILevelTracker _tracker;
        private readonly ILevelSaveDataManager _save;
        private readonly ITargetNumberCreator _target;
        private readonly ICardItemInfoManager _info;
        private readonly ICardPlacementCoordinator _placement;
        private readonly IGuessManager _guesses;
        public event EventHandler<DynamiteUpdateReason> Changed;
        public DynamiteRoundState Current => _tracker.GetGameOption() == GameOption.SinglePlayer
            && _levels.GetLevelData().Mechanic == LevelMechanic.Dynamite ? _save.GetLevelSaveData().Dynamite : null;

        public DynamiteService(ILevelDataCreator levels,ILevelTracker tracker,ILevelSaveDataManager save,
            ITargetNumberCreator target,ICardItemInfoManager info,ICardPlacementCoordinator placement,IGuessManager guesses)
        {
            _levels = levels; _tracker = tracker; _save = save; _target = target; _info = info; _placement = placement; _guesses = guesses;
            _placement.CardLockedEvent += OnLocked;
        }

        public void Prepare()
        {
            var state = Current; var answer = _target.GetTargetCardsList();
            if (state == null || answer == null || answer.Count < 1) return;
            // Bomb is allowed only before any guesses; reconcile a removed dynamite slot.
            if (state.BoardIndex < 0 || state.BoardIndex >= answer.Count)
                state.BoardIndex = UnityEngine.Random.Range(0,answer.Count);
            int fruit = answer[state.BoardIndex]-1;
            var info = _info.GetCardItemInfoList()[fruit];
            if (!state.Exploded && info.isLocked && info.possibleCardHolderIndicatorIndexes.Contains(state.BoardIndex)) state.Disarmed = true;
            if (state.Disarmed) _placement.TryRevealAndLockCard(state.BoardIndex,fruit,false);
            Changed?.Invoke(this,DynamiteUpdateReason.Prepared);
        }

        public bool Resolve(GuessEvaluation guess)
        {
            var state = Current;
            if (state == null || state.Exploded || guess.Number <= state.LastProcessedGuess) return false;
            if (state.BoardIndex < 0) Prepare();
            state.LastProcessedGuess = guess.Number;
            if (state.Disarmed) return false;
            state.GuessesUsed = System.Math.Min(DynamiteRoundState.GuessLimit,guess.Number);
            int fruit = _target.GetTargetCardsList()[state.BoardIndex];
            if (guess.Cards[state.BoardIndex] == fruit)
            {
                state.Disarmed = true;
                if (!guess.Won) _placement.TryRevealAndLockCard(state.BoardIndex,fruit-1);
            }
            else if (state.GuessesUsed == DynamiteRoundState.GuessLimit) state.Exploded = true;
            Changed?.Invoke(this,DynamiteUpdateReason.Guess);
            if (state.Exploded) _guesses.FailRound();
            return true;
        }

        private void OnLocked(object sender,(int cardIndex,int boardIndex) locked)
        {
            var state = Current;
            if (state == null || state.Disarmed || state.Exploded || locked.boardIndex != state.BoardIndex
                || _target.GetTargetCardsList()[state.BoardIndex] != locked.cardIndex+1) return;
            state.Disarmed = true;
            Changed?.Invoke(this,DynamiteUpdateReason.Lock);
        }
        public void Dispose() => _placement.CardLockedEvent -= OnLocked;
    }

}
