using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public static class ConstantValues
    {
        public static float DEFAULT_CARD_HOLDER_WIDTH = 60;
        public static float DEFAULT_CARD_HOLDER_HEIGHT = 75;
        public static float BOARD_CARD_HOLDER_WIDTH = 72;
        public static float BOARD_CARD_HOLDER_HEIGHT = 90;
        public static int NUM_OF_BOARD_CARD_HOLDERS = 3;
        public static int NUM_OF_INITIAL_CARD_HOLDERS = 5;
        public static float POSSIBLE_HOLDER_INDICATOR_WIDTH = 12;
        public static float POSSIBLE_HOLDER_INDICATOR_HEIGHT = 20;
        public static List<string> HOLDER_ID_LIST = new List<string>(){ "A", "B", "C", "D" };
        
        public static Dictionary<ProbabilityType, Color> GetProbabilityTypeToColorMapping()
        {
            Dictionary<ProbabilityType, Color> probabilityTypeToColorMapping = new Dictionary<ProbabilityType, Color>();
            probabilityTypeToColorMapping.Add(ProbabilityType.Certain, HexToColor("#63DE71"));
            probabilityTypeToColorMapping.Add(ProbabilityType.Probable, HexToColor("#FADD8C"));
            probabilityTypeToColorMapping.Add(ProbabilityType.NotExisted, HexToColor("#E03934"));
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