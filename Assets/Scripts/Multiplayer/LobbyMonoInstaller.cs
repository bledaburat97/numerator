using UnityEngine;
using Zenject;

namespace Scripts
{
    public class LobbyMonoInstaller : MonoInstaller
    {
        [SerializeField] private LobbyUIView lobbyUIView;
        public override void InstallBindings()
        {
            Container.Bind<ILevelTracker>().To<LevelTracker>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ILobbyPopupCreator>().To<LobbyPopupCreator>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ILobbyUIController>().To<LobbyUIController>().AsSingle()
                .WithArguments(lobbyUIView);
        }
    }
}