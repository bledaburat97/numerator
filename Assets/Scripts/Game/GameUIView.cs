using TMPro;
using UnityEngine;

namespace Scripts
{
    public class GameUIView : MonoBehaviour, IGameUIView
    {
        [SerializeField] private TMP_Text levelIdText;
        [SerializeField] private RectTransform scrollAreaRectTransform;
        public void SetLevelId(ILevelTracker levelTracker)
        {
            levelIdText.SetText("Level " + (levelTracker.GetLevelId() + 1));
        }

        public void DisableLevelId()
        {
            levelIdText.gameObject.SetActive(false);
        }

        public void IncreaseSizeAndPositionOfScrollArea(float sizeOfResultBlock)
        {
            scrollAreaRectTransform.localPosition = new Vector2(0, scrollAreaRectTransform.localPosition.y + sizeOfResultBlock / 2);
            scrollAreaRectTransform.sizeDelta = new Vector2(scrollAreaRectTransform.sizeDelta.x,scrollAreaRectTransform.sizeDelta.y + sizeOfResultBlock);
        }
    }

    public interface IGameUIView
    {
        void SetLevelId(ILevelTracker levelTracker);
        void DisableLevelId();
        void IncreaseSizeAndPositionOfScrollArea(float sizeOfResultBlock);
    }
}