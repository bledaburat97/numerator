using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class CardItemInfoPopupController : ICardItemInfoPopupController
    {
        private BaseButtonControllerFactory _baseButtonControllerFactory;
        private ICardInteractionManager _cardInteractionManager;
        private ICardItemInfoManager _cardItemInfoManager;
        private ICardHolderPositionManager _cardHolderPositionManager;
        private ICardItemInfoPopupView _view;
        private List<IBaseButtonController> _cardHolderIndicatorButtonControllers;
        private Dictionary<int, IBaseButtonController> _probabilityButtonControllers;
        private int _activeCardIndex;
        
        public event EventHandler<ProbabilityButtonClickedEventArgs> ProbabilityButtonClickedEvent;
        public event EventHandler<HolderIndicatorClickedEventArgs> HolderIndicatorClickedEvent;
        
        [Inject]
        public CardItemInfoPopupController(BaseButtonControllerFactory baseButtonControllerFactory, ICardInteractionManager cardInteractionManager, ICardItemInfoManager cardItemInfoManager, 
            ICardHolderPositionManager cardHolderPositionManager, ICardItemInfoPopupView view)
        {
            _view = view;
            BaseButtonViewFactory cardHolderIndicatorButtonViewFactory = new BaseButtonViewFactory();
            _view.Init(cardHolderIndicatorButtonViewFactory);
            _cardHolderIndicatorButtonControllers = new List<IBaseButtonController>();
            _probabilityButtonControllers = new Dictionary<int, IBaseButtonController>();
            _baseButtonControllerFactory = baseButtonControllerFactory;
            _cardInteractionManager = cardInteractionManager;
            _cardInteractionManager.OpenCardItemInfoPopupEvent += OpenCardItemInfoPopup;
            _cardItemInfoManager = cardItemInfoManager;
            _cardHolderPositionManager = cardHolderPositionManager;
            CreateProbabilityButtons();
        }

        public void Initialize()
        {
            ClearCardHolderIndicatorButtons();
            CreateCardHolderIndicatorButtons();
            _view.SetStatus(false);
        }

        private void ClearCardHolderIndicatorButtons()
        {
            foreach (IBaseButtonController button in _cardHolderIndicatorButtonControllers)
            {
                button.DestroyObject();
            }
            _cardHolderIndicatorButtonControllers.Clear();
        }

        private void SetCardItemInfoPopupStatus(bool status, int cardIndex)
        {
            if (status)
            {
                _view.SetStatus(true);
                _activeCardIndex = cardIndex;
                ResetCardItemInfoPopup();
                CardItemInfo cardItemInfo = _cardItemInfoManager.GetCardItemInfoList()[cardIndex];
                if (_cardHolderIndicatorButtonControllers.Count < cardItemInfo.possibleCardHolderIndicatorIndexes.Count)
                {
                    Debug.LogError("CardItemInfo Error");
                    Debug.LogError("Card Holder Indicator Button Count: " + _cardHolderIndicatorButtonControllers.Count);
                    return;
                }
                for (int i = 0; i < cardItemInfo.possibleCardHolderIndicatorIndexes.Count; i++)
                {
                    _cardHolderIndicatorButtonControllers[cardItemInfo.possibleCardHolderIndicatorIndexes[i]].SetImageStatus(false);
                    _cardHolderIndicatorButtonControllers[cardItemInfo.possibleCardHolderIndicatorIndexes[i]].SetTextStatus(true);
                }
                _probabilityButtonControllers[(int)cardItemInfo.probabilityType].SetImageStatus(true);
            }
            else
            {
                _view.SetStatus(false);
                _activeCardIndex = -1;
            }
        }

        private void ResetCardItemInfoPopup()
        {
            for (int i = 0; i < _cardHolderIndicatorButtonControllers.Count; i++)
            {
                _cardHolderIndicatorButtonControllers[i].SetImageStatus(true);
                _cardHolderIndicatorButtonControllers[i].SetTextStatus(false);
            }

            foreach (KeyValuePair<int, IBaseButtonController> probabilityToController in _probabilityButtonControllers)
            {
                probabilityToController.Value.SetImageStatus(false);
            }
        }
        
        private void CreateCardHolderIndicatorButtons()
        {
            for (int i = 0; i < _cardHolderPositionManager.GetHolderPositionList(CardHolderType.Board).Count; i++)
            {
                IBaseButtonView cardHolderIndicatorButtonView = _view.CreateCardHolderIndicatorButtonView();
                int index = i;
                IBaseButtonController cardHolderIndicatorButtonController = _baseButtonControllerFactory.Create(cardHolderIndicatorButtonView, () => OnCardHolderIndicatorButtonClicked(index));
                cardHolderIndicatorButtonController.SetText(ConstantValues.HOLDER_ID_LIST[index]);
                cardHolderIndicatorButtonController.SetLocalPosition(new Vector2(_cardHolderPositionManager.GetHolderPositionList(CardHolderType.Board)[index].x, 0));
                _cardHolderIndicatorButtonControllers.Add(cardHolderIndicatorButtonController);
            }
        }

        private void OnCardHolderIndicatorButtonClicked(int cardHolderIndicatorIndex)
        {
            _cardItemInfoManager.OnHolderIndicatorClicked(_activeCardIndex, cardHolderIndicatorIndex);
            HolderIndicatorClickedEvent?.Invoke(this, new HolderIndicatorClickedEventArgs(_activeCardIndex, cardHolderIndicatorIndex));
            SetCardItemInfoPopupStatus(true, _activeCardIndex);
        }
        
        private void CreateProbabilityButtons()
        {
            for (int i = 0; i < ConstantValues.NUM_OF_PROBABILITY_BUTTONS; i++)
            {
                int probabilityIndex = i;
                IBaseButtonView probabilityButtonView = _view.GetProbabilityButtonViewByIndex(i);
                IBaseButtonController probabilityButtonController =
                    _baseButtonControllerFactory.Create(probabilityButtonView, () => OnProbabilityButtonClicked(probabilityIndex));
                probabilityButtonController.SetColor(ConstantValues.GetProbabilityTypeToColorMapping()[probabilityIndex]);
                _probabilityButtonControllers.Add(probabilityIndex, probabilityButtonController);
            }
        }
        
        private void OnProbabilityButtonClicked(int probabilityIndex)
        {
            _cardItemInfoManager.OnProbabilityButtonClicked(_activeCardIndex, (ProbabilityType)probabilityIndex);
            ProbabilityButtonClickedEvent?.Invoke(this, new ProbabilityButtonClickedEventArgs(_activeCardIndex, (ProbabilityType)probabilityIndex));
            SetCardItemInfoPopupStatus(true, _activeCardIndex);
        }
        
        public IBaseButtonController GetProbabilityButton(ProbabilityType probabilityType)
        {
            return _probabilityButtonControllers[(int)probabilityType];
        }

        public IBaseButtonController GetHolderIndicatorButton(int index)
        {
            return _cardHolderIndicatorButtonControllers[index];
        }

        private void OpenCardItemInfoPopup(object sender, (bool, int) args)
        {
            SetCardItemInfoPopupStatus(args.Item1, args.Item2);
        }

        public void Unsubscribe()
        {
            _cardInteractionManager.OpenCardItemInfoPopupEvent -= OpenCardItemInfoPopup;
        }
    }
    
    public interface ICardItemInfoPopupController
    {
        void Initialize();
        IBaseButtonController GetProbabilityButton(ProbabilityType probabilityType);
        IBaseButtonController GetHolderIndicatorButton(int index);
        void Unsubscribe();
        event EventHandler<ProbabilityButtonClickedEventArgs> ProbabilityButtonClickedEvent;
        event EventHandler<HolderIndicatorClickedEventArgs> HolderIndicatorClickedEvent;
    }

    public class ProbabilityButtonClickedEventArgs : EventArgs
    {
        public int ActiveCardIndex { get; private set; }
        public ProbabilityType ProbabilityType { get; private set; }

        public ProbabilityButtonClickedEventArgs(int activeCardIndex, ProbabilityType probabilityType)
        {
            ActiveCardIndex = activeCardIndex;
            ProbabilityType = probabilityType;
        }
    }

    public class HolderIndicatorClickedEventArgs : EventArgs
    {
        public int ActiveCardIndex { get; private set; }
        public int HolderIndicatorIndex { get; private set; }

        public HolderIndicatorClickedEventArgs(int activeCardIndex, int holderIndicatorIndex)
        {
            ActiveCardIndex = activeCardIndex;
            HolderIndicatorIndex = holderIndicatorIndex;
        }
    }
}

