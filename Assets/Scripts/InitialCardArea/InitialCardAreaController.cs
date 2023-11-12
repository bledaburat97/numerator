﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class InitialCardAreaController : IInitialCardAreaController
    {
        private IInitialCardAreaView _view;
        private ISelectionController _selectionController;
        private List<ICardHolderController> _cardHolderControllerList = new List<ICardHolderController>();
        private ICardItemLocator _cardItemLocator;

        public void Initialize(IInitialCardAreaView view, ICardItemLocator cardItemLocator, Action<bool, int> onCardSelected)
        {
            _view = view;
            _view.Init(new CardHolderFactory(), new CardItemViewFactory());
            _cardItemLocator = cardItemLocator;
            CreateCardHolders();
            _selectionController = new SelectionController(_cardHolderControllerList.Count); //TODO: get level data
            CreateCardItemsData(onCardSelected);
        }
        
        private void CreateCardHolders()
        {
            CardHolderControllerFactory cardHolderControllerFactory = new CardHolderControllerFactory();
            
            foreach (CardHolderModel cardHolderModel in CardHolderModelCreator.GetInstance().GetCardHolderModelList(CardHolderType.Initial))
            {
                ICardHolderController cardHolderController = cardHolderControllerFactory.Spawn();
                ICardHolderView cardHolderView = _view.CreateCardHolderView();
                cardHolderController.Initialize(cardHolderView, cardHolderModel);
                _cardHolderControllerList.Add(cardHolderController);
            }
        }
        
        private void CreateCardItemsData(Action<bool, int> onCardSelected)
        {
            for (int i = 0; i < _cardHolderControllerList.Count; i++)
            {
                CardItemData cardItemData = new CardItemData()
                {
                    parent = _cardHolderControllerList[i].GetView().GetRectTransform(),
                    tempParent = _view.GetTempRectTransform(),
                    cardNumber = i + 1,
                    cardIndex = i,
                    onCardSelected = onCardSelected
                };
                CreateCardItem(cardItemData);
            }
        }
        
        private void CreateCardItem(CardItemData cardItemData)
        {
            CardItemControllerFactory cardItemControllerFactory = new CardItemControllerFactory();
            ICardItemView cardItemView = _view.CreateCardItemView(cardItemData.parent);
            ICardItemController cardItemController = cardItemControllerFactory.Spawn();
            cardItemController.Initialize(cardItemView, cardItemData, _selectionController, _cardItemLocator);
        }
        
    }
    
    public interface IInitialCardAreaController
    {
        void Initialize(IInitialCardAreaView view, ICardItemLocator cardItemLocator, Action<bool, int> onCardSelected);
    }
    
    public class CardItemData
    {
        public RectTransform parent;
        public RectTransform tempParent;
        public int cardNumber;
        public int cardIndex;
        public Action<bool, int> onCardSelected;
    }
}