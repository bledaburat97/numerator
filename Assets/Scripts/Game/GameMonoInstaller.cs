using Game;
using UnityEngine;
using UnityEngine.Rendering;
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
        [SerializeField] private FadePanelView nonGlowFadePanelView;
        [SerializeField] private LifeBarView lifeBarView;
        [SerializeField] private GameClockView gameClockView;
        [SerializeField] private GameUIView gameUI;
        [SerializeField] private LevelFinishPopupView levelFinishPopup;
        [SerializeField] private PowerUpMessagePopupView powerUpMessagePopup;
        public override void InstallBindings()
        {
            Container.Bind<IGameClockController>().To<GameClockController>().AsSingle().WithArguments(gameClockView);
            Container.Bind<ITurnOrderDeterminer>().To<TurnOrderDeterminer>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ILevelDataCreator>().To<LevelDataCreator>().FromComponentInHierarchy().AsSingle();
            Container.Bind<IUserReady>().To<UserReady>().FromComponentInHierarchy().AsSingle();
            Container.Bind<IResultAreaController>().To<ResultAreaController>().AsSingle().WithArguments(resultAreaView);
            Container.Bind<IResultManager>().To<ResultManager>().AsSingle();
            Container.Bind<ILifeBarController>().To<LifeBarController>().AsSingle()
                .WithArguments(lifeBarView);
            Container.Bind<IGuessManager>().To<GuessManager>().AsSingle();
            Container.Bind<IBoardAreaController>().To<BoardAreaController>().AsSingle().WithArguments(boardAreaView);
            Container.Bind<ICardItemInfoManager>().To<CardItemInfoManager>().AsSingle();
            Container.Bind<ICardItemInfoPopupController>().To<CardItemInfoPopupController>().AsSingle()
                .WithArguments(cardItemInfoPopupView);
            Container.Bind<IInitialCardAreaController>().To<InitialCardAreaController>().AsSingle()
                .WithArguments(initialCardAreaView);
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
            Container.Bind<ILevelFinishController>().To<LevelFinishController>().AsSingle().WithArguments(levelFinishPopup);
            Container.Bind<ILevelSaveDataManager>().To<LevelSaveDataManager>().AsSingle();
            Container.Bind<IPowerUpMessageController>().To<PowerUpMessageController>().AsSingle()
                .WithArguments(powerUpMessagePopup);
            Container.Bind<IHintProvider>().To<HintProvider>().AsSingle();
            Container.Bind<IBoxMovementHandler>().To<BoxMovementHandler>().AsSingle();
            Container.Bind<ILevelEndPopupController>().To<LevelEndPopupController>().AsSingle().WithArguments(levelFinishPopup);;
            Container.Bind<ILevelSuccessManager>().To<LevelSuccessManager>().AsSingle();
            Container.Bind<ILevelSuccessAnimationManager>().To<LevelSuccessAnimationManager>().AsSingle();
            Container.Bind<ILevelStartManager>().To<LevelStartManager>().AsSingle();
            Container.Bind<ILevelStartAnimationManager>().To<LevelStartAnimationManager>().AsSingle();
            Container.Bind<ISizeManager>().To<SizeManager>().AsSingle();
            Container.Bind<IBoardCardIndexManager>().To<BoardCardIndexManager>().AsSingle();
        }
    }
}