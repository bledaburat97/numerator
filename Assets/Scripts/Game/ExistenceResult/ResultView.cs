using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class ResultView : MonoBehaviour, IResultView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TMP_Text numberText;
        [SerializeField] private Image plusImage;
        [SerializeField] private Image minusImage;
        
        public void Init(CardPositionCorrectness cardPositionCorrectness, int count)
        {
            transform.localScale = Vector3.one;
            numberText.SetText(count.ToString());
            if(cardPositionCorrectness == CardPositionCorrectness.Correct) plusImage.gameObject.SetActive(true);
            else if(cardPositionCorrectness == CardPositionCorrectness.Wrong) minusImage.gameObject.SetActive(true);
        }

        public Vector2 GetSize()
        {
            return rectTransform.sizeDelta;
        }
    }
    
    public interface IResultView
    {
        void Init(CardPositionCorrectness cardPositionCorrectness, int count);
        Vector2 GetSize();
    }
}