using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class OptionButtonView : BaseButtonView, IOptionButtonView
    {
        [SerializeField] private Image point;
        public void Init(OptionButtonModel model)
        {
            transform.localScale = Vector3.one;
            innerBgOffsetMin = innerBg.rectTransform.offsetMin;
            innerBgOffsetMax = innerBg.rectTransform.offsetMax;
            button.onClick.AddListener(() => model.onClickAction.Invoke());
        }
        
        public void SetColorOfImage(Color color)
        {
            innerBg.color = color;
            outerBg.color = color;
        }
        
        public void SetPointStatus(bool status)
        {
            point.gameObject.SetActive(status);
        }
    }

    public interface IOptionButtonView : IBaseButtonView
    {
        void SetColorOfImage(Color color);
        void Init(OptionButtonModel model);
        void SetPointStatus(bool status);
    }
}