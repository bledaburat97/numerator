using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class ResultAreaView : NetworkBehaviour, IResultAreaView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private ResultBlockView resultBlockPrefab;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private VerticalLayoutGroup verticalLayoutGroup;
        
        public void Init()
        {

        }

        public void SetScrollPositionToBottom()
        {
            StartCoroutine(ScrollToBottomCoroutine());
        }
        
        IEnumerator ScrollToBottomCoroutine () {
            yield return new WaitForEndOfFrame ();
            float elapsedTime = 0f;
            float startingPosition = scrollRect.verticalNormalizedPosition;
            float targetPosition = 0f;
            float scrollDuration = startingPosition;

            while (elapsedTime < scrollDuration)
            {
                scrollRect.verticalNormalizedPosition = Mathf.Lerp(startingPosition, targetPosition, elapsedTime / scrollDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            scrollRect.verticalNormalizedPosition = targetPosition;
        }
        
        public IResultBlockView CreateResultBlock()
        {
            return Instantiate(resultBlockPrefab, transform);
        }

        public ResultAreaInfo GetResultAreaInfo()
        {
            return new ResultAreaInfo()
            {
                topPoint = rectTransform.position,
                resultBlockSize = new Vector2(rectTransform.rect.width, resultBlockPrefab.GetRectTransform().rect.height),
                spacing = verticalLayoutGroup.spacing,
            };
        }
    }

    public interface IResultAreaView
    {
        void Init();
        ResultAreaInfo GetResultAreaInfo();
        IResultBlockView CreateResultBlock();
        void SetScrollPositionToBottom();
    }

    public struct ResultAreaInfo
    {
        public Vector2 topPoint;
        public Vector2 resultBlockSize;
        public float spacing;
    }
}