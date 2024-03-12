using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class CardItemInfoPopupController : ICardItemInfoPopupController
    {
        [Inject] private BaseButtonControllerFactory _baseButtonControllerFactory;

        private ICardItemInfoPopupView _view;
        private ICardItemInfoManager _cardItemInfoManager;
        private List<IBaseButtonController> _cardHolderIndicatorButtonControllers;
        private Dictionary<int, IBaseButtonController> _probabilityButtonControllers;
        private LevelData _levelData;
        private ICardHolderModelCreator _cardHolderModelCreator;
        private int _activeCardIndex;
        private List<int> _pressableProbabilityButtonIndexList;
        private List<int> _pressableHolderIndicatorButtonIndexList;
        public CardItemInfoPopupController(ICardItemInfoPopupView view)
        {
            _view = view;
        }
        public void Initialize(ICardItemInfoManager cardItemInfoManager, ILevelDataCreator levelDataCreator, ICardHolderModelCreator cardHolderModelCreator, IInitialCardAreaController initialCardAreaController)
        {
            _cardItemInfoManager = cardItemInfoManager;
            BaseButtonViewFactory cardHolderIndicatorButtonViewFactory = new BaseButtonViewFactory();
            _levelData = levelDataCreator.GetLevelData();
            _cardHolderModelCreator = cardHolderModelCreator;
            _view.Init(cardHolderIndicatorButtonViewFactory);
            _cardHolderIndicatorButtonControllers = new List<IBaseButtonController>();
            _probabilityButtonControllers = new Dictionary<int, IBaseButtonController>();
            _pressableProbabilityButtonIndexList = new List<int>();
            _pressableHolderIndicatorButtonIndexList = new List<int>();
            CreateCardHolderIndicatorButtons();
            CreateProbabilityButtons();
            _view.SetStatus(false);
            initialCardAreaController.OpenCardItemInfoPopup += (sender, tuple) => SetCardItemInfoPopupStatus(tuple.Item1, tuple.Item2);
        }

        private void SetCardItemInfoPopupStatus(bool status, int cardIndex)
        {
            if (status)
            {
                _view.SetStatus(true);
                _activeCardIndex = cardIndex;
                ResetCardItemInfoPopup();
                CardItemInfo cardItemInfo = _cardItemInfoManager.GetCardItemInfoList()[cardIndex];
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
            int numOfBoardCards = _levelData.NumOfBoardHolders;
            for (int i = 0; i < numOfBoardCards; i++)
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
            _pressableHolderIndicatorButtonIndexList.Clear();
            foreach (CardHolderModel boardCardHolderModel in _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Board))
            {
                IBaseButtonView cardHolderIndicatorButtonView = _view.CreateCardHolderIndicatorButtonView();
                IBaseButtonController cardHolderIndicatorButtonController = _baseButtonControllerFactory.Create(cardHolderIndicatorButtonView);
                cardHolderIndicatorButtonController.Initialize(() => OnCardHolderIndicatorButtonClicked(boardCardHolderModel.index));
                cardHolderIndicatorButtonController.SetText(ConstantValues.HOLDER_ID_LIST[boardCardHolderModel.index]);
                cardHolderIndicatorButtonController.SetLocalPosition(new Vector2(boardCardHolderModel.localPosition.x, 0));
                _cardHolderIndicatorButtonControllers.Add(cardHolderIndicatorButtonController);
                _pressableHolderIndicatorButtonIndexList.Add(boardCardHolderModel.index);
            }
        }

        private void OnCardHolderIndicatorButtonClicked(int cardHolderIndicatorIndex)
        {
            if (!_pressableHolderIndicatorButtonIndexList.Contains(cardHolderIndicatorIndex)) return;
            _cardItemInfoManager.OnCardHolderIndicatorClicked(_activeCardIndex, cardHolderIndicatorIndex);
            SetCardItemInfoPopupStatus(true, _activeCardIndex);
        }
        
        private void CreateProbabilityButtons()
        {
            _pressableProbabilityButtonIndexList.Clear();
            for (int i = 0; i < ConstantValues.NUM_OF_PROBABILITY_BUTTONS; i++)
            {
                int probabilityIndex = i;
                IBaseButtonView probabilityButtonView = _view.GetProbabilityButtonViewByIndex(i);
                IBaseButtonController probabilityButtonController =
                    _baseButtonControllerFactory.Create(probabilityButtonView);
                probabilityButtonController.Initialize(() => OnProbabilityButtonClicked(probabilityIndex));
                probabilityButtonController.SetColor(ConstantValues.GetProbabilityTypeToColorMapping()[probabilityIndex]);
                _probabilityButtonControllers.Add(probabilityIndex, probabilityButtonController);
                _pressableProbabilityButtonIndexList.Add(probabilityIndex);
            }
        }
        
        private void OnProbabilityButtonClicked(int probabilityIndex)
        {
            if (!_pressableProbabilityButtonIndexList.Contains(probabilityIndex)) return;
            _cardItemInfoManager.OnProbabilityButtonClicked(_activeCardIndex, (ProbabilityType)probabilityIndex);
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
        
        public void SetPressableProbabilityButtonIndex(int index)
        {
            _pressableProbabilityButtonIndexList.Clear();
            _pressableProbabilityButtonIndexList.Add(index);
        }
        
        public void SetPressableHolderIndicatorButtonIndex(int index)
        {
            _pressableHolderIndicatorButtonIndexList.Clear();
            _pressableHolderIndicatorButtonIndexList.Add(index);
        }
    }
    
    public interface ICardItemInfoPopupController
    {
        void Initialize(ICardItemInfoManager cardItemInfoManager, ILevelDataCreator levelDataCreator, ICardHolderModelCreator cardHolderModelCreator, IInitialCardAreaController initialCardAreaController);
        IBaseButtonController GetProbabilityButton(ProbabilityType probabilityType);
        IBaseButtonController GetHolderIndicatorButton(int index);
        void SetPressableProbabilityButtonIndex(int index);
        void SetPressableHolderIndicatorButtonIndex(int index);
    }
}

