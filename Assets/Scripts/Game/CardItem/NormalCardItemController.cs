using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Scripts
{
    public class NormalCardItemController : DraggableCardItemController, INormalCardItemController
    {
        private INormalCardItemView _view;
        private ISelectionController _selectionController;
        private bool _isDragStart;
        private bool _isAlreadySelected;
        private Action<int> _onDragStart;
        private Func<int, RectTransform> _onDragComplete;
        private Func<int, int, RectTransform> _onPlaceByClick;
        private Action<bool, int> _onCardSelected;
        private bool _isSelectable;
        private Camera _cam;
        
        public void Initialize(INormalCardItemView cardItemView, CardItemData cardItemData, ISelectionController selectionController, ICardItemLocator cardItemLocator, Camera cam, ICardItemInfoManager cardItemInfoManager)
        {
            _view = cardItemView;
            _cam = cam;
            _cardItemData = cardItemData;
            _selectionController = selectionController;
            _selectionController.SetOnDeselectCards(DeselectCard);
            
            _view.Init(cardItemData.cardNumber);
            _view.InitLocalScale();
            _view.SetLocalPosition(Vector3.zero, 0f);
            _view.SetOnPointerDown(OnPointerDown);
            _view.SetOnDrag(OnDrag);
            _view.SetOnPointerUp(OnPointerUp);
            _view.SetSize(cardItemData.parent.sizeDelta);
            SetOnDragStart(cardItemLocator.OnDragStart);
            SetOnDragContinue(cardItemLocator.OnDragContinue);
            SetOnDragComplete(cardItemLocator.OnDragComplete);
            SetOnPlaceByClick(cardItemLocator.PlaceCardByClick);
            SetOnCardSelected(_cardItemData.onCardSelected);
            CardItemInfo cardItemInfo = cardItemInfoManager.GetCardItemInfoList()[_cardItemData.cardItemIndex];
            SetColor(cardItemInfo.probabilityType);
            SetLockStatus(cardItemInfo.isLocked);
            cardItemInfoManager.ProbabilityChanged += OnProbabilityChanged;
        }
        
        private void OnProbabilityChanged(object sender, ProbabilityChangedEventArgs args)
        {
            if (_cardItemData.cardItemIndex == args.cardIndex)
            {
                SetColor(args.probabilityType);
                SetLockStatus(args.isLocked);
            }
        }
        
        public INormalCardItemView GetView()
        {
            return _view;
        }

        public void SetSize(Vector2 size)
        {
            _view.SetSize(size);
        }

        private void SetOnCardSelected(Action<bool, int> action)
        {
            _onCardSelected += action;
        }
        
        private void SetOnDragStart(Action<int> action)
        {
            _onDragStart += action;
        }

        private void SetOnDragComplete(Func<int, RectTransform> func)
        {
            _onDragComplete += func;
        }

        private void SetOnPlaceByClick(Func<int, int, RectTransform> func)
        {
            _onPlaceByClick += func;
        }

        private void OnDrag(PointerEventData data)
        {
            if (!_isDragStart)
            {
                _onDragStart(_cardItemData.cardItemIndex);
                _view.SetParent(_cardItemData.tempParent);
                _view.SetSize(new Vector2(ConstantValues.BOARD_CARD_HOLDER_WIDTH, ConstantValues.BOARD_CARD_HOLDER_HEIGHT));
            }

            _isDragStart = true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_view.GetParent(), data.position,
                _cam, out Vector2 localPosition);
            _view.SetAnchoredPosition(localPosition);
            _onDragContinue(data.position, _cardItemData.cardItemIndex);
        }

        public void ResetPosition()
        {
            RectTransform parentTransform = _cardItemData.parent;
            _view.SetParent(parentTransform);
            _view.InitLocalScale();
            _view.SetLocalPosition(Vector3.zero, 0.3f);
            _view.SetSize(parentTransform.sizeDelta);
        }
        
        private void OnPointerUp(PointerEventData data)
        {
            if (!_isDragStart)
            {
                if (!_isAlreadySelected && _isSelectable)
                {
                    _selectionController.SetSelectionState(_cardItemData.cardItemIndex, true);
                    SetFrameStatus(true);
                    _onCardSelected.Invoke(true, _cardItemData.cardItemIndex);
                }
            }
            else
            {
                RectTransform cardHolderTransform = _onDragComplete(_cardItemData.cardItemIndex);
                RectTransform parentTransform;
                if (cardHolderTransform != null)
                {
                    parentTransform = cardHolderTransform;
                }
                else
                {
                    parentTransform = _cardItemData.parent;
                }
                _view.SetParent(parentTransform);
                _view.InitLocalScale();
                _view.SetLocalPosition(Vector3.zero, 0.3f);
                _view.SetSize(parentTransform.sizeDelta);
            }
        }

        public void MoveCardByClick(int boardCardHolderIndex)
        {
            _onDragStart.Invoke(_cardItemData.cardItemIndex);
            RectTransform cardHolderTransform = _onPlaceByClick(_cardItemData.cardItemIndex, boardCardHolderIndex);
            _view.SetParent(cardHolderTransform);
            _view.InitLocalScale();
            _view.SetLocalPosition(Vector3.zero, 0.3f);
            _view.SetSize(cardHolderTransform.sizeDelta);
        }

        private void OnPointerDown(PointerEventData data)
        {
            if (_isSelectable)
            {
                _isAlreadySelected = _selectionController.GetSelectionState(_cardItemData.cardItemIndex);
                _selectionController.DeselectAll();
            }
            
            _isDragStart = false;
        }

        private void DeselectCard()
        {
            SetFrameStatus(false);
            _onCardSelected(false, _cardItemData.cardItemIndex);
        }

        private void SetFrameStatus(bool status)
        {
            _view.SetSelectionStatus(status);
        }

        private void SetColor(ProbabilityType probabilityType)
        {
            _view.SetColor(ConstantValues.GetProbabilityTypeToColorMapping()[probabilityType]);
        }

        private void SetLockStatus(bool isLocked)
        {
            _view.SetLockImageStatus(isLocked);
            _isSelectable = !isLocked;
        }

        public Sequence BackFlipAnimation(float delayDuration)
        {
            return DOTween.Sequence()
                .AppendInterval(delayDuration)
                .Append(_view.SetLocalPosition(new Vector3(0f, 50f, 0f), 0.25f))
                .Append(_view.GetRectTransform().DORotate(new Vector3(0f, 90f, 0f), 0.15f))
                .AppendCallback(() => SetColor(ProbabilityType.Certain))
                .AppendCallback(() => _view.SetTextStatus(false))
                .AppendCallback(() => _view.SetLockImageStatus(false))
                .AppendCallback(() => _view.SetBackImageStatus(true))
                .AppendCallback(() => _view.SetNewAnchoredPositionOfRotatedImage())
                .Append(_view.GetRectTransform().DORotate(new Vector3(0f, 180f, 0f), 0.15f))
                .Append(_view.SetLocalPosition(new Vector3(0f, 0f, 0f), 0.15f).SetEase(Ease.OutBounce));
        }
        
    }

    public interface INormalCardItemController
    {
        void Initialize(INormalCardItemView cardItemView, CardItemData cardItemData, ISelectionController selectionController, ICardItemLocator cardItemLocator, Camera cam, ICardItemInfoManager cardItemInfoManager);
        void ResetPosition();
        INormalCardItemView GetView();
        void MoveCardByClick(int boardCardHolderIndex);
        Sequence BackFlipAnimation(float delayDuration);
    }
}