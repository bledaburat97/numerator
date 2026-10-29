using System;

namespace Scripts
{
    public sealed class TutorialInputGate
    {
        private bool _restricted;
        private int _card = -1, _board = -1;
        private bool _guess;
        private bool _settings;
        public Func<GameUIButtonType, Action, bool> FirstUseHandler;
        public bool IsRestricted => _restricted;
        public void Block() { _restricted = true; _card = _board = -1; _guess = _settings = false; }
        public void AllowPlacement(int card, int board) { Block(); _card = card; _board = board; }
        public void AllowGuess() { Block(); _guess = true; }
        public void AllowSettings() => _settings = true;
        public void Clear() { _restricted = false; _card = _board = -1; _guess = _settings = false; }
        public bool AllowsCard(int card) => !_restricted || card == _card;
        public bool AllowsPlacement(int card, int board) => !_restricted || (card == _card && board == _board);
        public bool AllowsButton(GameUIButtonType type) => !_restricted || (_guess && type == GameUIButtonType.Check)
            || (_settings && type == GameUIButtonType.Settings);
        public bool TryExplain(GameUIButtonType type, Action resume) => FirstUseHandler?.Invoke(type, resume) == true;
    }
}
