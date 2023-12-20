using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class ProbabilityButtonView : BaseButtonView, IProbabilityButtonView
    {
        [SerializeField] private Image point;
        public void Init(ProbabilityButtonModel model)
        {
            transform.localScale = Vector3.one;
            innerBgOffsetMin = innerBg.rectTransform.offsetMin;
            innerBgOffsetMax = innerBg.rectTransform.offsetMax;
            button.onClick.AddListener(() => model.onClickAction.Invoke());
            SetColorOfImage(ConstantValues.GetProbabilityTypeToColorMapping()[model.probabilityType]);
        }
        
        private void SetColorOfImage(Color color)
        {
            innerBg.color = color;
            outerBg.color = color;
        }
        
        public void SetFrameStatus(bool status)
        {
            point.gameObject.SetActive(status);
        }
    }

    public interface IProbabilityButtonView : IBaseButtonView
    {
        void Init(ProbabilityButtonModel model);
        void SetFrameStatus(bool status);
    }
}