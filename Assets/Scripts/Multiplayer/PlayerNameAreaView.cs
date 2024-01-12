using UnityEngine;

namespace Scripts
{
    public class PlayerNameAreaView : MonoBehaviour, IPlayerNameAreaView
    {
        [SerializeField] private PlayerNameView playerNamePrefab;
        private PlayerNameViewFactory _playerNameViewFactory;

        public IPlayerNameView CreatePlayerNameView()
        {
            _playerNameViewFactory = new PlayerNameViewFactory();
            IPlayerNameView playerNameView = _playerNameViewFactory.Spawn(transform, playerNamePrefab);
            return playerNameView;
        }
    }

    public interface IPlayerNameAreaView
    {
        IPlayerNameView CreatePlayerNameView();
    }
}