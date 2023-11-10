using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Views
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
        private ParentType _parentType;

        public void SetOnDragStart(Action<int> action)
        {
            _onDragStart += action;
        }

        public void SetOnDragContinue(Action<Vector2, int> action)
        {
            _onDragContinue += action;
        }

        public void SetOnDragComplete(Func<int, RectTransform> func)
        {
            _onDragComplete += func;
        }
        
        public void Initialize(ICardItemView cardItemView, CardItemData cardItemData, ISelectionController selectionController)
        {
            _view = cardItemView;
            _cardItemData = cardItemData;
            _selectionController = selectionController;
            _selectionController.deselectCards += DeselectCard;
            _parentType = ParentType.InitialHolder;
            foreach (ICardLetterController cardLetterController in cardItemData.cardLetterControllers)
            {
                cardLetterController.SetParent(_view.GetCardLetterHolderTransform());
            }
            
            foreach (IExistenceButtonController existenceButtonController in cardItemData.existenceButtonControllers)
            {
                existenceButtonController.SetParent(_view.GetExistenceButtonHolderTransform());
            }
            
            _view.Init(_cardItemData.cardNumber);
            _view.SetOnPointerDown(OnPointerDown);
            _view.SetOnDrag(OnDrag);
            _view.SetOnPointerUp(OnPointerUp);
            SetAdditionalInfoButtonsStatus(true);
        }

        private void OnDrag(PointerEventData data)
        {
            if (!_isDragStart)
            {
                _onDragStart(_cardItemData.cardIndex);
                _view.SetParent(_cardItemData.tempParent);
                _view.SetSize(new Vector2(ConstantValues.BOARD_CARD_HOLDER_WIDTH, ConstantValues.BOARD_CARD_HOLDER_HEIGHT));
                SetAdditionalInfoButtonsStatus(false);
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
                if (!_isAlreadySelected && _parentType == ParentType.InitialHolder)
                {
                    _selectionController.SetSelectionState(_cardItemData.cardIndex, true);
                    SetFrameStatus(true);
                }
            }
            else
            {
                RectTransform cardHolderTransform = _onDragComplete(_cardItemData.cardIndex);
                RectTransform parentTransform;
                if (cardHolderTransform != null)
                {
                    SetAdditionalInfoButtonsStatus(false);
                    parentTransform = cardHolderTransform;
                    _parentType = ParentType.BoardCardHolder;
                }
                else
                {
                    SetAdditionalInfoButtonsStatus(true);
                    parentTransform = _cardItemData.parent;
                    _parentType = ParentType.InitialHolder;
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

        private void DeselectCard(object sender, EventArgs args)
        {
            SetFrameStatus(false);
        }

        private void SetFrameStatus(bool status)
        {
            _view.SetFrameStatus(status);
        }

        private void SetAdditionalInfoButtonsStatus(bool status)
        {
            foreach (ICardLetterController cardLetterController in _cardItemData.cardLetterControllers)
            {
                cardLetterController.SetStatus(status);
            }

            foreach (IExistenceButtonController existenceButtonController in _cardItemData.existenceButtonControllers)
            {
                existenceButtonController.SetStatus(status);
            }
        }
    }

    public interface ICardItemController
    {
        void Initialize(ICardItemView cardItemView, CardItemData cardItemData, ISelectionController selectionController);
        void SetOnDragStart(Action<int> action);
        void SetOnDragContinue(Action<Vector2, int> action);
        void SetOnDragComplete(Func<int, RectTransform> func);
    }

    public class CardItemModel
    {
        public int cardNumber;
        public List<ICardLetterController> cardLetterControllerList = new List<ICardLetterController>(); 
        public List<IExistenceButtonController> existenceButtonControllerList = new List<IExistenceButtonController>();
    }

    public enum ParentType
    {
        InitialHolder,
        BoardCardHolder
    }
}