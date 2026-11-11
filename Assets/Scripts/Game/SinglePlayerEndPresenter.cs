using System;
using DG.Tweening;
using Scripts;
using UnityEngine.SceneManagement;

namespace Game
{
    public sealed class SinglePlayerEndPresenter : IDisposable
    {
        private readonly IGamePopupCreator _popups;
        private readonly IGameUIController _ui;
        private readonly IResultAreaController _history;
        private readonly ITargetNumberCreator _target;
        private readonly ICardItemInfoPopupController _cardInfo;
        private readonly IVerticalCrystalProgressController _crystals;
        private readonly ILevelEndPopupController _popup;
        private readonly ILevelSuccessAnimationManager _success;
        private readonly ILevelFailAnimationManager _failure;
        private Sequence _ending;
        private int _generation;
        private bool _disposed;

        public SinglePlayerEndPresenter(IGamePopupCreator popups, IGameUIController ui,
            IResultAreaController history, ITargetNumberCreator target, ICardItemInfoPopupController cardInfo,
            IVerticalCrystalProgressController crystals, ILevelEndPopupController popup,
            ILevelSuccessAnimationManager success, ILevelFailAnimationManager failure)
        {
            _popups = popups;
            _ui = ui;
            _history = history;
            _target = target;
            _cardInfo = cardInfo;
            _crystals = crystals;
            _popup = popup;
            _success = success;
            _failure = failure;
        }

        public void PrepareRound()
        {
            _generation++;
            _ending?.Kill();
            _ending = null;
            _success.Cancel();
        }

        public void Present(SinglePlayerRoundResult result, ISinglePlayerProgression progression)
        {
            if (_disposed) return;
            PrepareRound();
            _ui.SetAllButtonsUnclickable();
            _history.RevealAnswer(_target.GetHistoryTargetCards());
            _cardInfo.ClearCardHolderIndicatorButtons();
            _popup.SetAllStatusFalse();
            if (result.EarnedCrystals > 0)
                _crystals.PrepareForCollection(result.PreviousCrystalProgress, result.PreviousReward);
            _popup.SetOutcome(result.Won, result.EarnedCrystals);
            _popup.SetContinuationIdentity(result.Won?LevelDefinitionPreview.Resolve(result.NextLevelNumber-1):result.CurrentDefinition);
            var goal=result.AchievementTarget;
            _popup.SetAchievementTarget(goal==null?null:string.Format(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"),"{0}: {1:N0}/{2:N0}",goal.Title,goal.Progress,goal.Target));
            if(result.Won)_popup.PrepareRewardDetails(result.EarnedFruits);
            _popup.SetPopupStatus(!result.Won);
            _popup.InitButton(LevelFinishButtonType.Game,
                result.Won ? "Bölüm " + result.NextLevelNumber : "Tekrar oyna",
                result.Won ? (Action)progression.GoToNextLevel : progression.Retry);
            _popup.InitButton(LevelFinishButtonType.Menu, "Menü", () => SceneManager.LoadScene("Menu"));
            _popup.InitText(result.Won ? "Çözdün!" : "Tekrar dene");
            if (!result.Won)
            {
                _ending = _failure.FailLevelAnimation();
                return;
            }

            int generation = _generation;
            _ending = _success.SuccessLevelAnimation(result.EarnedCoins, result.EarnedCrystals,
                result.PreviousCoins, result.PreviousCrystalProgress, result.PreviousReward,result.DirectReward,result.Crystals);
            _ending.OnComplete(() =>
            {
                if (!_disposed && generation == _generation)
                    _popup.AfterRewardPresentation(()=>{ if(!_disposed && generation==_generation)_popups.ShowRewardTutorials(result.EarnedCoins, result.EarnedCrystals); });
            });
        }

        public void PresentPendingReward(RewardSaveState state,Action retry,bool won=true)
        {
            _ui.SetAllButtonsUnclickable();_history.RevealAnswer(_target.GetHistoryTargetCards());
            _popup.SetAllStatusFalse();_popup.SetOutcome(won);_popup.SetPopupStatus(true);_popup.InitText(won?"Çözdün!":"Tekrar dene");
            _popup.SetSaveStatus(state==RewardSaveState.Saving?"Sonuç kaydediliyor…":won?"Ödül kaydedilemedi.":"Sonuç kaydedilemedi.");
            if(state==RewardSaveState.RetryRequired)_popup.InitButton(LevelFinishButtonType.Game,"Tekrar kaydet",retry);
            _popup.ChangeFadeButtons(0,1).Complete(true);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            PrepareRound();
        }
    }
}
