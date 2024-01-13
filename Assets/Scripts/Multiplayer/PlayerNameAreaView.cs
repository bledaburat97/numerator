using System;
using UnityEngine;

namespace Scripts
{
    public class PlayerNameAreaView : MonoBehaviour, IPlayerNameAreaView
    {
        [SerializeField] private PlayerNameView playerNamePrefab;
        private PlayerNameViewFactory _playerNameViewFactory;
        private Action _onDestroyAction;
        public void Init(Action onDestroyAction)
        {
            _onDestroyAction = onDestroyAction;
        }

        public IPlayerNameView CreatePlayerNameView()
        {
            _playerNameViewFactory = new PlayerNameViewFactory();
            IPlayerNameView playerNameView = _playerNameViewFactory.Spawn(transform, playerNamePrefab);
            return playerNameView;
        }

        private void OnDestroy()
        {
            Debug.Log("OnDestroyPlayerNameAreaView");
            _onDestroyAction?.Invoke();
        }
    }

    public interface IPlayerNameAreaView
    {
        void Init(Action onDestroyAction);
        IPlayerNameView CreatePlayerNameView();
    }
}