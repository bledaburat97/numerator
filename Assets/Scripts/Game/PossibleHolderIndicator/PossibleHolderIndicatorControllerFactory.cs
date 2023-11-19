using Factory;

namespace Scripts
{
    public class PossibleHolderIndicatorControllerFactory : BaseClassFactory<PossibleHolderIndicatorController, IPossibleHolderIndicatorController>
    {
        protected override IPossibleHolderIndicatorController Create()
        {
            return new PossibleHolderIndicatorController();
        }
    }
}