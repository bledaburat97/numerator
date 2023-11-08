namespace Views
{
    public class ResultChecker : IResultChecker
    {
        private int[] _targetNumbers;

        public ResultChecker()
        {
            _targetNumbers = new int[] { 1, 2, 3 };
        }

        public Result CheckTargetAchieved(int[] finalNumbers)
        {
            Result result = new Result();
            
            for (int i = 0; i < finalNumbers.Length; i++)
            {
                for (int j = 0; j < _targetNumbers.Length; j++)
                {
                    if (finalNumbers[i] == _targetNumbers[j])
                    {
                        if (i == j) result.CorrectPos++;
                        else result.WrongPos++;
                    }
                }
            }

            result.NoExistence = finalNumbers.Length - result.CorrectPos - result.WrongPos;
            return result;
        }
    }
    
    public interface IResultChecker
    {
        Result CheckTargetAchieved(int[] finalNumbers);
    }

    public struct Result
    {
        public int CorrectPos;
        public int WrongPos;
        public int NoExistence;
    }
}