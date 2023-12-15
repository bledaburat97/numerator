using System.Collections.Generic;

namespace Scripts
{
    public class CardItemInfoPopupController : ICardItemInfoPopupController
    {
        private ICardItemInfoPopupView _view;
        private ICardItemInfoManager _cardItemInfoManager;
        private List<ICardHolderIndicatorButtonController> _cardHolderIndicatorButtonControllers;
        private Dictionary<ProbabilityType, IProbabilityButtonController> _probabilityButtonControllers;
        private LevelData _levelData;
        private List<ProbabilityType> _probabilityTypes = new List<ProbabilityType>()
            { ProbabilityType.Certain, ProbabilityType.Probable, ProbabilityType.NotExisted }; //TODO set somewhere else.
        private ICardHolderModelCreator _cardHolderModelCreator;
        private int _activeCardIndex;
        public void Initialize(ICardItemInfoPopupView view, ICardItemInfoManager cardItemInfoManager, ILevelTracker levelTracker, ICardHolderModelCreator cardHolderModelCreator)
        {
            _view = view;
            _cardItemInfoManager = cardItemInfoManager;
            CardHolderIndicatorButtonViewFactory cardHolderIndicatorButtonViewFactory = new CardHolderIndicatorButtonViewFactory();
            _levelData = levelTracker.GetLevelInfo().levelData;
            _cardHolderModelCreator = cardHolderModelCreator;
            _view.Init(cardHolderIndicatorButtonViewFactory);
            _cardHolderIndicatorButtonControllers = new List<ICardHolderIndicatorButtonController>();
            _probabilityButtonControllers = new Dictionary<ProbabilityType, IProbabilityButtonController>(); 
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
                _probabilityButtonControllers[cardItemInfo.probabilityType].SetFrameStatus(true);
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

            foreach (KeyValuePair<ProbabilityType, IProbabilityButtonController> probabilityToController in _probabilityButtonControllers)
            {
                probabilityToController.Value.SetFrameStatus(false);
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
            ProbabilityButtonControllerFactory probabilityButtonControllerFactory = new ProbabilityButtonControllerFactory();
            for (int i = 0; i < _probabilityTypes.Count; i++)
            {
                ProbabilityType probabilityType = _probabilityTypes[i];
                ProbabilityButtonModel probabilityButtonModel = new ProbabilityButtonModel()
                {
                    probabilityType = probabilityType,
                    onClickAction = () => OnProbabilityButtonClicked(probabilityType)
                };
                IProbabilityButtonController probabilityButtonController = probabilityButtonControllerFactory.Spawn();
                IProbabilityButtonView probabilityButtonView = _view.GetProbabilityButtonViewByIndex(i);
                probabilityButtonController.Initialize(probabilityButtonView, probabilityButtonModel);
                _probabilityButtonControllers.Add(probabilityButtonModel.probabilityType, probabilityButtonController);
            }
        }
        
        private void OnProbabilityButtonClicked(ProbabilityType probabilityType)
        {
            _cardItemInfoManager.OnProbabilityButtonClicked(_activeCardIndex, probabilityType);
            SetCardItemInfoPopupStatus(true, _activeCardIndex);
        }
    }
    
    public interface ICardItemInfoPopupController
    {
        void Initialize(ICardItemInfoPopupView view, ICardItemInfoManager cardItemInfoManager, ILevelTracker levelTracker, ICardHolderModelCreator cardHolderModelCreator);
        void SetCardItemInfoPopupStatus(bool status, int cardIndex);
    }
    
    public class CardHolderIndicatorButtonModel : BaseButtonModel
    {
        public float localXPosition;
    }
}

