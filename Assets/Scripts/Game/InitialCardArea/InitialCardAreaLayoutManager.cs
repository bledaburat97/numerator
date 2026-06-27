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
            return _sizeManager.GetSizeRatio() * _view.GetSizeOfInitialHolderPrefab();
        }

        public Vector2 GetHolderIndicatorSize()
        {
            return _sizeManager.GetSizeRatio() * _view.GetSizeOfHolderIndicatorPrefab();
        }

        public Vector2 GetBoxSize()
        {
            return _sizeManager.GetSizeRatio() * _view.GetSizeOfBoxPrefab();
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
