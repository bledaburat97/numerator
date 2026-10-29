using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class HandTutorialView : MonoBehaviour, IHandTutorialView
    {
        [SerializeField] private Image handImage;
        private Sequence _activeAnimation;
        public void Init(float anchorMaxYOfSafeArea, float heightOfCanvas)
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            handImage.raycastTarget = false;
            StopActiveAnimation();
        }
        public void StartDragAnimation(Vector2 startingPos, Vector2 endPos)
        {
            StopActiveAnimation(); handImage.gameObject.SetActive(true);
            RectTransform hand = handImage.rectTransform;
            float z = hand.position.z;
            hand.localScale = Vector3.one;
            hand.position = new Vector3(startingPos.x, startingPos.y, z);
            Color color = handImage.color; color.a = 0f; handImage.color = color;
            _activeAnimation = DOTween.Sequence()
                .Append(handImage.DOFade(1f, .16f))
                .Append(hand.DOScale(.85f, .15f))
                .Append(hand.DOMove(new Vector3(endPos.x, endPos.y, z), .7f).SetEase(Ease.InOutSine))
                .Append(hand.DOScale(1f, .12f))
                .Append(handImage.DOFade(0f, .18f))
                .AppendInterval(.25f).SetLoops(-1).SetLink(gameObject);
        }
        public void StartClickAnimation(Vector2 pos)
        {
            StopActiveAnimation(); handImage.gameObject.SetActive(true);
            RectTransform hand = handImage.rectTransform;
            hand.position = new Vector3(pos.x, pos.y, hand.position.z);
            hand.localScale = Vector3.one;
            _activeAnimation = DOTween.Sequence()
                .Append(handImage.DOFade(1f, .15f))
                .Append(hand.DOScale(.82f, .2f))
                .Append(hand.DOScale(1f, .2f))
                .Append(handImage.DOFade(0f, .18f))
                .AppendInterval(.3f).SetLoops(-1).SetLink(gameObject);
        }
        public void StopActiveAnimation()
        {
            if (_activeAnimation != null && _activeAnimation.IsActive()) _activeAnimation.Kill();
            _activeAnimation = null;
            if (handImage != null) handImage.gameObject.SetActive(false);
        }
        private void OnDestroy() => StopActiveAnimation();
        public void Destroy() { StopActiveAnimation(); TutorialObjectUtility.Destroy(gameObject); }
    }
    public interface IHandTutorialView
    {
        void Init(float anchorMaxYOfSafeArea, float heightOfCanvas);
        void StartDragAnimation(Vector2 startingPos, Vector2 endPos);
        void StartClickAnimation(Vector2 pos);
        void StopActiveAnimation();
        void Destroy();
    }
}
