using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class CardItemInfoPopupController : ICardItemInfoPopupController
    {
        [Inject] private BaseButtonControllerFactory _baseButtonControllerFactory;
        [Inject] private ICardInteractionManager _cardInteractionManager;
        [Inject] private ICardItemInfoManager _cardItemInfoManager;
        [Inject] private ILevelDataCreator _levelDataCreator;
        [Inject] private ICardHolderModelCreator _cardHolderModelCreator;
        [Inject] private ITutorialAbilityManager _tutorialAbilityManager;
        
        private ICardItemInfoPopupView _view;
        private List<IBaseButtonController> _cardHolderIndicatorButtonControllers;
        private Dictionary<int, IBaseButtonController> _probabilityButtonControllers;
        private LevelData _levelData;
        private int _activeCardIndex;
        public CardItemInfoPopupController(ICardItemInfoPopupView view)
        {
            _view = view;
        }
        public void Initialize()
        {
            BaseButtonViewFactory cardHolderIndicatorButtonViewFactory = new BaseButtonViewFactory();
            _levelData = _levelDataCreator.GetLevelData();
            _view.Init(cardHolderIndicatorButtonViewFactory);
            _cardHolderIndicatorButtonControllers = new List<IBaseButtonController>();
            _probabilityButtonControllers = new Dictionary<int, IBaseButtonController>();
            CreateCardHolderIndicatorButtons();
            CreateProbabilityButtons();
            _view.SetStatus(false);
            _cardInteractionManager.OpenCardItemInfoPopupEvent += OpenCardItemInfoPopup;
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
            foreach (CardHolderModel boardCardHolderModel in _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Board))
            {
                IBaseButtonView cardHolderIndicatorButtonView = _view.CreateCardHolderIndicatorButtonView();
                IBaseButtonController cardHolderIndicatorButtonController = _baseButtonControllerFactory.Create(cardHolderIndicatorButtonView);
                cardHolderIndicatorButtonController.Initialize(() => OnCardHolderIndicatorButtonClicked(boardCardHolderModel.index));
                cardHolderIndicatorButtonController.SetText(ConstantValues.HOLDER_ID_LIST[boardCardHolderModel.index]);
                cardHolderIndicatorButtonController.SetLocalPosition(new Vector2(boardCardHolderModel.localPosition.x, 0));
                _cardHolderIndicatorButtonControllers.Add(cardHolderIndicatorButtonController);
            }
        }

        private void OnCardHolderIndicatorButtonClicked(int cardHolderIndicatorIndex)
        {
            if (!_tutorialAbilityManager.IsHolderIndicatorButtonClickable(cardHolderIndicatorIndex)) return;
            _cardItemInfoManager.OnCardHolderIndicatorClicked(_activeCardIndex, cardHolderIndicatorIndex);
            SetCardItemInfoPopupStatus(true, _activeCardIndex);
        }
        
        private void CreateProbabilityButtons()
        {
            for (int i = 0; i < ConstantValues.NUM_OF_PROBABILITY_BUTTONS; i++)
            {
                int probabilityIndex = i;
                IBaseButtonView probabilityButtonView = _view.GetProbabilityButtonViewByIndex(i);
                IBaseButtonController probabilityButtonController =
                    _baseButtonControllerFactory.Create(probabilityButtonView);
                probabilityButtonController.Initialize(() => OnProbabilityButtonClicked(probabilityIndex));
                probabilityButtonController.SetColor(ConstantValues.GetProbabilityTypeToColorMapping()[probabilityIndex]);
                _probabilityButtonControllers.Add(probabilityIndex, probabilityButtonController);
            }
        }
        
        private void OnProbabilityButtonClicked(int probabilityIndex)
        {
            if (!_tutorialAbilityManager.IsProbabilityButtonClickable(probabilityIndex)) return;
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
    }
}

