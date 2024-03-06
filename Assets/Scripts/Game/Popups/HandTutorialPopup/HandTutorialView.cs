using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class HandTutorialView : MonoBehaviour, IHandTutorialView
    {
        [SerializeField] private Image handImage;
        private Sequence _activeAnimation = null;

        public void Init()
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
        }
        
        public void StartDragAnimation(Vector2 startingPos, Vector2 endPos)
        {
            if(_activeAnimation != null) return;
            handImage.gameObject.SetActive(true);
            RectTransform handRectTransform = handImage.rectTransform;
            Vector2 newStartingPos = new Vector2(startingPos.x, startingPos.y - 0.5f);
            Vector2 newEndPos = new Vector2(endPos.x, endPos.y - 0.5f);
            _activeAnimation =  DOTween.Sequence()
                .SetLoops(-1, LoopType.Restart)
                .Append(handRectTransform.DOMove(newStartingPos, 0f))
                .Append(handImage.DOFade(1f, 0.3f).SetEase(Ease.Linear))
                .Append(handRectTransform.DOScale(1.5f, 0f))
                .Append(handRectTransform.DOScale(1.1f, 0.4f).SetEase(Ease.Linear))
                .Append(handRectTransform.DOMove(newEndPos, 0.8f).SetEase(Ease.Linear))
                .Append(handRectTransform.DOScale(1.5f, 0.4f).SetEase(Ease.Linear))
                .Append(handImage.DOFade(0f, 0.2f).SetEase(Ease.Linear));
        }

        public void StartClickAnimation(Vector2 pos)
        {
            if(_activeAnimation != null) return;
            handImage.gameObject.SetActive(true);
            RectTransform handRectTransform = handImage.rectTransform;
            Vector2 newPos = new Vector2(pos.x, pos.y - 0.5f);
            _activeAnimation =  DOTween.Sequence()
                .SetLoops(-1, LoopType.Restart)
                .Append(handRectTransform.DOMove(newPos, 0f))
                .Append(handImage.DOFade(1f, 0.3f).SetEase(Ease.Linear))
                .Append(handRectTransform.DOScale(1.5f, 0f))
                .Append(handRectTransform.DOScale(1.1f, 0.4f).SetEase(Ease.Linear))
                .AppendInterval(0.2f)
                .Append(handRectTransform.DOScale(1.5f, 0.4f).SetEase(Ease.Linear))
                .Append(handImage.DOFade(0f, 0.2f).SetEase(Ease.Linear));
        }

        public void StopActiveAnimation()
        {
            if (_activeAnimation != null)
            {
                _activeAnimation.Pause();
                handImage.gameObject.SetActive(false);
                _activeAnimation = null;
            }
        }
    }

    public interface IHandTutorialView
    {
        void Init();
        void StartDragAnimation(Vector2 startingPos, Vector2 endPos);
        void StartClickAnimation(Vector2 pos);
        void StopActiveAnimation();
    }
}