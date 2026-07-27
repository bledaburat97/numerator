using Factory;

namespace Scripts
{
    public class CoinView : RewardTokenView, ICoinView
    {
    }

    public interface ICoinView : IRewardTokenView
    {
    }

    public class CoinViewFactory : BaseObjectFactory<CoinView, ICoinView>
    {
    }
}
