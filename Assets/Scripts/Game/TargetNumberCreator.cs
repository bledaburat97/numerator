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
        [InjectOptional] private ILevelSaveDataManager _saveData;

        public void SetTargetNumber(int numOfBoardHolders)
        {
            if (_levelTracker.GetGameOption() == GameOption.MultiPlayer)
            {
                _targetCardsList = new List<int>();
                return;
            }
            if (_levelDataCreator?.GetLevelData()?.Mechanic == LevelMechanic.Growing)
            {
                var state = _saveData.GetLevelSaveData().GrowingBoard;
                if (state.FullTarget.Count == 0)
                {
                    state.FullTarget = Enumerable.Range(1,_levelDataCreator.GetLevelData().NumOfCards).ToList();
                    ListRandomizer.Randomize(state.FullTarget);
                    state.FullTarget = state.FullTarget.Take(GrowingBoardState.MaximumCount).ToList();
                }
                _targetCardsList = state.FullTarget.Take(numOfBoardHolders).ToList();
                return;
            }
            var prepared=TutorialLessonController.PreparedAnswer(_levelDataCreator.GetLevelData().Lesson);
            var saved=_gameSaveService.GetSavedLevel();
            if(TutorialLessonController.IsBasics(_levelDataCreator.GetLevelData(),_levelTracker.IsFirstLevelTutorial()))
            {
                SetSavedTargetCardList(new List<int> {4,1});
            }
            else if(saved!=null)
            {
                SetSavedTargetCardList(saved.TargetCards.Take(numOfBoardHolders).ToList());
            }
            else if(prepared!=null)
            {
                SetSavedTargetCardList(prepared.ToList());
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

        public IReadOnlyList<int> GetHistoryTargetCards() =>
            _saveData?.GetLevelSaveData().GrowingBoard?.LegacyHistoryTarget ?? _targetCardsList;

        public IReadOnlyList<int> GetProtectedTargetCards() =>
            _saveData?.GetLevelSaveData().GrowingBoard?.FullTarget ?? _targetCardsList;

        public bool TryAppendTargetCard(int card)
        {
            if (_targetCardsList == null || _targetCardsList.Count >= GrowingBoardState.MaximumCount || _targetCardsList.Contains(card)) return false;
            _targetCardsList.Add(card);
            return true;
        }

        public void ApplyCommittedTarget(IReadOnlyList<int> cards)=>_targetCardsList=new List<int>(cards);

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
        IReadOnlyList<int> GetHistoryTargetCards();
        IReadOnlyList<int> GetProtectedTargetCards();
        bool TryAppendTargetCard(int card);
        bool TryRemoveLastTargetCard();
        void ApplyCommittedTarget(IReadOnlyList<int> cards);
        void SetTargetNumber(int numOfBoardHolders);
    }
}
