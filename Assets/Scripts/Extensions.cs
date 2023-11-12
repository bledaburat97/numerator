using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public static class Extensions
    {
        public static List<Vector2> GetLocalPositionList(this List<Vector2> localPositionList, int number, float spacing, Vector2 size)
        {
            for (int i = 0; i < number; i++)
            {
                float localXPos = ((float)(2 * i - number + 1) / 2) * (size.x + spacing);
                localPositionList.Add(new Vector2(localXPos, 0));
            }

            return localPositionList;
        }
    }
}