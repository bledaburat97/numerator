using System;
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
        [SerializeField] private FadePanelView nonGlowFadePanelView;
        [SerializeField] private LifeBarView lifeBarView;
        [SerializeField] private GameClockView gameClockView;
        [SerializeField] private GameUIView gameUI;
        [SerializeField] private LevelFinishPopupView levelFinishPopup;
        public override void InstallBindings()
        {
            //Container.BindFactory<IFadeButtonView, IFadeButtonController, FadeButtonControllerFactory>().To<FadeButtonController>();
            Container.BindFactory<IBaseButtonView, Action, IBaseButtonController, BaseButtonControllerFactory>().To<BaseButtonController>();
            Container.Bind<IGameClockController>().To<GameClockController>().AsSingle().WithArguments(gameClockView);
            Container.Bind<ITurnOrderDeterminer>().To<TurnOrderDeterminer>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ILevelDataCreator>().To<LevelDataCreator>().FromComponentInHierarchy().AsSingle();
            Container.Bind<IGameSaveService>().To<GameSaveService>().AsSingle();
            Container.Bind<IHapticController>().To<HapticController>().AsSingle();
            Container.Bind<ILevelTracker>().To<LevelTracker>().FromComponentInHierarchy().AsSingle();
            Container.Bind<IUserReady>().To<UserReady>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ICardHolderModelCreator>().To<CardHolderModelCreator>().AsSingle();
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
            Container.Bind<ITutorialAbilityManager>().To<TutorialAbilityManager>().AsSingle();
            Container.Bind<IFadePanelController>().To<FadePanelController>().AsSingle().WithArguments(fadePanelView);
            Container.Bind<IGamePopupCreator>().To<GamePopupCreator>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ICardItemLocator>().To<CardItemLocator>().AsSingle();
            Container.Bind<ITargetNumberCreator>().To<TargetNumberCreator>().FromComponentInHierarchy().AsSingle();
            Container.Bind<IGameUIController>().To<GameUIController>().AsSingle()
                .WithArguments(gameUI);
            Container.Bind<IUnmaskServiceAreaView>().To<UnmaskServiceAreaView>().FromComponentInHierarchy().AsSingle();
            Container.Bind<IMultiplayerGameController>().To<MultiplayerGameController>().FromComponentInHierarchy()
                .AsSingle();
            Container.Bind<IGameInitializer>().To<GameInitializer>().AsSingle();
            Container.Bind<ILevelFinishController>().To<LevelFinishController>().AsSingle().WithArguments(levelFinishPopup);
            Container.Bind<IHintProvider>().To<HintProvider>().AsSingle();
        }
    }
}