using UnityEngine;
using Zenject;

namespace Scripts
{
    public class HomeMonoInstaller : MonoInstaller
    {
        [SerializeField] private PlayButtonView singlePlayerButton;
        [SerializeField] private PlayButtonView multiPlayerButton;

        public override void InstallBindings()
        {
            Container.Bind<IGameOptionTracker>().To<GameOptionTracker>().FromComponentInHierarchy().AsSingle();
            Container.Bind<ISinglePlayerButtonController>().To<SinglePlayerButtonController>().AsSingle()
                .WithArguments(singlePlayerButton);
            Container.Bind<IMultiPlayerButtonController>().To<MultiPlayerButtonController>().AsSingle()
                .WithArguments(multiPlayerButton);
        }
    }
}