using System;
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
        [SerializeField] private Canvas canvas;
        public override void InstallBindings()
        {
            //Container.Bind<IActiveLevelIdController>().To<ActiveLevelIdController>().AsSingle();
            Container.Bind<IMenuHeaderController>().To<MenuHeaderController>().AsSingle()
                .WithArguments(menuHeader);
            Container.Bind<IMenuUIController>().To<MenuUIController>().AsSingle()
                .WithArguments(menuUI);
        }
    }
}