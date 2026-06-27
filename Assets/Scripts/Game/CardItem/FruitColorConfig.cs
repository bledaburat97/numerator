using UnityEngine;

namespace Scripts
{
    [CreateAssetMenu(fileName = "FruitColorConfig", menuName = "Config/FruitColorConfig")]
    public class FruitColorConfig : ScriptableObject
    {
        public Sprite fruitImage;
        public Color fruitColor;
    }
}
