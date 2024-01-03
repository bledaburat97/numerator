using System;
using UnityEngine;

namespace Scripts
{
    public class DragTutorialController : IDragTutorialController
    {
        private IDragTutorialView _view;
        private TutorialData _data;
        
        public DragTutorialController(IDragTutorialView view)
        {
            _view = view;
        }
        
        public void Initialize(IFadePanelController fadePanelController)
        {
            //open tutorial fadepanel.
            
        }

        private void DragAnimation()
        {
            //

        }
    }

    public interface IDragTutorialController
    {
        
    }
    
    public class TutorialData
    {
        public RectTransform startingObject;
        public RectTransform endingObject;
        public Vector3 startingPoint;
        public Vector3 endingPoint;
        public float delay;
        public string tutorialTextKey;
        public INormalCardItemController cardItem;
        public bool animateHand = true;
        public Action onStartAnimation;
        public bool isTextAreaVisible = true;
        public Action onComplete;
    }
}