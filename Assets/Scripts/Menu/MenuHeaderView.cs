using UnityEngine;

namespace Scripts
{
    public class MenuHeaderView : MonoBehaviour, IMenuHeaderView
    {
        [SerializeField] private TextHolderAdjustment starHolder;
        [SerializeField] private TextHolderAdjustment wildHolder;

        public void Init(int starCount, int wildCardCount)
        {
            starHolder.SetText(starCount.ToString());
            wildHolder.SetText(wildCardCount.ToString());
            starHolder.SetPosition();
            wildHolder.SetPosition();
        }
    }

    public interface IMenuHeaderView
    {
        void Init(int starCount, int wildCardCount);
    }
}