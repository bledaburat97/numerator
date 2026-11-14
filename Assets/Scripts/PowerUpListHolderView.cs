using System.Collections.Generic;
using System.Linq;
using Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class PowerUpListHolderView : MonoBehaviour, IPowerUpListHolderView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private PowerUpButtonView buttonPrefab;
        public PowerUpButtonView ButtonPrefab=>buttonPrefab;
        [SerializeField] private PowerUpButtonCollection buttons=new PowerUpButtonCollection();
        public PowerUpButtonCollection Buttons=>buttons;

        private bool _ultraActive;
        private bool _locked;
        private CanvasGroup _canvasGroup;
        private static IReadOnlyList<RewardType> DisplayOrder=>PowerUpDefinition.DisplayOrder;
        private int _levelId = int.MaxValue - 1;
        private float _width, _height;
        private int _rows = 1;

        private void EnsureExtraButtons()
        {foreach(var type in PowerUpTypeUtility.OrderedRewardTypes)buttons.GetOrCreate(type,buttonPrefab,transform);}

        public void SetProgression(int levelId)
        {
            if (_levelId == levelId) return;
            _levelId = levelId;
            if (_width > 0) ApplyLabLayout(_width, _height, _rows);
        }

        public void ApplyLabLayout(float width, float height, int rows = 1)
        {
            EnsureExtraButtons(); _width = width; _height = height; _rows = Mathf.Clamp(rows, 1, 2);
            // All powers are shown directly; hide the retired overflow control in older scenes.
            transform.Find("MorePowers")?.gameObject.SetActive(false);
            foreach (LayoutGroup group in GetComponents<LayoutGroup>()) group.enabled = false;
            var assigned = new HashSet<IBaseButtonView>(DisplayOrder.Select(GetPowerUpButton));
            foreach (var oldButton in GetComponentsInChildren<PowerUpButtonView>(true))
                if (!assigned.Contains(oldButton)) oldButton.gameObject.SetActive(false);
            foreach (var oldGlyph in GetComponentsInChildren<FruitLabGlyph>(true))
                if (oldGlyph.GetComponentInParent<PowerUpButtonView>() == null) oldGlyph.gameObject.SetActive(false);
            const float gap = 2f;
            // Both screens reserve every slot, including powers not introduced yet.
            int slots=DisplayOrder.Count;
            int columns = Mathf.Max(1, Mathf.CeilToInt(slots / (float)_rows));
            float buttonWidth = (width - gap * (columns - 1)) / columns;
            float buttonHeight = (height - gap * (_rows - 1)) / _rows;
            int index = 0;
            foreach (var type in DisplayOrder)
            {
                var button = (PowerUpButtonView)GetPowerUpButton(type);
                button.transform.Find("UseLock")?.gameObject.SetActive(false);
                bool unlocked = PowerUpTypeUtility.IsUnlocked(type, _levelId) && !_locked;
                int row = index / columns, column = index % columns;
                int rowCount = Mathf.Min(columns, slots - row * columns);
                float left = (width - rowCount * buttonWidth - (rowCount - 1) * gap) * .5f;
                FruitLabStyle.Place(button.GetButtonRectTransform(), transform, left + column * (buttonWidth + gap),
                    (_rows - row - 1) * (buttonHeight + gap), buttonWidth, buttonHeight);
                button.ApplyLayout(type,buttonWidth,buttonHeight);
                button.gameObject.SetActive(unlocked);
                index++;
            }
        }

        public void SetUltraActive(bool active){if(_ultraActive==active)return;_ultraActive=active;if(_width>0)ApplyLabLayout(_width,_height,_rows);}
        public void SetLocked(bool locked)
        {
            if(_locked==locked)return;_locked=locked;
            if(_width>0)ApplyLabLayout(_width,_height,_rows);
        }

        private TMP_Text CountText(RewardType type)=>GetAssignedButton(type).CountText;

        public IBaseButtonView GetRevealingPowerUpButton()
        {
            return GetPowerUpButton(RewardType.Revealing);
        }
        
        public IBaseButtonView GetLifePowerUpButton()
        {
            return GetPowerUpButton(RewardType.Life);
        }
        
        public IBaseButtonView GetBombPowerUpButton()
        {
            return GetPowerUpButton(RewardType.Bomb);
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }
        
        public void SetRevealingPowerUpCount(string count)
        {
            CountText(RewardType.Revealing).SetText(count);
        }
        
        public void SetLifePowerUpCount(string count)
        {
            CountText(RewardType.Life).SetText(count);
        }
        
        public void SetBombPowerUpCount(string count)
        {
            CountText(RewardType.Bomb).SetText(count);
        }

        public IBaseButtonView GetPowerUpButton(RewardType rewardType)
        {
            EnsureExtraButtons();return GetAssignedButton(rewardType);
        }
        public PowerUpButtonView GetAssignedButton(RewardType type)=>buttons.Get(type);

        public void SetPowerUpCount(RewardType rewardType, int count)
        {
            EnsureExtraButtons();
            CountText(rewardType).SetText(count.ToString());
        }

        public void SetPowerUpAlpha(RewardType rewardType, float alpha)
        {
            SetButtonAlpha(GetPowerUpButton(rewardType), alpha);
            SetTextAlpha(CountText(rewardType), 1f);
        }

        public void SetRaycastStatus(bool status)
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            _canvasGroup.interactable = status;
            _canvasGroup.blocksRaycasts = status;
        }

        private static void SetButtonAlpha(IBaseButtonView buttonView, float alpha)
        {
            if (buttonView == null) return;

            CanvasGroup canvasGroup = buttonView.GetButtonRectTransform().GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = buttonView.GetButtonRectTransform().gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = alpha;
        }

        private static void SetTextAlpha(TMP_Text text, float alpha)
        {
            if (text == null) return;

            Color color = text.color;
            color.a = alpha;
            text.color = color;
        }
    }

    public interface IPowerUpListHolderView
    {
        IBaseButtonView GetRevealingPowerUpButton();
        IBaseButtonView GetLifePowerUpButton();
        IBaseButtonView GetBombPowerUpButton();
        IBaseButtonView GetPowerUpButton(RewardType rewardType);
        RectTransform GetRectTransform();
        void SetRevealingPowerUpCount(string count);
        void SetLifePowerUpCount(string count);
        void SetBombPowerUpCount(string count);
        void SetPowerUpCount(RewardType rewardType, int count);
        void SetPowerUpAlpha(RewardType rewardType, float alpha);
        void SetRaycastStatus(bool status);
        void SetProgression(int levelId);
        void SetUltraActive(bool active);
        void SetLocked(bool locked);
    }
}
