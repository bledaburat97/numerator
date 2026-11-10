using Game;

namespace Scripts
{
    public interface ILevelMechanicsPresentation
    {
        void ShowDynamite(LevelMechanic mechanic, DynamiteRoundState state);
        void ShowPair(PairClue clue);
        void ShowHelperFeedback();
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
        public void ShowHelperFeedback()=>_view?.GetComponentInParent<UnityEngine.Canvas>().GetComponent<LevelEnvironmentView>()?.PulseHintProgress();
        public void ShowPair(PairClue clue) => _view?.SetPairClue(clue);
        public void ShowStatus(LevelMechanic mechanic, LevelData definition, int activeCount, int guessCount, bool split)
        {
            if(definition==null)return;
            var identity=LevelIdentity.For(definition);
            string text=identity.Group==LevelGroup.Normal && !definition.IsPlaytest?null:identity.Name;
            if(definition.TrialPowerUp.HasValue && definition.Mechanic==LevelMechanic.Normal && !definition.BonusReward.HasValue)
                text=PowerUpTypeUtility.Name(definition.TrialPowerUp.Value)+" denemesi";
            if(mechanic!=LevelMechanic.Split && mechanic!=LevelMechanic.Ice && split)text="Bölünmüş Tahta";
            _view?.SetMechanicStatus(text);
            if(_view!=null)LevelEnvironmentView.Ensure(_view.GetComponentInParent<UnityEngine.Canvas>()).Render(definition,activeCount,guessCount);
        }
    }
}
