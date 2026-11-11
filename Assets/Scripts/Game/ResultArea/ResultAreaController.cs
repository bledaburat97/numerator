using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace Scripts
{
    // Owns public evidence independently of any result-list instance or row pool.
    public sealed class RoundHistory : IResultHistorySource
    {
        private readonly List<ResultBlockModel> _entries=new List<ResultBlockModel>();
        public IReadOnlyList<ResultBlockModel> Entries=>_entries;
        public event Action Changed;
        public event Action<int> DetailsChanged;
        public event Action RowsUpdated;
        public int Generation {get;private set;}
        public int HistoryCount=>_entries.Count;
        public ResultBlockModel GetHistoryEntry(int index)=>_entries[index];
        public void Add(ResultBlockModel row){_entries.Add(row.Snapshot());ObserverNotification.Send(Changed);}
        public void Clear(){_entries.Clear();Generation++;ObserverNotification.Send(Changed);}
        public void LabelLatest(bool local,string name)
        {if(_entries.Count==0)return;var row=_entries[_entries.Count-1];row.isLocalGuess=local;row.playerName=name;ObserverNotification.Send(RowsUpdated);}
        public void Reveal(GuessEvaluation guess)
        {
            if(guess.Number<1 || guess.Number>_entries.Count)return;
            _entries[guess.Number-1].hintStates=guess.States;ObserverNotification.Send(DetailsChanged,guess.Number-1);
        }
        public void Review(IReadOnlyList<int> answer)
        {
            foreach(var row in _entries)
            {
                var partition=row.groups!=null && row.groups.Count==2?BoardPartition.ForCount(row.finalNumbers.Count):null;
                row.reviewStates=new GuessEvaluation(0,GuessSubmission.FromHistory(row.finalNumbers),answer,partition).States;
            }
            ObserverNotification.Send(RowsUpdated);
        }
    }
    public class ResultAreaController : IResultAreaController, IDisposable
    {
        private readonly IResultAreaView _view;
        public RoundHistory History {get;}
        public int HistoryCount=>History.HistoryCount;
        [Inject]
        public ResultAreaController(IResultAreaView view,RoundHistory history=null)
        {
            _view=view;History=history??new RoundHistory();_view.SetHistory(History.Entries);
            History.Changed+=Render;History.DetailsChanged+=RenderDetail;History.RowsUpdated+=RenderRows;
        }
        private void Render(){_view.SetHistory(History.Entries);_view.HistoryChanged();_view.SetEmptyState(History.HistoryCount==0);}
        private void RenderDetail(int index)=>_view.RefreshRow(index);
        private void RenderRows()=>_view.RefreshVisibleRows();
        public void Initialize(bool isNewGame)
        {_view.SetHistory(History.Entries);_view.SetEmptyState(History.HistoryCount==0);_view.GetCanvasGroup().alpha=isNewGame?0f:1f;}
        public Sequence ChangeFade(float duration,float alpha)=>DOTween.Sequence().Append(_view.GetCanvasGroup().DOFade(alpha,duration));
        public void EnableReview()=>_view.EnableReview();
        public void LabelLatestMultiplayerGuess(bool local,string name=null)
        {History.LabelLatest(local,name);_view.SetScrollPositionToBottom();}
        public void RevealAnswer(IReadOnlyList<int> answer){History.Review(answer);EnableReview();}
        public void RevealGuess(GuessEvaluation guess,bool animate=false)
        {History.Reveal(guess);if(animate && guess.Number>0 && guess.Number<=History.HistoryCount)_view.PlayHintForRow(guess.Number-1);}
        public void AddResultBlock(ResultBlockModel row,bool animate=true)
        {History.Add(row);int index=History.HistoryCount-1;ObserverNotification.Present(()=>{_view.SetScrollPositionToBottom();if(animate)_view.PlayFeedbackForRow(index);});}
        public void AddNewResultBlock(int number,int correct,int wrong)
        {
            var cards=new List<int>();while(number!=0){cards.Add(number%10);number/=10;}
            AddResultBlock(new ResultBlockModel{finalNumbers=cards,correctPosCount=correct,wrongPosCount=wrong});
        }
        public ResultAreaInfo GetResultAreaInfo()=>_view.GetResultAreaInfo();
        // Hiding/recycling a screen must not erase the round's evidence.
        public void RemoveResultBlocks(){_view.SetHistory(Array.Empty<ResultBlockModel>());_view.HistoryChanged();}
        public void Dispose(){History.Changed-=Render;History.DetailsChanged-=RenderDetail;History.RowsUpdated-=RenderRows;}
    }

    public interface IResultHistorySource
    {
        event Action Changed;
        event Action<int> DetailsChanged;
        int Generation { get; }
        int HistoryCount { get; }
        ResultBlockModel GetHistoryEntry(int index);
    }

    public interface IResultAreaController
    {
        ResultAreaInfo GetResultAreaInfo();
        void AddNewResultBlock(int finalNumber, int correctPosCount, int wrongPosCount);
        void Initialize(bool isNewGame);
        void AddResultBlock(ResultBlockModel resultBlockModel,bool animate=true);
        Sequence ChangeFade(float duration, float finalAlpha);
        void RemoveResultBlocks();
        void EnableReview();
        void RevealAnswer(IReadOnlyList<int> answer);
        void RevealGuess(GuessEvaluation guess,bool animate=false);
    }
}
