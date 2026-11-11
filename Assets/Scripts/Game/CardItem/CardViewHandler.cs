using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Scripts
{
    public class CardViewHandler : ICardViewHandler
    {
        private readonly INormalCardItemView _view;
        private readonly IHapticController _hapticController;
        private readonly ICardMoveHandler _cardMoveHandler;
        private readonly CardItemData _cardItemData;
        private readonly object _moveTweenId = new object();
        private int _moveVersion;
        private Camera _cam;

        public CardViewHandler(INormalCardItemView view, Camera cam, IHapticController hapticController,
            ICardMoveHandler cardMoveHandler, CardItemData cardItemData)
        {
            _view = view;
            _cam = cam;
            _hapticController = hapticController;
            _cardMoveHandler = cardMoveHandler;
            _cardItemData = cardItemData;
            _view.Init(cardItemData.CardNumber);
            _view.InitLocalScale();
            _view.SetLocalPosition(Vector3.zero);
            _view.SetOnPointerDown(cardMoveHandler.OnPointerDown);
            _view.SetOnDrag(OnDrag);
            _view.SetOnPointerUp(cardMoveHandler.OnPointerUp);
            _view.SetOnPointerCancel(cardMoveHandler.CancelPress);
            _view.SetSize(cardItemData.Size);
            SetProbability(cardItemData.InitialProbabilityType, cardItemData.InitialIsLocked);
        }

        public void SetLocalPosition(Vector2 localPosition)
        {
            _view.SetLocalPosition(localPosition);
        }

        private void OnDrag(PointerEventData data)
        {
            if (!_cardMoveHandler.CanHandleDrag(data)) return;

            if (!_cardMoveHandler.IsDragStarted())
            {
                InitializeDrag(_cardItemData.TempParent);
            }

            Vector2 localPosition = CalculateAnchoredPosition(data.position);
            UpdatePosition(localPosition);
            _cardMoveHandler.HandleDrag(data.position);
        }

        public void SetProbability(ProbabilityType probabilityType, bool isLocked)
        {
            _view.SetLockImageStatus(isLocked);
            _cardMoveHandler.SetMovementLocked(isLocked);
            switch (probabilityType)
            {
                case ProbabilityType.Certain:
                    _view.SetAlpha(1f);
                    _view.SetFrameStatus(false);
                    break;
                case ProbabilityType.NotExisted:
                    _view.SetAlpha(0.5f);
                    _view.SetFrameStatus(false);
                    break;
                default:
                    _view.SetAlpha(1f);
                    _view.SetFrameStatus(false);
                    break;
            }
        }

        public void InitializeDrag(RectTransform parent)
        {
            KillActiveMoveSequence();
            SetParent(parent);
        }

        private void SetParent(RectTransform parent)
        {
            _view.SetParent(parent);
        }

        private Vector2 CalculateAnchoredPosition(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_view.GetParent(), screenPosition, _cam,
                out Vector2 localPosition);
            return localPosition;
        }

        private void UpdatePosition(Vector2 localPosition)
        {
            _view.SetAnchoredPosition(localPosition);
        }

        public void MoveToParent(RectTransform parentTransform, Action onComplete = null)
        {
            PlaceCard(parentTransform, onComplete);
        }

        public void SnapToParent(RectTransform parentTransform)
        {
            // A locked placement supersedes any pending flight and its parent callback.
            KillActiveMoveSequence();
            SetParent(parentTransform);
            _view.InitLocalScale();
            _view.SetLocalPosition(Vector3.zero);
        }

        public void MoveToInitialParent(Action onComplete = null)
        {
            PlaceCard(_cardItemData.Parent, onComplete);
        }

        private void PlaceCard(RectTransform parentTransform, Action onComplete = null)
        {
            KillActiveMoveSequence();
            SetParent(_cardItemData.TempParent);
            _view.InitLocalScale();
            int version = _moveVersion;
            DOTween.Sequence().SetId(_moveTweenId).SetLink(_view.GetRectTransform().gameObject, LinkBehaviour.KillOnDestroy)
                .Append(_view.ChangePosition(parentTransform.position, 0.3f))
                .OnComplete(() =>
                {
                    if (version != _moveVersion) return;
                    _view.SetParent(parentTransform);
                    _view.InitLocalScale();
                    _view.SetLocalPosition(Vector3.zero);
                    onComplete?.Invoke();
                });
        }

        public void SuccessAnimation(float delayDuration)
        {
            DOTween.Sequence()
                .AppendInterval(delayDuration)
                .Append(_view.GetRectTransform().DOPunchScale(Vector3.one * 0.12f, 0.22f, 5, 0.75f))
                .OnComplete(() => _hapticController.Vibrate(HapticType.CardGrab));
        }


        public void BackFlipAnimation(float delayDuration, bool isGuessRight, string correctNumber)
        {
            if (isGuessRight)
            {
                DOTween.Sequence()
                    .AppendInterval(delayDuration)
                    .Append(_view.GetRectTransform().DOPunchScale(Vector3.one * 0.12f, 0.22f, 5, 0.75f))
                    .OnComplete(() => _hapticController.Vibrate(HapticType.CardGrab));
            }
            else
            {

            }
        }

        public INormalCardItemView GetView()
        {
            return _view;
        }

        public void SetCardAnimation(bool status)
        {
            _view.SetCardAnimation(status);
        }

        public void DestroyObject()
        {
            _cardMoveHandler.CancelPress();
            KillActiveMoveSequence();
            _view.DestroyObject();
        }

        public void AnimateProbabilityChange(float duration, ProbabilityType probabilityType, bool isLocked)
        {
            DOTween.Sequence().AppendCallback(() =>
            {
                _view.SetLockImageStatus(isLocked);
                _cardMoveHandler.SetMovementLocked(isLocked);
                switch (probabilityType)
                {
                    case ProbabilityType.Certain:
                        _view.SetAlpha(1f);
                        _view.SetFrameStatus(false);
                        break;
                    case ProbabilityType.NotExisted:
                        _view.SetAlpha(0.5f);
                        _view.SetFrameStatus(false);
                        break;
                    default:
                        _view.SetAlpha(1f);
                        _view.SetFrameStatus(false);
                        break;
                }
            });
        }

        public RectTransform GetRectTransform()
        {
            return _view.GetRectTransform();
        }

        public Sequence AnimateExplosion(float duration)
        {
            float popDuration = Mathf.Min(0.08f, duration * 0.4f);
            float fadeDuration = Mathf.Max(0.01f, duration - popDuration);
            RectTransform rectTransform = _view.GetRectTransform();

            return DOTween.Sequence()
                .Append(rectTransform.DOScale(Vector3.one * 1.15f, popDuration).SetEase(Ease.OutQuad))
                .Append(DOTween.To(() => 1f, _view.SetAlpha, 0f, fadeDuration))
                .Join(rectTransform.DOScale(Vector3.zero, fadeDuration).SetEase(Ease.InBack))
                .AppendCallback(_view.DestroyObject);
        }

        public void AnimateTurnIntoCertain(float delayDuration, float colorChangeDuration,
            float ribbonImageDuration)
        {
            // The view owns and cancels the short visual feedback. Movement and
            // the authoritative board parent are not changed by this animation.
            DOTween.Sequence().AppendInterval(delayDuration)
                .AppendCallback(() =>
                {
                    _view.AnimateLockImage(colorChangeDuration);
                    _hapticController.Vibrate(HapticType.Success);
                })
                .SetLink(_view.GetRectTransform().gameObject,LinkBehaviour.KillOnDisable);
        }
        
        public Sequence FallToTarget(Vector2 targetPosition, float fallDuration, float bounceDuration)
        {
            KillActiveMoveSequence();
            return DOTween.Sequence()
                .Append(_view.GetRectTransform().DOLocalMove(targetPosition, fallDuration).SetEase(Ease.InQuad))
                .Append(_view.GetRectTransform().DOLocalMove(targetPosition, bounceDuration).SetEase(Ease.OutBounce));
        }

        private void KillActiveMoveSequence()
        {
            // Kill every owned flight, even if DOTween is completing an older
            // sequence in the same frame. Stale callbacks cannot change parent.
            _moveVersion++;
            DOTween.Kill(_moveTweenId);
        }

    }

    public interface ICardViewHandler
    {
        void InitializeDrag(RectTransform parent);
        void SetProbability(ProbabilityType probabilityType, bool isLocked);
        void BackFlipAnimation(float delayDuration, bool isGuessRight, string correctNumber);
        INormalCardItemView GetView();
        void SetCardAnimation(bool status);
        void DestroyObject();
        void AnimateProbabilityChange(float duration, ProbabilityType probabilityType, bool isLocked);
        RectTransform GetRectTransform();
        void SuccessAnimation(float delayDuration);

        void AnimateTurnIntoCertain(float delayDuration, float colorChangeDuration,
            float ribbonImageDuration);

        Sequence AnimateExplosion(float duration);
        Sequence FallToTarget(Vector2 targetPosition, float fallDuration, float bounceDuration);
        void SetLocalPosition(Vector2 localPosition);
        void MoveToParent(RectTransform parentTransform, Action onComplete = null);
        void SnapToParent(RectTransform parentTransform);
        void MoveToInitialParent(Action onComplete = null);
    }
}
