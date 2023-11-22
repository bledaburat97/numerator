﻿using UnityEngine;

namespace Scripts
{
    public class BoardAreaView : MonoBehaviour, IBoardAreaView
    {
        [SerializeField] private CardHolderView boardCardHolderPrefab;
        private CardHolderFactory _cardHolderFactory;

        public void Init(CardHolderFactory cardHolderFactory)
        {
            _cardHolderFactory = cardHolderFactory;
        }
        
        public ICardHolderView CreateCardHolderView()
        {
            return _cardHolderFactory.Spawn(transform, boardCardHolderPrefab);
        }
    }

    public interface IBoardAreaView
    {
        void Init(CardHolderFactory cardHolderFactory);
        ICardHolderView CreateCardHolderView();
    }
}