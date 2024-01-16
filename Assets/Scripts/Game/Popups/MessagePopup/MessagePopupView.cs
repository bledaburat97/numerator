using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Scripts
{
    public class MessagePopupView : MonoBehaviour , IMessagePopupView
    {
        [SerializeField] private TMP_Text header;
        
        public void Init(string text)
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            header.text = text;
            header.alpha = 0f;
            Animate();
        }

        private void Animate()
        {
            DOTween.Sequence()
                .Append(header.DOFade(1f, 0.5f))
                .AppendInterval(1f)
                .Append(header.DOFade(0f, 0.5f))
                .OnComplete(() => Destroy(gameObject));
        }

    }

    public interface IMessagePopupView
    {
        void Init(string text);
    }
}