/*using System;

namespace Scripts
{
    public class ActiveLevelIdController : IActiveLevelIdController
    {
        private int _activeLevelId;
        private bool _isNewGame;
        private ILevelTracker _levelTracker;
        private IGameSaveService _gameSaveService;
        public event EventHandler<ActiveLevelChangedEventArgs> LevelSelectionChanged;
        
        public void Initialize(ILevelTracker levelTracker, IGameSaveService gameSaveService)
        {
            _levelTracker = levelTracker;
            _gameSaveService = gameSaveService;
            _activeLevelId = 0;
            _isNewGame = true;
            
            if (_gameSaveService.HasSavedGame())
            {
                _activeLevelId = gameSaveService.GetSavedLevel().LevelId;
                _isNewGame = false;
            }
            else
            {
                _activeLevelId = _levelTracker.GetLevelId();
                _isNewGame = true;
            }
            _levelTracker.SetLevelId(_activeLevelId);
        }

        public int GetActiveLevelId()
        {
            return _activeLevelId;
        }

        public bool IsNewGame()
        {
            return _isNewGame;
        }

        public void UpdateActiveLevelId(int levelId)
        {
            _activeLevelId = levelId;
            if (_gameSaveService.HasSavedGame())
            {
                if (levelId == _gameSaveService.GetSavedLevel().LevelId)
                {
                    _isNewGame = false;
                }
                else
                {
                    _isNewGame = true;
                }
            }
            else
            {
                _isNewGame = true;
            }
            _levelTracker.SetLevelId(_activeLevelId);
            LevelSelectionChanged?.Invoke(this, new ActiveLevelChangedEventArgs()
            {
                activeLevelId = _activeLevelId,
                isNewGame = _isNewGame
            });
        }
    }

    public interface IActiveLevelIdController
    {
        void Initialize(ILevelTracker levelTracker, IGameSaveService gameSaveService);
        event EventHandler<ActiveLevelChangedEventArgs> LevelSelectionChanged;
        int GetActiveLevelId();
        bool IsNewGame();
        void UpdateActiveLevelId(int levelId);
    }
    
    public class ActiveLevelChangedEventArgs : EventArgs
    {
        public int activeLevelId;
        public bool isNewGame;
    }
}
*/