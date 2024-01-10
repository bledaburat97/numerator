using UnityEngine;
using Zenject;

namespace Scripts
{
    public class WaitingSceneContext : MonoBehaviour
    {
        [Inject] private IReadyButtonController _readyButtonController;
        void Start()
        {
            CreateButtons();
        }
        
        private void CreateButtons()
        {
            _readyButtonController.Initialize();
        }
    }
}