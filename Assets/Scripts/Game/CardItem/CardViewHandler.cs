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
        private Sequence _activeMoveSequence;
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
            _view.SetSize(cardItemData.Size);
            SetProbability(cardItemData.InitialProbabilityType, cardItemData.InitialIsLocked);
        }

        public void SetLocalPosition(Vector2 localPosition)
        {
            _view.SetLocalPosition(localPosition);
        }

        private void OnDrag(PointerEventData data)
        {
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
            _view.SetColor(ConstantValues.GetProbabilityTypeToColorMapping()[(int)probabilityType]);
            _view.SetLockImageStatus(isLocked);
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

        public void MoveToInitialParent(Action onComplete = null)
        {
            PlaceCard(_cardItemData.Parent, onComplete);
        }

        private void PlaceCard(RectTransform parentTransform, Action onComplete = null)
        {
            KillActiveMoveSequence();
            SetParent(_cardItemData.TempParent);
            _view.InitLocalScale();
            _activeMoveSequence = DOTween.Sequence()
                .Append(_view.ChangePosition(parentTransform.position, 0.3f))
                .OnComplete(() =>
                {
                    _view.SetParent(parentTransform);
                    _view.InitLocalScale();
                    _view.SetLocalPosition(Vector3.zero);
                    _activeMoveSequence = null;
                    onComplete?.Invoke();
                })
                .OnKill(() => _activeMoveSequence = null);
        }

        public void SuccessAnimation(float delayDuration)
        {
            DOTween.Sequence()
                .AppendInterval(delayDuration)
                .Append(_view.AnimateColorChange(
                    ConstantValues.GetProbabilityTypeToColorMapping()[(int)ProbabilityType.Certain], 0.5f))
                .Join(_view.AnimateLockImage(0.2f))
                .OnComplete(() => _hapticController.Vibrate(HapticType.CardGrab));
        }


        public void BackFlipAnimation(float delayDuration, bool isGuessRight, string correctNumber)
        {
            if (isGuessRight)
            {
                DOTween.Sequence()
                    .AppendInterval(delayDuration)
                    .Append(_view.AnimateColorChange(
                        ConstantValues.GetProbabilityTypeToColorMapping()[(int)ProbabilityType.Certain], 0.5f))
                    .Join(_view.AnimateLockImage(0.2f))
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
            KillActiveMoveSequence();
            _view.DestroyObject();
        }

        public void AnimateProbabilityChange(float duration, ProbabilityType probabilityType, bool isLocked)
        {
            DOTween.Sequence().AppendCallback(() =>
                    _view.SetColor(ConstantValues.GetProbabilityTypeToColorMapping()[(int)probabilityType]))
                .AppendCallback(() => _view.SetLockImageStatus(isLocked));
        }

        public RectTransform GetRectTransform()
        {
            return _view.GetRectTransform();
        }

        public Sequence AnimateExplosion(float duration)
        {
            return DOTween.Sequence().AppendCallback(() =>
            {
                ActivateExplosionParticle();
                SetStatusOfImage(false);
            }).AppendInterval(duration).AppendCallback(_view.DestroyObject);
        }

        public void AnimateTurnIntoCertain(float delayDuration, float colorChangeDuration,
            float ribbonImageDuration)
        {
            DOTween.Sequence().AppendInterval(delayDuration)
                .Append(AnimateColorChange(colorChangeDuration, ProbabilityType.Certain))
                .Join(DOTween.Sequence().AppendInterval(colorChangeDuration - ribbonImageDuration)
                    .Append(AnimateRibbonImage(ribbonImageDuration)))
                .AppendCallback(ActivateGlitteringParticle);
        }

        public Sequence AnimateColorChange(float duration, ProbabilityType probabilityType)
        {
            Color newColor = ConstantValues.GetProbabilityTypeToColorMapping()[(int)probabilityType];
            SetColorOfInnerImage(newColor);
            return DOTween.Sequence()
                .Append(_view.GetInnerImage().rectTransform.DOSizeDelta(_view.GetImage().rectTransform.sizeDelta * 2, duration))
                .AppendCallback(() =>
                {
                    SetColorOfImage(newColor);
                    _view.GetInnerImage().rectTransform.sizeDelta = Vector2.zero;
                });
        }

        private Sequence AnimateRibbonImage(float duration)
        {
            return DOTween.Sequence();
        }

        private void ActivateGlitteringParticle()
        {

        }

        private void ActivateExplosionParticle()
        {

        }

        private void SetColorOfInnerImage(Color color)
        {
            _view.GetInnerImage().color = color;
        }

        private void SetColorOfImage(Color color)
        {
            _view.GetImage().color = color;
        }

        private void SetStatusOfImage(bool status)
        {
            _view.GetImage().gameObject.SetActive(status);
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
            if (_activeMoveSequence == null || !_activeMoveSequence.IsActive()) return;
            _activeMoveSequence.Kill();
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
        void MoveToInitialParent(Action onComplete = null);
    }
}
