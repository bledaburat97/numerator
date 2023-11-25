using System;
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
        private Action<bool, int> _onCardSelected;
        private bool _isSelectable;
        
        public void Initialize(INormalCardItemView cardItemView, CardItemData cardItemData, ISelectionController selectionController, ICardItemLocator cardItemLocator)
        {
            _view = cardItemView;
            _cardItemData = cardItemData;
            _isSelectable = true;
            _selectionController = selectionController;
            _selectionController.SetOnDeselectCards(DeselectCard);
            
            _view.Init(cardItemData.cardNumber);
            _view.InitPosition();
            _view.SetOnPointerDown(OnPointerDown);
            _view.SetOnDrag(OnDrag);
            _view.SetOnPointerUp(OnPointerUp);
            SetOnDragStart(cardItemLocator.OnDragStart);
            SetOnDragContinue(cardItemLocator.OnDragContinue);
            SetOnDragComplete(cardItemLocator.OnDragComplete);
            SetOnCardSelected(_cardItemData.onCardSelected);
        }

        public INormalCardItemView GetView()
        {
            return _view;
        }

        public void DisableSelectability()
        {
            _isSelectable = false;
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
                null, out Vector2 localPosition);
            _view.SetAnchoredPosition(localPosition);
            _onDragContinue(data.position, _cardItemData.cardItemIndex);
        }

        public void ResetPosition()
        {
            RectTransform parentTransform = _cardItemData.parent;
            _view.SetParent(parentTransform);
            _view.InitPosition();
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
                _view.InitPosition();
                _view.SetSize(parentTransform.sizeDelta);
            }
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
            _view.SetFrameStatus(status);
        }

        public void SetColor(Color color)
        {
            _view.SetColor(color);
        }
    }

    public interface INormalCardItemController
    {
        void Initialize(INormalCardItemView cardItemView, CardItemData cardItemData, ISelectionController selectionController, ICardItemLocator cardItemLocator);
        void SetColor(Color color);
        void ResetPosition();
        INormalCardItemView GetView();
        void DisableSelectability();
    }
}