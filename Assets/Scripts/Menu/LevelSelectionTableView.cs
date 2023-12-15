using Menu;
using UnityEngine;

namespace Scripts
{
    public class LevelSelectionTableView : MonoBehaviour, ILevelSelectionTableView
    {
        [SerializeField] private LevelButtonView levelButtonPrefab;
        private LevelButtonViewFactory _levelButtonViewFactory;
        private DirectionButtonViewFactory _directionButtonViewFactory;
        [SerializeField] private DirectionButtonView directionButtonPrefab;
        [SerializeField] private RectTransform pineTree;
        
        public void Init(LevelButtonViewFactory levelButtonViewFactory, DirectionButtonViewFactory directionButtonViewFactory)
        {
            _levelButtonViewFactory = levelButtonViewFactory;
            _directionButtonViewFactory = directionButtonViewFactory;
        }

        public ILevelButtonView CreateLevelButtonView()
        {
            return _levelButtonViewFactory.Spawn(pineTree, levelButtonPrefab);
        }

        public IDirectionButtonView CreateDirectionButton()
        {
            return _directionButtonViewFactory.Spawn(transform, directionButtonPrefab);
        }

    }

    public interface ILevelSelectionTableView
    {
        void Init(LevelButtonViewFactory levelButtonViewFactory, DirectionButtonViewFactory directionButtonViewFactory);
        ILevelButtonView CreateLevelButtonView();
        IDirectionButtonView CreateDirectionButton();
    }
}