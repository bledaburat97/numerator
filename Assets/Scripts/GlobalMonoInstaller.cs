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
            Container.Bind<Economy.IPlayerProfileRepository>().FromMethod(_=>Economy.PlayerProfileRuntime.Current).AsSingle();
            Container.Bind<Economy.IFruitInventoryService>().To<Economy.FruitInventoryService>().AsSingle();
            Container.Bind<Economy.EconomySettings>().FromMethod(_=>Economy.PlayerProfileRuntime.LoadSettings()).AsSingle();
            Container.Bind<ILevelTracker>().To<LevelTracker>().FromComponentsInHierarchy().AsSingle();
            Container.Bind<IHapticController>().To<HapticController>().AsSingle();
        }
    }
}
