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
        private IGameInitializer _gameInitializer;
        private IGuessManager _guessManager;
        private ILevelFinishPopupView _view;
        private ICircleProgressBarController _circleProgressBarController;
        private ITargetNumberCreator _targetNumberCreator;

        private bool _isGameOver;

        [Inject]
        public LevelFinishController(IFadePanelController fadePanelController, IHapticController hapticController,
            ILevelTracker levelTracker,
            IInitialCardAreaController initialCardAreaController,
            IGameSaveService gameSaveService,
            IGameInitializer gameInitializer, IResultManager resultManager, IGuessManager guessManager,
            ITargetNumberCreator targetNumberCreator,
            ILevelFinishPopupView view)
        {
            _view = view;
            _fadePanelController = fadePanelController;
            _hapticController = hapticController;
            _levelTracker = levelTracker;
            _initialCardAreaController = initialCardAreaController;
            _gameSaveService = gameSaveService;
            _gameInitializer = gameInitializer;
            _guessManager = guessManager;
            _targetNumberCreator = targetNumberCreator;
            _circleProgressBarController =
                new CircleProgressBarController(_view.GetCircleProgressBar(), _hapticController);
            resultManager.LevelSuccessEvent += OnLevelSuccess;
            guessManager.LevelFailEvent += OnLevelFail;
        }

        public void Initialize()
        {
            _isGameOver = false;
        }

        private void OnLevelSuccess(object sender, EventArgs args)
        {
            _isGameOver = true;
            InitLevelEnd();
            _fadePanelController.SetFadeImageStatus(true);
            _fadePanelController.SetFadeImageAlpha(0f);
            InitText("Well Done!");
            int rewardStarCount = _levelTracker.GetGiftStarCount();
            RewardType rewardType = _levelTracker.GetCurrentRewardType();
            _guessManager.GetActiveStarCounts(out int totalStarCount, out int newRewardStarCount);
            _levelTracker.IncrementLevelId(totalStarCount, newRewardStarCount);
            _view.GetButton(LevelFinishButtonType.Game).SetText("Level " + (_levelTracker.GetLevelId() + 1));
            CreateRewardCircle(rewardStarCount);
            InitStarsAndParticles(totalStarCount, newRewardStarCount);
            InitRewardItem(rewardType);
            SuccessLevelAnimation(_targetNumberCreator.GetTargetCardsList(), _initialCardAreaController.GetFinalCardItems(), true, totalStarCount,
                newRewardStarCount, rewardStarCount);
        }

        private void OnLevelFail(object sender, EventArgs args)
        {
            _isGameOver = true;
            InitLevelEnd();
            InitText("Try Again!");
            _view.GetButton(LevelFinishButtonType.Game).SetText("Retry");
            _view.GetStarCanvasGroup().gameObject.SetActive(false);
            _view.GetRewardItem().gameObject.SetActive(false);
            _view.GetCircleProgressBar().GetRectTransform().gameObject.SetActive(false);
            FailLevelAnimation(_targetNumberCreator.GetTargetCardsList(), _initialCardAreaController.GetFinalCardItems());
        }
        
        private void InitLevelEnd()
        {
            _gameSaveService.DeleteSave();
            _view.Init();
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

        private void FailLevelAnimation(List<int> targetCardIndexList, List<INormalCardItemController> cardItemList)
        {
            DOTween.Sequence()
                .AppendCallback(() => AnimateBackFlipCards(targetCardIndexList, cardItemList, false))
                .AppendInterval(1.2f + 0.3f * cardItemList.Count)
                .Append(_view.GetTopArea().DOFade(0f, 0.2f))
                .Join(_view.GetButtonArea().DOFade(0f, 0.2f))
                .Join(_view.GetBottomArea().DOFade(0f, 0.2f))
                .AppendCallback(() => _view.SetStatus(true))
                .AppendInterval(0.2f)
                .Append(_view.GetText().rectTransform.DOScale(1f, 0.5f))
                .AppendInterval(0.2f)
                .Append(AnimateButtons());
        }

        private void CreateRewardCircle(int rewardStarCount)
        {
            _circleProgressBarController.Initialize(rewardStarCount);
            _circleProgressBarController.CreateInitialStarImages();
        }

        private void InitStarsAndParticles(int numOfStars, int numOfRewardStars)
        {
            Vector2[] starsPosition = new Vector2[numOfStars];
            Vector2 size = new Vector2(ConstantValues.SIZE_OF_STARS_ON_LEVEL_SUCCESS,
                ConstantValues.SIZE_OF_STARS_ON_LEVEL_SUCCESS);
            starsPosition =
                starsPosition.GetLocalPositions(ConstantValues.SPACING_BETWEEN_STARS_ON_LEVEL_SUCCESS, size, 0);
            for (int i = 0; i < _view.GetStarList().Length; i++)
            {
                _view.GetStarList()[i].gameObject.SetActive(i < numOfStars);

            }

            for (int i = 0; i < numOfStars; i++)
            {
                bool isOriginal = numOfRewardStars < numOfStars - i;
                _view.GetStarList()[i].SetLocalScale(Vector3.zero);
                _view.GetStarList()[i].SetLocalPosition(starsPosition[i]);
                _view.GetStarList()[i].SetSize(size);
                _view.GetStarList()[i].SetColor(isOriginal);
                _view.GetStarList()[i].GetMovingRewardItem().SetStatus(false);
                _view.GetStarParticleList()[i].transform.localPosition = starsPosition[i];
                _view.GetStarParticleList()[i].gameObject.SetActive(false);
                var mainModule = _view.GetStarParticleList()[i].main;
                mainModule.startColor = isOriginal ? ConstantValues.YELLOW_STAR_COLOR : ConstantValues.BLUE_STAR_COLOR;
            }
        }

        private void InitRewardItem(RewardType rewardType)
        {
            _view.GetRewardItem().rectTransform.localScale = Vector3.zero;
            _view.GetRewardItem().rectTransform.localPosition = Vector3.zero;
            Color color = Color.white;
            switch (rewardType)
            {
                case RewardType.Retrieval:
                    color = Color.blue;
                    break;
                case RewardType.Life:
                    color = Color.red;
                    break;
                case RewardType.Hint:
                    color = Color.yellow;
                    break;
            }

            _view.GetRewardItem().color = color;

        }

        private void InitButton(LevelFinishButtonType buttonType, Action onClick)
        {
            IFadeButtonView buttonView = _view.GetButton(buttonType);
            buttonView.Init(onClick);
            buttonView.SetButtonStatus(true);
            buttonView.SetAlpha(0f);
        }

        private void InitText(string text)
        {
            _view.SetText(text);
            _view.GetText().rectTransform.localScale = Vector3.zero;
        }

        private void SuccessLevelAnimation(List<int> targetCardIndexList, List<INormalCardItemController> cardItemList,
            bool isSuccess, int numOfStars, int newRewardStarCount, int currentRewardStarCount)
        {
            DOTween.Sequence()
                .AppendCallback(() => AnimateSuccessAnimation(cardItemList))
                .AppendInterval(1.2f + 0.3f * cardItemList.Count)
                .Append(_view.GetTopArea().DOFade(0f, 0.2f))
                .Join(_view.GetScrollArea().DOFade(0f, 0.2f))
                .Join(_view.GetBoardArea().DOFade(0f, 0.2f))
                .Join(_view.GetButtonArea().DOFade(0f, 0.2f))
                .Join(_view.GetBottomArea().DOFade(0f, 0.2f))
                .AppendCallback(() => _view.SetStatus(true))
                .Append(_fadePanelController.AnimateFade(0.9f, 0.5f))
                .Append(_levelTracker.GetLevelId() > 10
                    ? _circleProgressBarController.MoveCircleProgressBar(0.8f)
                    : DOTween.Sequence())
                .AppendInterval(0.2f)
                .Append(_view.GetText().rectTransform.DOScale(1f, 0.5f))
                .AppendInterval(0.2f)
                .Append(AnimateStarCreation(numOfStars, 0.1f, 0.5f)).Play()
                .AppendInterval(0.2f)
                .Append(_circleProgressBarController.AddNewStars(_view.GetStarList(), newRewardStarCount, numOfStars))
                .AppendInterval(0.2f)
                .Append(TryCreateReward(newRewardStarCount, currentRewardStarCount));
        }

        private void AnimateSuccessAnimation(List<INormalCardItemController> cardItemList)
        {
            for (int i = 0; i < cardItemList.Count; i++)
            {
                float delay = 0.3f * i;
                cardItemList[i].SuccessAnimation(delay);
            }
        }

        private void AnimateBackFlipCards(List<int> targetCardIndexList, List<INormalCardItemController> cardItemList,
            bool isSuccess)
        {
            for (int i = 0; i < cardItemList.Count; i++)
            {
                float delay = 0.3f * i;
                cardItemList[i].BackFlipAnimation(delay, isSuccess, targetCardIndexList[i].ToString());
            }
        }

        private Sequence AnimateStarCreation(int numOfStars, float durationBetweenParticleAndStar, float duration)
        {
            Sequence starCreationAnimation = DOTween.Sequence();
            for (int i = 0; i < numOfStars; i++)
            {
                IStarImageView starImageView = _view.GetStarList()[i];
                int index = i;
                float delay = durationBetweenParticleAndStar + duration * i;
                Action particleActivation = () => ActivateStarParticle(index);
                starCreationAnimation.Pause().Append(starImageView.GetRectTransform().transform.DOScale(1f, duration))
                    .InsertCallback(delay, particleActivation.Invoke);
            }

            return starCreationAnimation;
        }

        private void ActivateStarParticle(int index)
        {
            _view.GetStarParticleList()[index].gameObject.SetActive(true);
            _view.GetStarParticleList()[index].Play();
            _hapticController.Vibrate(HapticType.Success);
        }

        private Sequence TryCreateReward(int newRewardStarCount, int currentRewardStarCount)
        {
            if (newRewardStarCount + currentRewardStarCount < ConstantValues.NUM_OF_STARS_FOR_WILD)
            {
                return DOTween.Sequence().Append(AnimateButtons());
            }

            Action onClickClaim = () =>
            {
                _view.GetRewardItem().rectTransform.localScale = Vector3.zero;
                DOTween.Sequence().AppendInterval(0.2f)
                    .AppendCallback(() => _view.GetStarCanvasGroup().alpha = 1f)
                    .AppendCallback(() => _view.GetButton(LevelFinishButtonType.Claim).SetButtonStatus(false))
                    .Append(AnimateButtons());
            };

            InitButton(LevelFinishButtonType.Claim, onClickClaim);

            return DOTween.Sequence()
                .Append(DOTween.Sequence().AppendInterval(0.4f).SetEase(Ease.OutQuad))
                .Append(_view.GetRewardItem().rectTransform.DOScale(Vector3.one * 5 / 3f, 1.6f)).SetEase(Ease.OutQuad)
                .Join(DOTween.Sequence().AppendInterval(1f))
                .Append(DOTween.Sequence().Append(_view.GetRewardItem().rectTransform.DOLocalMoveY(-190f, 1f))
                    .OnComplete(() => _hapticController.Vibrate(HapticType.Success)))
                .Join(_view.GetText().DOFade(0f, 0.6f))
                .Join(_view.GetStarCanvasGroup().DOFade(0f, 0.6f))
                .Join(DOTween.Sequence().AppendCallback(() =>
                {
                    _view.GetRewardParticle().gameObject.SetActive(true);
                    _view.GetRewardParticle().Play();
                    _circleProgressBarController.SetStatus(false);
                    _circleProgressBarController.DestroyStarImages();
                    _circleProgressBarController.CreateInitialStarImages();
                }))
                .AppendInterval(0.2f)
                .Append(_view.GetButton(LevelFinishButtonType.Claim).GetCanvasGroup().DOFade(1f, 0.3f));
        }

        private Sequence AnimateButtons()
        {
            return DOTween.Sequence()
                .Append(_view.GetButton(LevelFinishButtonType.Game).GetCanvasGroup().DOFade(1f, 0.3f))
                .Join(_view.GetButton(LevelFinishButtonType.Menu).GetCanvasGroup().DOFade(1f, 0.3f));
        }

        private void SuccessMultiplayerLevel()
        {

        }

        private void FailMultiplayerLevel()
        {

        }
        
        public bool IsGameOver()
        {
            return _isGameOver;
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
        void Initialize();
        void MultiplayerLevelFinish(MultiplayerLevelFinishInfo multiplayerLevelFinishInfo);
        bool IsGameOver();
    }
}