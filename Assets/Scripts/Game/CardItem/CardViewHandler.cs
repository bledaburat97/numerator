﻿using System;
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
        private Camera _cam;

        public CardViewHandler(INormalCardItemView view, Camera cam, IHapticController hapticController, ICardMoveHandler cardMoveHandler, CardItemData cardItemData)
        {
            _view = view;
            _cam = cam;
            _hapticController = hapticController;
            _cardMoveHandler = cardMoveHandler;
            _cardItemData = cardItemData;
            _cardMoveHandler.MoveCardToBoardEvent += MoveCardToBoard;
            _cardMoveHandler.MoveCardToInitialEvent += MoveCardToInitial;
            _view.Init(cardItemData.CardNumber);
            _view.InitLocalScale();
            _view.SetLocalPosition(Vector3.zero);
            _view.SetOnPointerDown(cardMoveHandler.OnPointerDown);
            _view.SetOnDrag(OnDrag);
            _view.SetOnPointerUp(cardMoveHandler.OnPointerUp);
            _view.SetSize(cardItemData.Parent.sizeDelta);
            SetProbability(cardItemData.InitialProbabilityType, cardItemData.InitialIsLocked);
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
            SetParent(parent);
            _view.SetSize(new Vector2(ConstantValues.BOARD_CARD_HOLDER_WIDTH, ConstantValues.BOARD_CARD_HOLDER_HEIGHT));
        }

        public void SetParent(RectTransform parent)
        {
            _view.SetParent(parent);
        }

        public Vector2 CalculateAnchoredPosition(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_view.GetParent(), screenPosition, _cam,
                out Vector2 localPosition);
            return localPosition;
        }

        public void UpdatePosition(Vector2 localPosition)
        {
            _view.SetAnchoredPosition(localPosition);
        }

        private void MoveCardToBoard(object sender, RectTransform boardHolderRectTransform)
        {
            PlaceCard(boardHolderRectTransform);
        }

        private void MoveCardToInitial(object sender, EventArgs args)
        {
            PlaceCard(_cardItemData.Parent);
        }
        
        private void PlaceCard(RectTransform parentTransform)
        {
            SetParent(_cardItemData.TempParent);
            _view.InitLocalScale();
            DOTween.Sequence().Append(_view.ChangePosition(parentTransform.position, 0.3f))
                .OnComplete(() => _view.SetParent(parentTransform));
            _view.SetSize(parentTransform.sizeDelta);
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
            
            
            /*
            DOTween.Sequence()
                .AppendInterval(delayDuration)
                .Append(_view.ChangeLocalPosition(new Vector3(0f, 50f, 0f), 0.25f))
                .Append(_view.GetRectTransform().DORotate(new Vector3(0f, 90f, 0f), 0.15f))
                .AppendCallback(() =>
                    _view.SetColor(isGuessRight
                        ? ConstantValues.GetProbabilityTypeToColorMapping()[(int)ProbabilityType.Certain]
                        : ConstantValues.GREY_CARD_COLOR))
                .AppendCallback(() => _view.SetTextStatus(false))
                .AppendCallback(() => _view.SetLockImageStatus(false))
                .AppendCallback(isGuessRight
                    ? () => _view.SetBackImageStatus(true)
                    : () => _view.SetBackText(correctNumber))
                .AppendCallback(() => _view.SetNewAnchoredPositionOfRotatedImage())
                .Append(_view.GetRectTransform().DORotate(new Vector3(0f, 180f, 0f), 0.15f))
                .Append(_view.ChangeLocalPosition(new Vector3(0f, 0f, 0f), 0.15f).SetEase(Ease.OutBounce))
                .OnComplete(() => _hapticController.Vibrate(HapticType.CardGrab));
                */
                
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

    }

    public interface ICardViewHandler
    {
        void InitializeDrag(RectTransform parent);
        void SetParent(RectTransform parent);
        Vector2 CalculateAnchoredPosition(Vector2 screenPosition);
        void UpdatePosition(Vector2 localPosition);
        void SetProbability(ProbabilityType probabilityType, bool isLocked);
        void BackFlipAnimation(float delayDuration, bool isGuessRight, string correctNumber);
        INormalCardItemView GetView();
        void SetCardAnimation(bool status);
        void DestroyObject();
        void AnimateProbabilityChange(float duration, ProbabilityType probabilityType, bool isLocked);
        RectTransform GetRectTransform();
        void SuccessAnimation(float delayDuration);
    }
}