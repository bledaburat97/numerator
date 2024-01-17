using Menu;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class MenuSceneMonoInstaller: MonoInstaller
    {
        [SerializeField] private LevelSelectionTableView levelSelectionTable;
        [SerializeField] private MenuHeaderView menuHeader;
        [SerializeField] private MenuUIView menuUI;
        
        public override void InstallBindings()
        {
            Container.Bind<IGameSaveService>().To<GameSaveService>().AsSingle();
            Container.Bind<ILevelTracker>().To<LevelTracker>().FromComponentInHierarchy().AsSingle();
            Container.Bind<IActiveLevelIdController>().To<ActiveLevelIdController>().AsSingle();
            Container.Bind<ILevelSelectionTableController>().To<LevelSelectionTableController>().AsSingle()
                .WithArguments(levelSelectionTable);
            Container.Bind<IMenuHeaderController>().To<MenuHeaderController>().AsSingle()
                .WithArguments(menuHeader);
            Container.Bind<IMenuUIController>().To<MenuUIController>().AsSingle()
                .WithArguments(menuUI);
        }
    }
}