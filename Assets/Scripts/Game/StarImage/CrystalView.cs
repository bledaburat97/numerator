using Factory;

namespace Scripts
{
    public class CrystalView : RewardTokenView, ICrystalView
    {
    }

    public interface ICrystalView : IRewardTokenView
    {
    }

    public class CrystalViewFactory : BaseObjectFactory<CrystalView, ICrystalView>
    {
    }
}
