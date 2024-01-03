using UnityEngine;

namespace Scripts
{
    public class UnmaskServiceAreaView : MonoBehaviour
    {
        [SerializeField] private UnmaskServiceView unmaskServiceView;
        [SerializeField] private UnmaskItemView unmaskItemView;
        private float _fade = 0.6f;

        void Start()
        {
            CreateMaskSystem(0.5f);
        }

        private void CreateMaskSystem(float duration)
        {
            unmaskServiceView.Init(Color.black, _fade);
            unmaskServiceView.SetAlpha(_fade, duration);
        }
    }

}