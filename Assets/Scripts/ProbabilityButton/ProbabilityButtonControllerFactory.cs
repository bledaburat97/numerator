using Factory;

namespace Scripts
{
    public class ProbabilityButtonControllerFactory : BaseClassFactory<ProbabilityButtonController, IProbabilityButtonController>
    {
        protected override IProbabilityButtonController Create()
        {
            return new ProbabilityButtonController();
        }
    }
}