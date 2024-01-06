﻿using System;
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

        public void Init(ResultBlockViewFactory resultBlockViewFactory)
        {
            _resultBlockControllerFactory = new ResultBlockControllerFactory();
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
            AddResultBlockServerRpc(finalNumber);
        }

        [ServerRpc (RequireOwnership = false)]
        private void AddResultBlockServerRpc(int finalNumber)
        {
            AddResultBlockClientRpc(finalNumber);
        }
        
        [ClientRpc]
        private void AddResultBlockClientRpc(int finalNumber)
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
            {finalNumbers = finalNumbers, 
                resultModels = new List<ResultModel>(){new ResultModel(){cardPositionCorrectness = CardPositionCorrectness.Correct, number = 2}}});
            SetScrollPositionToBottom();
        }
    }

    public interface IResultAreaView
    {
        void Init(ResultBlockViewFactory resultBlockViewFactory);
        void SetScrollPositionToBottom();
        void AddResultBlock(object sender, ResultBlockModel resultBlockModel);
    }
}