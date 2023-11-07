using TMPro;
using UnityEngine;

namespace Views
{
    public class CardLetterView : MonoBehaviour, ICardLetterView
    {
        [SerializeField] private TMP_Text letterText;

        public void Init(string letter)
        {
            transform.localScale = Vector3.one;
            SetLetterText(letter);
        }

        private void SetLetterText(string letter)
        {
            letterText.text = letter;
        }

        public void SetStatus(bool status)
        {
            transform.gameObject.SetActive(status);
        }

        public void SetParent(RectTransform parent)
        {
            transform.parent = parent;
        }
        
        
    }

    public interface ICardLetterView
    {
        void Init(string letter);
        void SetStatus(bool status);
        void SetParent(RectTransform parent);
    }
}