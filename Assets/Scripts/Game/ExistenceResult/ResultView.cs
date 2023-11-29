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
        
        public void Init(ResultModel model)
        {
            transform.localScale = Vector3.one;
            numberText.SetText(model.number.ToString());
            if(model.cardPositionCorrectness == CardPositionCorrectness.Correct) plusImage.gameObject.SetActive(true);
            else if(model.cardPositionCorrectness == CardPositionCorrectness.Wrong) minusImage.gameObject.SetActive(true);
        }

        public Vector2 GetSize()
        {
            return rectTransform.sizeDelta;
        }
    }
    
    public interface IResultView
    {
        void Init(ResultModel model);
        Vector2 GetSize();
    }
}