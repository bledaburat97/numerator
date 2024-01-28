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
        private ResultBlockViewFactory _resultBlockViewFactory;
        [SerializeField] private ScrollRect scrollRect;
        private ResultBlockControllerFactory _resultBlockControllerFactory;
        private ILevelTracker _levelTracker;
        private ITurnOrderDeterminer _turnOrderDeterminer;

        public void Init(ResultBlockViewFactory resultBlockViewFactory, ILevelTracker levelTracker, ITurnOrderDeterminer turnOrderDeterminer)
        {
            _resultBlockControllerFactory = new ResultBlockControllerFactory();
            _resultBlockViewFactory = resultBlockViewFactory;
            _levelTracker = levelTracker;
            _turnOrderDeterminer = turnOrderDeterminer;
        }

        private void SetScrollPositionToBottom()
        {
            if (_levelTracker.GetGameOption() == GameOption.MultiPlayer && !_turnOrderDeterminer.IsLocalTurn())
            {
                return;
            }
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
        
        private IResultBlockView CreateResultBlock()
        {
            return _resultBlockViewFactory.Spawn(transform, resultBlockPrefab);
        }
        
        public void AddResultBlock(object sender, ResultBlockModel resultBlockModel)
        {
            int finalNumber = 0;
            for (int i = 0; i < resultBlockModel.finalNumbers.Count; i++)
            {
                finalNumber += resultBlockModel.finalNumbers[i] * ((int) Math.Pow(10, i));
            }
            AddResultBlockServerRpc(finalNumber, resultBlockModel.correctPosCount, resultBlockModel.wrongPosCount);
        }

        [ServerRpc (RequireOwnership = false)]
        private void AddResultBlockServerRpc(int finalNumber, int correctPosCount, int wrongPosCount)
        {
            AddResultBlockClientRpc(finalNumber, correctPosCount, wrongPosCount);
        }
        
        [ClientRpc]
        private void AddResultBlockClientRpc(int finalNumber, int correctPosCount, int wrongPosCount)
        {
            IResultBlockController resultBlockController = _resultBlockControllerFactory.Spawn();
            IResultBlockView resultBlockView = CreateResultBlock();
            List<int> finalNumbers = new List<int>();
            while (finalNumber != 0)
            {
                int a = finalNumber % 10;
                finalNumbers.Add(a);
                finalNumber /= 10;
            }

            resultBlockController.Initialize(resultBlockView, new ResultBlockModel()
            {
                finalNumbers = finalNumbers,
                correctPosCount = correctPosCount,
                wrongPosCount = wrongPosCount
            });
            SetScrollPositionToBottom();
        }
        
    }

    public interface IResultAreaView
    {
        void Init(ResultBlockViewFactory resultBlockViewFactory, ILevelTracker levelTracker, ITurnOrderDeterminer turnOrderDeterminer);
        void AddResultBlock(object sender, ResultBlockModel resultBlockModel);
    }
}