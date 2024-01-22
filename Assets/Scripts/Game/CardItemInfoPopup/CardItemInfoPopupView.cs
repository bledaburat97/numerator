using UnityEngine;

namespace Scripts
{
    public class CardItemInfoPopupView : MonoBehaviour, ICardItemInfoPopupView
    {
        [SerializeField] private OptionButtonView[] probabilityButtonList = new OptionButtonView[ConstantValues.NUM_OF_PROBABILITY_BUTTONS];
        [SerializeField] private CardHolderIndicatorButtonView cardHolderIndicatorButtonPrefab;
        [SerializeField] private RectTransform cardHolderIndicatorButtonHolderRT;
        private CardHolderIndicatorButtonViewFactory _cardHolderIndicatorButtonViewFactory;
        
        public void Init(CardHolderIndicatorButtonViewFactory cardHolderIndicatorButtonViewFactory)
        {
            _cardHolderIndicatorButtonViewFactory = cardHolderIndicatorButtonViewFactory;
        }

        public void SetStatus(bool status)
        {
            gameObject.SetActive(status);
        }

        public ICardHolderIndicatorButtonView CreateCardHolderIndicatorButtonView()
        {
            return _cardHolderIndicatorButtonViewFactory.Spawn(cardHolderIndicatorButtonHolderRT,
                cardHolderIndicatorButtonPrefab);
        }

        public IOptionButtonView GetProbabilityButtonViewByIndex(int index)
        {
            return probabilityButtonList[index];
        }
    }
    
    public interface ICardItemInfoPopupView
    {
        void Init(CardHolderIndicatorButtonViewFactory cardHolderIndicatorButtonViewFactory);
        ICardHolderIndicatorButtonView CreateCardHolderIndicatorButtonView();
        IOptionButtonView GetProbabilityButtonViewByIndex(int index);
        void SetStatus(bool status);
    }

    public enum ProbabilityType
    {
        Certain = 0,
        Probable = 1,
        NotExisted = 2
    }
}