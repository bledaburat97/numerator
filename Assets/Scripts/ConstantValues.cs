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
        public static List<string> HOLDER_ID_LIST = new List<string>(){ "A", "B", "C", "D" };

        public static Dictionary<ProbabilityType, Color> GetProbabilityTypeToColorMapping()
        {
            Dictionary<ProbabilityType, Color> probabilityTypeToColorMapping = new Dictionary<ProbabilityType, Color>();
            probabilityTypeToColorMapping.Add(ProbabilityType.Certain, Color.green);
            probabilityTypeToColorMapping.Add(ProbabilityType.Probable, Color.yellow);
            probabilityTypeToColorMapping.Add(ProbabilityType.NotExisted, Color.red);
            return probabilityTypeToColorMapping;
        }
    }
}