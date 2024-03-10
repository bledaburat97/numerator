using Newtonsoft.Json;
using UnityEngine;
using System.Collections.Generic;

namespace Scripts
{
    public static class LevelDataGetter
    {
        public static List<LevelData> GetStartingLevelsData()
        {
            TextAsset data = Resources.Load<TextAsset>("Levels/starting_levels");
            List<LevelData> levels = JsonConvert.DeserializeObject<LevelDataList>(data.text).Levels;
            return levels;
        }
        
        public static List<LevelData> GetLoopLevelsData()
        {
            TextAsset data = Resources.Load<TextAsset>("Levels/loop_levels");
            List<LevelData> levels = JsonConvert.DeserializeObject<LevelDataList>(data.text).Levels;
            return levels;
        }
        
        private class LevelDataList
        {
            public List<LevelData> Levels;
        }
    }
}