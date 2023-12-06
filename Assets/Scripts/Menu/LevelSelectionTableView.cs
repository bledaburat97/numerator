using Menu;
using UnityEngine;

namespace Scripts
{
    public class LevelSelectionTableView : MonoBehaviour, ILevelSelectionTableView
    {
        [SerializeField] private LevelButtonView levelButtonPrefab;
        private LevelButtonViewFactory _levelButtonViewFactory;
        
        public void Init(LevelButtonViewFactory levelButtonViewFactory)
        {
            _levelButtonViewFactory = levelButtonViewFactory;
        }

        public ILevelButtonView CreateLevelButtonView()
        {
            return _levelButtonViewFactory.Spawn(transform, levelButtonPrefab);
        }
    }

    public interface ILevelSelectionTableView
    {
        void Init(LevelButtonViewFactory levelButtonViewFactory);
        ILevelButtonView CreateLevelButtonView();
    }
}