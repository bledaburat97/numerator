using UnityEngine;

namespace Game
{
    public class SizeManager : ISizeManager
    {
        private float _sizeRatio;
        private const int MaxBoardHolderCount = 5;

        public void SetSizeRatio(Vector2 sizeOfTheBoardArea, Vector2 sizeOfTheBoardHolder, float wagonSpacingToBoardHolderWidthRatio)
        {
            float requiredWidth = sizeOfTheBoardHolder.x *
                (MaxBoardHolderCount + wagonSpacingToBoardHolderWidthRatio * (MaxBoardHolderCount - 1));
            _sizeRatio = Mathf.Max(0.01f, Mathf.Min(
                sizeOfTheBoardArea.x / requiredWidth,
                sizeOfTheBoardArea.y / sizeOfTheBoardHolder.y));
        }

        public float GetSizeRatio()
        {
            if (_sizeRatio == 0)
            {
                Debug.LogError("SizeRatio is not set");
                return 1;
            }
            
            return _sizeRatio;
        }
    }

    public interface ISizeManager
    {
        void SetSizeRatio(Vector2 sizeOfTheBoardArea, Vector2 sizeOfTheBoardHolder, float wagonSpacingToBoardHolderWidthRatio);
        float GetSizeRatio();
    }
    
    
}
