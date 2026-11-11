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
        [SerializeField] private PowerUpListHolderView powerUpAreaView;
        [SerializeField] private LevelFinishPopupView levelFinishPopup;
        public override void InstallBindings()
        {
            Container.Bind<TutorialInputGate>().AsSingle();
            Container.Bind<LevelEnvironmentView>().FromInstance(LevelEnvironmentView.Ensure(canvas)).AsSingle();
            Container.BindInterfacesAndSelfTo<PowerUpRewardPresenter>().AsSingle()
                .WithArguments(PowerUpRewardView.Ensure(canvas));
            Container.Bind<IBoardAreaView>().FromInstance(boardAreaView).AsSingle();
            Container.Bind<IInitialCardAreaView>().FromInstance(initialCardAreaView).AsSingle();
            Container.Bind<ISizeManager>().To<SizeManager>().AsSingle();
            Container.Bind<IGameClockController>().To<GameClockController>().AsSingle().WithArguments(gameClockView);
            Container.Bind<ITurnOrderDeterminer>().To<TurnOrderDeterminer>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ILevelDataCreator>().To<LevelDataCreator>().FromComponentInHierarchy().AsSingle();
            Container.Bind<IUserReady>().To<UserReady>().FromComponentInHierarchy().AsSingle();
            Container.BindInterfacesAndSelfTo<ResultAreaController>().AsSingle().WithArguments(resultAreaView);
            Container.Bind<RoundHistory>().AsSingle();
            Container.Bind<IResultHistorySource>().To<RoundHistory>().FromResolve();
            Container.Bind<IPositionAssistanceView>().To<PositionAssistanceView>().AsSingle().WithArguments(gameUI, resultAreaView);
            Container.Bind<PositionAssistanceController>().AsSingle();
            Container.Bind<Economy.UltraActivationService>().FromMethod(context=>new Economy.UltraActivationService(context.Container.Resolve<Economy.IPlayerProfileRepository>())).AsSingle();
            Container.Bind<UltraPowerUpController>().AsSingle();
            BindPowerUpEffects(Container);
            Container.Bind<PowerUpUseService>().FromMethod(context=>new PowerUpUseService(
                context.Container.Resolve<ILevelTracker>(),context.Container.Resolve<ILevelDataCreator>(),context.Container.Resolve<ILevelSaveDataManager>(),
                context.Container.Resolve<IRoundStateManager>(),context.Container.Resolve<ICardItemInfoManager>(),context.Container.Resolve<IBoardHolderCountManager>(),
                context.Container.Resolve<ITargetNumberCreator>(),context.Container.Resolve<Economy.PowerUpUseJournal>(),context.Container.Resolve<IGameSaveService>(),
                ()=>context.Container.Resolve<IGameSaveSnapshotProvider>(),context.Container.Resolve<IPowerUpCatalog>(),
                ()=>context.Container.Resolve<IceBoardService>())).AsSingle();
            Container.Bind<IPositionNotesSettings>().To<PositionAssistanceController>().FromResolve();
            Container.Bind<PairEffect>().AsSingle();
            Container.Bind<BoardSplitService>().AsSingle();
            Container.Bind<GrowingBoardService>().AsSingle();
            Container.Bind<DynamiteService>().AsSingle();
            Container.BindInterfacesAndSelfTo<GrowingBoardPresenter>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<DynamitePresenter>().AsSingle().NonLazy();
            Container.Bind<IceBoardService>().AsSingle();
            Container.BindInterfacesAndSelfTo<IceBoardPresenter>().AsSingle();
            Container.Bind<HelperEffect>().AsSingle();
            Container.Bind<EliminateFruitEffect>().AsSingle();
            Container.Bind<LevelMechanicsController>().AsSingle();
            Container.Bind<IResultManager>().To<ResultManager>().AsSingle();
            Container.Bind<ILifeBarController>().To<LifeBarController>().AsSingle()
                .WithArguments(lifeBarView);
            Container.Bind<IGuessManager>().To<GuessManager>().AsSingle();
            Container.Bind<IBoardLayoutManager>().To<BoardLayoutManager>().AsSingle();
            Container.Bind<IBoardHolderCountManager>().To<BoardHolderCountManager>().AsSingle();
            Container.Bind<BoardAreaController>().AsSingle();
            Container.Bind<IBoardAreaController>().To<BoardAreaController>().FromResolve();
            Container.Bind<IBoardHolderClickRouter>().To<BoardHolderClickRouter>().AsSingle().NonLazy();
            Container.Bind<ICardItemInfoManager>().To<CardItemInfoManager>().AsSingle();
            Container.Bind<CardLockService>().AsSingle();
            Container.BindInterfacesAndSelfTo<CardKnowledgePresenter>().AsSingle().NonLazy();
            Container.Bind<ICardItemInfoPopupController>().To<CardItemInfoPopupController>().AsSingle()
                .WithArguments(cardItemInfoPopupView);
            Container.Bind<IInitialCardAreaLayoutManager>().To<InitialCardAreaLayoutManager>().AsSingle();
            Container.Bind<IInitialCardAreaFactory>().To<InitialCardAreaFactory>().AsSingle();
            Container.Bind<InitialCardAreaController>().AsSingle();
            Container.Bind<IInitialCardAreaController>().To<InitialCardAreaController>().FromResolve();
            Container.Bind<IInitialCardAreaPresentation>().To<InitialCardAreaController>().FromResolve();
            Container.Bind<ICardInteractionManager>().To<CardInteractionManager>().AsSingle();
            Container.Bind<IFadePanelController>().To<FadePanelController>().AsSingle().WithArguments(fadePanelView);
            Container.Bind<IGamePopupCreator>().To<GamePopupCreator>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ICardItemLocator>().To<CardItemLocator>().AsSingle();
            Container.Bind<ITargetNumberCreator>().To<TargetNumberCreator>().FromComponentInHierarchy().AsSingle();
            Container.Bind<GameUIController>().AsSingle()
                .WithArguments(gameUI);
            Container.Bind<IGameUIController>().To<GameUIController>().FromResolve();
            Container.Bind<IMultiplayerHudPresentation>().To<GameUIController>().FromResolve();
            Container.Bind<IGamePowerUpAreaController>().To<GamePowerUpAreaController>().AsSingle()
                .WithArguments(ResolvePowerUpAreaView());
            Container.Bind<IVerticalCrystalProgressController>().To<VerticalCrystalProgressController>().AsSingle()
                .WithArguments(ResolveVerticalCrystalProgressView());
            Container.Bind<IUnmaskServiceAreaView>().To<UnmaskServiceAreaView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<IMultiplayerGameController>().To<MultiplayerGameController>().FromComponentInHierarchy()
                .AsSingle();
            Container.Bind<ILevelSaveDataManager>().To<LevelSaveDataManager>().AsSingle();
            Container.Bind<IRoundStateManager>().To<RoundStateManager>().AsSingle();
            Container.Bind<IGameSaveSnapshotProvider>().To<GameSaveSnapshotProvider>().AsSingle();
            Container.Bind<IPowerUpMessageController>().To<PowerUpMessageController>().AsSingle();
            Container.Bind<IRevealingPowerUpController>().To<RevealingPowerUpController>().AsSingle().NonLazy();
            Container.Bind<IBombPowerUpController>().To<BombPowerUpController>().AsSingle().NonLazy();
            Container.Bind<IHintProvider>().To<HintProvider>().AsSingle().NonLazy();
            Container.Bind<ICardPlacementCoordinator>().To<CardPlacementCoordinator>().AsSingle();
            Container.Bind<ITutorialBootstrapper>().To<TutorialBootstrapper>().AsSingle();
            Container.Bind<LevelEndPopupController>().AsSingle().WithArguments(levelFinishPopup);
            Container.Bind<ILevelEndPopupController>().To<LevelEndPopupController>().FromResolve();
            Container.Bind<IMultiplayerResultPresentation>().To<LevelEndPopupController>().FromResolve();
            Container.Bind<ILevelSuccessBoardAnimationController>().To<LevelSuccessBoardAnimationController>().AsSingle();
            Container.Bind<ILevelSuccessCleanupAnimationController>().To<LevelSuccessCleanupAnimationController>().AsSingle();
            Container.Bind<ILevelSuccessRewardCollectionAnimationController>()
                .To<LevelSuccessRewardCollectionAnimationController>().AsSingle();
            Container.Bind<ILevelSuccessAnimationManager>().To<LevelSuccessAnimationManager>().AsSingle();
            Container.Bind<ILevelStartAnimationManager>().To<LevelStartAnimationManager>().AsSingle();
            Container.Bind<ILevelFailAnimationManager>().To<LevelFailAnimationManager>().AsSingle();
            Container.Bind<RoundBuilder>().AsSingle();
            Container.Bind<LevelPresentation>().AsSingle();
            Container.Bind<MultiplayerRoundPresenter>().AsSingle();
            Container.Bind<SinglePlayerRoundCompletion>().AsSingle();
            Container.Bind<SinglePlayerEndPresenter>().AsSingle();
            Container.Bind<SinglePlayerSession>().AsSingle();
            Container.Bind<ISinglePlayerProgression>().To<SinglePlayerSession>().FromResolve();
            Container.Bind<MultiplayerSession>().AsSingle();
            Container.Bind<ILevelSession>().FromMethod(context =>
                context.Container.Resolve<ILevelTracker>().GetGameOption() == GameOption.MultiPlayer
                    ? (ILevelSession)context.Container.Resolve<MultiplayerSession>()
                    : context.Container.Resolve<SinglePlayerSession>()).AsSingle();
            Container.Bind<BoardCardIndexManager>().AsSingle();
            Container.Bind<IBoardPlacementQuery>().To<BoardCardIndexManager>().FromResolve();
            Container.Bind<IBoardPlacementCommands>().To<BoardCardIndexManager>().FromResolve();
        }

        public static void BindPowerUpEffects(DiContainer container)
        {
            container.Bind<IPowerUpEffect>().FromMethod(_=>new LifePowerUpEffect(()=>container.Resolve<IGuessManager>())).AsCached();
            container.Bind<IPowerUpEffect>().FromMethod(_=>new BombPowerUpEffect(()=>container.Resolve<IBombPowerUpController>())).AsCached();
            container.Bind<IPowerUpEffect>().FromMethod(_=>new RevealPowerUpEffect(()=>container.Resolve<ICardPlacementCoordinator>())).AsCached();
            container.Bind<IPowerUpEffect>().FromMethod(_=>new InspectPowerUpEffect(()=>container.Resolve<HelperEffect>())).AsCached();
            container.Bind<IPowerUpEffect>().FromMethod(_=>new PairPowerUpEffect(()=>container.Resolve<PairEffect>())).AsCached();
            container.Bind<IPowerUpEffect>().FromMethod(_=>new SplitPowerUpEffect(()=>container.Resolve<BoardSplitService>())).AsCached();
            container.Bind<IPowerUpEffect>().FromMethod(_=>new UltraPowerUpEffect(()=>container.Resolve<UltraPowerUpController>())).AsCached();
            container.Bind<IPowerUpCatalog>().To<PowerUpCatalog>().AsSingle();
        }

        private PowerUpListHolderView ResolvePowerUpAreaView()
        {
            if (powerUpAreaView != null) return powerUpAreaView;

            return FindObjectOfType<PowerUpListHolderView>(true);
        }

        private IVerticalCrystalProgressView ResolveVerticalCrystalProgressView()
        {
            if (levelFinishPopup != null && levelFinishPopup.GetVerticalCrystalProgressView() != null)
            {
                return levelFinishPopup.GetVerticalCrystalProgressView();
            }

            return FindObjectOfType<VerticalCrystalProgressView>(true);
        }
    }
}
