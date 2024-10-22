using System;
using DG.Tweening;
using Scripts;
using UnityEngine.SceneManagement;
using Zenject;

namespace Game
{
    public class LevelSuccessManager : ILevelSuccessManager
    {
        private bool _isGameOver;
        private ILevelTracker _levelTracker;
        private IGuessManager _guessManager;
        private ILevelStartManager _levelStartManager;
        private ILevelEndPopupController _levelEndPopupController;
        private ILevelSuccessAnimationManager _levelSuccessAnimationManager;
        private ICardItemInfoPopupController _cardItemInfoPopupController;
        private IFadePanelController _fadePanelController;

        [Inject]
        public LevelSuccessManager(IResultManager resultManager,
        ILevelTracker levelTracker, IGuessManager guessManager, ILevelStartManager levelStartManager,
        ILevelEndPopupController levelEndPopupController, ILevelSuccessAnimationManager levelSuccessAnimationManager, 
        ICardItemInfoPopupController cardItemInfoPopupController, IFadePanelController fadePanelController)
        {
            _levelTracker = levelTracker;
            _guessManager = guessManager;
            _levelStartManager = levelStartManager;
            _levelEndPopupController = levelEndPopupController;
            _levelSuccessAnimationManager = levelSuccessAnimationManager;
            _cardItemInfoPopupController = cardItemInfoPopupController;
            _fadePanelController = fadePanelController;
        }

        public void LevelSuccess()
        {
            int rewardStarCount = _levelTracker.GetGiftStarCount();
            RewardType rewardType = _levelTracker.GetCurrentRewardType();
            _guessManager.GetActiveStarCounts(out int totalStarCount, out int newRewardStarCount);
            _levelTracker.IncrementLevelId(totalStarCount, newRewardStarCount);
            _levelEndPopupController.SetAllStatusFalse();
            InitButtons();
            _levelEndPopupController.InitText("Well Done");
            _levelEndPopupController.CreateRewardCircle(rewardStarCount);
            _levelEndPopupController.InitStarsAndParticles(totalStarCount, newRewardStarCount);
            _levelEndPopupController.InitRewardItem(rewardType);
            _levelSuccessAnimationManager.SuccessLevelAnimation(totalStarCount, newRewardStarCount, rewardStarCount);
        }
        
        private void InitButtons()
        {
            _levelEndPopupController.InitButton(LevelFinishButtonType.Game, "Level " + (_levelTracker.GetLevelId() + 1), OnNextLevelButtonClicked);
            _levelEndPopupController.InitButton(LevelFinishButtonType.Menu, "Menu", OnMenuButtonClicked);
        }

        private void OnMenuButtonClicked()
        {
            SceneManager.LoadScene("Menu");
        }

        private void OnNextLevelButtonClicked()
        {
            DOTween.Sequence().AppendCallback(() => _levelEndPopupController.SetPopupStatus(false))
                .Append(_fadePanelController.AnimateFade(0f, 0.2f))
                .AppendCallback(() => _fadePanelController.SetFadeImageStatus(false))
                .AppendCallback(() => _levelStartManager.StartLevel());
        }
    }

    public interface ILevelSuccessManager
    {
        void LevelSuccess();
    }
}