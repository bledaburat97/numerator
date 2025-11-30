using System.Collections.Generic;
using DG.Tweening;
using Scripts;
using UnityEngine;
using Zenject;

namespace Game
{
    public class LevelFailAnimationManager : ILevelFailAnimationManager
    {
        private ILevelEndPopupController _levelEndPopupController;
        private IInitialCardAreaController _initialCardAreaController;
        private IFadePanelController _fadePanelController;
        private IGameUIController _gameUIController;
        private ILifeBarController _lifeBarController;
        private IResultAreaController _resultAreaController;
        private IBoardAreaController _boardAreaController;
        private List<ICardViewHandler> _trueCards;
        [Inject]
        public LevelFailAnimationManager(ILevelEndPopupController levelEndPopupController,
            IInitialCardAreaController initialCardAreaController,
            IFadePanelController fadePanelController, IGameUIController gameUIController,
            ILifeBarController lifeBarController,
            IResultAreaController resultAreaController, IBoardAreaController boardAreaController)
        {
            _levelEndPopupController = levelEndPopupController;
            _initialCardAreaController = initialCardAreaController;
            _fadePanelController = fadePanelController;
            _gameUIController = gameUIController;
            _lifeBarController = lifeBarController;
            _resultAreaController = resultAreaController;
            _boardAreaController = boardAreaController;
            _trueCards = new List<ICardViewHandler>();
        }

        public void FailLevelAnimation()
        {
            float explosionDuration = 0.3f;
            float fadeDuration = 0.4f;
            float fadeAmount = 0.4f;
            float fallBoxesDuration = 1f;
            float buttonsFadeOutDuration = 0.3f;
            float levelEndButtonsFadeInDuration = 0.3f;
            _trueCards = _initialCardAreaController.CreateTempCards();
            DOTween.Sequence()
                .Append(ExplodeAllCards(explosionDuration))
                .Append(FadeOutTopAreaButtons(buttonsFadeOutDuration))
                .Join(FadeOutLifeBar(buttonsFadeOutDuration))
                .Join(FadeOutInitialHolders(buttonsFadeOutDuration))
                .Join(FadeOutGameButtons(buttonsFadeOutDuration))
                .Append(FallBoxesToBoard(fallBoxesDuration))
                .Append(FadeInLevelEndButtons(levelEndButtonsFadeInDuration))
                .Join(_fadePanelController.AnimateFade(fadeAmount, fadeDuration));
        }
        
        private Sequence ExplodeAllCards(float explosionDuration)
        {
            Sequence sequence = DOTween.Sequence();

            List<ICardViewHandler> cardsOnInitialHolder = _initialCardAreaController.GetCardsOnInitialHolder();
            
            foreach (ICardViewHandler card in cardsOnInitialHolder)
            {
                sequence.Join(card.AnimateExplosion(explosionDuration));
            }
            
            List<ICardViewHandler> cardsOnBoard = _initialCardAreaController.GetCardsOnBoard();

            foreach (ICardViewHandler card in cardsOnBoard)
            {
                sequence.Join(card.AnimateExplosion(explosionDuration));
            }
            
            return sequence;
        }

        private Sequence FallBoxesToBoard(float duration)
        {
            Sequence sequence = DOTween.Sequence();
            foreach (ICardViewHandler card in _trueCards)
            {
                sequence.Join(card.FallToTarget(Vector2.zero, duration - 0.1f, 0.1f));
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

        public Sequence FadeOutResultArea(float duration)
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
        
        public Sequence SendBoardHolders(float duration)
        {
            return _boardAreaController.MoveBoardHoldersToOutsideScene(duration).OnComplete(()=>
            {
                foreach (ICardViewHandler card in _trueCards)
                {
                    card.DestroyObject();
                }
                _boardAreaController.ClearBoardHolders();
            });
        }

        private Sequence FadeInLevelEndButtons(float duration)
        {
            return _levelEndPopupController.ChangeFadeButtons(duration, 1f);
        }
    }

    public interface ILevelFailAnimationManager
    {
        Sequence FadeOutResultArea(float duration);
        Sequence SendBoardHolders(float duration);
        void FailLevelAnimation();
    }
}