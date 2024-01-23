using UnityEngine;

namespace Scripts
{
    public class SpaceShipView : MonoBehaviour, ISpaceShipView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private ParticleSystem firstFlame;
        [SerializeField] private ParticleSystem secondFlame;

        public void Init()
        {
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localScale = Vector3.one;
            firstFlame.gameObject.SetActive(false);
            secondFlame.gameObject.SetActive(false);
        }
        
        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }
        
        public void StartFlames()
        {
            firstFlame.gameObject.SetActive(true);
            secondFlame.gameObject.SetActive(true);
            firstFlame.Play();
            secondFlame.Play();
        }

        public void StopFlames()
        {
            firstFlame.Stop();
            secondFlame.Stop();
            firstFlame.gameObject.SetActive(false);
            secondFlame.gameObject.SetActive(false);
        }
    }

    public interface ISpaceShipView
    {
        void Init();
        RectTransform GetRectTransform();
        void StartFlames();
        void StopFlames();
    }
}