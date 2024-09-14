using System;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class LobbySceneMonoInstaller : MonoInstaller
    {
        [SerializeField] private LobbyUIView lobbyUIView;
        public override void InstallBindings()
        {
            Container.BindFactory<IBaseButtonView, Action, IBaseButtonController, BaseButtonControllerFactory>().To<BaseButtonController>();
            Container.Bind<ILevelTracker>().To<LevelTracker>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ILobbyPopupCreator>().To<LobbyPopupCreator>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ILobbyUIController>().To<LobbyUIController>().AsSingle()
                .WithArguments(lobbyUIView);
            Container.Bind<IHapticController>().To<HapticController>().AsSingle();
        }
    }
}