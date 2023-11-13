using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Scripts
{
    public class CardItemController : ICardItemController
    {
        private ICardItemView _view;
        private CardItemData _cardItemData;
        private ISelectionController _selectionController;
        private bool _isDragStart;
        private bool _isAlreadySelected;
        private Action<int> _onDragStart;
        private Action<Vector2, int> _onDragContinue;
        private Func<int, RectTransform> _onDragComplete;
        private Action<bool, int> _onCardSelected;
        private CardHolderType _parentType;
        
        public void Initialize(ICardItemView cardItemView, CardItemData cardItemData, ISelectionController selectionController, ICardItemLocator cardItemLocator)
        {
            _view = cardItemView;
            _cardItemData = cardItemData;
            _selectionController = selectionController;
            _selectionController.SetOnDeselectCards(DeselectCard);
            _parentType = CardHolderType.Initial;
            
            _view.Init(_cardItemData.cardNumber);
            _view.SetOnPointerDown(OnPointerDown);
            _view.SetOnDrag(OnDrag);
            _view.SetOnPointerUp(OnPointerUp);
            SetOnDragStart(cardItemLocator.OnDragStart);
            SetOnDragContinue(cardItemLocator.OnDragContinue);
            SetOnDragComplete(cardItemLocator.OnDragComplete);
            SetOnCardSelected(_cardItemData.onCardSelected);
            //SetAdditionalInfoButtonsStatus(true);
        }

        private void SetOnCardSelected(Action<bool, int> action)
        {
            _onCardSelected += action;
        }
        
        private void SetOnDragStart(Action<int> action)
        {
            _onDragStart += action;
        }

        private void SetOnDragContinue(Action<Vector2, int> action)
        {
            _onDragContinue += action;
        }

        private void SetOnDragComplete(Func<int, RectTransform> func)
        {
            _onDragComplete += func;
        }

        private void OnDrag(PointerEventData data)
        {
            if (!_isDragStart)
            {
                _onDragStart(_cardItemData.cardIndex);
                _view.SetParent(_cardItemData.tempParent);
                _view.SetSize(new Vector2(ConstantValues.BOARD_CARD_HOLDER_WIDTH, ConstantValues.BOARD_CARD_HOLDER_HEIGHT));
                //SetAdditionalInfoButtonsStatus(false);
            }

            _isDragStart = true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_view.GetParent(), data.position,
                null, out Vector2 localPosition);
            _view.SetAnchoredPosition(localPosition);
            _onDragContinue(data.position, _cardItemData.cardIndex);
        }
        
        private void OnPointerUp(PointerEventData data)
        {
            if (!_isDragStart)
            {
                if (!_isAlreadySelected && _parentType == CardHolderType.Initial)
                {
                    _selectionController.SetSelectionState(_cardItemData.cardIndex, true);
                    SetFrameStatus(true);
                    _onCardSelected.Invoke(true, _cardItemData.cardIndex);
                }
            }
            else
            {
                RectTransform cardHolderTransform = _onDragComplete(_cardItemData.cardIndex);
                RectTransform parentTransform;
                if (cardHolderTransform != null)
                {
                    //SetAdditionalInfoButtonsStatus(false);
                    parentTransform = cardHolderTransform;
                    _parentType = CardHolderType.Board;
                }
                else
                {
                    //SetAdditionalInfoButtonsStatus(true);
                    parentTransform = _cardItemData.parent;
                    _parentType = CardHolderType.Initial;
                }
                _view.SetParent(parentTransform);
                _view.InitPosition();
                _view.SetSize(parentTransform.sizeDelta);
            }
        }

        private void OnPointerDown(PointerEventData data)
        {
            _isAlreadySelected = _selectionController.GetSelectionState(_cardItemData.cardIndex);
            _selectionController.DeselectAll();
            
            _isDragStart = false;
        }

        private void DeselectCard()
        {
            SetFrameStatus(false);
            _onCardSelected(false, _cardItemData.cardIndex);
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

    public interface ICardItemController
    {
        void Initialize(ICardItemView cardItemView, CardItemData cardItemData, ISelectionController selectionController, ICardItemLocator cardItemLocator);
        void SetColor(Color color);
    }
}