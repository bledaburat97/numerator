using System;
using System.Collections.Generic;
using DG.Tweening;
using Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Scripts
{
    public class LevelFinishController : ILevelFinishController
    {
        private IFadePanelController _fadePanelController;
        private IHapticController _hapticController;
        private ILevelTracker _levelTracker;
        private IInitialCardAreaController _initialCardAreaController;
        private IGameSaveService _gameSaveService;
        private IGuessManager _guessManager;
        private ILevelFinishPopupView _view;
        private ICircleProgressBarController _circleProgressBarController;
        private ITargetNumberCreator _targetNumberCreator;


        [Inject]
        public LevelFinishController(IFadePanelController fadePanelController, IHapticController hapticController,
            ILevelTracker levelTracker,
            IInitialCardAreaController initialCardAreaController,
            IGameSaveService gameSaveService,
            IResultManager resultManager, IGuessManager guessManager,
            ITargetNumberCreator targetNumberCreator,
            ILevelFinishPopupView view)
        {
            _view = view;
            _fadePanelController = fadePanelController;
            _hapticController = hapticController;
            _levelTracker = levelTracker;
            _initialCardAreaController = initialCardAreaController;
            _gameSaveService = gameSaveService;
            _guessManager = guessManager;
            _targetNumberCreator = targetNumberCreator;
            _circleProgressBarController =
                new CircleProgressBarController(_view.GetCircleProgressBar(), _hapticController);
            guessManager.LevelFailEvent += OnLevelFail;
        }

        private void OnLevelFail(object sender, EventArgs args)
        {
            //_isGameOver = true;
            InitLevelEnd();
            //InitText("Try Again!");
            _view.GetButton(LevelFinishButtonType.Game).SetText("Retry");
            _view.GetStarCanvasGroup().gameObject.SetActive(false);
            _view.GetRewardItem().gameObject.SetActive(false);
            _view.GetCircleProgressBar().GetRectTransform().gameObject.SetActive(false);
            FailLevelAnimation(_targetNumberCreator.GetTargetCardsList(), _initialCardAreaController.GetFinalCardItems());
        }
        
        private void InitLevelEnd()
        {
            /*
            _gameSaveService.DeleteSave();
            InitButton(LevelFinishButtonType.Game, () =>
            {
                _view.SetStatus(false);
                _view.GetTopArea().alpha = 1f;
                _view.GetScrollArea().alpha = 1f;
                _view.GetBoardArea().alpha = 1f;
                _view.GetButtonArea().alpha = 1f;
                _view.GetBottomArea().alpha = 1f;
                _gameInitializer.Initialize();
            });
            InitButton(LevelFinishButtonType.Menu, () => SceneManager.LoadScene("Menu"));
            */
        }

        public void MultiplayerLevelFinish(MultiplayerLevelFinishInfo multiplayerLevelFinishInfo)
        {
            /*
            _fadePanelController.SetFadeImageStatus(true);
            _fadePanelController.SetFadeImageAlpha(0f);
            IMultiplayerLevelEndPopupController multiplayerLevelEndPopupController =
                _multiplayerLevelEndPopupControllerFactory.Spawn();
            IMultiplayerLevelEndPopupView multiplayerLevelEndPopupView =
                _multiplayerLevelEndPopupViewFactory.Spawn(transform, multiplayerLevelEndPopupPrefab);
            if(!isSuccess) _hapticController.Vibrate(HapticType.Failure);
            multiplayerLevelEndPopupController.Initialize(multiplayerLevelEndPopupView, isSuccess, userReady, onPlayerReady, _baseButtonControllerFactory, _fadePanelController);
            */
        }

        private void FailLevelAnimation(List<int> targetCardIndexList, List<ICardViewHandler> cardItemList)
        {
            DOTween.Sequence()
                //.AppendCallback(() => AnimateBackFlipCards(targetCardIndexList, cardItemList, false))
                .AppendInterval(1.2f + 0.3f * cardItemList.Count)
                .Append(_view.GetTopArea().DOFade(0f, 0.2f))
                .Join(_view.GetButtonArea().DOFade(0f, 0.2f))
                .Join(_view.GetBottomArea().DOFade(0f, 0.2f))
                .AppendCallback(() => _view.SetStatus(true))
                .AppendInterval(0.2f)
                .Append(_view.GetText().rectTransform.DOScale(1f, 0.5f))
                .AppendInterval(0.2f);
            //.Append(AnimateButtons());
        }



        private void SuccessMultiplayerLevel()
        {

        }

        private void FailMultiplayerLevel()
        {

        }
        

    }

    public struct MultiplayerLevelFinishInfo
    {
        public bool isSuccess;
        public Action onPlayAgain;
    }

    public enum RewardType
    {
        Retrieval,
        Life,
        Hint
    }

    public interface ILevelFinishController
    {
        //void Initialize();
        void MultiplayerLevelFinish(MultiplayerLevelFinishInfo multiplayerLevelFinishInfo);
    }
}