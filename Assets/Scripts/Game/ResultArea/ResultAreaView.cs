using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class ResultAreaView : MonoBehaviour, IResultAreaView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private ResultBlockView resultBlockPrefab;
        private ResultBlockViewFactory _resultBlockViewFactory;
        [SerializeField] private ScrollRect scrollRect;
        
        public void Init(ResultBlockViewFactory resultBlockViewFactory)
        {
            _resultBlockViewFactory = resultBlockViewFactory;
        }

        public void SetScrollPositionToBottom()
        {
            StartCoroutine(ScrollToBottomCoroutine());
        }
        
        IEnumerator ScrollToBottomCoroutine () {
            yield return new WaitForEndOfFrame ();
            scrollRect.verticalNormalizedPosition = 0f;
            Canvas.ForceUpdateCanvases ();
        }

        public IResultBlockView CreateResultBlock()
        {
            return _resultBlockViewFactory.Spawn(transform, resultBlockPrefab);
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }
    }

    public interface IResultAreaView
    {
        void Init(ResultBlockViewFactory resultBlockViewFactory);
        IResultBlockView CreateResultBlock();
        void SetScrollPositionToBottom();
    }
}