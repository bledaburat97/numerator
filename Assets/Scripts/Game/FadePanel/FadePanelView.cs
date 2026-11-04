using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Scripts
{
    public class FadePanelView : MonoBehaviour, IFadePanelView
    {
        [SerializeField] private Image fadeImage;
        [SerializeField] private Image tutorialFadeImage;
        [SerializeField] private Image boardFadeImage;
        private EventTrigger.Entry _boardDismiss;
        public void SetBoardDismiss(System.Action dismiss)
        {
            // Selection dismissal covers the whole UI; eligible slots render above it.
            boardFadeImage.rectTransform.SetParent(fadeImage.transform.parent,false);
            FruitLabStyle.Stretch(boardFadeImage.rectTransform);
            boardFadeImage.transform.SetAsLastSibling();
            var trigger=boardFadeImage.GetComponent<EventTrigger>()??boardFadeImage.gameObject.AddComponent<EventTrigger>();
            if(_boardDismiss!=null)trigger.triggers.Remove(_boardDismiss);
            _boardDismiss=new EventTrigger.Entry {eventID=EventTriggerType.PointerDown};
            _boardDismiss.callback.AddListener(_=>dismiss?.Invoke());trigger.triggers.Add(_boardDismiss);
            boardFadeImage.raycastTarget=true;
        }

        public Image GetFadeImage()
        {
            return fadeImage;
        }
        
        public Image GetTutorialFadeImage()
        {
            return tutorialFadeImage;
        }

        public Image GetBoardFadeImage()
        {
            return boardFadeImage;
        }
    }

    public interface IFadePanelView
    {
        void SetBoardDismiss(System.Action dismiss);
        Image GetFadeImage();
        Image GetTutorialFadeImage();
        Image GetBoardFadeImage();
    }
}
