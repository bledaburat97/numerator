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
            Container.BindInterfacesAndSelfTo<MainNavigationController>().AsSingle()
                .WithArguments(MainNavigationView.Ensure(menuUI.GetComponentInParent<Canvas>())).NonLazy();
            Container.BindInterfacesAndSelfTo<MenuHeaderController>().AsSingle()
                .WithArguments(menuHeader);
            Container.Bind<IMenuUIController>().To<MenuUIController>().AsSingle()
                .WithArguments(menuUI);
            Container.BindInterfacesAndSelfTo<FruitCollectionPresenter>().AsSingle().WithArguments(menuUI.GetCollectionView(),menuUI.GetGardenNoticeView());
            menuUI.GetAchievementsView().SetVisible(false);
            Container.BindInterfacesAndSelfTo<ClosestBadgePresenter>().AsSingle().WithArguments(
                ClosestBadgeView.Ensure(menuUI.GetComponentInParent<Canvas>().GetComponent<FruitLabMenuLayout>().HomeContent)).NonLazy();
            Container.BindInterfacesAndSelfTo<MenuPowerUpCountHolderController>().AsSingle()
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
