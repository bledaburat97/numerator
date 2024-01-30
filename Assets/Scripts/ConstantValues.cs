using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public static class ConstantValues
    {
        public static float SPACING_BETWEEN_BOARD_CARDS = 8f;
        public static float BOARD_CARD_HOLDER_WIDTH = 64f;
        public static float BOARD_CARD_HOLDER_HEIGHT = 80f;
        
        public static float SPACING_BETWEEN_INITIAL_CARDS = 10f;
        public static float INITIAL_CARD_HOLDER_WIDTH = 52f;
        public static float INITIAL_CARD_HOLDER_HEIGHT = 65f;
        
        

        public static float RESULT_CARD_WIDTH = 32f;
        public static float RESULT_CARD_HEIGHT = 40f;
        public static float POSSIBLE_HOLDER_INDICATOR_WIDTH = 10f;
        public static float POSSIBLE_HOLDER_INDICATOR_HEIGHT = 16f;
        public static List<string> HOLDER_ID_LIST = new List<string>(){ "A", "B", "C", "D",  "E" };
        public static float SPACING_BETWEEN_STARS_ON_LEVEL_SUCCESS = 30f;
        public static float SIZE_OF_STARS_ON_LEVEL_SUCCESS = 70f;
        public static Color BOARD_CARD_HOLDER_COLOR = HexToColor("#D65865");
        public static Color INITIAL_CARD_HOLDER_COLOR = HexToColor("#6B6DCC");
        public static Color ABLE_TO_MOVE_TEXT_COLOR = HexToColor("#52C567");
        public static Color NOT_ABLE_TO_MOVE_TEXT_COLOR = HexToColor("#FF5240");
        public static int NUM_OF_STARS_FOR_WILD = 6;
        public static int NUM_OF_PROBABILITY_BUTTONS = 3;
        public static int NUM_OF_DIFFICULTY_BUTTONS = 3;

        public static Dictionary<int, Color> GetProbabilityTypeToColorMapping()
        {
            Dictionary<int, Color> probabilityTypeToColorMapping = new Dictionary<int, Color>();
            probabilityTypeToColorMapping.Add((int)ProbabilityType.Certain, HexToColor("#57B762"));
            probabilityTypeToColorMapping.Add((int)ProbabilityType.Probable, HexToColor("#FADD8C"));
            probabilityTypeToColorMapping.Add((int)ProbabilityType.NotExisted, HexToColor("#E03934"));
            return probabilityTypeToColorMapping;
        }

        private static Color HexToColor(string hex)
        {
            Color color = Color.white; // Default color in case of invalid hex string

            if (ColorUtility.TryParseHtmlString(hex, out color))
            {
                return color;
            }
            else
            {
                Debug.LogWarning("Invalid hex color string: " + hex);
                return color;
            }
        }
    }
}