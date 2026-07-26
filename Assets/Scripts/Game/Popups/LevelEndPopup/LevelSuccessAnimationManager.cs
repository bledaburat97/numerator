using System.Collections.Generic;
using DG.Tweening;
using Scripts;
using Zenject;

namespace Game
{
    public class LevelSuccessAnimationManager : ILevelSuccessAnimationManager
    {
        private ILevelEndPopupController _levelEndPopupController;
        private IInitialCardAreaController _initialCardAreaController;
        private IFadePanelController _fadePanelController;
        private IGameUIController _gameUIController;
        private ILifeBarController _lifeBarController;
        private IResultAreaController _resultAreaController;
        private IBoardAreaController _boardAreaController;
        private ICardPlacementCoordinator _cardPlacementCoordinator;
        
        private List<ICardViewHandler> _cardsOnBoard;

        [Inject]
        public LevelSuccessAnimationManager(ILevelEndPopupController levelEndPopupController, IInitialCardAreaController initialCardAreaController,
        IFadePanelController fadePanelController, IGameUIController gameUIController, ILifeBarController lifeBarController, 
        IResultAreaController resultAreaController, IBoardAreaController boardAreaController,
        ICardPlacementCoordinator cardPlacementCoordinator)
        {
            _levelEndPopupController = levelEndPopupController;
            _initialCardAreaController = initialCardAreaController;
            _fadePanelController = fadePanelController;
            _gameUIController = gameUIController;
            _lifeBarController = lifeBarController;
            _resultAreaController = resultAreaController;
            _boardAreaController = boardAreaController;
            _cardPlacementCoordinator = cardPlacementCoordinator;
            _cardsOnBoard = new List<ICardViewHandler>();
        }
        
        public Sequence SuccessLevelAnimation(int numOfStars, int newRewardStarCount, int currentRewardStarCount)
        {
            _fadePanelController.SetFadeImageStatus(true);
            _fadePanelController.SetFadeImageAlpha(0f);
            float cardDelayDuration = 0.1f;
            float cardColorChangingDuration = 0.24f;
            float cardDismissDuration = 0.22f;
            float fadeDuration = 0.28f;
            float fadeAmount = 0.3f;
            float movementDurationOfCircleProgressBar = 0.45f;
            float scalingUpDurationOfText = 0.22f;
            float buttonsFadeOutDuration = 0.2f;
            _cardsOnBoard = _cardPlacementCoordinator.GetCardsOnBoard();
            return DOTween.Sequence()
                .AppendCallback(() => TurnCardsIntoCertain(_cardsOnBoard, cardDelayDuration, cardColorChangingDuration))
                .Join(_boardAreaController.PlayAllSuccessFrameAnimations(cardDelayDuration))
                .Insert(0.1f, DOTween.Sequence()
                    .AppendCallback(() => _levelEndPopupController.SetPopupStatus(true))
                    .Append(_levelEndPopupController.ScaleUpText(scalingUpDurationOfText))
                    .Join(_fadePanelController.AnimateFade(fadeAmount, fadeDuration)))
                .AppendInterval(0.05f)
                .Append(DismissCardsOnInitialHolders(cardDismissDuration))
                .Append(FadeOutTopAreaButtons(buttonsFadeOutDuration))
                .Join(FadeOutLifeBar(buttonsFadeOutDuration))
                .Join(FadeOutLevelId(buttonsFadeOutDuration))
                .Join(FadeOutResultArea(buttonsFadeOutDuration))
                .Join(FadeOutInitialHolders(buttonsFadeOutDuration))
                .Join(FadeOutGameButtons(buttonsFadeOutDuration))
                .Append(newRewardStarCount > 0
                    ? _levelEndPopupController.MoveCircleProgressBar(movementDurationOfCircleProgressBar)
                    : DOTween.Sequence())
                .AppendInterval(0.1f)
                .Append(_levelEndPopupController.AnimateStarCreation(numOfStars, 0.1f, 0.28f))
                .AppendInterval(0.1f)
                .Append(_levelEndPopupController.AddNewStarsToCircleProgressBar(newRewardStarCount, numOfStars))
                .AppendInterval(0.1f)
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

        private Sequence DismissCardsOnInitialHolders(float dismissDuration)
        {
            List<ICardViewHandler> cardViewHandlerList = _cardPlacementCoordinator.GetCardsOnInitialHolder();
            Sequence sequence = DOTween.Sequence();
            
            foreach (ICardViewHandler card in cardViewHandlerList)
            {
                sequence.Join(card.AnimateExplosion(dismissDuration));
            }

            return sequence;
        }

        private Sequence FadeOutTopAreaButtons(float duration)
        {
            return _gameUIController.ChangeFadeTopAreaButtons(duration, 0f);
        }

        private Sequence FadeOutLifeBar(float duration)
        {
            return _lifeBarController.ChangeFade(duration, 0f).OnComplete(() =>
            {
                _lifeBarController.ClearBoundaries();
                _lifeBarController.ClearLifeBarStarInfoList();
            });
        }

        private Sequence FadeOutLevelId(float duration)
        {
            return _gameUIController.ChangeFadeUserText(duration, 0f);
        }

        private Sequence FadeOutResultArea(float duration)
        {
            return _resultAreaController.ChangeFade(duration, 0f)
                .OnComplete(() => _resultAreaController.RemoveResultBlocks());
        }

        private Sequence FadeOutInitialHolders(float duration)
        {
            return _initialCardAreaController.ChangeFadeInitialArea(duration, 0f).OnComplete(()=>_initialCardAreaController.ClearInitialCardHolders());
        }

        private Sequence FadeOutGameButtons(float duration)
        {
            return _gameUIController.ChangeFadeMiddleAreaButtons(duration, 0f);
        }
    }

    public interface ILevelSuccessAnimationManager
    {
        Sequence SuccessLevelAnimation(int numOfStars, int newRewardStarCount, int currentRewardStarCount);
    }
}
