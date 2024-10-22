using DG.Tweening;
using Scripts;
using UnityEngine.SceneManagement;
using Zenject;

namespace Game
{
    public class LevelFailManager : ILevelFailManager
    {
        private bool _isGameOver;
        private ILevelStartManager _levelStartManager;
        private ILevelEndPopupController _levelEndPopupController;
        private ILevelFailAnimationManager _levelFailAnimationManager;
        private IFadePanelController _fadePanelController;
        
        [Inject]
        public LevelFailManager(ILevelStartManager levelStartManager, ILevelEndPopupController levelEndPopupController, 
            ILevelFailAnimationManager levelFailAnimationManager, IFadePanelController fadePanelController)
        {
            _levelStartManager = levelStartManager;
            _levelEndPopupController = levelEndPopupController;
            _levelFailAnimationManager = levelFailAnimationManager;
            _fadePanelController = fadePanelController;
        }

        public void LevelFail()
        {
            _levelEndPopupController.SetAllStatusFalse();
            _levelEndPopupController.SetPopupStatus(true);
            InitButtons();
            _levelFailAnimationManager.FailLevelAnimation();
        }
        
        private void InitButtons()
        {
            _levelEndPopupController.InitButton(LevelFinishButtonType.Game, "Retry", OnRetryLevelButtonClicked);
            _levelEndPopupController.InitButton(LevelFinishButtonType.Menu, "Menu", OnMenuButtonClicked);
        }

        private void OnMenuButtonClicked()
        {
            SceneManager.LoadScene("Menu");
        }

        private void OnRetryLevelButtonClicked()
        {
            float animationDuration = 1f;
            DOTween.Sequence().AppendCallback(() => _levelEndPopupController.SetPopupStatus(false))
                .Append(_fadePanelController.AnimateFade(0f, animationDuration))
                .Join(_levelFailAnimationManager.FadeOutResultArea(animationDuration))
                .Join(_levelFailAnimationManager.SendBoardHolders(animationDuration))
                .AppendCallback(() => _levelEndPopupController.SetPopupStatus(false))
                .AppendCallback(() => _fadePanelController.SetFadeImageStatus(false))
                .AppendCallback(() => _levelStartManager.StartLevel());
        }
    }

    public interface ILevelFailManager
    {
        void LevelFail();
    }
}