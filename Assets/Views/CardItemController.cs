using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Views
{
    public class CardItemController : ICardItemController
    {
        private ICardItemView _view;
        private CardItemData _cardItemData;
        private ICardHolderController _currentCardHolderController;
        
        public void Initialize(ICardItemView cardItemView, CardItemData cardItemData)
        {
            _view = cardItemView;
            _cardItemData = cardItemData;
            
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
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_view.GetParent(), data.position,
                null, out Vector2 localPosition);
            _view.SetAnchoredPosition(localPosition); 
            CardItemLocator.GetInstance().OnDrag(data.position);
        }
        
        private void OnPointerUp(PointerEventData data)
        {
            _currentCardHolderController = CardItemLocator.GetInstance().GetSelectedBoardCardHolderController();
            
            if (_currentCardHolderController != null)
            {
                ICardHolderView boardCardHolderView = _currentCardHolderController.GetView();
                _view.SetParent(boardCardHolderView.GetRectTransform());
                _view.InitPosition();
                _view.SetSize(boardCardHolderView.GetRectTransform().sizeDelta);
                SetAdditionalInfoButtonsStatus(false);
                _currentCardHolderController.SetAvailability(false);
            }
            else
            {
                _view.SetParent(_cardItemData.parent);
                _view.InitPosition();
                _view.SetSize(_cardItemData.parent.sizeDelta);
                SetAdditionalInfoButtonsStatus(true);
            }
        }

        private void OnPointerDown(PointerEventData data)
        {
            //TODO: SetLastBoardCardHolderController availability to true;
            if (_currentCardHolderController != null)
            {
                _currentCardHolderController.SetAvailability(true);
            }

            _currentCardHolderController = null;
            _view.SetParent(_cardItemData.tempParent);
            _view.SetSize(new Vector2(ConstantValues.BOARD_CARD_HOLDER_WIDTH, ConstantValues.BOARD_CARD_HOLDER_HEIGHT));
            SetAdditionalInfoButtonsStatus(false);
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
        void Initialize(ICardItemView cardItemView, CardItemData cardItemData);
    }

    public class CardItemModel
    {
        public int cardNumber;
        public List<ICardLetterController> cardLetterControllerList = new List<ICardLetterController>(); 
        public List<IExistenceButtonController> existenceButtonControllerList = new List<IExistenceButtonController>();
    }
}