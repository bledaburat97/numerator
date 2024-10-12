using UnityEngine;

namespace Game
{
    public class MovingRewardItemView : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private ParticleSystem firstFlame;
        [SerializeField] private ParticleSystem secondFlame;
        
        public void Init()
        {
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
}