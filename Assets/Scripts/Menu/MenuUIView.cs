using Scripts;
using UnityEngine;

namespace Menu
{
    public class MenuUIView :  MonoBehaviour, IMenuUIView
    {
        [SerializeField] private BaseButtonView singlePlayerButton;
        [SerializeField] private BaseButtonView multiplayerButton;
        [SerializeField] private BaseButtonView collectionButton;
        [SerializeField] private FruitCollectionView collectionView;
        [SerializeField] private GardenNoticeView gardenNotice;
        public IBaseButtonView GetCollectionButton()=>collectionButton;
        public FruitCollectionView GetCollectionView()=>collectionView;
        public GardenNoticeView GetGardenNoticeView()
        {
            if(gardenNotice!=null)return gardenNotice;
            var canvas=GetComponentInParent<Canvas>();
            var parent=canvas.transform.Find("SafeArea") ?? canvas.transform;
            var rect=GardenUi.Rect(parent,"GardenNotice");
            gardenNotice=rect.GetComponent<GardenNoticeView>() ?? rect.gameObject.AddComponent<GardenNoticeView>();
            gardenNotice.ApplyLayout();gardenNotice.Hide();return gardenNotice;
        }

        public IBaseButtonView GetSinglePlayerButton()
        {
            return singlePlayerButton;
        }
        
        public IBaseButtonView GetMultiplayerButton()
        {
            return multiplayerButton;
        }
        
        public void SetSinglePlayerButtonText(string text)
        {
            singlePlayerButton.SetText(text);
        }

        public void SetMultiplayerAvailability(bool unlocked)
        {
            CanvasGroup group = multiplayerButton.GetComponent<CanvasGroup>();
            if (group == null) group = multiplayerButton.gameObject.AddComponent<CanvasGroup>();
            group.alpha = unlocked ? 1f : .45f;
            group.interactable = unlocked;
            group.blocksRaycasts = unlocked;
        }
    }

    public interface IMenuUIView
    {
        IBaseButtonView GetSinglePlayerButton();
        IBaseButtonView GetMultiplayerButton();
        IBaseButtonView GetCollectionButton();
        void SetSinglePlayerButtonText(string text);
        void SetMultiplayerAvailability(bool unlocked);
    }
}
