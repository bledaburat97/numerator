using UnityEngine;

namespace Game
{
    [CreateAssetMenu()]
    public class CrystalModel : ScriptableObject
    {
        public CrystalType type;
        public Sprite sprite;
    }
}
