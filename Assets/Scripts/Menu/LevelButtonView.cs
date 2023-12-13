using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class LevelButtonView : MonoBehaviour, ILevelButtonView
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private Button button;
        [SerializeField] protected Image innerBg;
        [SerializeField] private StarImageView[] starImageViews = new StarImageView[3];
        [SerializeField] private RectTransform starHolder;
        private LevelButtonModel _model;
        
        public void Init(LevelButtonModel model)
        {
            _model = model;
            transform.localPosition = new Vector3(model.localPosition.x, model.localPosition.y, transform.localPosition.z);
            transform.localScale = Vector3.one;
            starHolder.gameObject.SetActive(false);
            text.SetText((_model.levelId + 1).ToString());
            button.enabled = false;
            for (int i = 0; i < starImageViews.Length; i++)
            {
                starImageViews[i].SetStarStatus(i < _model.starCount);
            }
        }

        public void SetButtonActive()
        {
            button.enabled = true;
            starHolder.gameObject.SetActive(true);
            SetOnSelectAction(_model.onSelect);
            innerBg.color = ConstantValues.BOARD_CARD_HOLDER_COLOR;
        }
        
        private void SetOnSelectAction(Action<int> onSelect)
        {
            button.onClick.AddListener(() => onSelect.Invoke(_model.levelId));
        }
        
        public void Select(bool status)
        {
            if (status) innerBg.color = Color.magenta;
            else innerBg.color = ConstantValues.BOARD_CARD_HOLDER_COLOR;
        }
        
        public void Destroy()
        {
            Destroy(gameObject);
        }

    }

    public interface ILevelButtonView
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