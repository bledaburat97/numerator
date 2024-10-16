using TMPro;
using UnityEngine;

namespace Scripts
{
    public class InitialHolderView : MonoBehaviour, IInitialHolderView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private RectTransform possibleHolderIndicatorHolderTransform;
        [SerializeField] private PossibleHolderIndicatorView possibleHolderIndicatorPrefab;
        [SerializeField] private TMP_Text text;
        [SerializeField] private RectTransform boxHolder;
        
        public void SetLocalScale()
        {
            transform.localScale = Vector3.one;
        }

        public void SetLocalPosition(Vector2 localPosition)
        {
            transform.localPosition = localPosition;
        }

        public void SetSize(Vector2 size)
        {
            rectTransform.sizeDelta = size;
        }
        
        public Vector3 GetGlobalPosition()
        {
            return transform.position;
        }
        
        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }
        
        public void DestroyObject()
        {
            Destroy(gameObject);
        }
        
        /*
        public void SetColor()
        {
            frame.color = ConstantValues.INITIAL_CARD_HOLDER_COLOR;
            text.color = ConstantValues.INITIAL_CARD_HOLDER_COLOR;
        }
        */
        
        public void SetText(int number)
        {
            text.SetText(number.ToString());
        }
        
        public IPossibleHolderIndicatorView CreatePossibleHolderIndicatorView()
        {
            return Instantiate(possibleHolderIndicatorPrefab, possibleHolderIndicatorHolderTransform);
        }

        public RectTransform GetBoxHolderRectTransform()
        {
            return boxHolder;
        }

        public Vector2 GetSizeOfHolderIndicatorPrefab()
        {
            return possibleHolderIndicatorPrefab.GetRectTransform().sizeDelta;
        }
        
    }

    public interface IInitialHolderView : IBaseHolderView
    {
        void SetText(int number);
        IPossibleHolderIndicatorView CreatePossibleHolderIndicatorView();
        RectTransform GetBoxHolderRectTransform();
        Vector2 GetSizeOfHolderIndicatorPrefab();
    }
}