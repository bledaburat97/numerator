using UnityEngine;
using Zenject;

namespace Scripts
{
    public class LobbyMonoInstaller : MonoInstaller
    {
        [SerializeField] private PlayButtonView createGameButton;
        [SerializeField] private PlayButtonView joinGameButton;

        public override void InstallBindings()
        {
            Container.Bind<ICreateGameButtonController>().To<CreateGameButtonController>().AsSingle()
                .WithArguments(createGameButton);
            Container.Bind<IJoinGameButtonController>().To<JoinGameButtonController>().AsSingle()
                .WithArguments(joinGameButton);
            Container.Bind<ILobbyPopupCreator>().To<LobbyPopupCreator>().FromComponentInHierarchy().AsSingle();
        }
    }
}