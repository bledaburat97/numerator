using Newtonsoft.Json;
using UnityEngine;
using System.Collections.Generic;

namespace Scripts
{
    public static class LevelDataGetter
    {
        public static List<LevelData> GetLevelDataFromJson()
        {
            TextAsset data = Resources.Load<TextAsset>("Levels/default_levels");
            List<LevelData> levels = JsonConvert.DeserializeObject<LevelDataList>(data.text).Levels;
            return levels;
        }
        
        private class LevelDataList
        {
            public List<LevelData> Levels;
        }
    }
}