using System;
using System.Collections.Generic;
using Game;
using UnityEngine;

namespace Scripts
{
    // A known last fruit remains a real locked card. Only its view is hidden
    // while the slot freezes; thawing restores that same instance and knowledge.
    public sealed class IceBoardPresenter : IDisposable
    {
        private readonly IceBoardService _ice;
        private readonly IBoardAreaController _board;
        private readonly IInitialCardAreaController _cards;
        private readonly ICardItemInfoManager _info;
        private readonly HashSet<NormalCardItemView> _hidden=new HashSet<NormalCardItemView>();
        public IceBoardPresenter(IceBoardService ice,IBoardAreaController board,IInitialCardAreaController cards,
            ICardItemInfoManager info)
        {_ice=ice;_board=board;_cards=cards;_info=info;_ice.Changed+=Render;}
        public void Render()
        {
            _hidden.RemoveWhere(card=>card==null);
            var desired=new HashSet<NormalCardItemView>();
            var facts=_info.GetCardItemInfoList();int frozen=_ice.FrozenIndex;
            for(int index=0;facts!=null && index<facts.Count;index++)
            {
                if(!facts[index].isLocked || !facts[index].possibleCardHolderIndicatorIndexes.Contains(frozen)
                    || frozen<0 || !_cards.TryGetCardItem(index,out var card))continue;
                if(card.GetView() is NormalCardItemView view)desired.Add(view);
            }
            foreach(var card in _hidden)if(!desired.Contains(card))card.gameObject.SetActive(true);
            foreach(var card in desired)if(!_hidden.Contains(card))card.gameObject.SetActive(false);
            _hidden.Clear();foreach(var card in desired)_hidden.Add(card);
            _board.ShowFrozenSlot(frozen);
        }
        public void Dispose()
        {
            _ice.Changed-=Render;
            foreach(var card in _hidden)if(card!=null)card.gameObject.SetActive(true);
            _hidden.Clear();
        }
    }
}
