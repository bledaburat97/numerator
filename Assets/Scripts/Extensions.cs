using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public static class Extensions
    {
        public static Vector2[] GetLocalPositions(this Vector2[] localPositions, float spacing, Vector2 size, float constantYPos)
        {
            for (int i = 0; i < localPositions.Length; i++)
            {
                float localXPos = ((float)(2 * i - localPositions.Length + 1) / 2) * (size.x + spacing);
                localPositions[i] = new Vector2(localXPos, constantYPos);
            }

            return localPositions;
        }
        
        public static List<Vector2> GetLocalPositionList(this List<Vector2> localPositions, int count, float spacing, Vector2 size, float constantYPos)
        {
            for (int i = 0; i < count; i++)
            {
                float localXPos = ((float)(2 * i - count + 1) / 2) * (size.x + spacing);
                localPositions.Add(new Vector2(localXPos, constantYPos));
            }

            return localPositions;
        }
    }
}