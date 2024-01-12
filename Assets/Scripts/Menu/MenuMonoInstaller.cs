using UnityEngine;
using Zenject;

namespace Scripts
{
    public class MenuMonoInstaller: MonoInstaller
    {
        [SerializeField] private PlayButtonView singlePlayerButton;
        [SerializeField] private PlayButtonView multiPlayerButton;
        [SerializeField] private LevelSelectionTableView levelSelectionTable;
        [SerializeField] private MenuHeaderView menuHeader;
        public override void InstallBindings()
        {
            Container.Bind<IGameSaveService>().To<GameSaveService>().AsSingle();
            Container.Bind<ILevelTracker>().To<LevelTracker>().FromComponentInHierarchy().AsSingle();
            Container.Bind<IActiveLevelIdController>().To<ActiveLevelIdController>().AsSingle();
            Container.Bind<ILevelSelectionTableController>().To<LevelSelectionTableController>().AsSingle()
                .WithArguments(levelSelectionTable);
            Container.Bind<IMenuHeaderController>().To<MenuHeaderController>().AsSingle()
                .WithArguments(menuHeader);
            Container.Bind<ISinglePlayerButtonController>().To<SinglePlayerButtonController>().AsSingle()
                .WithArguments(singlePlayerButton);
            Container.Bind<IMultiPlayerButtonController>().To<MultiPlayerButtonController>().AsSingle()
                .WithArguments(multiPlayerButton);
        }
    }
}