using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class InitialCardHolderController : IInitialCardHolderController
    {
        private List<IPossibleHolderIndicatorView> _holderIndicatorList = new List<IPossibleHolderIndicatorView>();
        private List<int> _activeHolderIndicatorIndexes = new List<int>();
        private int _index;
        private IInitialHolderView _view;
        public InitialCardHolderController(IInitialHolderView initialHolderView)
        {
            _view = initialHolderView;
        }

        public void Initialize(int index, CardItemInfo cardItemInfo, Vector2 localPosition, Vector2 size, 
            List<Vector2> holderIndicatorLocalPositions, Vector2 holderIndicatorSize)
        {
            _index = index;
            _view.SetLocalScale();
            _view.SetLocalPosition(localPosition);
            _view.SetSize(size);
            _view.SetText(_index + 1);
            
            CreatePossibleHolderIndicators(holderIndicatorLocalPositions, holderIndicatorSize);
            SetHolderIndicatorList(cardItemInfo.possibleCardHolderIndicatorIndexes);
        }

        public void RemoveFirstHolderIndicator(List<Vector2> holderIndicatorNewLocalPositions)
        {
            IPossibleHolderIndicatorView holderIndicator = _holderIndicatorList[0];
            _holderIndicatorList.Remove(holderIndicator);
            holderIndicator.DestroyObject();
            if (_holderIndicatorList.Count != holderIndicatorNewLocalPositions.Count)
            {
                Debug.LogError("Holder indicator count mismatch");
                return;
            }
            for (int i = 0; i < holderIndicatorNewLocalPositions.Count; i++)
            {
                _holderIndicatorList[i].SetLocalPosition(holderIndicatorNewLocalPositions[i]);
                _holderIndicatorList[i].SetText(ConstantValues.HOLDER_ID_LIST[i]);
            }
        }
        
        private void CreatePossibleHolderIndicators(List<Vector2> holderIndicatorLocalPositions, Vector2 size)
        {
            for (int i = 0; i < holderIndicatorLocalPositions.Count; i++)
            {
                IPossibleHolderIndicatorView possibleHolderIndicatorView = _view.CreatePossibleHolderIndicatorView();
                possibleHolderIndicatorView.SetLocalScale();
                possibleHolderIndicatorView.SetSize(size);
                possibleHolderIndicatorView.SetLocalPosition(holderIndicatorLocalPositions[i]);
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
        void Initialize(int index, CardItemInfo cardItemInfo, Vector2 localPosition, Vector2 size, 
            List<Vector2> holderIndicatorLocalPositions, Vector2 holderIndicatorSize);
        void SetLocalPosition(Vector2 localXPos);
        List<int> GetActiveHolderIndicatorIndexes();
        IInitialHolderView GetView();
        Vector3 GetPositionOfCardHolder();
        void DestroyObject();
        void SetHolderIndicatorList(List<int> activeHolderIndicatorIndexList);
        void RemoveFirstHolderIndicator(List<Vector2> holderIndicatorNewLocalPositions);
    }
    
}