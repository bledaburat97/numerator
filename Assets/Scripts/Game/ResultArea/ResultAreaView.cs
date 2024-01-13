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
        private IGamePopupCreator _gamePopupCreator;

        public void Init(ResultBlockViewFactory resultBlockViewFactory, IGamePopupCreator gamePopupCreator)
        {
            _resultBlockControllerFactory = new ResultBlockControllerFactory();
            _resultBlockViewFactory = resultBlockViewFactory;
            _gamePopupCreator = gamePopupCreator;
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
        private IResultBlockView CreateResultBlock()
        {
            return _resultBlockViewFactory.Spawn(transform, resultBlockPrefab);
        }


        public RectTransform GetRectTransform()
        {
            return rectTransform;
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
        
        public override void OnNetworkSpawn()
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
        }

        private void OnClientDisconnectCallback(ulong clientId)
        {
            //if (NetworkManager.Singleton.ConnectedClientsIds.Contains(clientId))
            if (clientId == NetworkManager.ServerClientId)
            {
                _gamePopupCreator.CreateDisconnectionPopup();
            }
        }
    }

    public interface IResultAreaView
    {
        void Init(ResultBlockViewFactory resultBlockViewFactory, IGamePopupCreator gamePopupCreator);
        void SetScrollPositionToBottom();
        void AddResultBlock(object sender, ResultBlockModel resultBlockModel);
    }
}