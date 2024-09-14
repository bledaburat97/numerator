using UnityEngine;

namespace Scripts
{
    [CreateAssetMenu()]
    public class PowerUpModel : ScriptableObject
    {
        public GameUIButtonType type;
        public Sprite sprite;
    }
}