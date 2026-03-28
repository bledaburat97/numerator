using UnityEngine;
using Zenject;

namespace Scripts
{
    public class TutorialBootstrapper : ITutorialBootstrapper
    {
        private readonly ILevelTracker _levelTracker;
        private readonly IInitialCardAreaController _initialCardAreaController;
        private readonly ICardItemLocator _cardItemLocator;
        private readonly IUnmaskServiceAreaView _unmaskServiceAreaView;
        private readonly IGameUIController _gameUIController;
        private readonly IResultAreaController _resultAreaController;
        private readonly ICardItemInfoPopupController _cardItemInfoPopupController;
        private readonly ICardInteractionManager _cardInteractionManager;
        private readonly IBoardAreaController _boardAreaController;
        private readonly IBoardLayoutManager _boardLayoutManager;
        private readonly ICardPlacementCoordinator _cardPlacementCoordinator;

        [Inject]
        public TutorialBootstrapper(
            ILevelTracker levelTracker,
            IInitialCardAreaController initialCardAreaController,
            ICardItemLocator cardItemLocator,
            IUnmaskServiceAreaView unmaskServiceAreaView,
            IGameUIController gameUIController,
            IResultAreaController resultAreaController,
            ICardItemInfoPopupController cardItemInfoPopupController,
            ICardInteractionManager cardInteractionManager,
            IBoardAreaController boardAreaController,
            IBoardLayoutManager boardLayoutManager,
            ICardPlacementCoordinator cardPlacementCoordinator)
        {
            _levelTracker = levelTracker;
            _initialCardAreaController = initialCardAreaController;
            _cardItemLocator = cardItemLocator;
            _unmaskServiceAreaView = unmaskServiceAreaView;
            _gameUIController = gameUIController;
            _resultAreaController = resultAreaController;
            _cardItemInfoPopupController = cardItemInfoPopupController;
            _cardInteractionManager = cardInteractionManager;
            _boardAreaController = boardAreaController;
            _boardLayoutManager = boardLayoutManager;
            _cardPlacementCoordinator = cardPlacementCoordinator;
        }

        public void Bootstrap(
            Transform parent,
            RectTransform safeAreaRectTransform,
            RectTransform canvasRectTransform,
            HandTutorialView handTutorialPrefab,
            TutorialMessagePopupView tutorialMessagePopupPrefab)
        {
            if (_levelTracker.IsFirstLevelTutorial())
            {
                IHandTutorialView handTutorialView =
                    CreateHandTutorialView(parent, safeAreaRectTransform, canvasRectTransform, handTutorialPrefab);
                ITutorialMessagePopupView tutorialMessagePopupView =
                    new TutorialMessagePopupViewFactory().Spawn(parent, tutorialMessagePopupPrefab);

                ITutorialController firstLevelTutorialController = new FirstLevelTutorialController();
                firstLevelTutorialController.Initialize(
                    _initialCardAreaController,
                    _cardItemLocator,
                    handTutorialView,
                    _unmaskServiceAreaView,
                    tutorialMessagePopupView,
                    _gameUIController,
                    _resultAreaController,
                    _cardItemInfoPopupController,
                    _cardInteractionManager,
                    _boardAreaController,
                    _boardLayoutManager,
                    _cardPlacementCoordinator);
            }
            else if (_levelTracker.IsCardInfoTutorial())
            {
                IHandTutorialView handTutorialView =
                    CreateHandTutorialView(parent, safeAreaRectTransform, canvasRectTransform, handTutorialPrefab);
                ITutorialMessagePopupView tutorialMessagePopupView =
                    new TutorialMessagePopupViewFactory().Spawn(parent, tutorialMessagePopupPrefab);

                ITutorialController cardInfoTutorialController = new CardInfoTutorialController();
                cardInfoTutorialController.Initialize(
                    _initialCardAreaController,
                    _cardItemLocator,
                    handTutorialView,
                    _unmaskServiceAreaView,
                    tutorialMessagePopupView,
                    _gameUIController,
                    _resultAreaController,
                    _cardItemInfoPopupController,
                    _cardInteractionManager,
                    _boardAreaController,
                    _boardLayoutManager,
                    _cardPlacementCoordinator);
            }
        }

        private IHandTutorialView CreateHandTutorialView(
            Transform parent,
            RectTransform safeAreaRectTransform,
            RectTransform canvasRectTransform,
            HandTutorialView handTutorialPrefab)
        {
            IHandTutorialView handTutorialView = new HandTutorialViewFactory().Spawn(parent, handTutorialPrefab);
            handTutorialView.Init(safeAreaRectTransform.anchorMax.y, canvasRectTransform.rect.height);
            _unmaskServiceAreaView.Init(safeAreaRectTransform.anchorMax.y, canvasRectTransform.rect.height);
            return handTutorialView;
        }
    }

    public interface ITutorialBootstrapper
    {
        void Bootstrap(
            Transform parent,
            RectTransform safeAreaRectTransform,
            RectTransform canvasRectTransform,
            HandTutorialView handTutorialPrefab,
            TutorialMessagePopupView tutorialMessagePopupPrefab);
    }
}
