using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Scripts
{
    public class CardItemInfoPopupView : MonoBehaviour, ICardItemInfoPopupView
    {
        [SerializeField] private ProbabilityButtonView[] probabilityButtonList = new ProbabilityButtonView[3];
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

        public IProbabilityButtonView GetProbabilityButtonViewByIndex(int index)
        {
            return probabilityButtonList[index];
        }

    }
    
    public interface ICardItemInfoPopupView
    {
        void Init(CardHolderIndicatorButtonViewFactory cardHolderIndicatorButtonViewFactory);
        ICardHolderIndicatorButtonView CreateCardHolderIndicatorButtonView();
        IProbabilityButtonView GetProbabilityButtonViewByIndex(int index);
        void SetStatus(bool status);
    }

    public enum ProbabilityType
    {
        Certain,
        Probable,
        NotExisted
    }
}