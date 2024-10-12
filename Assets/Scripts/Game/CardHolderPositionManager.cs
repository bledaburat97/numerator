using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class CardHolderPositionManager : ICardHolderPositionManager
    {
        private List<Vector2> _boardHolderPositionList;
        private List<Vector2> _initialHolderPositionList;
        private List<Vector2> _holderIndicatorPositionList;
        private ILevelDataCreator _levelDataCreator;
        private int _numOfBoardHolders;
        
        [Inject]
        public CardHolderPositionManager(ILevelDataCreator levelDataCreator)
        {
            _boardHolderPositionList = new List<Vector2>();
            _initialHolderPositionList = new List<Vector2>();
            _holderIndicatorPositionList = new List<Vector2>();
            _levelDataCreator = levelDataCreator;
        }
        
        public void Initialize(int numOfBoardHolders)
        {
            _numOfBoardHolders = numOfBoardHolders;
            SetBoardHolderPositionList();
            SetInitialHolderPositionList();
            SetHolderIndicatorPositionList();
        }
        
        private void SetBoardHolderPositionList()
        {
            _boardHolderPositionList.Clear();
            float spacing = ConstantValues.SPACING_BETWEEN_BOARD_CARDS;
            Vector2 cardHolderSize = new Vector2(ConstantValues.BOARD_CARD_HOLDER_WIDTH, ConstantValues.BOARD_CARD_HOLDER_HEIGHT);
            float verticalLocalPos = 0f;
            _boardHolderPositionList = _boardHolderPositionList.GetLocalPositionList(_numOfBoardHolders, spacing, cardHolderSize, verticalLocalPos);
        }

        private void SetInitialHolderPositionList()
        {
            _initialHolderPositionList.Clear();
            int numOfInitialHolders = _levelDataCreator.GetLevelData().NumOfCards;
            List<Vector2> localPositionsOfSecondLine = new List<Vector2>();
            float spacing = ConstantValues.SPACING_BETWEEN_INITIAL_CARDS;
            Vector2 cardHolderSize = new Vector2(ConstantValues.INITIAL_CARD_HOLDER_WIDTH, ConstantValues.INITIAL_CARD_HOLDER_HEIGHT);

            float firstLineYPos = cardHolderSize.y / 2 + ConstantValues.POSSIBLE_HOLDER_INDICATOR_HEIGHT + 6f;
            float secondLineYPos = -cardHolderSize.y / 2 - 1f;
            
            _initialHolderPositionList = _initialHolderPositionList.GetLocalPositionList(numOfInitialHolders / 2, spacing, cardHolderSize, firstLineYPos);
            localPositionsOfSecondLine = localPositionsOfSecondLine.GetLocalPositionList(numOfInitialHolders - numOfInitialHolders / 2, spacing, cardHolderSize, secondLineYPos);
            
            _initialHolderPositionList.AddRange(localPositionsOfSecondLine);
        }

        private void SetHolderIndicatorPositionList()
        {
            _holderIndicatorPositionList.Clear();
            float spacing = 1f;
            Vector2 holderIndicatorSize = new Vector2(ConstantValues.POSSIBLE_HOLDER_INDICATOR_WIDTH,
                ConstantValues.POSSIBLE_HOLDER_INDICATOR_HEIGHT);
            float verticalLocalPos = 0f;
            _holderIndicatorPositionList = _holderIndicatorPositionList.GetLocalPositionList(
                _numOfBoardHolders, spacing, holderIndicatorSize, verticalLocalPos);
        }

        public List<Vector2> GetHolderPositionList(CardHolderType cardHolderType)
        {
            if (cardHolderType == CardHolderType.Board) return _boardHolderPositionList;
            return _initialHolderPositionList;
        }

        public List<Vector2> GetHolderIndicatorPositionList()
        {
            return _holderIndicatorPositionList;
        }
    }

    public interface ICardHolderPositionManager
    {
        void Initialize(int numOfBoardHolders);
        List<Vector2> GetHolderPositionList(CardHolderType cardHolderType);
        List<Vector2> GetHolderIndicatorPositionList();
    }

    public enum CardHolderType
    {
        Board,
        Initial
    }
    
}