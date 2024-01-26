﻿using Menu;
using UnityEngine;

namespace Scripts
{
    public class LevelSelectionTableView : MonoBehaviour, ILevelSelectionTableView
    {
        [SerializeField] private LevelButtonView levelButtonPrefab;
        private LevelButtonViewFactory _levelButtonViewFactory;
        private BaseButtonViewFactory _baseButtonViewFactory;
        [SerializeField] private BaseButtonView forwardButtonPrefab;
        [SerializeField] private BaseButtonView backwardButtonPrefab;
        public void Init(LevelButtonViewFactory levelButtonViewFactory, BaseButtonViewFactory baseButtonViewFactory)
        {
            _levelButtonViewFactory = levelButtonViewFactory;
            _baseButtonViewFactory = baseButtonViewFactory;
        }

        public ILevelButtonView CreateLevelButtonView()
        {
            return _levelButtonViewFactory.Spawn(transform, levelButtonPrefab);
        }

        public IBaseButtonView CreateForwardButton()
        {
            return _baseButtonViewFactory.Spawn(transform, forwardButtonPrefab);
        }
        
        public IBaseButtonView CreateBackwardButton()
        {
            return _baseButtonViewFactory.Spawn(transform, backwardButtonPrefab);
        }

    }

    public interface ILevelSelectionTableView
    {
        void Init(LevelButtonViewFactory levelButtonViewFactory, BaseButtonViewFactory baseButtonViewFactory);
        ILevelButtonView CreateLevelButtonView();
        IBaseButtonView CreateForwardButton();
        IBaseButtonView CreateBackwardButton();
    }
}