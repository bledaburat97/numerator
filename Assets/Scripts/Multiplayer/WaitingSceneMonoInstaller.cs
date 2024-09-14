﻿using System;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class WaitingSceneMonoInstaller : MonoInstaller
    {
        [SerializeField] private WaitingSceneUIView waitingSceneUI;
        [SerializeField] private PlayerNameAreaView playerNameArea;
        [SerializeField] private FadePanelView fadePanelView;

        public override void InstallBindings()
        {
            Container.BindFactory<IBaseButtonView, Action, IBaseButtonController, BaseButtonControllerFactory>().To<BaseButtonController>();
            Container.Bind<IUserReady>().To<UserReady>().FromComponentInHierarchy().AsSingle();
            Container.Bind<IWaitingSceneUIController>().To<WaitingSceneUIController>().AsSingle()
                .WithArguments(waitingSceneUI);
            Container.Bind<IPlayerNameAreaController>().To<PlayerNameAreaController>().AsSingle()
                .WithArguments(playerNameArea);
            Container.Bind<IWaitingScenePopupCreator>().To<WaitingScenePopupCreator>().FromComponentInHierarchy().AsSingle();
            Container.Bind<IHapticController>().To<HapticController>().AsSingle();
            Container.Bind<IFadePanelController>().To<FadePanelController>().AsSingle().WithArguments(fadePanelView, fadePanelView);
        }
    }
}