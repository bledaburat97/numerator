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
        [SerializeField] private CircleProgressBarView circleProgressBarView;
        public override void InstallBindings()
        {
            Container.Bind<IMenuHeaderController>().To<MenuHeaderController>().AsSingle()
                .WithArguments(menuHeader);
            Container.Bind<IMenuUIController>().To<MenuUIController>().AsSingle()
                .WithArguments(menuUI);
            Container.Bind<IMenuPowerUpCountHolderController>().To<MenuPowerUpCountHolderController>().AsSingle()
                .WithArguments(ResolvePowerUpListHolderView());
            Container.Bind<IRewardProgressDisplayController>().To<RewardProgressDisplayController>().AsSingle()
                .WithArguments(ResolveCircleProgressBarView());
        }

        private PowerUpListHolderView ResolvePowerUpListHolderView()
        {
            if (powerUpListHolderView != null) return powerUpListHolderView;

            return FindObjectOfType<PowerUpListHolderView>(true);
        }

        private CircleProgressBarView ResolveCircleProgressBarView()
        {
            if (circleProgressBarView != null) return circleProgressBarView;

            return FindObjectOfType<CircleProgressBarView>(true);
        }
    }
}
