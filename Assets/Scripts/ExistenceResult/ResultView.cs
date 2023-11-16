using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class ResultView : MonoBehaviour, IResultView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TMP_Text numberText;
        [SerializeField] private Image image;
        
        public void Init(ResultModel model)
        {
            transform.localScale = Vector3.one;
            numberText.SetText(model.number.ToString());
            image.color = model.color;
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