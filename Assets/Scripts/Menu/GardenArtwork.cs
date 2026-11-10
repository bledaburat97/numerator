using System.Collections.Generic;
using Scripts;
using Scripts.Economy;
using UnityEngine;

namespace Menu
{
    // Nine species share the same stage names and layout. Mature products reuse
    // the puzzle sprites; there is one visual identity for each collected fruit.
    public static class GardenArtwork
    {
        private static readonly string[] Paths={"Art/Garden/Growth/","Art/Garden/Pilot/"};
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        public static Sprite Get(string atlas, string sprite = null)
        {
            string key = atlas + "/" + sprite;
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
            foreach(string path in Paths)
            {
                if(sprite==null)
                {var asset=Resources.Load<Sprite>(path+atlas);if(asset!=null)return Cache[key]=asset;}
                else foreach (var asset in Resources.LoadAll<Sprite>(path + atlas)) Cache[atlas + "/" + asset.name] = asset;
                if(Cache.TryGetValue(key,out cached))return cached;
            }
            return Cache.TryGetValue(key, out cached) ? cached : null;
        }
        public static Sprite Stage(FruitId fruit, GardenGrowthStage stage)
        {
            // The mature fruit is the EXACT puzzle sprite, not a second interpretation.
            if (stage == GardenGrowthStage.Fruit) return FruitLabStyle.Icon(fruit.ToString());
            string atlas = fruit+"Growth";
            return Get(atlas, stage.ToString());
        }
        public static void ResetCache() => Cache.Clear();
    }
}
