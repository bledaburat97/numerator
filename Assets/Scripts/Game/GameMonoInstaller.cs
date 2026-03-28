using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class GameMonoInstaller : MonoInstaller
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private BoardAreaView boardAreaView;
        [SerializeField] private CardItemInfoPopupView cardItemInfoPopupView;
        [SerializeField] private ResultAreaView resultAreaView;
        [SerializeField] private InitialCardAreaView initialCardAreaView;
        [SerializeField] private FadePanelView fadePanelView;
        [SerializeField] private LifeBarView lifeBarView;
        [SerializeField] private GameClockView gameClockView;
        [SerializeField] private GameUIView gameUI;
        [SerializeField] private LevelFinishPopupView levelFinishPopup;
        public override void InstallBindings()
        {
            Container.Bind<IBoardAreaView>().FromInstance(boardAreaView).AsSingle();
            Container.Bind<IInitialCardAreaView>().FromInstance(initialCardAreaView).AsSingle();
            Container.Bind<ISizeManager>().To<SizeManager>().AsSingle();
            Container.Bind<IGameClockController>().To<GameClockController>().AsSingle().WithArguments(gameClockView);
            Container.Bind<ITurnOrderDeterminer>().To<TurnOrderDeterminer>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ILevelDataCreator>().To<LevelDataCreator>().FromComponentInHierarchy().AsSingle();
            Container.Bind<IUserReady>().To<UserReady>().FromComponentInHierarchy().AsSingle();
            Container.Bind<IResultAreaController>().To<ResultAreaController>().AsSingle().WithArguments(resultAreaView);
            Container.Bind<IResultManager>().To<ResultManager>().AsSingle();
            Container.Bind<ILifeBarController>().To<LifeBarController>().AsSingle()
                .WithArguments(lifeBarView);
            Container.Bind<IGuessManager>().To<GuessManager>().AsSingle();
            Container.Bind<IBoardLayoutManager>().To<BoardLayoutManager>().AsSingle();
            Container.Bind<IBoardStateManager>().To<BoardStateManager>().AsSingle();
            Container.Bind<BoardAreaController>().AsSingle();
            Container.Bind<IBoardAreaController>().To<BoardAreaController>().FromResolve();
            Container.Bind<ICardItemInfoManager>().To<CardItemInfoManager>().AsSingle();
            Container.Bind<ICardItemInfoPopupController>().To<CardItemInfoPopupController>().AsSingle()
                .WithArguments(cardItemInfoPopupView);
            Container.Bind<IInitialCardAreaLayoutManager>().To<InitialCardAreaLayoutManager>().AsSingle();
            Container.Bind<IInitialCardAreaFactory>().To<InitialCardAreaFactory>().AsSingle();
            Container.Bind<IInitialCardAreaController>().To<InitialCardAreaController>().AsSingle();
            Container.Bind<ICardInteractionManager>().To<CardInteractionManager>().AsSingle();
            Container.Bind<IFadePanelController>().To<FadePanelController>().AsSingle().WithArguments(fadePanelView);
            Container.Bind<IGamePopupCreator>().To<GamePopupCreator>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ICardItemLocator>().To<CardItemLocator>().AsSingle();
            Container.Bind<ITargetNumberCreator>().To<TargetNumberCreator>().FromComponentInHierarchy().AsSingle();
            Container.Bind<IGameUIController>().To<GameUIController>().AsSingle()
                .WithArguments(gameUI);
            Container.Bind<IUnmaskServiceAreaView>().To<UnmaskServiceAreaView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<IMultiplayerGameController>().To<MultiplayerGameController>().FromComponentInHierarchy()
                .AsSingle();
            Container.Bind<ILevelSaveDataManager>().To<LevelSaveDataManager>().AsSingle();
            Container.Bind<IRoundStateManager>().To<RoundStateManager>().AsSingle();
            Container.Bind<IGameSaveSnapshotProvider>().To<GameSaveSnapshotProvider>().AsSingle();
            Container.Bind<IPowerUpMessageController>().To<PowerUpMessageController>().AsSingle();
            Container.Bind<IHintProvider>().To<HintProvider>().AsSingle();
            Container.Bind<ICardPlacementCoordinator>().To<CardPlacementCoordinator>().AsSingle();
            Container.Bind<ITutorialBootstrapper>().To<TutorialBootstrapper>().AsSingle();
            Container.Bind<ILevelEndPopupController>().To<LevelEndPopupController>().AsSingle().WithArguments(levelFinishPopup);;
            Container.Bind<ILevelSuccessManager>().To<LevelSuccessManager>().AsSingle();
            Container.Bind<ILevelSuccessAnimationManager>().To<LevelSuccessAnimationManager>().AsSingle();
            Container.Bind<ILevelStartManager>().To<LevelStartManager>().AsSingle();
            Container.Bind<ILevelStartAnimationManager>().To<LevelStartAnimationManager>().AsSingle();
            Container.Bind<ILevelFailManager>().To<LevelFailManager>().AsSingle();
            Container.Bind<ILevelFailAnimationManager>().To<LevelFailAnimationManager>().AsSingle();
            Container.Bind<ILevelEndManager>().To<LevelEndManager>().AsSingle();
            Container.Bind<IBoardCardIndexManager>().To<BoardCardIndexManager>().AsSingle();
        }
    }
}
