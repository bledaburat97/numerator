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
            return _resultAreaController.ChangeFade(duration, 1f);
        }

        private Sequence FadeInInitialHolderArea(float duration)
        {
            return _initialCardAreaController.ChangeFadeInitialArea(duration, 1f);
        }

        private Sequence FallBoxes(float duration)
        {
            return _initialCardAreaController.FallToInitialHolders(duration);
        }

        private Sequence FadeInTopAreaButtons(float duration)
        {
            return _gameUIController.ChangeFadeTopAreaButtons(duration, 1f);
        }

        private Sequence FadeInLifeBar(float duration)
        {
            return _lifeBarController.ChangeFade(duration, 1f);
        }

        private Sequence FadeInMiddleGameButtons(float duration)
        {
            return _gameUIController.ChangeFadeMiddleAreaButtons(duration, 1f);
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