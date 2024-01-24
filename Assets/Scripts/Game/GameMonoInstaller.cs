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
        [SerializeField] private FadePanelView nonGlowFadePanelView;
        [SerializeField] private BaseButtonView settingsButtonView;
        [SerializeField] private BaseButtonView checkButtonView;
        [SerializeField] private BaseButtonView resetButtonView;
        [SerializeField] private StarProgressBarView starProgressBarView;
        [SerializeField] private GameClockView gameClockView;
        public override void InstallBindings()
        {
            Container.Bind<IGameClockController>().To<GameClockController>().AsSingle().WithArguments(gameClockView);
            Container.Bind<ITurnOrderDeterminer>().To<TurnOrderDeterminer>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ILevelDataCreator>().To<LevelDataCreator>().FromComponentInHierarchy().AsSingle();
            Container.Bind<IGameSaveService>().To<GameSaveService>().AsSingle();
            Container.Bind<ILevelTracker>().To<LevelTracker>().FromComponentInHierarchy().AsSingle();
            Container.Bind<IUserReady>().To<UserReady>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ICardHolderModelCreator>().To<CardHolderModelCreator>().AsSingle();
            Container.Bind<IResultAreaController>().To<ResultAreaController>().AsSingle().WithArguments(resultAreaView);
            Container.Bind<IResultManager>().To<ResultManager>().AsSingle();
            Container.Bind<IGameUIView>().To<GameUIView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ISettingsButtonController>().To<SettingsButtonController>().AsSingle()
                .WithArguments(settingsButtonView);
            Container.Bind<ICheckButtonController>().To<CheckButtonController>().AsSingle()
                .WithArguments(checkButtonView);
            Container.Bind<IResetButtonController>().To<ResetButtonController>().AsSingle()
                .WithArguments(resetButtonView);
            Container.Bind<IStarProgressBarController>().To<StarProgressBarController>().AsSingle()
                .WithArguments(starProgressBarView);
            Container.Bind<ILevelManager>().To<LevelManager>().AsSingle();
            Container.Bind<IBoardAreaController>().To<BoardAreaController>().AsSingle().WithArguments(boardAreaView);
            Container.Bind<ICardItemInfoManager>().To<CardItemInfoManager>().AsSingle();
            Container.Bind<ICardItemInfoPopupController>().To<CardItemInfoPopupController>().AsSingle()
                .WithArguments(cardItemInfoPopupView);
            Container.Bind<IInitialCardAreaController>().To<InitialCardAreaController>().AsSingle()
                .WithArguments(initialCardAreaView);
            Container.Bind<IFadePanelController>().To<FadePanelController>().AsSingle().WithArguments(fadePanelView, nonGlowFadePanelView);
            Container.Bind<IGamePopupCreator>().To<GamePopupCreator>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ICardItemLocator>().To<CardItemLocator>().AsSingle().WithArguments(canvas);
            Container.Bind<ITargetNumberCreator>().To<TargetNumberCreator>().FromComponentInHierarchy().AsSingle();
        }
    }
}