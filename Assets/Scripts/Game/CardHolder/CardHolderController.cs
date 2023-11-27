using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class CardHolderController : ICardHolderController
    {
        private bool _isAvailable;
        private ICardHolderView _view;
        private int _index;
        private CardHolderModel _model;
        private List<IPossibleHolderIndicatorController> _possibleHolderIndicatorControllerList = new List<IPossibleHolderIndicatorController>();
        private List<int> _activeHolderIndicatorIndexes = new List<int>();
        public void Initialize(ICardHolderView cardHolderView, CardHolderModel model)
        {
            _view = cardHolderView;
            _model = model;
            _index = model.index;
            _view.Init(model, new PossibleHolderIndicatorViewFactory());
            CreatePossibleHolderIndicators();
        }
        
        private void CreatePossibleHolderIndicators()
        {
            if(_model.possibleHolderIndicatorLocalPositionList == null) return;
            PossibleHolderIndicatorControllerFactory holderIndicatorControllerFactory = new PossibleHolderIndicatorControllerFactory();
            for (int i = 0; i < _model.possibleHolderIndicatorLocalPositionList.Count; i++)
            {
                IPossibleHolderIndicatorController holderIndicatorController = holderIndicatorControllerFactory.Spawn();
                IPossibleHolderIndicatorView possibleHolderIndicatorView = _view.CreatePossibleHolderIndicatorView();
                
                holderIndicatorController.Initialize(possibleHolderIndicatorView, new PossibleHolderIndicatorModel()
                {
                    text = ConstantValues.HOLDER_ID_LIST[i],
                    localPosition = _model.possibleHolderIndicatorLocalPositionList[i]
                });
                _possibleHolderIndicatorControllerList.Add(holderIndicatorController);
            }
        }

        public int GetIndex()
        {
            return _index;
        }

        public ICardHolderView GetView()
        {
            return _view;
        }

        public void SetHolderIndicatorListStatus(List<int> activeHolderIndicatorIndexList)
        {
            _activeHolderIndicatorIndexes = activeHolderIndicatorIndexList;
            for (int i = 0; i < _possibleHolderIndicatorControllerList.Count; i++)
            {
                bool status;
                if (activeHolderIndicatorIndexList.Contains(i)) status = true;
                else status = false;
                _possibleHolderIndicatorControllerList[i].SetStatus(status);
            }
        }

        public void SetHighlightStatus(bool status)
        {
            _view.SetHighlightStatus(status);
        }

        public void SetLocalPosition(Vector2 localXPos)
        {
            _view.SetLocalPosition(localXPos);
        }

        public List<int> GetActiveHolderIndicatorIndexes()
        {
            return _activeHolderIndicatorIndexes;
        }
    }
    
    public interface ICardHolderController
    {
        void Initialize(ICardHolderView cardHolderView, CardHolderModel model);
        ICardHolderView GetView();
        int GetIndex();
        void SetHolderIndicatorListStatus(List<int> activeHolderIndicatorIndexList);
        void SetHighlightStatus(bool status);
        void SetLocalPosition(Vector2 localXPos);
        List<int> GetActiveHolderIndicatorIndexes();
    }
    
    public class CardHolderModel
    {
        public int index;
        public Vector3 localPosition;
        public Vector2 size;
        public List<Vector2> possibleHolderIndicatorLocalPositionList;
        public CardItemType cardItemType;
    }
}