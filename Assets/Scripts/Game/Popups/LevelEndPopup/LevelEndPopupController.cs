using System;
using DG.Tweening;
using Scripts;
using TMPro;
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
            ICircleProgressBarView circleProgressBarView = _view.GetCircleProgressBar();
            _circleProgressBarController = circleProgressBarView == null
                ? null
                : new CircleProgressBarController(circleProgressBarView, _hapticController);
        }

        public void SetAllStatusFalse()
        {
            TMP_Text text = _view.GetText();
            if (text != null)
            {
                text.gameObject.SetActive(false);
                text.alpha = 1f;
            }

            CanvasGroup starCanvasGroup = _view.GetStarCanvasGroup();
            if (starCanvasGroup != null)
            {
                starCanvasGroup.gameObject.SetActive(false);
                starCanvasGroup.alpha = 1f;
            }

            RectTransform rewardItemHolder = _view.GetRewardItemHolder();
            if (rewardItemHolder != null)
            {
                rewardItemHolder.gameObject.SetActive(false);
                rewardItemHolder.localScale = Vector3.zero;
            }

            RectTransform[] rewardItemList = _view.GetRewardItemList();
            if (rewardItemList != null)
            {
                foreach (RectTransform rewardItem in rewardItemList)
                {
                    if (rewardItem != null)
                    {
                        rewardItem.gameObject.SetActive(false);
                    }
                }
            }

            ParticleSystem rewardParticle = _view.GetRewardParticle();
            if (rewardParticle != null)
            {
                rewardParticle.gameObject.SetActive(false);
            }

            ICircleProgressBarView circleProgressBar = _view.GetCircleProgressBar();
            RectTransform circleProgressRectTransform = circleProgressBar?.GetRectTransform();
            if (circleProgressRectTransform != null)
            {
                circleProgressRectTransform.gameObject.SetActive(false);
            }

            IVerticalCrystalProgressView verticalCrystalProgressView = _view.GetVerticalCrystalProgressView();
            if (verticalCrystalProgressView != null)
            {
                verticalCrystalProgressView.SetAlpha(0f);
                verticalCrystalProgressView.ResetFill();
                verticalCrystalProgressView.SetStatus(false);
            }

            SetButtonStatus(LevelFinishButtonType.Game, false);
            SetButtonStatus(LevelFinishButtonType.Menu, false);
            SetButtonStatus(LevelFinishButtonType.Claim, false);
        }
        
        public void InitButton(LevelFinishButtonType buttonType, string text, Action onClick)
        {
            IFadeButtonView buttonView = _view.GetButton(buttonType);
            if (buttonView == null) return;

            buttonView.SetButtonStatus(true);
            buttonView.Init(onClick);
            buttonView.SetAlpha(0f);
            buttonView.SetText(text);
        }

        public void InitText(string text)
        {
            TMP_Text titleText = _view.GetText();
            if (titleText == null) return;

            titleText.gameObject.SetActive(true);
            titleText.alpha = 1f;
            _view.SetText(text);
            titleText.rectTransform.localScale = Vector3.zero;
        }
        
        public void CreateRewardCircle(int rewardStarCount, RewardType rewardType)
        {
            if (_circleProgressBarController == null || _view.GetCircleProgressBar() == null) return;

            _view.GetCircleProgressBar().GetRectTransform().gameObject.SetActive(true);
            _view.GetCircleProgressBar().ShowRewardPreview(rewardType);
            _circleProgressBarController.Initialize(rewardStarCount);
            _circleProgressBarController.CreateInitialStarImages();
        }
        
        public void InitStarsAndParticles(int numOfStars, int numOfRewardStars)
        {
            CanvasGroup starCanvasGroup = _view.GetStarCanvasGroup();
            StarImageView[] starList = _view.GetStarList();
            ParticleSystem[] starParticleList = _view.GetStarParticleList();
            if (starCanvasGroup == null || starList == null || starParticleList == null) return;

            starCanvasGroup.gameObject.SetActive(true);
            starCanvasGroup.alpha = 1f;
            Vector2[] starsPosition = new Vector2[numOfStars];
            Vector2 size = new Vector2(ConstantValues.SIZE_OF_STARS_ON_LEVEL_SUCCESS,
                ConstantValues.SIZE_OF_STARS_ON_LEVEL_SUCCESS);
            starsPosition =
                starsPosition.GetLocalPositions(ConstantValues.SPACING_BETWEEN_STARS_ON_LEVEL_SUCCESS, size, 0);
            for (int i = 0; i < starList.Length; i++)
            {
                starList[i].gameObject.SetActive(i < numOfStars);
            }

            for (int i = 0; i < numOfStars && i < starList.Length && i < starParticleList.Length; i++)
            {
                starList[i].SetLocalScale(Vector3.zero);
                starList[i].SetLocalPosition(starsPosition[i]);
                starList[i].SetSize(size);
                starList[i].SetColor(i >= numOfStars - numOfRewardStars ? false : true);
                starParticleList[i].transform.localPosition = starsPosition[i];
                starParticleList[i].gameObject.SetActive(false);
                var mainModule = starParticleList[i].main;
                mainModule.startColor = i >= numOfStars - numOfRewardStars
                    ? ConstantValues.BLUE_STAR_COLOR
                    : ConstantValues.YELLOW_STAR_COLOR;
            }
        }
        
        public void InitRewardItem(RewardType rewardType)
        {
            RectTransform rewardItemHolder = _view.GetRewardItemHolder();
            RectTransform[] rewardItemList = _view.GetRewardItemList();
            if (rewardItemHolder == null || rewardItemList == null) return;

            rewardItemHolder.gameObject.SetActive(true);
            rewardItemHolder.localScale = Vector3.zero;
            rewardItemHolder.localPosition = Vector3.zero;
            switch (rewardType)
            {
                case RewardType.Revealing:
                    SetRewardItemStatus(rewardItemList, 0);
                    break;
                case RewardType.Life:
                    SetRewardItemStatus(rewardItemList, 1);
                    break;
                case RewardType.Bomb:
                    SetRewardItemStatus(rewardItemList, 2);
                    break;
            }

        }
        
        public Sequence AnimateStarCreation(int numOfStars, float durationBetweenParticleAndStar, float duration)
        {
            Sequence starCreationAnimation = DOTween.Sequence();
            StarImageView[] starList = _view.GetStarList();
            if (starList == null) return starCreationAnimation;

            for (int i = 0; i < numOfStars && i < starList.Length; i++)
            {
                IStarImageView starImageView = starList[i];
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
            ParticleSystem[] starParticleList = _view.GetStarParticleList();
            if (starParticleList == null || index < 0 || index >= starParticleList.Length) return;

            starParticleList[index].gameObject.SetActive(true);
            starParticleList[index].Play();
            _hapticController?.Vibrate(HapticType.Success);
        }

        public Sequence TryCreateReward(int newRewardStarCount, int currentRewardStarCount)
        {
            float buttonFadeDuration = 0.3f;
            if (newRewardStarCount + currentRewardStarCount < ConstantValues.NUM_OF_STARS_FOR_WILD)
            {
                return DOTween.Sequence().Append(ChangeFadeButtons(buttonFadeDuration, 1f));
            }

            SetButtonStatus(LevelFinishButtonType.Game, false);
            SetButtonStatus(LevelFinishButtonType.Menu, false);

            Action onClickClaim = () =>
            {
                DOTween.Sequence()
                    .AppendCallback(() =>
                    {
                        SetButtonStatus(LevelFinishButtonType.Game, true);
                        SetButtonStatus(LevelFinishButtonType.Menu, true);
                        SetButtonAlpha(LevelFinishButtonType.Game, 0f);
                        SetButtonAlpha(LevelFinishButtonType.Menu, 0f);
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
                    _circleProgressBarController?.SetStatus(false);
                    _circleProgressBarController?.DestroyStarImages();
                    _circleProgressBarController?.CreateInitialStarImages();
                }))
                .AppendInterval(0.2f)
                .Append(_view.GetButton(LevelFinishButtonType.Claim).GetCanvasGroup().DOFade(1f, 0.3f));
        }

        public Sequence PresentRewardForClaim(
            Sequence rewardPresentationSequence,
            Func<Sequence> hideRewardSequenceFactory,
            float buttonFadeDuration)
        {
            IFadeButtonView claimButton = _view.GetButton(LevelFinishButtonType.Claim);
            if (claimButton == null)
            {
                return DOTween.Sequence()
                    .Append(rewardPresentationSequence ?? DOTween.Sequence())
                    .AppendInterval(0.2f)
                    .AppendCallback(() => hideRewardSequenceFactory?.Invoke())
                    .AppendInterval(buttonFadeDuration)
                    .Append(ChangeFadeButtons(buttonFadeDuration, 1f));
            }

            bool isClaiming = false;
            Action onClickClaim = () =>
            {
                if (isClaiming) return;

                isClaiming = true;
                Sequence hideRewardSequence = hideRewardSequenceFactory?.Invoke() ?? DOTween.Sequence();
                CanvasGroup claimCanvasGroup = claimButton.GetCanvasGroup();
                DOTween.Sequence()
                    .AppendCallback(() =>
                    {
                        SetButtonStatus(LevelFinishButtonType.Game, true);
                        SetButtonStatus(LevelFinishButtonType.Menu, true);
                        SetButtonAlpha(LevelFinishButtonType.Game, 0f);
                        SetButtonAlpha(LevelFinishButtonType.Menu, 0f);
                    })
                    .Append(claimCanvasGroup == null
                        ? DOTween.Sequence()
                        : claimCanvasGroup.DOFade(0f, buttonFadeDuration))
                    .Join(hideRewardSequence)
                    .AppendCallback(() => claimButton.SetButtonStatus(false))
                    .Append(ChangeFadeButtons(buttonFadeDuration, 1f));
            };

            CanvasGroup claimCanvasGroup = claimButton.GetCanvasGroup();

            return DOTween.Sequence()
                .AppendCallback(() =>
                {
                    SetButtonStatus(LevelFinishButtonType.Game, false);
                    SetButtonStatus(LevelFinishButtonType.Menu, false);
                    InitButton(LevelFinishButtonType.Claim, "Claim", onClickClaim);
                })
                .Append(rewardPresentationSequence ?? DOTween.Sequence())
                .Append(claimCanvasGroup == null
                    ? DOTween.Sequence()
                    : claimCanvasGroup.DOFade(1f, buttonFadeDuration));
        }

        public Sequence ChangeFadeButtons(float duration, float finalAlpha)
        {
            Sequence sequence = DOTween.Sequence();
            AppendButtonFade(sequence, LevelFinishButtonType.Game, duration, finalAlpha);
            AppendButtonFade(sequence, LevelFinishButtonType.Menu, duration, finalAlpha);
            return sequence;
        }

        public void SetPopupStatus(bool status)
        {
            _view.SetStatus(status);
        }

        public Sequence MoveCircleProgressBar(float duration)
        {
            return _circleProgressBarController == null
                ? DOTween.Sequence()
                : _circleProgressBarController.MoveCircleProgressBar(duration);
        }

        public Sequence ScaleUpText(float duration)
        {
            TMP_Text text = _view.GetText();
            return text == null
                ? DOTween.Sequence()
                : DOTween.Sequence().Append(text.rectTransform.DOScale(1f, duration));
        }

        public Sequence AddNewStarsToCircleProgressBar(int newRewardStarCount, int numOfStars)
        {
            return _circleProgressBarController == null
                ? DOTween.Sequence()
                : _circleProgressBarController.AddNewStars(_view.GetStarList(), newRewardStarCount, numOfStars);
        }

        private void SetButtonStatus(LevelFinishButtonType buttonType, bool status)
        {
            _view.GetButton(buttonType)?.SetButtonStatus(status);
        }

        private void SetButtonAlpha(LevelFinishButtonType buttonType, float alpha)
        {
            _view.GetButton(buttonType)?.SetAlpha(alpha);
        }

        private static void SetRewardItemStatus(RectTransform[] rewardItemList, int index)
        {
            if (index < 0 || index >= rewardItemList.Length || rewardItemList[index] == null) return;

            rewardItemList[index].gameObject.SetActive(true);
        }

        private void AppendButtonFade(Sequence sequence, LevelFinishButtonType buttonType, float duration, float finalAlpha)
        {
            IFadeButtonView buttonView = _view.GetButton(buttonType);
            if (buttonView == null) return;

            CanvasGroup canvasGroup = buttonView.GetCanvasGroup();
            if (canvasGroup == null) return;

            sequence.Join(canvasGroup.DOFade(finalAlpha, duration));
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
        Sequence PresentRewardForClaim(Sequence rewardPresentationSequence, Func<Sequence> hideRewardSequenceFactory,
            float buttonFadeDuration);
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
