using UnityEngine;
using Zenject;

namespace Scripts
{
    public class HomeContext : MonoBehaviour
    {
        [Inject] private IGameOptionTracker _gameOptionTracker;
        [Inject] private ISinglePlayerButtonController _singlePlayerButtonController;
        [Inject] private IMultiPlayerButtonController _multiPlayerButtonController;
        void Start()
        {
            CreatePlayButtons();
        }
        
        private void CreatePlayButtons()
        {
            _singlePlayerButtonController.Initialize(_gameOptionTracker);
            _multiPlayerButtonController.Initialize(_gameOptionTracker);
        }
    }
}