using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class InitialCardHolderController : IInitialCardHolderController
    {
        private List<IPossibleHolderIndicatorView> _holderIndicatorList = new List<IPossibleHolderIndicatorView>();
        private List<int> _activeHolderIndicatorIndexes = new List<int>();
        private ICardHolderPositionManager _cardHolderPositionManager;
        private int _index;
        private IInitialHolderView _view;
        public InitialCardHolderController(IInitialHolderView initialHolderView, ICardHolderPositionManager cardHolderPositionManager)
        {
            _view = initialHolderView;
            _cardHolderPositionManager = cardHolderPositionManager;
        }

        public void Initialize(int index, CardItemInfo cardItemInfo)
        {
            _index = index;
            _view.SetLocalScale();
            _view.SetLocalPosition(_cardHolderPositionManager.GetHolderPositionList(CardHolderType.Initial)[_index]);
            _view.SetSize(new Vector2(ConstantValues.INITIAL_CARD_HOLDER_WIDTH, ConstantValues.INITIAL_CARD_HOLDER_HEIGHT));
            _view.SetText(_index + 1);
            
            CreatePossibleHolderIndicators();
            SetHolderIndicatorList(cardItemInfo.possibleCardHolderIndicatorIndexes);
        }

        public void RemoveFirstHolderIndicator()
        {
            IPossibleHolderIndicatorView holderIndicator = _holderIndicatorList[0];
            _holderIndicatorList.Remove(holderIndicator);
            holderIndicator.DestroyObject();
            if (_holderIndicatorList.Count != _cardHolderPositionManager.GetHolderIndicatorPositionList().Count)
            {
                Debug.LogError("Holder indicator count mismatch");
                return;
            }
            for (int i = 0; i < _cardHolderPositionManager.GetHolderIndicatorPositionList().Count; i++)
            {
                _holderIndicatorList[i].SetLocalPosition(_cardHolderPositionManager.GetHolderIndicatorPositionList()[i]);
                _holderIndicatorList[i].SetText(ConstantValues.HOLDER_ID_LIST[i]);
            }
        }
        
        private void CreatePossibleHolderIndicators()
        {
            for (int i = 0; i < _cardHolderPositionManager.GetHolderIndicatorPositionList().Count; i++)
            {
                IPossibleHolderIndicatorView possibleHolderIndicatorView = _view.CreatePossibleHolderIndicatorView();
                possibleHolderIndicatorView.SetLocalScale();
                possibleHolderIndicatorView.SetLocalPosition(_cardHolderPositionManager.GetHolderIndicatorPositionList()[i]);
                possibleHolderIndicatorView.SetText(ConstantValues.HOLDER_ID_LIST[i]);
                _holderIndicatorList.Add(possibleHolderIndicatorView);
            }
        }
    
        public void SetHolderIndicatorList(List<int> activeHolderIndicatorIndexList)
        {
            _activeHolderIndicatorIndexes = activeHolderIndicatorIndexList;
            for (int i = 0; i < _holderIndicatorList.Count; i++)
            {
                _holderIndicatorList[i].SetStatus(activeHolderIndicatorIndexList.Contains(i));
            }
        }
    
        public void SetLocalPosition(Vector2 localXPos)
        {
            _view.SetLocalPosition(localXPos);
        }
    
        public List<int> GetActiveHolderIndicatorIndexes()
        {
            return _activeHolderIndicatorIndexes;
        }
        
        public void DestroyObject()
        {
            _view.DestroyObject();
            _view = null;
        }
        
        public IInitialHolderView GetView()
        {
            return _view;
        }
        
        public Vector3 GetPositionOfCardHolder()
        {
            return _view.GetGlobalPosition();
        }
    }

    public interface IInitialCardHolderController
    {
        void Initialize(int index, CardItemInfo cardItemInfo);
        void SetLocalPosition(Vector2 localXPos);
        List<int> GetActiveHolderIndicatorIndexes();
        void RemoveFirstHolderIndicator();
        IInitialHolderView GetView();
        Vector3 GetPositionOfCardHolder();
        void DestroyObject();
        void SetHolderIndicatorList(List<int> activeHolderIndicatorIndexList);
    }
    
}