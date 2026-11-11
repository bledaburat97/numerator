using System;
using System.Collections.Generic;
using System.Linq;
using Scripts;
using UnityEngine;
using DG.Tweening;
using Zenject;

namespace Game
{
    public class HintProvider : IHintProvider,IDisposable
    {
        private readonly IGuessManager _guesses;
        [InjectOptional] private EliminateFruitEffect _eliminate;
        [InjectOptional] private IRoundStateManager _round;
        [InjectOptional] private IceBoardService _ice;
        [InjectOptional] private LazyInject<IGameSaveSnapshotProvider> _snapshots;
        [InjectOptional] private IGameSaveService _saves;
        private readonly ITargetNumberCreator _targetNumberCreator;
        private readonly ICardItemInfoManager _cardItemInfoManager;
        private readonly IInitialCardAreaController _initialCardAreaController;
        private readonly IBoardPlacementQuery _boardPlacementQuery;
        private readonly ICardPlacementCoordinator _cardPlacementCoordinator;
        private readonly CardLockService _locks;
        private readonly List<(IRewardTokenView token, Sequence animation)> _activeHints = new List<(IRewardTokenView, Sequence)>();
        
        [Inject]
        public HintProvider(IGuessManager guessManager, ITargetNumberCreator targetNumberCreator,
            ICardItemInfoManager cardItemInfoManager, IInitialCardAreaController initialCardAreaController,
            IBoardPlacementQuery boardPlacementQuery, ICardPlacementCoordinator cardPlacementCoordinator,CardLockService locks)
        {
            _guesses=guessManager;
            guessManager.LevelFailEvent += CancelPendingHints;
            _targetNumberCreator = targetNumberCreator;
            _eliminate = new EliminateFruitEffect(targetNumberCreator, cardItemInfoManager);
            _cardItemInfoManager = cardItemInfoManager;
            _initialCardAreaController = initialCardAreaController;
            _boardPlacementQuery = boardPlacementQuery;
            _cardPlacementCoordinator = cardPlacementCoordinator;
            _locks=locks;
        }

        public Action<IRewardTokenView> PrepareHint(bool canReveal)
            =>(canReveal?PrepareReveal(false)??PrepareDestroy(false):PrepareDestroy(false)??PrepareReveal(false))??ConsumeRewardItem;
        private bool TryApplyRevealHint(IRewardTokenView token)
        {var show=PrepareReveal(true);if(show==null)return false;show(token);return true;}
        private Action<IRewardTokenView> PrepareReveal(bool save)
        {
            if(!TryGetExistedCardIndex(out int card,out int slot) || !_locks.TryLock(slot,card))return null;
            if(save)Checkpoint();
            return token=>{
                if(!_initialCardAreaController.TryGetCardItem(card,out var item)){ConsumeRewardItem(token);return;}
                Track(token,new RewardTokenAnimationManager().RevealCard(token,item.GetRectTransform(),()=>_cardPlacementCoordinator.PresentLockedCard(slot,card)));
            };
        }
        private bool TryApplyDestroyHint(IRewardTokenView token)
        {var show=PrepareDestroy(true);if(show==null)return false;show(token);return true;}
        private Action<IRewardTokenView> PrepareDestroy(bool save)
        {
            var history=_round?.GetTriedCardsList();
            IReadOnlyList<int> preferred=history!=null && history.Count>0?history[history.Count-1]
                :_boardPlacementQuery.GetOccupiedCardIndexesOnBoard().Where(i=>i>=0).Select(i=>i+1).ToArray();
            if(!_eliminate.TryApply(preferred,history?.Count??0,out int card))return null;
            _cardPlacementCoordinator.TryRemoveCardFromBoard(card);
            if(save)Checkpoint();
            return token=>{
                if(!_initialCardAreaController.TryGetCardItem(card,out var item)){ConsumeRewardItem(token);return;}
                item.GetCardMoveHandler().SetMovementLocked(true);item.GetCardViewHandler().MoveToInitialParent();
                Track(token,new RewardTokenAnimationManager().DestroyCard(token,item.GetRectTransform(),()=>_initialCardAreaController.DestroyCard(card)));
            };
        }

        private static void ConsumeRewardItem(IRewardTokenView rewardTokenView)
        {
            rewardTokenView.AnimateFadeOut(0.25f);
        }

        private void Checkpoint()
        {
            if (_saves != null && _snapshots != null && _snapshots.Value.TryCreateSnapshot(out var state,true)) _saves.Save(state);
        }

        private void Track(IRewardTokenView token, Sequence animation)
        {
            _activeHints.RemoveAll(hint => hint.animation == null || !hint.animation.IsActive());
            _activeHints.Add((token, animation));
        }

        private void CancelPendingHints(object sender, EventArgs args)
        {
            foreach (var hint in _activeHints)
            {
                if (hint.animation == null || !hint.animation.IsActive()) continue;
                hint.animation.Kill();
                hint.token.Destroy();
            }
            _activeHints.Clear();
        }
        public void Dispose(){_guesses.LevelFailEvent-=CancelPendingHints;CancelPendingHints(this,EventArgs.Empty);}

        private bool TryGetExistedCardIndex( out int cardIndex, out int boardHolderIndex)
        {
            List<int> targetCardNumbers = _targetNumberCreator.GetTargetCardsList();
            IReadOnlyList<int> cardIndexesOnBoard = _boardPlacementQuery.GetOccupiedCardIndexesOnBoard();
            List<CardItemInfo> cardItemInfoList = _cardItemInfoManager.GetCardItemInfoList();
            List<(int, int)> firstList = new List<(int, int)>();
            List<(int, int)> secondList = new List<(int, int)>();
            List<(int, int)> thirdList = new List<(int, int)>();
            cardIndex = -1;
            boardHolderIndex = -1;
            for (int i = 0; i < targetCardNumbers.Count; i++)
            {
                CardItemInfo targetCardItemInfo = cardItemInfoList[targetCardNumbers[i] - 1];
                if (targetCardItemInfo.isLocked || _ice?.IsBlocked(i)==true) continue;

                if (!(targetCardItemInfo.probabilityType == ProbabilityType.Certain &&
                    targetCardItemInfo.possibleCardHolderIndicatorIndexes.Count == 1 &&
                    targetCardItemInfo.possibleCardHolderIndicatorIndexes[0] == i))
                {
                    if (!cardIndexesOnBoard.Contains(targetCardNumbers[i] - 1))
                    {
                        firstList.Add((targetCardNumbers[i] - 1, i));
                    }
                    else
                    {
                        secondList.Add((targetCardNumbers[i] - 1, i));
                    }
                }
                else
                {
                    thirdList.Add((targetCardNumbers[i] - 1, i));
                }
            }

            if (firstList.Count > 0)
            {
                ListRandomizer.Randomize(firstList);
                boardHolderIndex = firstList[0].Item2;
                cardIndex = firstList[0].Item1;
                return true;
            }
            
            if (secondList.Count > 0)
            {
                ListRandomizer.Randomize(secondList);
                boardHolderIndex = secondList[0].Item2;
                cardIndex = secondList[0].Item1;
                return true;
            }

            if (thirdList.Count > 0)
            {
                ListRandomizer.Randomize(thirdList);
                boardHolderIndex = thirdList[0].Item2;
                cardIndex = thirdList[0].Item1;
                return true;
            }
            return false;
        }
    }
    
    public interface IHintProvider
    {
        Action<IRewardTokenView> PrepareHint(bool canReveal);
    }
}
