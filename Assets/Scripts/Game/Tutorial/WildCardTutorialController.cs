using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Scripts
{
    public class WildCardTutorialController : IWildCardTutorialController
    {
        private IUnmaskServiceAreaView _unmaskServiceAreaView;
        private ITutorialMessagePopupView _tutorialMessagePopupView;
        private Queue<Action> _tutorialAnimationActions;

        public void Initialize(IUnmaskServiceAreaView unmaskServiceAreaView, ITutorialMessagePopupView tutorialMessagePopupView)
        {
            _unmaskServiceAreaView = unmaskServiceAreaView;
            _tutorialMessagePopupView = tutorialMessagePopupView;
            _unmaskServiceAreaView.InstantiateTutorialFade();
            _tutorialMessagePopupView.Init();
            InitializeTutorialAnimationActions();
        }

        private void InitializeTutorialAnimationActions()
        {
            _tutorialAnimationActions = new Queue<Action>();
            _tutorialMessagePopupView.SetText("You can drag the wild card to see correct number at any position.");
            AddTutorialAction(() => ExecuteNextTutorialActionWithDelay(5));
            ExecuteNextTutorialAction();
        }

        private void ExecuteNextTutorialActionWithDelay(float duration)
        {
            DOTween.Sequence().AppendInterval(duration).OnComplete(ExecuteNextTutorialAction);
        }
        
        private void ExecuteNextTutorialAction()
        {
            if (_tutorialAnimationActions.Count > 0)
            {
                _tutorialAnimationActions.Dequeue()?.Invoke();
            }
            else
            {
                _tutorialMessagePopupView.Destroy();
                _unmaskServiceAreaView.CloseTutorialFade();
                PlayerPrefs.SetInt("wild_card_tutorial_completed", 1);
            }
        }
        
        private void AddTutorialAction(Action action)
        {
            _tutorialAnimationActions.Enqueue(action);
        }
    }

    public interface IWildCardTutorialController
    {
        void Initialize(IUnmaskServiceAreaView unmaskServiceAreaView, ITutorialMessagePopupView tutorialMessagePopupView);
    }
}