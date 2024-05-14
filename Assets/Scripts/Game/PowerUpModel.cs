using UnityEngine;

namespace Scripts
{
    [CreateAssetMenu()]
    public class PowerUpModel : ScriptableObject
    {
        public PowerUpType type;
        public Sprite sprite;
    }
}