using System;
using UnityEngine;

namespace Scripts
{
    public class CardReleaseHandler : ICardReleaseHandler
    {
        private readonly IHapticController _hapticController;
        private readonly ICardViewHandler _cardViewHandler;
        private readonly IBoardAreaController _boardAreaController;
        private Func<int, int> _onDragComplete;
        
        public CardReleaseHandler(IHapticController hapticController, ICardViewHandler cardViewHandler,
            IBoardAreaController boardAreaController, Func<int, int> onDragComplete)
        {
            _hapticController = hapticController;
            _cardViewHandler = cardViewHandler;
            _boardAreaController = boardAreaController;
            _onDragComplete = onDragComplete;
        }

        public void Release(int cardItemIndex, RectTransform initialHolderTransform, RectTransform tempTransform)
        {
            _hapticController.Vibrate(HapticType.CardRelease);
            int boardHolderIndex = _onDragComplete(cardItemIndex);
            _cardViewHandler.SetParent(tempTransform);

            Vector2 targetPosition;
            RectTransform parentTransform;
            if (boardHolderIndex != -1)
            {
                targetPosition = _boardAreaController.GetBoardHolderPositionAtIndex(boardHolderIndex);
                parentTransform = _boardAreaController.GetRectTransformOfBoardHolder(boardHolderIndex);
            }
            else
            {
                targetPosition = initialHolderTransform.position;
                parentTransform = initialHolderTransform;
            }

            _cardViewHandler.PlaceCard(targetPosition, parentTransform);
        }
    }

    public interface ICardReleaseHandler
    {
        void Release(int cardItemIndex, RectTransform initialHolderTransform, RectTransform tempTransform);
    }

}