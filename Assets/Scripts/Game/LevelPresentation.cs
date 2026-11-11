using System;
using DG.Tweening;
using Scripts;
using Zenject;

namespace Game
{
    public sealed class LevelPresentation : IDisposable
    {
        private readonly IGamePopupCreator _gamePopupCreator;
        private readonly IGamePowerUpAreaController _gamePowerUpAreaController;
        private readonly IVerticalCrystalProgressController _verticalCrystalProgressController;
        private readonly ILevelStartAnimationManager _levelStartAnimationManager;
        private readonly ILevelEndPopupController _levelEndPopupController;
        private readonly ILevelFailAnimationManager _levelFailAnimationManager;
        private readonly IFadePanelController _fadePanelController;
        private Sequence _intro, _transition;
        [Inject]
        public LevelPresentation(
            IGamePopupCreator gamePopupCreator,
            IGamePowerUpAreaController gamePowerUpAreaController,
            IVerticalCrystalProgressController verticalCrystalProgressController,
            ILevelStartAnimationManager levelStartAnimationManager,
            ILevelEndPopupController levelEndPopupController,
            ILevelFailAnimationManager levelFailAnimationManager,
            IFadePanelController fadePanelController)
        {
            _gamePopupCreator = gamePopupCreator;
            _gamePowerUpAreaController = gamePowerUpAreaController;
            _verticalCrystalProgressController = verticalCrystalProgressController;
            _levelStartAnimationManager = levelStartAnimationManager;
            _levelEndPopupController = levelEndPopupController;
            _levelFailAnimationManager = levelFailAnimationManager;
            _fadePanelController = fadePanelController;
        }

        public void PrepareRound()
        {
            _gamePopupCreator.Initialize(); _gamePopupCreator.PrepareTutorials();
            _levelEndPopupController.SetPopupStatus(false); _levelEndPopupController.SetAllStatusFalse();
            _gamePowerUpAreaController.Refresh(); _verticalCrystalProgressController.Initialize();
        }
        public void PlayIntro(Action completed)
        {
            _intro?.Kill(); _intro = _levelStartAnimationManager.StartLevelStartAnimation();
            if (_intro == null) completed(); else _intro.OnComplete(() => completed());
        }
        public void Retry(Action completed)
        {
            _transition?.Kill();
            _transition = DOTween.Sequence()
                .AppendCallback(() => _levelEndPopupController.SetPopupStatus(false))
                .Append(_fadePanelController.AnimateFade(0, 1))
                .Join(_levelFailAnimationManager.FadeOutResultArea(1))
                .Join(_levelFailAnimationManager.SendBoardHolders(1))
                .AppendCallback(() => _fadePanelController.SetFadeImageStatus(false))
                .AppendCallback(() => completed());
        }
        public void Next(Action completed)
        {
            _transition?.Kill();
            _transition = DOTween.Sequence()
                .AppendCallback(() => _levelEndPopupController.SetPopupStatus(false))
                .Append(_fadePanelController.AnimateFade(0, .2f))
                .AppendCallback(() => _fadePanelController.SetFadeImageStatus(false))
                .AppendCallback(() => completed());
        }
        public void Cancel() { _intro?.Kill(); _transition?.Kill(); }
        public void Dispose()=>Cancel();
    }
}
