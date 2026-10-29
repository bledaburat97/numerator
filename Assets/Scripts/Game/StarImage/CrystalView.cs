using Factory;
using UnityEngine;

namespace Scripts
{
    public class CrystalView : RewardTokenView, ICrystalView
    {
        [Header("Flight")]
        [SerializeField, Min(.1f)] private float hintFlightDuration = .78f;
        [SerializeField, Min(.1f)] private float collectionFlightDuration = .62f;
        [SerializeField, Range(0f, .4f)] private float arcBend = .16f;
        [SerializeField, Min(0f)] private float maxArcOffset = 40f;
        [SerializeField] private CrystalFlightTrail flightTrail;

        public float GetFlightDuration(bool collection) => collection ? collectionFlightDuration : hintFlightDuration;
        public float ArcBend => arcBend;
        public float MaxArcOffset => maxArcOffset;
        public CrystalFlightTrail FlightTrail => flightTrail;
    }

    public interface ICrystalView : IRewardTokenView
    {
    }

    public class CrystalViewFactory : BaseObjectFactory<CrystalView, ICrystalView>
    {
    }
}
