using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class ResultAreaController : IResultAreaController
    {
        private IResultAreaView _view;
        private ResultBlockControllerFactory _resultBlockControllerFactory;
        private List<IResultBlockController> _resultBlockControllerList;
        public void Initialize(IResultAreaView view, IBoardAreaManager boardAreaManager)
        {
            _view = view;
            _view.Init(new ResultBlockViewFactory());
            _resultBlockControllerFactory = new ResultBlockControllerFactory();
            _resultBlockControllerList = new List<IResultBlockController>();
            boardAreaManager.ResultAdded += AddResultBlock;
        }
        
        private void AddResultBlock(object sender, List<int> cardNumbers)
        {
            IResultBlockController resultBlockController = _resultBlockControllerFactory.Spawn();
            IResultBlockView resultBlockView = _view.CreateResultBlock();
            ResultBlockModel resultBlockModel = new ResultBlockModel()
            {
                //localPosition = GetLocalPositionOfNewResultBlock(resultBlockView.GetRectTransform().sizeDelta),
                cardNumbers = cardNumbers
            };
            resultBlockController.Initialize(resultBlockView, resultBlockModel);
            _view.SetScrollPositionToBottom();
            _resultBlockControllerList.Add(resultBlockController);
        }
        


        private Vector2 GetLocalPositionOfNewResultBlock(Vector2 sizeOfResultBlock) //TODO get from extensions.
        {
            int currentNumOfResultBlocks = _resultBlockControllerList.Count;
            float localXPosition;
            
            if (currentNumOfResultBlocks < ConstantValues.MAX_NUM_OF_RESULT_BLOCKS / 2)
            {
                localXPosition = (sizeOfResultBlock.x - _view.GetRectTransform().sizeDelta.x) / 2;
            }
            else
            {
                localXPosition = _view.GetRectTransform().sizeDelta.x - sizeOfResultBlock.x;
            }
            float localYPosition = 30 * ((float)ConstantValues.MAX_NUM_OF_RESULT_BLOCKS / 4 - currentNumOfResultBlocks - 1f / 2);
            
            return new Vector2(localXPosition, localYPosition);
        }
    }

    public interface IResultAreaController
    {
        void Initialize(IResultAreaView view, IBoardAreaManager boardAreaManager);
    }
}