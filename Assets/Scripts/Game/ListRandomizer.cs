using System.Collections.Generic;
using Random = System.Random;

namespace Game
{
    public static class ListRandomizer
    {
        public static void Randomize<T>(List<T> list)
        {
            Random random = new Random();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}