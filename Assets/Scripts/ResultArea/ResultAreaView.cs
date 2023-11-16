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
            StartCoroutine(ForceScrollDown());
        }
        
        IEnumerator ForceScrollDown () {
            // Wait for end of frame AND force update all canvases before setting to bottom.
            yield return new WaitForEndOfFrame ();
            scrollRect.verticalNormalizedPosition = 0f; //local position increases by size of an added object.
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
        RectTransform GetRectTransform();
        void SetScrollPositionToBottom();
    }
}