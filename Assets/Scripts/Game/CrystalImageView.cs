using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class CrystalImageView : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private List<CrystalModel> crystalModels;
        private Dictionary<CrystalType, Sprite> _crystalSprites;

        public void Init()
        {
            _crystalSprites = new Dictionary<CrystalType, Sprite>();
            foreach (CrystalModel crystalModel in crystalModels)
            {
                _crystalSprites.Add(crystalModel.type, crystalModel.sprite);
            }
        }

        public void SetCrystalImage(CrystalType crystalType)
        {
            image.sprite = _crystalSprites[crystalType];
        }

        public void SetStatus(bool status)
        {
            gameObject.SetActive(status);
        }
        
        public void SetLocalPosition(Vector2 localPosition)
        {
            transform.localPosition = localPosition;
        }

        public void SetLocalScale(Vector3 localScale)
        {
            transform.localScale = localScale;
        }
        
        public void SetSize(Vector2 size)
        {
            image.rectTransform.sizeDelta = size;
        }

        public RectTransform GetRectTransform()
        {
            return image.rectTransform;
        }

        public void SetParent(RectTransform parent)
        {
            image.rectTransform.SetParent(parent);
        }
    }

    public enum CrystalType
    {
        Blue,
        Red,
        Yellow
    }
}