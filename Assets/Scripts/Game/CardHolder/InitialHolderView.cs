using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class InitialHolderView : MonoBehaviour, IInitialHolderView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private RectTransform possibleHolderIndicatorHolderTransform;
        [SerializeField] private PossibleHolderIndicatorView possibleHolderIndicatorPrefab;
        [SerializeField] private TMP_Text text;
        private Image _fruitGhost;

        private void Awake() => ApplyLabStyle();

        public void ApplyLabStyle()
        {
            Transform surface = transform.Find("Image");
            if (surface != null) FruitLabStyle.RecessSurface(surface.GetComponent<Image>());
            if (text != null) text.gameObject.SetActive(false);
        }
        
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
        
        public void SetFruit(Sprite fruit)
        {
            if (text != null) text.gameObject.SetActive(false);
            _fruitGhost = FruitLabStyle.Detail(transform, "FruitGhost", new Color(1f, 1f, 1f, .44f));
            _fruitGhost.sprite = fruit;
            _fruitGhost.type = Image.Type.Simple;
            _fruitGhost.preserveAspect = true;
            _fruitGhost.material = Resources.Load<Material>("Art/FruitLabGhost");
            _fruitGhost.rectTransform.anchorMin = new Vector2(.13f, .13f);
            _fruitGhost.rectTransform.anchorMax = new Vector2(.87f, .87f);
            _fruitGhost.rectTransform.offsetMin = _fruitGhost.rectTransform.offsetMax = Vector2.zero;
            // Keep the stamp behind the movable card, including after reapplying a style.
            _fruitGhost.transform.SetSiblingIndex(1);
        }
        
        public IPossibleHolderIndicatorView CreatePossibleHolderIndicatorView()
        {
            return Instantiate(possibleHolderIndicatorPrefab, possibleHolderIndicatorHolderTransform);
        }

        public RectTransform GetInitialHolderRectTransform()
        {
            return rectTransform;
        }

        public Vector2 GetSizeOfHolderIndicatorPrefab()
        {
            return possibleHolderIndicatorPrefab.GetRectTransform().sizeDelta;
        }
        
    }

    public interface IInitialHolderView : IBaseHolderView
    {
        void SetFruit(Sprite fruit);
        IPossibleHolderIndicatorView CreatePossibleHolderIndicatorView();
        RectTransform GetInitialHolderRectTransform();
        Vector2 GetSizeOfHolderIndicatorPrefab();
    }
}
