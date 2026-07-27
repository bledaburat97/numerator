using System.Collections.Generic;
using DG.Tweening;
using Zenject;

namespace Scripts
{
    public class LevelSuccessBoardAnimationController : ILevelSuccessBoardAnimationController
    {
        private readonly IBoardAreaController _boardAreaController;
        private readonly ICardPlacementCoordinator _cardPlacementCoordinator;

        [Inject]
        public LevelSuccessBoardAnimationController(
            IBoardAreaController boardAreaController,
            ICardPlacementCoordinator cardPlacementCoordinator)
        {
            _boardAreaController = boardAreaController;
            _cardPlacementCoordinator = cardPlacementCoordinator;
        }

        public Sequence Play(float cardDelayDuration, float cardColorChangingDuration, float ribbonImageDuration)
        {
            List<ICardViewHandler> cardsOnBoard = _cardPlacementCoordinator.GetCardsOnBoard();
            return DOTween.Sequence()
                .AppendCallback(() =>
                    TurnCardsIntoCertain(cardsOnBoard, cardDelayDuration, cardColorChangingDuration, ribbonImageDuration))
                .Join(_boardAreaController.PlayAllSuccessFrameAnimations(cardDelayDuration));
        }

        private static void TurnCardsIntoCertain(
            List<ICardViewHandler> cardViewHandlerList,
            float delayDuration,
            float colorChangingDuration,
            float ribbonImageDuration)
        {
            for (int i = 0; i < cardViewHandlerList.Count; i++)
            {
                float delay = delayDuration * i;
                cardViewHandlerList[i].AnimateTurnIntoCertain(delay, colorChangingDuration, ribbonImageDuration);
            }
        }
    }

    public interface ILevelSuccessBoardAnimationController
    {
        Sequence Play(float cardDelayDuration, float cardColorChangingDuration, float ribbonImageDuration);
    }
}
