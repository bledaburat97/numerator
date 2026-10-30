using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class TargetNumberCreator : NetworkBehaviour, ITargetNumberCreator
    {
        private List<int> _targetCardsList;
        [Inject] private ILevelDataCreator _levelDataCreator;
        [Inject] private IGameSaveService _gameSaveService;
        [Inject] private ILevelTracker _levelTracker;

        public void SetTargetNumber(int numOfBoardHolders)
        {
            if (_levelTracker.GetGameOption() == GameOption.MultiPlayer)
            {
                _targetCardsList = new List<int>();
                return;
            }
            if (_levelTracker.IsFirstLevelTutorial())
            {
                SetSavedTargetCardList(new List<int>() { 4, 1 });
            }
            else if (_gameSaveService.GetSavedLevel() != null)
            {
                SetSavedTargetCardList(_gameSaveService.GetSavedLevel().TargetCards.Take(numOfBoardHolders).ToList());
            }
            else if (_levelTracker.IsCardInfoTutorial())
            {
                SetSavedTargetCardList(new List<int>(){4,6});
            }
            else
            {
                CreateTargetNumber(numOfBoardHolders);
            }
        }
        

        public List<int> GetTargetCardsList()
        {
            return _targetCardsList;
        }

        public bool TryRemoveLastTargetCard()
        {
            if (_targetCardsList == null || _targetCardsList.Count <= 1) return false;
            _targetCardsList.RemoveAt(_targetCardsList.Count - 1);
            return true;
        }


        private void SetTargetCardsList(int targetNumber)
        {
            Debug.Log("targetNumber " + targetNumber);

            List<int> targetCardsList = new List<int>();
            while (targetNumber != 0)
            {
                int a = targetNumber % 10;
                targetCardsList.Add(a);
                targetNumber /= 10;
            }
            _targetCardsList = targetCardsList;
        }

        private void SetSavedTargetCardList(List<int> targetCardsList)
        {
            _targetCardsList = targetCardsList;
        }
        
        public void CreateMultiplayerTargetNumber()
        {
            _targetCardsList = new List<int>();
        }
        public void RevealMultiplayerAnswer(int[] answer) => _targetCardsList = new List<int>(answer);
        
        private void CreateTargetNumber(int numOfBoardHolders)
        {
            LevelData levelData = _levelDataCreator.GetLevelData();
            SetTargetCardsList(GetTargetNumber(levelData.NumOfCards, numOfBoardHolders));
        }
        

        private int GetTargetNumber(int numOfCards, int numOfBoardHolders)
        {
            List<int> cards = Enumerable.Range(1, numOfCards).ToList();
            ListRandomizer.Randomize(cards);

            List<int> targetCardsList = cards.Take(numOfBoardHolders).ToList();

            int targetNumber = 0;
            for (int i = 0; i < targetCardsList.Count; i++)
            {
                targetNumber += targetCardsList[i] * ((int) Math.Pow(10, i));
            }

            return targetNumber;
        }
        
    }

    public interface ITargetNumberCreator
    {
        void CreateMultiplayerTargetNumber();
        List<int> GetTargetCardsList();
        bool TryRemoveLastTargetCard();
        void SetTargetNumber(int numOfBoardHolders);
    }
}
