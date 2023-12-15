using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class InitialCardHolderController : BaseCardHolderController, IInitialCardHolderController
    {
        private List<IPossibleHolderIndicatorController> _possibleHolderIndicatorControllerList = new List<IPossibleHolderIndicatorController>();
        private List<int> _activeHolderIndicatorIndexes = new List<int>();

        public void Initialize(ICardHolderView cardHolderView, CardHolderModel model, Camera cam, ICardItemInfoManager cardItemInfoManager)
        {
            _view = cardHolderView;
            _model = model;
            _view.Init(model, new PossibleHolderIndicatorViewFactory(), cam);
            CreatePossibleHolderIndicators();
            CardItemInfo cardItemInfo = cardItemInfoManager.GetCardItemInfoList()[model.index];
            SetHolderIndicatorListStatus(cardItemInfo.possibleCardHolderIndicatorIndexes);
            cardItemInfoManager.HolderIndicatorListChanged += OnHolderIndicatorListChanged;
        }
        
        private void OnHolderIndicatorListChanged(object sender, HolderIndicatorListChangedEventArgs args)
        {
            if (_model.index == args.cardIndex)
            {
                SetHolderIndicatorListStatus(args.holderIndicatorIndexList);
            }
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
    
        private void SetHolderIndicatorListStatus(List<int> activeHolderIndicatorIndexList)
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
    
        public void SetLocalPosition(Vector2 localXPos)
        {
            _view.SetLocalPosition(localXPos);
        }
    
        public List<int> GetActiveHolderIndicatorIndexes()
        {
            return _activeHolderIndicatorIndexes;
        }

        public void SetText(string cardNumber)
        {
            _view.SetText(cardNumber);
        }
    }

    public interface IInitialCardHolderController : IBaseCardHolderController
    {
        void Initialize(ICardHolderView cardHolderView, CardHolderModel model, Camera cam,
            ICardItemInfoManager cardItemInfoManager);
        void SetLocalPosition(Vector2 localXPos);
        List<int> GetActiveHolderIndicatorIndexes();
        void SetText(string cardNumber);
    }
    
}