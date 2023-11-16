using System.Collections.Generic;
using Unity.VisualScripting;

namespace Scripts
{
    public static class ResultModelListCreator
    {
        private static readonly List<int> _targetNumbers = new List<int>(){1,2,3};

        public static List<ResultModel> GetResultModelList(List<int> cardNumbers)
        {
            int correctPos = 0;
            int wrongPos = 0;
            
            for (int i = 0; i < cardNumbers.Count; i++)
            {
                for (int j = 0; j < _targetNumbers.Count; j++)
                {
                    if (cardNumbers[i] == _targetNumbers[j])
                    {
                        if (i == j) correctPos++;
                        else wrongPos++;
                    }
                }
            }
            
            List<ResultModel> resultModels = new List<ResultModel>();
            if (correctPos > 0)
            {
                resultModels.Add(new ResultModel()
                {
                    number = correctPos,
                    color = ConstantValues.GetCardPositionToColorMapping()[CardPositionCorrectness.Correct]
                });
            }

            if (wrongPos > 0)
            {
                resultModels.Add(new ResultModel()
                {
                    number = wrongPos,
                    color = ConstantValues.GetCardPositionToColorMapping()[CardPositionCorrectness.Wrong]
                });
            }

            if (cardNumbers.Count - correctPos - wrongPos > 0)
            {
                resultModels.Add(new ResultModel()
                {
                    number = cardNumbers.Count - correctPos - wrongPos,
                    color = ConstantValues.GetCardPositionToColorMapping()[CardPositionCorrectness.NotExisted]
                });
            }

            return resultModels;
        }
    }
}