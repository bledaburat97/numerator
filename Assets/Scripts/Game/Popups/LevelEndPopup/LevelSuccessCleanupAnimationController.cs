using System.Collections.Generic;
using DG.Tweening;
using Zenject;

namespace Scripts
{
    public class LevelSuccessCleanupAnimationController : ILevelSuccessCleanupAnimationController
    {
        private readonly IInitialCardAreaController _initialCardAreaController;
        private readonly IGameUIController _gameUIController;
        private readonly ILifeBarController _lifeBarController;
        private readonly IResultAreaController _resultAreaController;
        private readonly ICardPlacementCoordinator _cardPlacementCoordinator;

        [Inject]
        public LevelSuccessCleanupAnimationController(
            IInitialCardAreaController initialCardAreaController,
            IGameUIController gameUIController,
            ILifeBarController lifeBarController,
            IResultAreaController resultAreaController,
            ICardPlacementCoordinator cardPlacementCoordinator)
        {
            _initialCardAreaController = initialCardAreaController;
            _gameUIController = gameUIController;
            _lifeBarController = lifeBarController;
            _resultAreaController = resultAreaController;
            _cardPlacementCoordinator = cardPlacementCoordinator;
        }

        public Sequence DismissCardsOnInitialHolders(float duration)
        {
            List<ICardViewHandler> cardViewHandlerList = _cardPlacementCoordinator.GetCardsOnInitialHolder();
            Sequence sequence = DOTween.Sequence();

            foreach (ICardViewHandler card in cardViewHandlerList)
            {
                sequence.Join(card.AnimateExplosion(duration));
            }

            return sequence;
        }

        public Sequence FadeOutObsoleteGameUi(float duration)
        {
            return DOTween.Sequence()
                .Append(_gameUIController.ChangeFadeTopAreaButtons(duration, 0f))
                .Join(_gameUIController.ChangeFadeUserText(duration, 0f))
                .Join(_resultAreaController.ChangeFade(duration, 0f)
                    .OnComplete(() => _resultAreaController.RemoveResultBlocks()))
                .Join(_initialCardAreaController.ChangeFadeInitialArea(duration, 0f)
                    .OnComplete(() => _initialCardAreaController.ClearInitialCardHolders()))
                .Join(_gameUIController.ChangeFadeMiddleAreaButtons(duration, 0f));
        }

        public Sequence FadeOutLifeBar(float duration)
        {
            return _lifeBarController.ChangeFade(duration, 0f).OnComplete(() =>
            {
                _lifeBarController.ClearBoundaries();
                _lifeBarController.ClearLifeBarRewardInfoList();
            });
        }
    }

    public interface ILevelSuccessCleanupAnimationController
    {
        Sequence DismissCardsOnInitialHolders(float duration);
        Sequence FadeOutObsoleteGameUi(float duration);
        Sequence FadeOutLifeBar(float duration);
    }
}
