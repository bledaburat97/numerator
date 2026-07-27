using TMPro;
using UnityEngine;

namespace Scripts
{
    public class TextHolderAdjustment : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private RectTransform holder;
        [SerializeField] private float difference;

        public void SetText(string count)
        {
            if (text == null) return;

            text.SetText(count);
        }
        
        public void SetPosition()
        {
            if (text == null || holder == null) return;

            float horizontalTextSize = text.preferredWidth;
            holder.sizeDelta = new Vector2(difference + horizontalTextSize, holder.sizeDelta.y);
        }
    }
}
