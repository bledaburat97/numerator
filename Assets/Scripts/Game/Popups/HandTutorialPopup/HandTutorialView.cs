using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class HandTutorialView : MonoBehaviour, IHandTutorialView
    {
        [SerializeField] private Image handImage;
        private Sequence _activeAnimation = null;
        private float _anchorMaxYOfSafeArea;
        private float _heightOfCanvas;
        public void Init(float anchorMaxYOfSafeArea, float heightOfCanvas)
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            _anchorMaxYOfSafeArea = anchorMaxYOfSafeArea;
            _heightOfCanvas = heightOfCanvas;
        }
        
        public void StartDragAnimation(Vector2 startingPos, Vector2 endPos)
        {
            if(_activeAnimation != null) return;
            handImage.gameObject.SetActive(true);
            RectTransform handRectTransform = handImage.rectTransform;
            Vector2 localStartingPos = transform.InverseTransformPoint(new Vector3(startingPos.x, startingPos.y - 0.4f, 0));
            float newLocalStartingPositionY = (localStartingPos.y + _heightOfCanvas / 2) / _anchorMaxYOfSafeArea - _heightOfCanvas / 2;
            Vector3 newLocalStartingPos = new Vector3(localStartingPos.x, newLocalStartingPositionY , 0);
            
            Vector2 localEndPos = transform.InverseTransformPoint(new Vector3(endPos.x, endPos.y - 0.4f, 0));
            float newLocalEndPositionY = (localEndPos.y + _heightOfCanvas / 2) / _anchorMaxYOfSafeArea - _heightOfCanvas / 2;
            Vector3 newLocalEndPos = new Vector3(localEndPos.x, newLocalEndPositionY , 0);
            handRectTransform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            _activeAnimation =  DOTween.Sequence()
                .SetLoops(-1, LoopType.Restart)
                .Append(handRectTransform.DOLocalMove(newLocalStartingPos, 0f))
                .Append(handImage.DOFade(0f, 0f).SetEase(Ease.Linear))
                .AppendInterval(0.5f)
                .Append(handImage.DOFade(1f, 0.3f).SetEase(Ease.Linear))
                .Append(handRectTransform.DOScale(1.5f, 0f))
                .Append(handRectTransform.DOScale(1.1f, 0.4f).SetEase(Ease.Linear))
                .Append(handRectTransform.DOLocalMove(newLocalEndPos, 0.8f).SetEase(Ease.Linear))
                .Append(handRectTransform.DOScale(1.5f, 0.4f).SetEase(Ease.Linear))
                .Append(handImage.DOFade(0f, 0.2f).SetEase(Ease.Linear));
        }

        public void StartClickAnimation(Vector2 pos)
        {
            if(_activeAnimation != null) return;
            handImage.gameObject.SetActive(true);
            RectTransform handRectTransform = handImage.rectTransform;
            
            Vector2 localPos = transform.InverseTransformPoint(new Vector3(pos.x, pos.y - 0.4f, 0));
            float newLocalPositionY = (localPos.y + _heightOfCanvas / 2) / _anchorMaxYOfSafeArea - _heightOfCanvas / 2;
            Vector3 newLocalPos = new Vector3(localPos.x, newLocalPositionY , 0);
            handRectTransform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            _activeAnimation =  DOTween.Sequence()
                .SetLoops(-1, LoopType.Restart)
                .Append(handRectTransform.DOLocalMove(newLocalPos, 0f))
                .Append(handImage.DOFade(0f, 0f).SetEase(Ease.Linear))
                .AppendInterval(0.5f)
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
                Color color = handImage.color;
                color.a = 0f;
                handImage.color = color; 
                handImage.gameObject.SetActive(false);
                _activeAnimation.Pause();
                _activeAnimation = null;
            }
        }
        
        public void Destroy()
        {
            Destroy(gameObject);
        }
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