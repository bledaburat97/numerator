using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class TutorialMessagePopupView : MonoBehaviour, ITutorialMessagePopupView
    {
        [SerializeField] private TMP_Text text;
        
        public void Init()
        {
            transform.localScale = Vector3.one;
            transform.localPosition = new Vector3(0f, 295f, 0);
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }

        public void SetText(string text)
        {
            this.text.SetText(text);
        }
    }

    public interface ITutorialMessagePopupView
    {
        void Init();
        void Destroy();
        void SetText(string text);
    }
}