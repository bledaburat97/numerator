using System;
using System.Collections.Generic;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class ResultAreaController : IResultAreaController
    {
        private ILevelTracker _levelTracker;
        private ITurnOrderDeterminer _turnOrderDeterminer;
        private IMultiplayerGameController _multiplayerGameController;
        private IResultAreaView _view;
        private List<IResultBlockController> _resultBlockControllers;
        
        [Inject]
        public ResultAreaController(ILevelTracker levelTracker, ITurnOrderDeterminer turnOrderDeterminer, IMultiplayerGameController multiplayerGameController, IResultAreaView view)
        {
            _levelTracker = levelTracker;
            _turnOrderDeterminer = turnOrderDeterminer;
            _multiplayerGameController = multiplayerGameController;
            _view = view;
            _view.Init();
            _resultBlockControllers = new List<IResultBlockController>();
        }

        public void Initialize()
        {
            RemoveResultBlocks();
        }

        public void AddResultBlock(ResultBlockModel resultBlockModel)
        {
            int finalNumber = 0;
            for (int i = 0; i < resultBlockModel.finalNumbers.Count; i++)
            {
                finalNumber += resultBlockModel.finalNumbers[i] * ((int) Math.Pow(10, i));
            }
            
            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                AddNewResultBlock(finalNumber, resultBlockModel.correctPosCount, resultBlockModel.wrongPosCount);
            }
            else
            {
                _multiplayerGameController.AddResultBlock(finalNumber, resultBlockModel.correctPosCount, resultBlockModel.wrongPosCount);
            }
        }
        
        public void AddNewResultBlock(int finalNumber, int correctPosCount, int wrongPosCount)
        {
            IResultBlockController resultBlockController = new ResultBlockController();
            IResultBlockView resultBlockView = _view.CreateResultBlock();
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
            _resultBlockControllers.Add(resultBlockController);
            
            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer || _turnOrderDeterminer.IsLocalTurn())
            {
                _view.SetScrollPositionToBottom();
            }
        }
        public ResultAreaInfo GetResultAreaInfo()
        {
            return _view.GetResultAreaInfo();
        }

        private void RemoveResultBlocks()
        {
            while (_resultBlockControllers.Count > 0)
            {
                _resultBlockControllers[0].DestroyResultBlock();
                _resultBlockControllers[0] = null;
                _resultBlockControllers.Remove(_resultBlockControllers[0]);
            }
        }

    }

    public interface IResultAreaController
    {
        ResultAreaInfo GetResultAreaInfo();
        void AddNewResultBlock(int finalNumber, int correctPosCount, int wrongPosCount);
        void Initialize();
        void AddResultBlock(ResultBlockModel resultBlockModel);
    }
}