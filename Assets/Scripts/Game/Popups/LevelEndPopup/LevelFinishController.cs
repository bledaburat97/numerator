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
        [Inject] IFadePanelController _fadePanelController;
        [Inject] private IHapticController _hapticController;
        [Inject] private ILevelTracker _levelTracker;
        [Inject] private IBoardAreaController _boardAreaController;
        [Inject] private IInitialCardAreaController _initialCardAreaController;
        [Inject] private IGameSaveService _gameSaveService;
        [Inject] private IGameInitializer _gameInitializer;
        
        private ILevelFinishPopupView _view;
        private ICircleProgressBarController _circleProgressBarController;
        
        private bool _isSuccess;
        private int _starCount;
        private int _newRewardStarCount;

        public LevelFinishController(ILevelFinishPopupView view)
        {
            _view = view;
        }
        
        public void LevelFinish(LevelFinishInfo levelFinishInfo)
        {
            _gameSaveService.DeleteSave();
            int[] finalCardIndexes = _boardAreaController.GetCardIndexesOnBoard();
            List<INormalCardItemController> cards = new List<INormalCardItemController>();
            for (int i = 0; i < finalCardIndexes.Length; i++)
            {
                if (finalCardIndexes[i] != -1)
                {
                    cards.Add(_initialCardAreaController.GetCardItemController(finalCardIndexes[i]));
                }
            }
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
            }, true);
            InitButton(LevelFinishButtonType.Menu, () => SceneManager.LoadScene("Menu"), true);
            if (levelFinishInfo.isSuccess)
            {
                _fadePanelController.SetFadeImageStatus(true);
                _fadePanelController.SetFadeImageAlpha(0f);
                InitText("Well Done!");
                int rewardStarCount = _levelTracker.GetGiftStarCount();
                RewardType rewardType = _levelTracker.GetCurrentRewardType();
                _levelTracker.IncrementLevelId(levelFinishInfo.starCount, levelFinishInfo.newRewardStarCount);
                CreateRewardCircle(rewardStarCount);
                InitStarsAndParticles(levelFinishInfo.starCount, levelFinishInfo.newRewardStarCount);
                InitRewardItem(rewardType);
                SuccessLevelAnimation(levelFinishInfo.targetNumbers, cards, true, levelFinishInfo.starCount, levelFinishInfo.newRewardStarCount, rewardStarCount);
            }
            else
            {
                InitText("Try Again!");
                _view.GetStarCanvasGroup().gameObject.SetActive(false);
                _view.GetRewardItem().gameObject.SetActive(false);
                _view.GetCircleProgressBar().GetRectTransform().gameObject.SetActive(false);
                FailLevelAnimation(levelFinishInfo.targetNumbers, cards);
            }
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
            _circleProgressBarController = new CircleProgressBarController();
            _circleProgressBarController.Initialize(_view.GetCircleProgressBar(), _hapticController, rewardStarCount);
            _circleProgressBarController.CreateInitialStarImages();
        }
        
        private void InitStarsAndParticles(int numOfStars, int numOfRewardStars)
        {
            Vector2[] starsPosition = new Vector2[numOfStars];
            Vector2 size = new Vector2(ConstantValues.SIZE_OF_STARS_ON_LEVEL_SUCCESS,
                ConstantValues.SIZE_OF_STARS_ON_LEVEL_SUCCESS);
            starsPosition = starsPosition.GetLocalPositions(ConstantValues.SPACING_BETWEEN_STARS_ON_LEVEL_SUCCESS, size, 0);
            for(int i = numOfStars; i < _view.GetStarList().Length; i++)
            {
                _view.GetStarList()[i].gameObject.SetActive(false);
            }
            
            for (int i = 0; i < numOfStars; i++)
            {
                bool isOriginal = numOfRewardStars < numOfStars - i;
                _view.GetStarList()[i].SetLocalScale(Vector3.zero);
                _view.GetStarList()[i].SetLocalPosition(starsPosition[i]);
                _view.GetStarList()[i].SetSize(size);
                _view.GetStarList()[i].SetColor(isOriginal);
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
        
        private void InitButton(LevelFinishButtonType buttonType, Action onClick, bool isSuccess)
        {
            IFadeButtonView buttonView = _view.GetButton(buttonType);
            if (buttonType == LevelFinishButtonType.Game)
            {
                if(isSuccess) buttonView.SetText("Level " + (_levelTracker.GetLevelId() + 1));
                else buttonView.SetText("Retry");
            }
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
                .AppendCallback(() => AnimateBackFlipCards(targetCardIndexList, cardItemList, isSuccess))
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

        private void AnimateBackFlipCards(List<int> targetCardIndexList, List<INormalCardItemController> cardItemList, bool isSuccess)
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
            
            InitButton(LevelFinishButtonType.Claim, onClickClaim, true);
            

            return DOTween.Sequence()
                .Append(DOTween.Sequence().AppendInterval(0.4f).SetEase(Ease.OutQuad))
                .Append(_view.GetRewardItem().rectTransform.DOScale(Vector3.one * 5 / 3f, 1.6f)).SetEase(Ease.OutQuad)
                .Join(DOTween.Sequence().AppendInterval(1f))
                .Append(DOTween.Sequence().Append(_view.GetRewardItem().rectTransform.DOLocalMoveY(-190f, 1f)).OnComplete(() => _hapticController.Vibrate(HapticType.Success)))
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
        
        private void FailLevel()
        {
            
        }

        private void SuccessMultiplayerLevel()
        {
            
        }

        private void FailMultiplayerLevel()
        {
            
        }
        
    }

    public struct LevelFinishInfo
    {
        public bool isSuccess;
        public int starCount;
        public int newRewardStarCount;
        public List<int> targetNumbers;
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
        void LevelFinish(LevelFinishInfo levelFinishInfo);
        void MultiplayerLevelFinish(MultiplayerLevelFinishInfo multiplayerLevelFinishInfo);
    }
}