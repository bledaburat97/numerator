using UnityEngine;
using Zenject;

namespace Scripts
{
    public class WaitingSceneMonoInstaller : MonoInstaller
    {
        [SerializeField] private PlayButtonView readyButton;

        public override void InstallBindings()
        {
            Container.Bind<IReadyButtonController>().To<ReadyButtonController>().AsSingle()
                .WithArguments(readyButton);
        }
    }
}