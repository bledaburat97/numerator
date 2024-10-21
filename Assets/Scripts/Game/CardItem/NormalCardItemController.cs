using DG.Tweening;
using UnityEngine;
namespace Scripts
{
    public class NormalCardItemController : INormalCardItemController
    {
        private ICardMoveHandler _cardMoveHandler;
        private ICardViewHandler _cardViewHandler;
        private CardItemData _cardItemData;
        
        public NormalCardItemController(INormalCardItemView cardItemView, Camera cam,
            IHapticController hapticController, CardItemData cardItemData, IBoardAreaController boardAreaController)
        {
            _cardMoveHandler = new CardMoveHandler(hapticController, boardAreaController, cardItemData.CardItemIndex);
            _cardViewHandler = new CardViewHandler(cardItemView, cam, hapticController, _cardMoveHandler, cardItemData);
        }
        
        public INormalCardItemView GetView()
        {
            return _cardViewHandler.GetView();
        }

        public ICardMoveHandler GetCardMoveHandler()
        {
            return _cardMoveHandler;
        }
        
        public void SetCardAnimation(bool status)
        {
            _cardViewHandler.SetCardAnimation(status);
        }

        public void SuccessAnimation(float delayDuration)
        {
            _cardViewHandler.SuccessAnimation(delayDuration);
        }
        
        public void BackFlipAnimation(float delayDuration, bool isGuessRight, string correctNumber)
        {
            _cardViewHandler.BackFlipAnimation(delayDuration, isGuessRight, correctNumber);
        }

        public void DestroyObject()
        {
            _cardViewHandler.DestroyObject();
        }

        public void AnimateProbabilityChange(float duration, ProbabilityType probabilityType, bool isLocked)
        {
            _cardViewHandler.AnimateProbabilityChange(duration, probabilityType, isLocked);
        }
        
        public void SetProbability(ProbabilityType probabilityType, bool isLocked)
        {
            _cardViewHandler.SetProbability(probabilityType, isLocked);
        }
        
        public RectTransform GetRectTransform()
        {
            return _cardViewHandler.GetRectTransform();
        }

        public ICardViewHandler GetCardViewHandler()
        {
            return _cardViewHandler;
        }
        
    }
    public interface INormalCardItemController
    {
        INormalCardItemView GetView();
        void BackFlipAnimation(float delayDuration, bool isGuessRight, string correctNumber);
        void SetCardAnimation(bool status);
        void DestroyObject();
        void SetProbability(ProbabilityType probabilityType, bool isLocked);
        void AnimateProbabilityChange(float duration, ProbabilityType probabilityType, bool isLocked);
        RectTransform GetRectTransform();
        void SuccessAnimation(float delayDuration);
        ICardMoveHandler GetCardMoveHandler();
        ICardViewHandler GetCardViewHandler();
    }
}