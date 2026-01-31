using UnityEngine;

namespace Game
{
    public class SizeManager : ISizeManager
    {
        private float _sizeRatio;
        private const int MaxBoardHolderCount = 5;
        private const float ExtraSpacingToBoardHolderWidthRatio = 10 / 70f;

        public void SetSizeRatio(Vector2 sizeOfTheBoardArea, Vector2 sizeOfTheBoardHolder, float wagonSpacingToBoardHolderWidthRatio)
        {
            float idealBoardHolderAreaWidthToHeightRatio =
                (MaxBoardHolderCount + wagonSpacingToBoardHolderWidthRatio * (MaxBoardHolderCount - 1)
                                     + ExtraSpacingToBoardHolderWidthRatio) * sizeOfTheBoardHolder.x / sizeOfTheBoardHolder.y;
            float boardAreaRatio = sizeOfTheBoardArea.x / sizeOfTheBoardArea.y;
    
            if (idealBoardHolderAreaWidthToHeightRatio > boardAreaRatio)
            {
                float height = sizeOfTheBoardArea.x / idealBoardHolderAreaWidthToHeightRatio;
                _sizeRatio = height / sizeOfTheBoardHolder.y;
            }
            else
            {
                _sizeRatio = sizeOfTheBoardArea.y / sizeOfTheBoardHolder.y;
            }
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