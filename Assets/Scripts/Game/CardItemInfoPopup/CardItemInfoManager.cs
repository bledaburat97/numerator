using System;
using System.Collections.Generic;
using Game;
using Zenject;

namespace Scripts
{
    public class CardItemInfoManager : ICardItemInfoManager, ICardKnowledgeEvents
    {
        public event Action Changed;
        private List<CardItemInfo> _cardItemInfoList = new List<CardItemInfo>();
        private ILevelSaveDataManager _levelSaveDataManager;
        private IBoardHolderCountManager _boardHolderCountManager;

        [Inject]
        public CardItemInfoManager(ILevelSaveDataManager levelSaveDataManager, IBoardHolderCountManager boardHolderCountManager)
        {
            _levelSaveDataManager = levelSaveDataManager;
            _boardHolderCountManager = boardHolderCountManager;
        }
        
        public void Initialize()
        {
            _cardItemInfoList = _levelSaveDataManager.GetLevelSaveData().CardItemInfoList;
            ObserverNotification.Send(Changed);
        }

        public void RemoveLastCardHolderIndicator()
        {
            int boardCount = _boardHolderCountManager.GetBoardHolderCount();
            foreach (CardItemInfo cardItemInfo in _cardItemInfoList)
            {
                cardItemInfo.possibleCardHolderIndicatorIndexes.RemoveAll(index => index >= boardCount);
            }
            ObserverNotification.Send(Changed);
        }

        public List<CardItemInfo> GetCardItemInfoList()
        {
            return _cardItemInfoList;
        }
        
        public void ApplyPositionOptions(IReadOnlyList<CardItemInfo> planned)
        {
            for(int index=0;index<_cardItemInfoList.Count;index++)
                _cardItemInfoList[index].possibleCardHolderIndicatorIndexes=new List<int>(planned[index].possibleCardHolderIndicatorIndexes);
            ObserverNotification.Send(Changed);
        }

        public void MakeCardNotExisted(int cardIndex)
        {
            CardItemInfo cardItemInfo = _cardItemInfoList[cardIndex];
            cardItemInfo.isExisted = false;
            ObserverNotification.Send(Changed);
        }
        
        public void MakeCardCertain(int cardIndex, List<int> possibleCardHolderIndicatorIndexes)
        {
            SetCertain(_cardItemInfoList[cardIndex],possibleCardHolderIndicatorIndexes);
            ObserverNotification.Send(Changed);
        }

        public static void SetCertain(CardItemInfo card,List<int> positions)
        {card.isLocked=true;card.probabilityType=ProbabilityType.Certain;card.possibleCardHolderIndicatorIndexes=new List<int>(positions);}

        public void OnProbabilityButtonClicked(int activeCardIndex, ProbabilityType probabilityType)
        {
            if (IsCardLocked(activeCardIndex)) return;

            ChangeTheCardInfoByProbability(activeCardIndex, probabilityType);
            ObserverNotification.Send(Changed);
        }

        public void OnHolderIndicatorClicked(int activeCardIndex, int holderIndicatorIndex)
        {
            if (IsCardLocked(activeCardIndex)) return;

            ChangeTheCardInfoByHolderIndicator(activeCardIndex, holderIndicatorIndex);
            ObserverNotification.Send(Changed);
        }

        private void ChangeTheCardInfoByProbability(int cardIndex, ProbabilityType probabilityType)
        {
            CardItemInfo cardItemInfo = _cardItemInfoList[cardIndex];
            if (probabilityType == ProbabilityType.NotExisted)
            {
                cardItemInfo.probabilityType = ProbabilityType.NotExisted;
                cardItemInfo.possibleCardHolderIndicatorIndexes = new List<int>();

            }
            else if (probabilityType == ProbabilityType.Probable)
            {
                if (cardItemInfo.probabilityType != ProbabilityType.Probable)
                {
                    cardItemInfo.probabilityType = ProbabilityType.Probable;
                    cardItemInfo.possibleCardHolderIndicatorIndexes = GetAllPossibleCardHolderIndicatorIndexes();
                }
            }
            else if (probabilityType == ProbabilityType.Certain)
            {
                if (cardItemInfo.probabilityType == ProbabilityType.NotExisted)
                {
                    cardItemInfo.possibleCardHolderIndicatorIndexes = GetAllPossibleCardHolderIndicatorIndexes();
                }
                cardItemInfo.probabilityType = ProbabilityType.Certain;
            }
        }
        
        private void ChangeTheCardInfoByHolderIndicator(int cardIndex, int cardHolderIndicatorIndex)
        {
            CardItemInfo cardItemInfo = _cardItemInfoList[cardIndex];
            if (cardItemInfo.possibleCardHolderIndicatorIndexes.Contains(cardHolderIndicatorIndex))
            {
                cardItemInfo.possibleCardHolderIndicatorIndexes.Remove(cardHolderIndicatorIndex);
                if (cardItemInfo.possibleCardHolderIndicatorIndexes.Count == 0)
                {
                    if (cardItemInfo.probabilityType == ProbabilityType.Certain)
                    {
                        cardItemInfo.probabilityType = ProbabilityType.Probable; 
                        cardItemInfo.possibleCardHolderIndicatorIndexes = GetAllPossibleCardHolderIndicatorIndexes();
                    }
                    else if (cardItemInfo.probabilityType == ProbabilityType.Probable)
                    {
                        cardItemInfo.probabilityType = ProbabilityType.NotExisted;
                        cardItemInfo.possibleCardHolderIndicatorIndexes = new List<int>();
                    }
                }
            }
            else
            {
                cardItemInfo.possibleCardHolderIndicatorIndexes.Add(cardHolderIndicatorIndex);
            }
        }

        private List<int> GetAllPossibleCardHolderIndicatorIndexes()
        {
            List<int> possibleCardHolderIndexes = new List<int>();
            for (int i = 0; i < _boardHolderCountManager.GetBoardHolderCount(); i++)
            {
                possibleCardHolderIndexes.Add(i);
            }

            return possibleCardHolderIndexes;
        }

        private bool IsCardLocked(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= _cardItemInfoList.Count) return false;

            CardItemInfo cardItemInfo = _cardItemInfoList[cardIndex];
            return cardItemInfo != null && cardItemInfo.isLocked;
        }
    }

    public interface ICardKnowledgeEvents { event Action Changed; }

    public sealed class CardLockService
    {
        private readonly ICardItemInfoManager _info;
        private readonly IBoardPlacementQuery _query;
        private readonly IBoardPlacementCommands _commands;
        public CardLockService(ICardItemInfoManager info,IBoardPlacementQuery query,IBoardPlacementCommands commands)
        {_info=info;_query=query;_commands=commands;}
        public bool TryLock(int slot,int card)
        {
            var facts=_info.GetCardItemInfoList();
            if(slot<0 || slot>=_query.GetPlacedCardIndexesOnBoard().Count || card<0 || card>=facts.Count || !facts[card].isExisted)return false;
            if(facts[card].isLocked && (facts[card].possibleCardHolderIndicatorIndexes.Count!=1 || facts[card].possibleCardHolderIndicatorIndexes[0]!=slot))return false;
            if(_query.TryGetOccupiedCardIndexOnBoardHolder(slot,out var occupant) && occupant!=card)
            {
                if(facts[occupant].isLocked)return false;
                _commands.TryRemoveCardFromBoard(occupant);
            }
            if(!_commands.TryPlaceCardOnBoardHolder(slot,card))return false;
            _info.MakeCardCertain(card,new List<int>{slot});return true;
        }
    }

    public sealed class CardKnowledgePresenter : IDisposable
    {
        private readonly ICardItemInfoManager _info;
        private readonly IInitialCardAreaController _cards;
        private readonly ICardKnowledgeEvents _changes;
        private readonly IInitialCardAreaEvents _actors;
        public CardKnowledgePresenter(ICardItemInfoManager info,IInitialCardAreaController cards)
        {
            _info=info;_cards=cards;_changes=info as ICardKnowledgeEvents;_actors=cards as IInitialCardAreaEvents;
            if(_changes!=null)_changes.Changed+=Render;
            if(_actors!=null)_actors.Rebuilt+=Render;
        }
        public void Render()
        {
            var facts=_info.GetCardItemInfoList();
            for(int index=0;index<facts.Count;index++)
            {
                if(!_cards.TryGetCardItem(index,out _))continue;
                var fact=facts[index];
                _cards.SetProbabilityOfCardItem(index,fact.probabilityType,fact.isLocked);
                _cards.SetHolderIndicatorListOfCardHolder(index,fact.possibleCardHolderIndicatorIndexes);
            }
        }
        public void Dispose()
        {
            if(_changes!=null)_changes.Changed-=Render;
            if(_actors!=null)_actors.Rebuilt-=Render;
        }
    }

    public interface ICardItemInfoManager
    {
        void Initialize();
        List<CardItemInfo> GetCardItemInfoList();
        void MakeCardCertain(int cardIndex, List<int> possibleCardHolderIndicatorIndexes);
        void MakeCardNotExisted(int cardIndex);
        void OnProbabilityButtonClicked(int activeCardIndex, ProbabilityType probabilityType);
        void OnHolderIndicatorClicked(int activeCardIndex, int holderIndicatorIndex);
        void RemoveLastCardHolderIndicator();
        void ApplyPositionOptions(IReadOnlyList<CardItemInfo> planned);
    }


    public class CardItemInfo
    {
        public List<int> possibleCardHolderIndicatorIndexes;
        public ProbabilityType probabilityType;
        public bool isLocked;
        public bool isExisted;
    }
}
