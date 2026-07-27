using DG.Tweening;
using Scripts;
using Zenject;

namespace Game
{
    public class LevelSuccessAnimationManager : ILevelSuccessAnimationManager
    {
        private ILevelEndPopupController _levelEndPopupController;
        private IFadePanelController _fadePanelController;
        private ILevelSuccessBoardAnimationController _boardAnimationController;
        private ILevelSuccessCleanupAnimationController _cleanupAnimationController;
        private ILevelSuccessRewardCollectionAnimationController _rewardCollectionAnimationController;

        [Inject]
        public LevelSuccessAnimationManager(
            ILevelEndPopupController levelEndPopupController,
            IFadePanelController fadePanelController,
            ILevelSuccessBoardAnimationController boardAnimationController,
            ILevelSuccessCleanupAnimationController cleanupAnimationController,
            ILevelSuccessRewardCollectionAnimationController rewardCollectionAnimationController)
        {
            _levelEndPopupController = levelEndPopupController;
            _fadePanelController = fadePanelController;
            _boardAnimationController = boardAnimationController;
            _cleanupAnimationController = cleanupAnimationController;
            _rewardCollectionAnimationController = rewardCollectionAnimationController;
        }
        
        public Sequence SuccessLevelAnimation(
            int earnedCoinCount,
            int earnedCrystalCount,
            int previousCoinCount,
            int previousCrystalProgressCount,
            RewardType rewardTypeBeforeCollection)
        {
            _fadePanelController.SetFadeImageStatus(true);
            _fadePanelController.SetFadeImageAlpha(0f);
            float cardDelayDuration = 0.1f;
            float cardColorChangingDuration = 0.24f;
            float ribbonImageDuration = 0.2f;
            float cardDismissDuration = 0.22f;
            float fadeDuration = 0.28f;
            float fadeAmount = 0.3f;
            float scalingUpDurationOfText = 0.22f;
            float buttonsFadeOutDuration = 0.2f;
            return DOTween.Sequence()
                .Append(_boardAnimationController.Play(cardDelayDuration, cardColorChangingDuration, ribbonImageDuration))
                .Insert(0.1f, DOTween.Sequence()
                    .AppendCallback(() => _levelEndPopupController.SetPopupStatus(true))
                    .Append(_levelEndPopupController.ScaleUpText(scalingUpDurationOfText))
                    .Join(_fadePanelController.AnimateFade(fadeAmount, fadeDuration)))
                .AppendInterval(0.05f)
                .Append(_cleanupAnimationController.DismissCardsOnInitialHolders(cardDismissDuration))
                .Append(_cleanupAnimationController.FadeOutObsoleteGameUi(buttonsFadeOutDuration))
                .Append(_rewardCollectionAnimationController.Play(
                    earnedCoinCount,
                    earnedCrystalCount,
                    previousCoinCount,
                    previousCrystalProgressCount,
                    rewardTypeBeforeCollection))
                .Append(_cleanupAnimationController.FadeOutLifeBar(buttonsFadeOutDuration))
                .Append(_rewardCollectionAnimationController.PlayPostCollectionFlow(0.3f));
        }
    }

    public interface ILevelSuccessAnimationManager
    {
        Sequence SuccessLevelAnimation(
            int earnedCoinCount,
            int earnedCrystalCount,
            int previousCoinCount,
            int previousCrystalProgressCount,
            RewardType rewardTypeBeforeCollection);
    }
}
