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
            Container.BindInterfacesAndSelfTo<Menu.MainNavigationController>().AsSingle()
                .WithArguments(Menu.MainNavigationView.Ensure(lobbyUIView.GetComponentInParent<Canvas>())).NonLazy();
            Container.Bind<ILobbyPopupCreator>().To<LobbyPopupCreator>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ILobbyUIController>().To<LobbyUIController>().AsSingle()
                .WithArguments(lobbyUIView);
        }
    }
}