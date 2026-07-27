using UnityEngine;

namespace Scripts
{
    public class BoundaryView : MonoBehaviour, IBoundaryView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private CoinView coinPrefab;
        [SerializeField] private CrystalView crystalPrefab;
        private CoinViewFactory _coinViewFactory;
        private CrystalViewFactory _crystalViewFactory;
        
        public void Init(Vector2 localPosition, CoinViewFactory coinViewFactory, CrystalViewFactory crystalViewFactory)
        {
            transform.localPosition = localPosition;
            transform.localScale = Vector3.one;
            _coinViewFactory = coinViewFactory;
            _crystalViewFactory = crystalViewFactory;
        }

        public IRewardTokenView CreateRewardToken(LifeBarRewardType rewardType)
        {
            switch (rewardType)
            {
                case LifeBarRewardType.Crystal:
                    if (crystalPrefab == null) return null;

                    return _crystalViewFactory.Spawn(transform, crystalPrefab);
                case LifeBarRewardType.Coin:
                default:
                    if (coinPrefab == null) return null;

                    return _coinViewFactory.Spawn(transform, coinPrefab);
            }
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }

        public void DestroyObject()
        {
            Destroy(gameObject);
        }
    }

    public interface IBoundaryView
    {
        void Init(Vector2 localPosition, CoinViewFactory coinViewFactory, CrystalViewFactory crystalViewFactory);
        IRewardTokenView CreateRewardToken(LifeBarRewardType rewardType);
        RectTransform GetRectTransform();
        void DestroyObject();
    }
}
