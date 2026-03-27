using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    
    public class ResultImageView : MonoBehaviour, IResultImageView
    {
        [SerializeField] private Image image;
        
        public void Init(CardPositionCorrectness cardPositionCorrectness)
        {
            transform.localScale = Vector3.one;
            if (cardPositionCorrectness == CardPositionCorrectness.Correct)
            {
                image.color = Color.green;
            }
            else if (cardPositionCorrectness == CardPositionCorrectness.Wrong)
            {
                image.color = Color.yellow;
            }
        }
    }

    public interface IResultImageView
    {
        void Init(CardPositionCorrectness cardPositionCorrectness);
    }
}