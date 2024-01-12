using UnityEngine;
using Zenject;

namespace Scripts
{
    public class WaitingSceneMonoInstaller : MonoInstaller
    {
        [SerializeField] private PlayButtonView readyButton;
        [SerializeField] private WaitingSceneUIView waitingSceneUI;
        [SerializeField] private PlayerNameAreaView playerNameArea;
        public override void InstallBindings()
        {
            Container.Bind<IReadyButtonController>().To<ReadyButtonController>().AsSingle()
                .WithArguments(readyButton);
            Container.Bind<IWaitingSceneUIController>().To<WaitingSceneUIController>().AsSingle()
                .WithArguments(waitingSceneUI);
            Container.Bind<IPlayerNameAreaController>().To<PlayerNameAreaController>().AsSingle()
                .WithArguments(playerNameArea);
            Container.Bind<IWaitingScenePopupCreator>().To<WaitingScenePopupCreator>().FromComponentInHierarchy().AsSingle();
        }
    }
}