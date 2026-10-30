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

        [Inject]
        public LevelEndPopupController(LevelFinishPopupView view)
        {
            _view = view;
        }

        public void SetAllStatusFalse()
        {
            TMP_Text text = _view.GetText();
            if (text != null)
            {
                text.gameObject.SetActive(false);
                text.alpha = 1f;
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

        public void SetMultiplayerOpponentLeft()
        {
            SetButtonStatus(LevelFinishButtonType.Game, false); SetButtonStatus(LevelFinishButtonType.Claim, false);
            _view.SetMultiplayerOpponentLeft();
        }
        public void SetOutcome(bool success, int earnedCrystalCount = 0) => _view.SetOutcome(success, earnedCrystalCount);
        public void SetMultiplayerResult(bool won, bool disconnected, RewardType? reward) => _view.SetMultiplayerResult(won, disconnected, reward);
        
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
                    _view.SetRewardPresentation();
                    InitButton(LevelFinishButtonType.Claim, "Ödülü al", onClickClaim);
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

        public Sequence ScaleUpText(float duration)
        {
            TMP_Text text = _view.GetText();
            return text == null
                ? DOTween.Sequence()
                : DOTween.Sequence().Append(text.rectTransform.DOScale(1f, duration));
        }

        private void SetButtonStatus(LevelFinishButtonType buttonType, bool status)
        {
            _view.GetButton(buttonType)?.SetButtonStatus(status);
        }

        private void SetButtonAlpha(LevelFinishButtonType buttonType, float alpha)
        {
            _view.GetButton(buttonType)?.SetAlpha(alpha);
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
        void SetOutcome(bool success, int earnedCrystalCount = 0);
        void SetPopupStatus(bool status);
        Sequence ScaleUpText(float duration);
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
