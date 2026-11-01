using System.Collections.Generic;
using System.Linq;
using Game;

namespace Scripts
{
    // Commits the model immediately; the caller owns the crystal/level animation.
    public sealed class EliminateFruitEffect
    {
        private readonly ITargetNumberCreator _target;
        private readonly ICardItemInfoManager _info;
        private readonly ICardPlacementCoordinator _placement;
        private readonly ILevelSaveDataManager _save;

        public EliminateFruitEffect(ITargetNumberCreator target, ICardItemInfoManager info,
            ICardPlacementCoordinator placement, ILevelSaveDataManager save = null)
        { _target = target; _info = info; _placement = placement; _save = save; }

        public bool TryApply(IReadOnlyList<int> preferredCards, int guessNumber, out int cardIndex)
        {
            cardIndex = -1;
            if (guessNumber > 0 && _save != null && _save.GetLevelSaveData().LastEliminationGuessNumber == guessNumber) return false;
            var cards = _info.GetCardItemInfoList();
            var answer = _target.GetProtectedTargetCards();
            var eligible = Enumerable.Range(0, cards.Count).Where(i => cards[i].isExisted && !cards[i].isLocked
                && !answer.Contains(i + 1)).ToList();
            var preferred = eligible.Where(i => preferredCards != null && preferredCards.Contains(i + 1)).ToList();
            var choices = preferred.Count > 0 ? preferred : eligible;
            if (choices.Count == 0) return false;
            cardIndex = choices[UnityEngine.Random.Range(0, choices.Count)];
            _info.MakeCardNotExisted(cardIndex);
            _placement.TryRemoveCardFromBoard(cardIndex);
            if (_save != null) _save.GetLevelSaveData().LastEliminationGuessNumber = guessNumber;
            return true;
        }
    }
}
