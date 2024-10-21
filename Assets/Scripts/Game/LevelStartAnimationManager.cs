using DG.Tweening;
using Scripts;
using UnityEngine.Rendering;
using Zenject;

namespace Game
{
    public class LevelStartAnimationManager : ILevelStartAnimationManager
    {
        private IBoardAreaController _boardAreaController;
        private IResultAreaController _resultAreaController;
        private ILifeBarController _lifeBarController;
        private IInitialCardAreaController _initialCardAreaController;
        private IGameUIController _gameUIController;

        [Inject] 
        public LevelStartAnimationManager(IBoardAreaController boardAreaController, IResultAreaController resultAreaController,
            ILifeBarController lifeBarController, IInitialCardAreaController initialCardAreaController, IGameUIController gameUIController)
        {
            _boardAreaController = boardAreaController;
            _resultAreaController = resultAreaController;
            _lifeBarController = lifeBarController;
            _initialCardAreaController = initialCardAreaController;
            _gameUIController = gameUIController;
        }

        public void StartLevelStartAnimation()
        {
            float initialAreaFadeInDuration = 0.2f;
            float fallBoxesDuration = 2f;
            float moveWagonsDuration = 1f;
            float buttonsFadeInDuration = 0.3f;

            DOTween.Sequence().Append(FadeInInitialHolderArea(initialAreaFadeInDuration))
                .Append(FallBoxes(fallBoxesDuration))
                .Append(MoveWagons(moveWagonsDuration))
                .Append(FadeInTopAreaButtons(buttonsFadeInDuration))
                .Join(FadeInLifeBar(buttonsFadeInDuration))
                .Join(FadeInMiddleGameButtons(buttonsFadeInDuration))
                .Join(FadeInResultArea(buttonsFadeInDuration));
        }

        private Sequence FadeInResultArea(float duration)
        {
            return _resultAreaController.FadeIn(duration);
        }

        private Sequence FadeInInitialHolderArea(float duration)
        {
            return _initialCardAreaController.FadeInInitialArea(duration);
        }

        private Sequence FallBoxes(float duration)
        {
            return _initialCardAreaController.FallToInitialHolders(duration);
        }

        private Sequence FadeInTopAreaButtons(float duration)
        {
            return _gameUIController.FadeInTopButtons(duration);
        }

        private Sequence FadeInLifeBar(float duration)
        {
            return _lifeBarController.FadeIn(duration);
        }

        private Sequence FadeInMiddleGameButtons(float duration)
        {
            return _gameUIController.FadeInMiddleButtons(duration);
        }

        private Sequence MoveWagons(float duration)
        {
            return _boardAreaController.MoveBoardHoldersToScene(duration);
        }
    }

    public interface ILevelStartAnimationManager
    {
        void StartLevelStartAnimation();
    }
}