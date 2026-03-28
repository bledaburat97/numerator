using UnityEngine;

namespace Scripts
{
    public class MenuHeaderView : MonoBehaviour, IMenuHeaderView
    {
        [SerializeField] private TextHolderAdjustment starHolder;

        public void Init(int starCount)
        {
            starHolder.SetText(starCount.ToString());
            starHolder.SetPosition();
        }
    }

    public interface IMenuHeaderView
    {
        void Init(int starCount);
    }
}