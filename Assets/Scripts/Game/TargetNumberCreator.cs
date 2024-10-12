using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using Unity.Netcode;
using UnityEngine;
using Zenject;
using Random = System.Random;

namespace Scripts
{
    public class TargetNumberCreator : NetworkBehaviour, ITargetNumberCreator
    {
        private NetworkVariable<int> _targetNumber = new NetworkVariable<int>();
        private List<int> _targetCardsList;
        [Inject] private ILevelDataCreator _levelDataCreator;
        [Inject] private ILevelSaveDataManager _levelSaveDataManager;
        
        public override void OnNetworkSpawn()
        {
            _targetNumber.OnValueChanged += SetTargetNumber;
        }

        public List<int> GetTargetCardsList()
        {
            return _targetCardsList;
        }

        private void SetTargetNumber(int oldValue, int newValue)
        {
            SetTargetCardsList(_targetNumber.Value);
        }

        private void SetTargetCardsList(int targetNumber)
        {
            List<int> targetCardsList = new List<int>();
            while (targetNumber != 0)
            {
                int a = targetNumber % 10;
                targetCardsList.Add(a);
                targetNumber /= 10;
            }
            _targetCardsList = targetCardsList;
        }

        public void SetSavedTargetCardList(List<int> targetCardsList)
        {
            _targetCardsList = targetCardsList;
        }
        
        public void CreateMultiplayerTargetNumber()
        {
            LevelData levelData = _levelDataCreator.GetLevelData();

            if (IsServer)
            {
                CreateTargetNumberServerRpc(levelData.NumOfCards, levelData.NumOfBoardHolders);
            }
        }
        
        public void CreateTargetNumber(int removedBoardHolderCount)
        {
            LevelData levelData = _levelDataCreator.GetLevelData();
            SetTargetCardsList(GetTargetNumber(levelData.NumOfCards, levelData.NumOfBoardHolders - removedBoardHolderCount));
        }
        
        [ServerRpc (RequireOwnership = false)]
        private void CreateTargetNumberServerRpc(int numOfCards, int numOfBoardHolders)
        {
            _targetNumber.Value = GetTargetNumber(numOfCards, numOfBoardHolders);
        }

        private int GetTargetNumber(int numOfCards, int numOfBoardHolders)
        {
            List<int> cards = Enumerable.Range(1, numOfCards).ToList();
            ListRandomizer.Randomize(cards);

            for (int i = 0; i < numOfBoardHolders; i++)
            {
                Debug.Log(cards[i]);
            }

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
        void CreateTargetNumber(int removedBoardHolderCount);
        List<int> GetTargetCardsList();
        void SetSavedTargetCardList(List<int> targetCardsList);
    }
}