using Game;

namespace Scripts
{
    // The committed state is already durable. These operations update live objects and presentation.
    public sealed class PowerUpRuntimeEffects
    {
        private readonly IBombPowerUpController _bomb;
        private readonly IGuessManager _guesses;
        private readonly ICardPlacementCoordinator _placement;
        private readonly HelperEffect _helper;
        private readonly PairEffect _pair;
        private readonly BoardSplitService _split;
        private readonly UltraPowerUpController _ultra;
        public PowerUpRuntimeEffects(IBombPowerUpController bomb,IGuessManager guesses,ICardPlacementCoordinator placement,
            HelperEffect helper,PairEffect pair,BoardSplitService split,UltraPowerUpController ultra)
        {_bomb=bomb;_guesses=guesses;_placement=placement;_helper=helper;_pair=pair;_split=split;_ultra=ultra;}
        public bool TryApply(PowerUpUseRequest request,LevelSaveData checkpoint)
        {
            switch(request.Type)
            {
                case RewardType.Bomb:return _bomb.TryApply(checkpoint);
                case RewardType.Life:return _guesses.GrantExtraLives(PowerUpDefinition.Get(RewardType.Life).ExtraGuesses);
                case RewardType.Revealing:return _placement.TryRevealAndLockCard(request.BoardIndex,checkpoint.TargetCards[request.BoardIndex]-1);
                case RewardType.Helper:return _helper.TryApply();
                case RewardType.Pair:return _pair.ApplyClue(checkpoint.PairClue);
                case RewardType.Split:return _split.TryApply();
                case RewardType.Ultra:return _ultra.ApplyCommitted();
                default:return false;
            }
        }
    }
}
