using TMPro;
using UnityEngine;

namespace Scripts
{
    public class TextHolderAdjustment : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private RectTransform holder;
        [SerializeField] private float difference;
        private float _fixedWidth;

        public void SetFixedWidth(float width) => _fixedWidth = width;

        public void SetText(string count)
        {
            if (text == null) return;

            text.SetText(count);
        }
        
        public void SetPosition()
        {
            if (text == null || holder == null) return;

            float horizontalTextSize = text.preferredWidth;
            holder.sizeDelta = new Vector2(_fixedWidth > 0f ? _fixedWidth : difference + horizontalTextSize, holder.sizeDelta.y);
        }
    }
}
