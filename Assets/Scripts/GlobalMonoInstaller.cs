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
            Container.Bind<Economy.PowerUpUseJournal>().AsSingle();
            Container.Bind<IGameSaveService>().To<GameSaveService>().AsSingle();
            Container.Bind<Economy.IPlayerProfileRepository>().FromMethod(_=>Economy.PlayerProfileRuntime.Current).AsSingle();
            Container.Bind<Economy.IFruitInventoryService>().To<Economy.FruitInventoryService>().AsSingle();
            Container.Bind<Economy.WalletRewardRules>().FromMethod(_=>Economy.PlayerProfileRuntime.CreateRewardRules()).AsSingle();
            Container.Bind<Economy.IGameWinRewardService>().To<Economy.GameWinRewardService>().AsSingle();
            Container.Bind<Economy.EconomySettings>().FromMethod(_=>Economy.PlayerProfileRuntime.LoadSettings()).AsSingle();
            Container.Bind<Economy.AchievementRules>().AsSingle();
            Container.Bind<Economy.IAchievementService>().To<Economy.AchievementService>().AsSingle();
            Container.Bind<Economy.IGardenClock>().To<Economy.SystemGardenClock>().AsSingle();
            Container.Bind<Economy.IGardenService>().To<Economy.GardenService>().AsSingle();
            Container.Bind<Economy.GardenVisitCoordinator>().AsSingle();
            Container.Bind<ILevelTracker>().To<LevelTracker>().FromComponentsInHierarchy().AsSingle();
            Container.Bind<IHapticController>().To<HapticController>().AsSingle();
        }
    }
}
