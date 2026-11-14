using System;

namespace Scripts
{
    public sealed class TutorialInputGate
    {
        private bool _restricted;
        private int _card = -1, _board = -1;
        private bool _guess;
        private bool _settings;
        private bool _navigationLocked;
        public void SetNavigationLocked(bool locked)=>_navigationLocked=locked;
        private int _noteCard=-1;
        private GameUIButtonType? _power;
        public event Action<int> NoteOpened;
        public event Action<int> FruitExcluded;
        public event Action<int,int> PositionExcluded;
        public Func<GameUIButtonType, bool> FirstUseHandler;
        public bool IsRestricted => _restricted;
        public void Block() { _restricted = true; _card = _board = _noteCard = -1; _guess = _settings = false;_power=null; }
        public void AllowPower(GameUIButtonType type){Block();_power=type;}
        public void AllowPlacement(int card, int board) { Block(); _card = card; _board = board; }
        public void AllowGuess() { Block(); _guess = true; }
        public void AllowSettings() => _settings = true;
        public void Clear() { _restricted = false; _card = _board = _noteCard = -1; _guess = _settings = false;_power=null; }
        public void AllowNotes(int card){Block();_noteCard=card;}
        public bool AllowsNotes(int card)=>!_restricted || _noteCard==card;
        public void NotifyNoteOpened(int card)=>ObserverNotification.Send(NoteOpened,card);
        public void NotifyFruitExcluded(int card)=>ObserverNotification.Send(FruitExcluded,card);
        public void NotifyPositionExcluded(int card,int slot)
        {if(PositionExcluded!=null)foreach(Action<int,int> handler in PositionExcluded.GetInvocationList())ObserverNotification.Run(()=>handler(card,slot));}
        public bool AllowsCard(int card) => !_restricted || card == _card || card==_noteCard;
        public bool AllowsPlacement(int card, int board) => !_restricted || (card == _card && board == _board);
        public bool AllowsButton(GameUIButtonType type) => !(_navigationLocked && type==GameUIButtonType.Settings)
            && (!_restricted || (_guess && type == GameUIButtonType.Check)
            || (_settings && type == GameUIButtonType.Settings) || _power==type);
        public bool TryExplain(GameUIButtonType type) => FirstUseHandler?.Invoke(type) == true;
    }
}
