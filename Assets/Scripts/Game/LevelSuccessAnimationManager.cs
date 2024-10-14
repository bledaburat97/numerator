using System.Collections.Generic;
using DG.Tweening;
using Scripts;
using UnityEngine.SceneManagement;
using Zenject;

namespace Game
{
    public class LevelSuccessAnimationManager : ILevelSuccessAnimationManager
    {
        private ILevelEndPopupController _levelEndPopupController;
        private IInitialCardAreaController _initialCardAreaController;
        private IFadePanelController _fadePanelController;
        
        [Inject]
        public LevelSuccessAnimationManager(ILevelEndPopupController levelEndPopupController, IInitialCardAreaController initialCardAreaController,
        IFadePanelController fadePanelController)
        {
            _levelEndPopupController = levelEndPopupController;
            _initialCardAreaController = initialCardAreaController;
            _fadePanelController = fadePanelController;
        }
        
        public void SuccessLevelAnimation(int numOfStars, int newRewardStarCount, int currentRewardStarCount)
        {
            _fadePanelController.SetFadeImageStatus(true);
            _fadePanelController.SetFadeImageAlpha(0f);
            float cardDelayDuration = 0.3f;
            float cardColorChangingDuration = 0.5f;
            float explosionDuration = 0.3f;
            float fadeDuration = 0.4f;
            float fadeAmount = 0.4f;
            float movementDurationOfCircleProgressBar = 0.8f;
            float scalingUpDurationOfText = 0.3f;
            List<ICardViewHandler> cards = _initialCardAreaController.GetFinalCardItems();
            DOTween.Sequence()
                .AppendCallback(() => TurnCardsIntoCertain(cards, cardDelayDuration, cardColorChangingDuration))
                .AppendInterval(cardColorChangingDuration + cardDelayDuration * (cards.Count - 1))
                .Append(ExplodeCardsOnInitialHolders(explosionDuration))
                .Append(FadeOutPowerUpButtons())
                .Join(FadeOutLifeBar())
                .Join(FadeOutSettingsButton())
                .Join(FadeOutLevelId())
                .Join(FadeOutResultArea())
                .Join(FadeOutInitialHolders())
                .Join(FadeOutGameButtons())
                .Join(SendBoardHolders())
                .AppendCallback(() => _levelEndPopupController.SetPopupStatus(true))
                .Append(_fadePanelController.AnimateFade(fadeAmount, fadeDuration))
                .Append(newRewardStarCount > 0
                    ? _levelEndPopupController.MoveCircleProgressBar(movementDurationOfCircleProgressBar)
                    : DOTween.Sequence())
                .AppendInterval(0.2f)
                .Append(_levelEndPopupController.ScaleUpText(scalingUpDurationOfText))
                .AppendInterval(0.2f)
                .Append(_levelEndPopupController.AnimateStarCreation(numOfStars, 0.1f, 0.5f)).Play()
                .AppendInterval(0.2f)
                .Append(_levelEndPopupController.AddNewStarsToCircleProgressBar(newRewardStarCount, numOfStars))
                .AppendInterval(0.2f)
                .Append(_levelEndPopupController.TryCreateReward(newRewardStarCount, currentRewardStarCount));
        }

        private void TurnCardsIntoCertain(List<ICardViewHandler> cardViewHandlerList, float delayDuration, float colorChangingDuration)
        {
            float ribbonImageDuration = 0.2f;
            for (int i = 0; i < cardViewHandlerList.Count; i++)
            {
                float delay = delayDuration * i;
                cardViewHandlerList[i].AnimateTurnIntoCertain(delay, colorChangingDuration, ribbonImageDuration);
            }
        }

        private Sequence ExplodeCardsOnInitialHolders(float explosionDuration)
        {
            List<ICardViewHandler> cardViewHandlerList = _initialCardAreaController.GetCardsOnInitialHolder();
            Sequence sequence = DOTween.Sequence();
            
            foreach (ICardViewHandler card in cardViewHandlerList)
            {
                sequence.Join(card.AnimateExplosion(explosionDuration));
            }

            return sequence;
        }

        private Sequence FadeOutPowerUpButtons()
        {
            return DOTween.Sequence();
        }

        private Sequence FadeOutLifeBar()
        {
            return DOTween.Sequence();
        }

        private Sequence FadeOutSettingsButton()
        {
            return DOTween.Sequence();
        }

        private Sequence FadeOutLevelId()
        {
            return DOTween.Sequence();

        }

        private Sequence FadeOutResultArea()
        {
            return DOTween.Sequence();

        }

        private Sequence FadeOutInitialHolders()
        {
            return DOTween.Sequence();

        }

        private Sequence FadeOutGameButtons()
        {
            return DOTween.Sequence();
        }

        private Sequence SendBoardHolders()
        {
            return DOTween.Sequence();

        }
    }

    public interface ILevelSuccessAnimationManager
    {
        void SuccessLevelAnimation(int numOfStars, int newRewardStarCount, int currentRewardStarCount);
    }
}