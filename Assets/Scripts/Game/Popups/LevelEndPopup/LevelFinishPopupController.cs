using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class LevelFinishPopupController
    {
        private LevelFinishPopupView _view;
        private ILevelTracker _levelTracker;
        private IFadePanelController _fadePanelController;
        private IHapticController _hapticController;
        private BaseButtonControllerFactory _baseButtonControllerFactory;
        
        public void Initialize(LevelFinishPopupView view, LevelEndEventArgs args,
            IFadePanelController fadePanelController, IHapticController hapticController,
            BaseButtonControllerFactory baseButtonControllerFactory)
        {
            _view = view;
            _levelTracker = args.levelTracker;
            _fadePanelController = fadePanelController;
            _hapticController = hapticController;
            _baseButtonControllerFactory = baseButtonControllerFactory;
            if(!args.isLevelCompleted) _hapticController.Vibrate(HapticType.Failure);
            _view.Init();
            _view.SetStatus(false);
            _view.SetTitle(args.isLevelCompleted ? "Well Done!" : "Try Again!");
            if (args.isLevelCompleted)
            {
                CreateCrystals(args.starCount);
                _levelTracker.AddCrystals(args.starCount);
            }

            SetButton(args.isLevelCompleted);
            Animate();
        }

        private void CreateCrystals(int crystalCount)
        {
            Vector2[] crystalsPosition = new Vector2[crystalCount];
            Vector2 size = new Vector2(ConstantValues.SIZE_OF_STARS_ON_LEVEL_SUCCESS,
                ConstantValues.SIZE_OF_STARS_ON_LEVEL_SUCCESS);
            crystalsPosition = crystalsPosition.GetLocalPositions(ConstantValues.SPACING_BETWEEN_STARS_ON_LEVEL_SUCCESS, size, 0);
            
            for (int i = 0; i < crystalCount; i++)
            {
                _view.CreateCrystal(crystalsPosition[i], size, (CrystalType) i);
            }

            _view.CreateParticles(crystalsPosition.ToList());
        }

        private void SetButton(bool isLevelCompleted)
        {
            IBaseButtonController buttonController = _baseButtonControllerFactory.Create(_view.GetButton());

            if (isLevelCompleted)
            {
                buttonController.Initialize(OnContinue);
                buttonController.SetText("CONTINUE");
            }

            else
            {
                buttonController.Initialize(OnRetry);
                buttonController.SetText("RETRY");
            }
        }

        private void OnContinue()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
            SceneManager.LoadScene("Menu");
        }
        
        private void OnRetry()
        {
            NetworkManager.Singleton.StartHost();
            NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }

        private void Animate()
        {
            List<CrystalImageView> crystalImages = _view.GetCrystalList();

            Sequence animationSequence = DOTween.Sequence();

            animationSequence.AppendInterval(0.4f)
                .Append(_fadePanelController.GetFadeImage().DOFade(0.6f, 0.5f))
                .AppendInterval(0.2f)
                .AppendCallback(()=> _view.SetStatus(true))
                .AppendInterval(0.2f)
                .Append(AnimateStarCreation(crystalImages)).Play();
        }
        
        private Sequence AnimateStarCreation(List<CrystalImageView> crystals)
        {
            Sequence starCreationAnimation = DOTween.Sequence();
            for (int i = 0; i < crystals.Count; i++)
            {
                CrystalImageView crystal = crystals[i];
                int index = i;
                float delay = .1f + 0.5f * i;
                Action particleActivation = () => _view.ActivateParticle(index);
                particleActivation += () => _hapticController.Vibrate(HapticType.Success);
                starCreationAnimation.Pause().Append(crystal.GetRectTransform().transform.DOScale(1f, 0.5f))
                    .InsertCallback(delay, particleActivation.Invoke);
            }

            return starCreationAnimation;
        }
    }
}