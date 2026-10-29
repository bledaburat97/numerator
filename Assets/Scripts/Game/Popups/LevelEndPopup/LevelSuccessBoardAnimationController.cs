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
                .Append(TurnCardsIntoCertain(cardsOnBoard, cardDelayDuration, cardColorChangingDuration, ribbonImageDuration))
                .Join(_boardAreaController.PlayAllSuccessFrameAnimations(cardDelayDuration));
        }

        private static Sequence TurnCardsIntoCertain(
            List<ICardViewHandler> cardViewHandlerList,
            float delayDuration,
            float colorChangingDuration,
            float ribbonImageDuration)
        {
            Sequence sequence = DOTween.Sequence().AppendCallback(() =>
            {
                foreach (ICardViewHandler card in cardViewHandlerList)
                    card.SetProbability(ProbabilityType.Certain, true);
            });
            for (int i = 0; i < cardViewHandlerList.Count; i++)
            {
                ICardViewHandler card = cardViewHandlerList[i];
                sequence.InsertCallback(delayDuration * i, () =>
                {
                    card.GetView().SetResultOutline(CardResultState.Correct);
                    card.AnimateTurnIntoCertain(0f, colorChangingDuration, ribbonImageDuration);
                });
            }
            return sequence.AppendInterval(colorChangingDuration);
        }
    }

    public interface ILevelSuccessBoardAnimationController
    {
        Sequence Play(float cardDelayDuration, float cardColorChangingDuration, float ribbonImageDuration);
    }
}
