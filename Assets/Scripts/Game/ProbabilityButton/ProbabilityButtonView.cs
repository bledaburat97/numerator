using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class ProbabilityButtonView : MonoBehaviour, IProbabilityButtonView
    {
        [SerializeField] private Image image;
        [SerializeField] private Button button;
        [SerializeField] private Image frame;
        public void Init(ProbabilityButtonModel model)
        {
            transform.localScale = Vector3.one;
            button.onClick.AddListener(() => model.onClickAction.Invoke());
            SetColorOfImage(ConstantValues.GetProbabilityTypeToColorMapping()[model.probabilityType]);
        }
        
        private void SetColorOfImage(Color color)
        {
            image.color = color;
        }
        
        public void SetFrameStatus(bool status)
        {
            frame.gameObject.SetActive(status);
        }
    }

    public interface IProbabilityButtonView
    {
        void Init(ProbabilityButtonModel model);
        void SetFrameStatus(bool status);
    }
}