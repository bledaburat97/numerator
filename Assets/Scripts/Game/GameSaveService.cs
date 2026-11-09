using System.Collections.Generic;
using System.Linq;
using Game;
using Newtonsoft.Json;
using UnityEngine;

namespace Scripts
{
    public class GameSaveService : IGameSaveService
    {
        private readonly string _saveGameKey = "saved_game_key";
        
        public LevelSaveData GetSavedLevel()
        {
            if (!HasSavedGame()) return null;
            try
            {
                var data = JsonConvert.DeserializeObject<LevelSaveData>(PlayerPrefs.GetString(_saveGameKey));
                if (data == null || data.TargetCards == null || data.CardItemInfoList == null || data.TriedCardsList == null
                    || data.RemainingGuessCount <= 0) return null;
                if (data.Definition?.Mechanic == LevelMechanic.Growing)
                {
                    GrowingBoardState.UpgradeLegacy(data);
                    if (!GrowingBoardState.IsValid(data)) return null;
                }
                if (data.Definition?.Mechanic == LevelMechanic.Dynamite && !DynamiteRoundState.IsValid(data)) return null;
                data.RevealedGuessNumbers ??= new List<int>();
                return data;
            }
            catch (JsonException) { return null; }
        }

        public bool HasSavedGame()
        {
            return PlayerPrefs.HasKey(_saveGameKey);
        }

        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey(_saveGameKey);
            PlayerPrefs.Save();
        }
        
        public void Save(LevelSaveData levelSaveData)
        {
            if (levelSaveData == null) return;
            string data = JsonConvert.SerializeObject(levelSaveData);
            PlayerPrefs.SetString(_saveGameKey, data);
            PlayerPrefs.Save();
        }
        
    }

    public interface IGameSaveService
    {
        LevelSaveData GetSavedLevel();
        void DeleteSave();
        void Save(LevelSaveData levelSaveData);
        bool HasSavedGame();
    }

    public class LevelSaveData
    {
        public string RoundId;
        public List<List<int>> TriedCardsList;
        public List<int> TargetCards;
        public List<CardItemInfo> CardItemInfoList;
        public int RemainingGuessCount;
        public int RemovedBoardHolderCount;
        public int LevelId = -1;
        public LevelData Definition;
        public List<int> RevealedGuessNumbers = new List<int>();
        public int LastEliminationGuessNumber;
        public bool TrialStockPrepared;
        public bool PairWasApplied;
        public bool BoardSplitApplied;
        public bool UltraActive;
        public PairClue PairClue;
        public GrowingBoardState GrowingBoard;
        public DynamiteRoundState Dynamite;
        public List<bool> ActiveRewardTokens;
        public PositionNotesSnapshot PositionNotes;
    }
    
    // Round state belongs to the existing checkpoint, including unopened fruit.
    public sealed class GrowingBoardState
    {
        public const int InitialCount = 2, MaximumCount = 4, GuessesPerGrowth = 2, BonusLives = 4;
        public int ActiveCount = InitialCount;
        public int WrongSinceGrowth;
        public int LastProcessedGuess;
        public bool BonusGranted;
        public bool BonusAtStart;
        public List<int> FullTarget = new List<int>();
        // Only pre-cap-change history uses this fifth fruit; the live board stays at four.
        public List<int> LegacyHistoryTarget;

        public GrowingBoardState Copy() => new GrowingBoardState {
            ActiveCount = ActiveCount, WrongSinceGrowth = WrongSinceGrowth,
            LastProcessedGuess = LastProcessedGuess, BonusGranted = BonusGranted, BonusAtStart = BonusAtStart,
            FullTarget = new List<int>(FullTarget),
            LegacyHistoryTarget = LegacyHistoryTarget == null ? null : new List<int>(LegacyHistoryTarget)
        };

        public static bool IsValid(LevelSaveData save) => IsValidVersion(save,MaximumCount);

        public static void UpgradeLegacy(LevelSaveData save)
        {
            var state = save.GrowingBoard;
            if (state == null) return;
            if (state.FullTarget?.Count == 5 && IsValidVersion(save,5))
            {
                if (save.TriedCardsList.Any(guess => guess.Count == 5)) state.LegacyHistoryTarget = new List<int>(state.FullTarget);
                state.FullTarget = state.FullTarget.Take(MaximumCount).ToList();
                state.ActiveCount = System.Math.Min(state.ActiveCount,MaximumCount);
                save.TargetCards = state.FullTarget.Take(state.ActiveCount).ToList();
                if (state.ActiveCount == MaximumCount)
                {
                    state.WrongSinceGrowth = 0;
                    if (!state.BonusGranted) { state.BonusGranted = true; save.RemainingGuessCount += BonusLives; }
                }
                foreach (var card in save.CardItemInfoList)
                {
                    if (card?.possibleCardHolderIndicatorIndexes == null) continue;
                    if (card.isLocked && card.possibleCardHolderIndicatorIndexes.Any(index => index >= MaximumCount))
                    {
                        card.isLocked = false; card.probabilityType = ProbabilityType.Probable;
                        card.possibleCardHolderIndicatorIndexes = Enumerable.Range(0,state.ActiveCount).ToList();
                    }
                    else card.possibleCardHolderIndicatorIndexes.RemoveAll(index => index >= MaximumCount);
                }
            }
            // Move an unclaimed legacy growth bonus forward exactly once; keep spent rewards/guesses.
            if (!state.BonusAtStart && IsValidVersion(save,MaximumCount))
            {
                if (!state.BonusGranted) save.RemainingGuessCount += BonusLives;
                state.BonusGranted = state.BonusAtStart = true;
            }
        }

        private static bool IsValidVersion(LevelSaveData save,int maximum)
        {
            var state = save.GrowingBoard;
            if (state == null || save.Definition == null || save.Definition.NumOfBoardHolders != InitialCount
                || save.TargetCards == null || save.TriedCardsList == null || save.CardItemInfoList == null
                || save.CardItemInfoList.Count != save.Definition.NumOfCards
                || state.FullTarget == null || state.FullTarget.Count != maximum
                || state.FullTarget.Distinct().Count() != maximum
                || state.FullTarget.Any(card => card < 1 || card > save.Definition.NumOfCards)
                || state.FullTarget.Any(card => save.CardItemInfoList[card-1] == null || !save.CardItemInfoList[card-1].isExisted)
                || save.RemovedBoardHolderCount != 0 || state.LastProcessedGuess != save.TriedCardsList.Count) return false;
            if (state.LegacyHistoryTarget != null && (state.LegacyHistoryTarget.Count != 5
                || state.LegacyHistoryTarget.Distinct().Count() != 5
                || state.LegacyHistoryTarget.Any(card => card < 1 || card > save.Definition.NumOfCards)
                || !state.LegacyHistoryTarget.Take(maximum).SequenceEqual(state.FullTarget))) return false;
            int expected = System.Math.Min(maximum,InitialCount+save.TriedCardsList.Count/GuessesPerGrowth);
            if (state.ActiveCount != expected || (state.BonusAtStart ? !state.BonusGranted : state.BonusGranted != (expected == maximum))
                || state.WrongSinceGrowth != (expected == maximum ? 0 : save.TriedCardsList.Count%GuessesPerGrowth)
                || !save.TargetCards.SequenceEqual(state.FullTarget.Take(expected))
                || save.RemainingGuessCount <= 0
                || save.RemainingGuessCount > save.Definition.MaxNumOfTries+(state.BonusGranted ? BonusLives : 0)) return false;
            for (int i = 0; i < save.TriedCardsList.Count; i++)
            {
                int count = save.TriedCardsList[i]?.Count ?? 0;
                bool archivedFive = state.LegacyHistoryTarget != null && i >= 6 && count == 5;
                if (!archivedFive && count != System.Math.Min(maximum,InitialCount+i/GuessesPerGrowth)) return false;
            }
            return true;
        }
    }


    public sealed class DynamiteRoundState
    {
        public const int GuessLimit = 4;
        public int BoardIndex = -1;
        public int LastProcessedGuess;
        public int GuessesUsed;
        public bool Disarmed, Exploded;
        [JsonIgnore] public int Remaining => System.Math.Max(0,GuessLimit-GuessesUsed);
        [JsonIgnore] public bool Revealed => BoardIndex >= 0;
        public DynamiteRoundState Copy() => (DynamiteRoundState)MemberwiseClone();

        public static bool IsValid(LevelSaveData save)
        {
            var state = save.Dynamite;
            int count = save.TargetCards?.Count ?? 0;
            if (state == null || save.Definition == null || save.TriedCardsList == null || save.CardItemInfoList == null || count < 1 || count > 5
                || count != save.Definition.NumOfBoardHolders-save.RemovedBoardHolderCount || save.CardItemInfoList.Count != save.Definition.NumOfCards
                || save.TargetCards.Any(fruit => fruit < 1 || fruit > save.CardItemInfoList.Count)
                || state.Exploded || save.RemainingGuessCount <= 0 || state.LastProcessedGuess != save.TriedCardsList.Count
                || state.GuessesUsed < 0 || state.GuessesUsed > GuessLimit || state.GuessesUsed > state.LastProcessedGuess
                || (!state.Disarmed && (state.GuessesUsed != state.LastProcessedGuess || state.GuessesUsed >= GuessLimit))
                || state.BoardIndex < -1 || state.BoardIndex >= count || (state.BoardIndex < 0 && (state.LastProcessedGuess > 0 || state.Disarmed))
                || save.TriedCardsList.Any(guess => guess == null || guess.Count != count)) return false;
            if (!state.Disarmed) return true;
            int fruit = save.TargetCards[state.BoardIndex];
            if (fruit < 1 || fruit > save.CardItemInfoList.Count) return false;
            var info = save.CardItemInfoList[fruit-1];
            return (info != null && info.isLocked && info.possibleCardHolderIndicatorIndexes.Contains(state.BoardIndex))
                || save.TriedCardsList.Any(guess => guess[state.BoardIndex] == fruit);
        }
    }

}
