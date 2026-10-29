using System;
using Menu;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class MenuSceneMonoInstaller: MonoInstaller
    {
        [SerializeField] private MenuHeaderView menuHeader;
        [SerializeField] private MenuUIView menuUI;
        [SerializeField] private PowerUpListHolderView powerUpListHolderView;
        [SerializeField] private VerticalCrystalProgressView crystalProgressView;
        public override void InstallBindings()
        {
            Container.Bind<IMenuHeaderController>().To<MenuHeaderController>().AsSingle()
                .WithArguments(menuHeader);
            Container.Bind<IMenuUIController>().To<MenuUIController>().AsSingle()
                .WithArguments(menuUI);
            Container.Bind<IMenuPowerUpCountHolderController>().To<MenuPowerUpCountHolderController>().AsSingle()
                .WithArguments(ResolvePowerUpListHolderView());
            Container.Bind<IRewardProgressDisplayController>().To<RewardProgressDisplayController>().AsSingle()
                .WithArguments(ResolveCrystalProgressView());
        }

        private PowerUpListHolderView ResolvePowerUpListHolderView()
        {
            if (powerUpListHolderView != null) return powerUpListHolderView;

            return FindObjectOfType<PowerUpListHolderView>(true);
        }

        private VerticalCrystalProgressView ResolveCrystalProgressView()
        {
            if (crystalProgressView != null) return crystalProgressView;

            return FindObjectOfType<VerticalCrystalProgressView>(true);
        }
    }
}
