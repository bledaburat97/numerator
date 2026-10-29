using System.Collections.Generic;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class InitialCardAreaLayoutManager : IInitialCardAreaLayoutManager
    {
        private const float SpacingToInitialHolderWidthRatio = 2f / 54f;
        private const float SpacingToHolderIndicatorWidthRatio = 1f / 11f;
        private const int MaxCardsPerRow = 5;

        private readonly IInitialCardAreaView _view;
        private readonly ISizeManager _sizeManager;

        [Inject]
        public InitialCardAreaLayoutManager(IInitialCardAreaView view, ISizeManager sizeManager)
        {
            _view = view;
            _sizeManager = sizeManager;
        }

        public List<Vector2> GetHolderIndicatorLocalPositions(int numOfBoardHolders)
        {
            List<Vector2> holderIndicatorLocalPositions = new List<Vector2>();
            Vector2 holderIndicatorSize = GetHolderIndicatorSize();
            float spacing = holderIndicatorSize.x * SpacingToHolderIndicatorWidthRatio;
            return holderIndicatorLocalPositions.GetLocalPositionList(numOfBoardHolders, spacing, holderIndicatorSize, 0f);
        }

        public List<Vector2> GetInitialHolderLocalPositions(int holderCount)
        {
            List<Vector2> localPositions = new List<Vector2>();
            Vector2 initialHolderSize = GetInitialHolderSize();
            float spacing = initialHolderSize.x * SpacingToInitialHolderWidthRatio;
            return localPositions.GetLocalPositionList(holderCount, spacing, initialHolderSize, 0f);
        }

        public Vector2 GetInitialHolderSize()
        {
            return GetTraySizeRatio() * _view.GetSizeOfInitialHolderPrefab();
        }

        public Vector2 GetHolderIndicatorSize()
        {
            return GetTraySizeRatio() * _view.GetSizeOfHolderIndicatorPrefab();
        }

        public Vector2 GetBoxSize()
        {
            return GetTraySizeRatio() * _view.GetSizeOfBoxPrefab();
        }

        private float GetTraySizeRatio()
        {
            Vector2 available = _view.GetRectTransform().rect.size;
            Vector2 holder = _view.GetSizeOfInitialHolderPrefab();
            float rowWidth = holder.x * (MaxCardsPerRow + SpacingToInitialHolderWidthRatio * (MaxCardsPerRow - 1));
            // Keep both fruit rows between the side buttons, including on short screens.
            return Mathf.Max(0.01f, Mathf.Min(_sizeManager.GetSizeRatio(),
                Mathf.Min(available.x / rowWidth, available.y / (holder.y * 2f))));
        }
    }

    public interface IInitialCardAreaLayoutManager
    {
        List<Vector2> GetHolderIndicatorLocalPositions(int numOfBoardHolders);
        List<Vector2> GetInitialHolderLocalPositions(int holderCount);
        Vector2 GetInitialHolderSize();
        Vector2 GetHolderIndicatorSize();
        Vector2 GetBoxSize();
    }
}
