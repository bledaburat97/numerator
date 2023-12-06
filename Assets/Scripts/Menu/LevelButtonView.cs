using System;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class LevelButtonView : BaseButtonView, ILevelButtonView
    {
        [SerializeField] private Image lockImage;
        [SerializeField] private StarImageView[] starImageViews = new StarImageView[3];

        private LevelButtonModel _model;
        
        public void Init(LevelButtonModel model)
        {
            _model = model;
            transform.localPosition = new Vector3(model.localPosition.x, model.localPosition.y, transform.localPosition.z);
            transform.localScale = Vector3.one;
            lockImage.gameObject.SetActive(true);
            text.gameObject.SetActive(false);
            button.enabled = false;
            innerBgOffsetMin = innerBg.rectTransform.offsetMin;
            innerBgOffsetMax = innerBg.rectTransform.offsetMax;
            innerBg.color = Color.gray;
            for (int i = 0; i < starImageViews.Length; i++)
            {
                starImageViews[i].SetStarStatus(i < _model.starCount);
            }
        }

        public void SetButtonActive()
        {
            button.enabled = true;
            text.gameObject.SetActive(true);
            lockImage.gameObject.SetActive(false);
            text.SetText((_model.levelId + 1).ToString());
            SetOnSelectAction(_model.onSelect);
            innerBg.color = Color.blue;
        }
        
        private void SetOnSelectAction(Action<int> onSelect)
        {
            button.onClick.AddListener(() => onSelect.Invoke(_model.levelId));
        }
        
        public void Select(bool status)
        {
            if (status) innerBg.color = Color.magenta;
            else innerBg.color = Color.blue;
        }
        
        public void Destroy()
        {
            Destroy(gameObject);
        }

    }

    public interface ILevelButtonView : IBaseButtonView
    {
        void Init(LevelButtonModel model);
        void SetButtonActive();
        void Select(bool status);
        void Destroy();
    }
    
    public class LevelButtonModel
    {
        public Vector2 localPosition;
        public Action<int> onSelect;
        public int levelId;
        public int starCount;
    }
}