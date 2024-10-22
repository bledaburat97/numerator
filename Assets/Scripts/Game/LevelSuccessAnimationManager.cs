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
        private IGameUIController _gameUIController;
        private ILifeBarController _lifeBarController;
        private IResultAreaController _resultAreaController;
        private IBoardAreaController _boardAreaController;
        
        private List<ICardViewHandler> _cardsOnBoard;

        [Inject]
        public LevelSuccessAnimationManager(ILevelEndPopupController levelEndPopupController, IInitialCardAreaController initialCardAreaController,
        IFadePanelController fadePanelController, IGameUIController gameUIController, ILifeBarController lifeBarController, 
        IResultAreaController resultAreaController, IBoardAreaController boardAreaController)
        {
            _levelEndPopupController = levelEndPopupController;
            _initialCardAreaController = initialCardAreaController;
            _fadePanelController = fadePanelController;
            _gameUIController = gameUIController;
            _lifeBarController = lifeBarController;
            _resultAreaController = resultAreaController;
            _boardAreaController = boardAreaController;
            _cardsOnBoard = new List<ICardViewHandler>();
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
            float buttonsFadeOutDuration = 0.3f;
            float wagonMoveDuration = 1f;
            _cardsOnBoard = _initialCardAreaController.GetCardsOnBoard();
            DOTween.Sequence()
                .AppendCallback(() => TurnCardsIntoCertain(_cardsOnBoard, cardDelayDuration, cardColorChangingDuration))
                .AppendInterval(cardColorChangingDuration + cardDelayDuration * (_cardsOnBoard.Count - 1))
                .Append(ExplodeCardsOnInitialHolders(explosionDuration))
                .Append(FadeOutTopAreaButtons(buttonsFadeOutDuration))
                .Join(FadeOutLifeBar(buttonsFadeOutDuration))
                .Join(FadeOutLevelId(buttonsFadeOutDuration))
                .Join(FadeOutResultArea(buttonsFadeOutDuration))
                .Join(FadeOutInitialHolders(buttonsFadeOutDuration))
                .Join(FadeOutGameButtons(buttonsFadeOutDuration))
                .Join(SendBoardHolders(wagonMoveDuration))
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

        private Sequence SendBoardHolders(float duration)
        {
            return _boardAreaController.MoveBoardHoldersToOutsideScene(duration).OnComplete(()=>
            {
                foreach (ICardViewHandler card in _cardsOnBoard)
                {
                    card.DestroyObject();
                }
                _boardAreaController.ClearBoardHolders();
            });
        }
    }

    public interface ILevelSuccessAnimationManager
    {
        void SuccessLevelAnimation(int numOfStars, int newRewardStarCount, int currentRewardStarCount);
    }
}