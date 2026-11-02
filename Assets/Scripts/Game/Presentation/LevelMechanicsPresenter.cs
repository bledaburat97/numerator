using Game;

namespace Scripts
{
    public interface ILevelMechanicsPresentation
    {
        void ShowDynamite(LevelMechanic mechanic, DynamiteRoundState state);
        void ShowPair(PairClue clue);
        void ShowStatus(LevelMechanic mechanic, LevelData definition, int activeCount, int guessCount, bool split);
    }

    public sealed class LevelMechanicsPresenter : ILevelMechanicsPresentation
    {
        private readonly GameUIView _view;
        private readonly IBoardAreaController _board;
        public LevelMechanicsPresenter(GameUIView view, IBoardAreaController board) { _view = view; _board = board; }
        public void ShowDynamite(LevelMechanic mechanic, DynamiteRoundState state)
        {
            if (_view?.SetDynamiteLayout(mechanic == LevelMechanic.Dynamite) == true) _board?.RefreshLayout();
            _board?.ShowDynamite(state);
        }
        public void ShowPair(PairClue clue) => _view?.SetPairClue(clue);
        public void ShowStatus(LevelMechanic mechanic, LevelData definition, int activeCount, int guessCount, bool split)
        {
            string text = definition != null && definition.IsPlaytest ? LevelPlaytestSchedule.Status(definition) : LevelMechanicRules.Description(mechanic);
            if (mechanic == LevelMechanic.Growing) text += " · " + activeCount + "/" + GrowingBoardState.MaximumCount + " alan";
            if (mechanic == LevelMechanic.Helper) text += " · " + guessCount % 3 + "/3";
            if (mechanic != LevelMechanic.Split && split) text = string.IsNullOrEmpty(text) ? "Bölünmüş Tahta" : text + " · Bölünmüş Tahta";
            _view?.SetMechanicStatus(text);
        }
    }
}
