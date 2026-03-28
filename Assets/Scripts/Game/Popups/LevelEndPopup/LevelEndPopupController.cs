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

        public void SetAllStatusFalse()
        {
            _view.GetText().gameObject.SetActive(false);
            _view.GetText().alpha = 1f;
            _view.GetStarCanvasGroup().gameObject.SetActive(false);
            _view.GetStarCanvasGroup().alpha = 1f;
            _view.GetRewardItemHolder().gameObject.SetActive(false);
            _view.GetRewardItemHolder().localScale = Vector3.zero;
            foreach (RectTransform rewardItem in _view.GetRewardItemList())
            {
                rewardItem.gameObject.SetActive(false);
            }

            _view.GetRewardParticle().gameObject.SetActive(false);
            _view.GetCircleProgressBar().GetRectTransform().gameObject.SetActive(false);
            _view.GetButton(LevelFinishButtonType.Game).SetButtonStatus(false);
            _view.GetButton(LevelFinishButtonType.Menu).SetButtonStatus(false);
            _view.GetButton(LevelFinishButtonType.Claim).SetButtonStatus(false);
        }
        
        public void InitButton(LevelFinishButtonType buttonType, string text, Action onClick)
        {
            _view.GetButton(buttonType).SetButtonStatus(true);
            IFadeButtonView buttonView = _view.GetButton(buttonType);
            buttonView.Init(onClick);
            buttonView.SetAlpha(0f);
            buttonView.SetText(text);
        }

        public void InitText(string text)
        {
            _view.GetText().gameObject.SetActive(true);
            _view.GetText().alpha = 1f;
            _view.SetText(text);
            _view.GetText().rectTransform.localScale = Vector3.zero;
        }
        
        public void CreateRewardCircle(int rewardStarCount, RewardType rewardType)
        {
            _view.GetCircleProgressBar().GetRectTransform().gameObject.SetActive(true);
            _view.GetCircleProgressBar().ShowRewardPreview(rewardType);
            _circleProgressBarController.Initialize(rewardStarCount);
            _circleProgressBarController.CreateInitialStarImages();
        }
        
        public void InitStarsAndParticles(int numOfStars, int numOfRewardStars)
        {
            _view.GetStarCanvasGroup().gameObject.SetActive(true);
            _view.GetStarCanvasGroup().alpha = 1f;
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
                _view.GetStarList()[i].SetLocalScale(Vector3.zero);
                _view.GetStarList()[i].SetLocalPosition(starsPosition[i]);
                _view.GetStarList()[i].SetSize(size);
                _view.GetStarList()[i].SetColor(i >= numOfStars - numOfRewardStars ? false : true);
                _view.GetStarParticleList()[i].transform.localPosition = starsPosition[i];
                _view.GetStarParticleList()[i].gameObject.SetActive(false);
                var mainModule = _view.GetStarParticleList()[i].main;
                mainModule.startColor = i >= numOfStars - numOfRewardStars
                    ? ConstantValues.BLUE_STAR_COLOR
                    : ConstantValues.YELLOW_STAR_COLOR;
            }
        }
        
        public void InitRewardItem(RewardType rewardType)
        {
            _view.GetRewardItemHolder().gameObject.SetActive(true);
            _view.GetRewardItemHolder().localScale = Vector3.zero;
            _view.GetRewardItemHolder().localPosition = Vector3.zero;
            switch (rewardType)
            {
                case RewardType.Revealing:
                    _view.GetRewardItemList()[0].gameObject.SetActive(true);
                    break;
                case RewardType.Life:
                    _view.GetRewardItemList()[1].gameObject.SetActive(true);
                    break;
                case RewardType.Bomb:
                    _view.GetRewardItemList()[2].gameObject.SetActive(true);
                    break;
            }

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
            float buttonFadeDuration = 0.3f;
            if (newRewardStarCount + currentRewardStarCount < ConstantValues.NUM_OF_STARS_FOR_WILD)
            {
                return DOTween.Sequence().Append(ChangeFadeButtons(buttonFadeDuration, 1f));
            }

            _view.GetButton(LevelFinishButtonType.Game).SetButtonStatus(false);
            _view.GetButton(LevelFinishButtonType.Menu).SetButtonStatus(false);

            Action onClickClaim = () =>
            {
                DOTween.Sequence()
                    .AppendCallback(() =>
                    {
                        _view.GetButton(LevelFinishButtonType.Game).SetButtonStatus(true);
                        _view.GetButton(LevelFinishButtonType.Menu).SetButtonStatus(true);
                        _view.GetButton(LevelFinishButtonType.Game).SetAlpha(0f);
                        _view.GetButton(LevelFinishButtonType.Menu).SetAlpha(0f);
                    })
                    .Append(_view.GetButton(LevelFinishButtonType.Claim).GetCanvasGroup().DOFade(0f, buttonFadeDuration))
                    .Join(_view.GetRewardItemHolder().DOScale(Vector3.zero, buttonFadeDuration))
                    .AppendCallback(() => _view.GetButton(LevelFinishButtonType.Claim).SetButtonStatus(false))
                    .Append(ChangeFadeButtons(buttonFadeDuration, 1f));
            };

            InitButton(LevelFinishButtonType.Claim, "Claim", onClickClaim);

            return DOTween.Sequence()
                .Append(DOTween.Sequence().AppendInterval(0.4f).SetEase(Ease.OutQuad))
                .Append(_view.GetRewardItemHolder().DOScale(Vector3.one * 5 / 3f, 1.6f)).SetEase(Ease.OutQuad)
                .Join(DOTween.Sequence().AppendInterval(1f))
                .Append(DOTween.Sequence().Append(_view.GetRewardItemHolder().DOLocalMoveY(-190f, 1f))
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

        public Sequence ChangeFadeButtons(float duration, float finalAlpha)
        {
            return DOTween.Sequence()
                .Append(_view.GetButton(LevelFinishButtonType.Game).GetCanvasGroup().DOFade(finalAlpha, duration))
                .Join(_view.GetButton(LevelFinishButtonType.Menu).GetCanvasGroup().DOFade(finalAlpha, duration));
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
        void InitButton(LevelFinishButtonType buttonType, string text, Action onClick);
        void InitText(string text);
        void CreateRewardCircle(int rewardStarCount, RewardType rewardType);
        void InitStarsAndParticles(int numOfStars, int numOfRewardStars);
        void InitRewardItem(RewardType rewardType);
        void SetPopupStatus(bool status);
        Sequence MoveCircleProgressBar(float duration);
        Sequence ScaleUpText(float duration);
        Sequence AnimateStarCreation(int numOfStars, float durationBetweenParticleAndStar, float duration);
        Sequence AddNewStarsToCircleProgressBar(int newRewardStarCount, int numOfStars);
        Sequence TryCreateReward(int newRewardStarCount, int currentRewardStarCount);
        Sequence ChangeFadeButtons(float duration, float finalAlpha);
        void SetAllStatusFalse();
    }
    
    public enum RewardType
    {
        Revealing,
        Life,
        Bomb
    }
}
