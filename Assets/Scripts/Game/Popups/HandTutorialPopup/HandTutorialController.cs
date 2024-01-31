using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class HandTutorialController : IHandTutorialController
    {
        private IHandTutorialView _view;
        public void Initialize(IHandTutorialView view, Vector2 startingPos, Vector2 endPos)
        {
            _view = view;
            _view.Init();
            SwipeAnimation(startingPos, endPos);
        }

        private void SwipeAnimation(Vector2 startingPos, Vector2 endPos)
        {
            Image hand = _view.GetHand();
            RectTransform handRectTransform = hand.rectTransform;
            Vector2 newStartingPos = new Vector2(startingPos.x, startingPos.y - 0.5f);
            Vector2 newEndPos = new Vector2(endPos.x, endPos.y - 0.5f);
            Sequence swipeAnimationSequence =
                DOTween.Sequence()
                    .SetLoops(-1, LoopType.Restart)
                    .Append(handRectTransform.DOMove(newStartingPos, 0f))
                    .Append(hand.DOFade(1f, 0.3f).SetEase(Ease.Linear))
                    .Append(handRectTransform.DOScale(1.5f, 0f))
                    .Append(handRectTransform.DOScale(1.1f, 0.4f).SetEase(Ease.Linear))
                    .Append(handRectTransform.DOMove(newEndPos, 0.8f).SetEase(Ease.Linear))
                    .Append(handRectTransform.DOScale(1.5f, 0.4f).SetEase(Ease.Linear))
                    .Append(hand.DOFade(0f, 0.2f).SetEase(Ease.Linear));
        }
    }

    public interface IHandTutorialController
    {
        void Initialize(IHandTutorialView view, Vector2 startingPos, Vector2 endPos);
    }
}