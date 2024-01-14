using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Random = System.Random;

namespace Scripts
{
    public class TargetNumberCreator : NetworkBehaviour, ITargetNumberCreator
    {
        private NetworkVariable<int> _targetNumber = new NetworkVariable<int>();
        private List<int> _targetCardsList;

        public void Initialize()
        {
        }
        
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
            int targetNumber = _targetNumber.Value;
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
        
        public void CreateTargetNumber(int numOfCards, int numOfBoardHolders)
        {
            if (IsServer)
            {
                CreateTargetNumberServerRpc(numOfCards, numOfBoardHolders);
            }
        }
        
        [ServerRpc (RequireOwnership = false)]
        private void CreateTargetNumberServerRpc(int numOfCards, int numOfBoardHolders)
        {
            List<int> cards = Enumerable.Range(1, numOfCards).ToList();
            Random random = new Random();
            for (int i = numOfCards - 1; i > 0; i--)
            {
                int j = random.Next(0, i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }

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

            _targetNumber.Value = targetNumber;
        }
        
    }

    public interface ITargetNumberCreator
    {
        void Initialize();
        void CreateTargetNumber(int numOfCards, int numOfBoardHolders);
        List<int> GetTargetCardsList();
        void SetSavedTargetCardList(List<int> targetCardsList);
    }
}