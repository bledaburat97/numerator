using System.Collections.Generic;

namespace Scripts
{
    public class CardItemInfoPopupController : ICardItemInfoPopupController
    {
        private ICardItemInfoPopupView _view;
        private ICardItemInfoManager _cardItemInfoManager;
        private List<ICardHolderIndicatorButtonController> _cardHolderIndicatorButtonControllers;
        private Dictionary<int, IOptionButtonController> _probabilityButtonControllers;
        private LevelData _levelData;
        private ICardHolderModelCreator _cardHolderModelCreator;
        private int _activeCardIndex;

        public CardItemInfoPopupController(ICardItemInfoPopupView view)
        {
            _view = view;
        }
        public void Initialize(ICardItemInfoManager cardItemInfoManager, ILevelDataCreator levelDataCreator, ICardHolderModelCreator cardHolderModelCreator)
        {
            _cardItemInfoManager = cardItemInfoManager;
            CardHolderIndicatorButtonViewFactory cardHolderIndicatorButtonViewFactory = new CardHolderIndicatorButtonViewFactory();
            _levelData = levelDataCreator.GetLevelData();
            _cardHolderModelCreator = cardHolderModelCreator;
            _view.Init(cardHolderIndicatorButtonViewFactory);
            _cardHolderIndicatorButtonControllers = new List<ICardHolderIndicatorButtonController>();
            _probabilityButtonControllers = new Dictionary<int, IOptionButtonController>(); 
            CreateCardHolderIndicatorButtons();
            CreateProbabilityButtons();
            _view.SetStatus(false);
        }

        public void SetCardItemInfoPopupStatus(bool status, int cardIndex)
        {
            if (status)
            {
                _view.SetStatus(true);
                _activeCardIndex = cardIndex;
                ResetCardItemInfoPopup();
                CardItemInfo cardItemInfo = _cardItemInfoManager.GetCardItemInfoList()[cardIndex];
                for (int i = 0; i < cardItemInfo.possibleCardHolderIndicatorIndexes.Count; i++)
                {
                    _cardHolderIndicatorButtonControllers[cardItemInfo.possibleCardHolderIndicatorIndexes[i]].SetStatus(true);
                }
                _probabilityButtonControllers[(int)cardItemInfo.probabilityType].SetPointStatus(true);
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
                _cardHolderIndicatorButtonControllers[i].SetStatus(false);
            }

            foreach (KeyValuePair<int, IOptionButtonController> probabilityToController in _probabilityButtonControllers)
            {
                probabilityToController.Value.SetPointStatus(false);
            }
        }
        
        private void CreateCardHolderIndicatorButtons()
        {
            CardHolderIndicatorButtonControllerFactory cardHolderIndicatorButtonControllerFactory = new CardHolderIndicatorButtonControllerFactory();
            foreach (CardHolderModel boardCardHolderModel in _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Board))
            {
                CardHolderIndicatorButtonModel cardHolderIndicatorButtonModel = new CardHolderIndicatorButtonModel()
                {
                    text = ConstantValues.HOLDER_ID_LIST[boardCardHolderModel.index],
                    localXPosition = boardCardHolderModel.localPosition.x, 
                    OnClick = () => OnCardHolderIndicatorButtonClicked(boardCardHolderModel.index)
                };
                ICardHolderIndicatorButtonController cardHolderIndicatorButtonController = cardHolderIndicatorButtonControllerFactory.Spawn();
                ICardHolderIndicatorButtonView cardHolderIndicatorButtonView = _view.CreateCardHolderIndicatorButtonView();
                cardHolderIndicatorButtonController.Initialize(cardHolderIndicatorButtonView, cardHolderIndicatorButtonModel);
                _cardHolderIndicatorButtonControllers.Add(cardHolderIndicatorButtonController);
            }
        }

        private void OnCardHolderIndicatorButtonClicked(int cardHolderIndicatorIndex)
        {
            _cardItemInfoManager.OnCardHolderIndicatorClicked(_activeCardIndex, cardHolderIndicatorIndex);
            SetCardItemInfoPopupStatus(true, _activeCardIndex);
        }
        
        private void CreateProbabilityButtons()
        {
            OptionButtonControllerFactory probabilityButtonControllerFactory = new OptionButtonControllerFactory();
            for (int i = 0; i < ConstantValues.NUM_OF_PROBABILITY_BUTTONS; i++)
            {
                int probabilityIndex = i;
                OptionButtonModel probabilityButtonModel = new OptionButtonModel()
                {
                    optionIndex = probabilityIndex,
                    onClickAction = () => OnProbabilityButtonClicked(probabilityIndex)
                };
                IOptionButtonController probabilityButtonController = probabilityButtonControllerFactory.Spawn();
                IOptionButtonView probabilityButtonView = _view.GetProbabilityButtonViewByIndex(i);
                probabilityButtonController.Initialize(probabilityButtonView, probabilityButtonModel);
                probabilityButtonController.SetColor(ConstantValues.GetProbabilityTypeToColorMapping()[probabilityButtonModel.optionIndex]);
                _probabilityButtonControllers.Add(probabilityButtonModel.optionIndex, probabilityButtonController);
            }
        }
        
        private void OnProbabilityButtonClicked(int probabilityIndex)
        {
            _cardItemInfoManager.OnProbabilityButtonClicked(_activeCardIndex, (ProbabilityType)probabilityIndex);
            SetCardItemInfoPopupStatus(true, _activeCardIndex);
        }
    }
    
    public interface ICardItemInfoPopupController
    {
        void Initialize(ICardItemInfoManager cardItemInfoManager, ILevelDataCreator levelDataCreator, ICardHolderModelCreator cardHolderModelCreator);
        void SetCardItemInfoPopupStatus(bool status, int cardIndex);
    }
    
    public class CardHolderIndicatorButtonModel : BaseButtonModel
    {
        public float localXPosition;
    }
}

