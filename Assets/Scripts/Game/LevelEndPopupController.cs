using System;
using DG.Tweening;
using Scripts;
using UnityEngine;
using Zenject;

namespace Game
{
    public class LevelEndPopupController : ILevelEndPopupController
    {
        private LevelFinishPopupView _view;
        private IHapticController _hapticController;
        private ICircleProgressBarController _circleProgressBarController;

        [Inject]
        public LevelEndPopupController(LevelFinishPopupView view, IHapticController hapticController)
        {
            _view = view;
            _hapticController = hapticController;
            _circleProgressBarController =
                new CircleProgressBarController(_view.GetCircleProgressBar(), _hapticController);
        }
        
        public void InitButton(LevelFinishButtonType buttonType, Action onClick)
        {
            IFadeButtonView buttonView = _view.GetButton(buttonType);
            buttonView.Init(onClick);
            buttonView.SetAlpha(0f);
        }

        public void SetGameButtonText(string text)
        {
            _view.GetButton(LevelFinishButtonType.Game).SetText(text);
        }

        public void InitText(string text)
        {
            _view.SetText(text);
            _view.GetText().rectTransform.localScale = Vector3.zero;
        }
        
        public void CreateRewardCircle(int rewardStarCount)
        {
            _circleProgressBarController.Initialize(rewardStarCount);
            _circleProgressBarController.CreateInitialStarImages();
        }
        
        public void InitStarsAndParticles(int numOfStars, int numOfRewardStars)
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
        
        public void InitRewardItem(RewardType rewardType)
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
        
        public Sequence AnimateStarCreation(int numOfStars, float durationBetweenParticleAndStar, float duration)
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

        public Sequence TryCreateReward(int newRewardStarCount, int currentRewardStarCount)
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

        public void SetPopupStatus(bool status)
        {
            _view.SetStatus(status);
        }

        public Sequence MoveCircleProgressBar(float duration)
        {
            return _circleProgressBarController.MoveCircleProgressBar(duration);
        }

        public Sequence ScaleUpText(float duration)
        {
            return DOTween.Sequence().Append(_view.GetText().rectTransform.DOScale(1f, duration));
        }

        public Sequence AddNewStarsToCircleProgressBar(int newRewardStarCount, int numOfStars)
        {
            return _circleProgressBarController.AddNewStars(_view.GetStarList(), newRewardStarCount, numOfStars);
        }
    }

    public interface ILevelEndPopupController
    {
        void InitButton(LevelFinishButtonType buttonType, Action onClick);
        void SetGameButtonText(string text);
        void InitText(string text);
        void CreateRewardCircle(int rewardStarCount);
        void InitStarsAndParticles(int numOfStars, int numOfRewardStars);
        void InitRewardItem(RewardType rewardType);
        void SetPopupStatus(bool status);
        Sequence MoveCircleProgressBar(float duration);
        Sequence ScaleUpText(float duration);
        Sequence AnimateStarCreation(int numOfStars, float durationBetweenParticleAndStar, float duration);
        Sequence AddNewStarsToCircleProgressBar(int newRewardStarCount, int numOfStars);
        Sequence TryCreateReward(int newRewardStarCount, int currentRewardStarCount);
    }
}