using TMPro;
using UnityEngine;

namespace Scripts
{
    public class TextIconAdjustment : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private RectTransform icon;
        [SerializeField] private int gap;

        public void SetText(string count)
        {
            text.SetText(count);
        }
        
        public void SetPosition()
        {
            float horizontalTextSize = text.preferredWidth;
            float horizontalIconSize = icon.sizeDelta.x;
            
            text.rectTransform.localPosition = new Vector2( - (gap + horizontalIconSize) / 2, 0);
            icon.localPosition = new Vector2((gap + horizontalTextSize) / 2 , 0);
        }
        
        
        
    }
}