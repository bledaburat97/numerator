using UnityEngine;

namespace Scripts
{
    public class CardItemInfoPopupView : MonoBehaviour, ICardItemInfoPopupView
    {
        [SerializeField] private BaseButtonView[] probabilityButtonList = new BaseButtonView[ConstantValues.NUM_OF_PROBABILITY_BUTTONS];
        [SerializeField] private BaseButtonView cardHolderIndicatorButton;
        [SerializeField] private RectTransform cardHolderIndicatorButtonHolderRT;
        private BaseButtonViewFactory _cardHolderIndicatorButtonViewFactory;
        
        public void Init(BaseButtonViewFactory cardHolderIndicatorButtonViewFactory)
        {
            _cardHolderIndicatorButtonViewFactory = cardHolderIndicatorButtonViewFactory;
        }

        public void SetStatus(bool status)
        {
            gameObject.SetActive(status);
        }

        public IBaseButtonView CreateCardHolderIndicatorButtonView()
        {
            return _cardHolderIndicatorButtonViewFactory.Spawn(cardHolderIndicatorButtonHolderRT,
                cardHolderIndicatorButton);
        }

        public IBaseButtonView GetProbabilityButtonViewByIndex(int index)
        {
            return probabilityButtonList[index];
        }
    }
    
    public interface ICardItemInfoPopupView
    {
        void Init(BaseButtonViewFactory cardHolderIndicatorButtonViewFactory);
        IBaseButtonView CreateCardHolderIndicatorButtonView();
        IBaseButtonView GetProbabilityButtonViewByIndex(int index);
        void SetStatus(bool status);
    }

    public enum ProbabilityType
    {
        Certain = 0,
        Probable = 1,
        NotExisted = 2
    }
}