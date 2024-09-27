using System;

using Zenject;
namespace Scripts
{
    public class GlobalMonoInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindFactory<IBaseButtonView, Action, IBaseButtonController, BaseButtonControllerFactory>()
                .To<BaseButtonController>();
            Container.Bind<IGameSaveService>().To<GameSaveService>().AsSingle();
            Container.Bind<ILevelTracker>().To<LevelTracker>().FromComponentsInHierarchy().AsSingle();
            Container.Bind<IHapticController>().To<HapticController>().AsSingle();
        }
    }
}