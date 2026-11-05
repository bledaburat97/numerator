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
        [SerializeField] private PowerUpButtonView revealingPowerUpButton;
        [SerializeField] private PowerUpButtonView lifePowerUpButton;
        [SerializeField] private PowerUpButtonView bombPowerUpButton;
        [SerializeField] private TMP_Text revealingPowerUpCount;
        [SerializeField] private TMP_Text lifePowerUpCount;
        [SerializeField] private TMP_Text bombPowerUpCount;

        [SerializeField] private PowerUpButtonView helperPowerUpButton;
        [SerializeField] private TMP_Text helperPowerUpCount;
        [SerializeField] private PowerUpButtonView pairPowerUpButton;
        [SerializeField] private TMP_Text pairPowerUpCount;
        [SerializeField] private PowerUpButtonView splitPowerUpButton;
        [SerializeField] private TMP_Text splitPowerUpCount;
        [SerializeField] private PowerUpButtonView ultraPowerUpButton;
        [SerializeField] private TMP_Text ultraPowerUpCount;
        private bool _ultraActive;
        private CanvasGroup _canvasGroup;
        private static readonly RewardType[] DisplayOrder = { RewardType.Bomb, RewardType.Revealing, RewardType.Life, RewardType.Helper, RewardType.Pair, RewardType.Split, RewardType.Ultra };
        private int _levelId = int.MaxValue - 1;
        private float _width, _height;
        private int _rows = 1;

        private void EnsureExtraButtons()
        {
            Create(ref helperPowerUpButton, ref helperPowerUpCount, "HelperPowerUpButton");
            Create(ref pairPowerUpButton, ref pairPowerUpCount, "PairPowerUpButton");
            Create(ref splitPowerUpButton, ref splitPowerUpCount, "SplitPowerUpButton");
            Create(ref ultraPowerUpButton, ref ultraPowerUpCount, "UltraPowerUpButton");
            void Create(ref PowerUpButtonView button, ref TMP_Text count, string objectName)
            {
                if (button != null) return;
                button = transform.Find(objectName)?.GetComponent<PowerUpButtonView>();
                if (button == null) button = Instantiate(revealingPowerUpButton, revealingPowerUpButton.transform.parent, false);
                button.name = objectName;
                count = button.transform.Find("CountHolder")?.GetComponentInChildren<TMP_Text>(true);
                if (count != null) count.text = "0";
            }
        }

        public void SetProgression(int levelId)
        {
            if (_levelId == levelId) return;
            _levelId = levelId;
            if (_width > 0) ApplyLabLayout(_width, _height, _rows);
        }

        public void ApplyLabLayout(float width, float height, int rows = 1)
        {
            EnsureExtraButtons(); _width = width; _height = height; _rows = Mathf.Clamp(rows, 1, 2);
            foreach (LayoutGroup group in GetComponents<LayoutGroup>()) group.enabled = false;
            var assigned = new HashSet<IBaseButtonView>(DisplayOrder.Select(GetPowerUpButton));
            foreach (var oldButton in GetComponentsInChildren<PowerUpButtonView>(true))
                if (!assigned.Contains(oldButton)) oldButton.gameObject.SetActive(false);
            foreach (var oldGlyph in GetComponentsInChildren<FruitLabGlyph>(true))
                if (oldGlyph.GetComponentInParent<PowerUpButtonView>() == null) oldGlyph.gameObject.SetActive(false);
            var visible = DisplayOrder.Where(t => PowerUpTypeUtility.IsUnlocked(t, _levelId)).ToArray();
            const float gap = 2f;
            int columns = Mathf.Max(1, Mathf.CeilToInt(visible.Length / (float)_rows));
            float buttonWidth = (width - gap * (columns - 1)) / columns;
            float buttonHeight = (height - gap * (_rows - 1)) / _rows;
            int index = 0;
            foreach (var type in DisplayOrder)
            {
                var button = (PowerUpButtonView)GetPowerUpButton(type);
                bool unlocked = visible.Contains(type);
                button.gameObject.SetActive(unlocked);
                if (!unlocked) continue;
                int row = index / columns, column = index % columns;
                int rowCount = Mathf.Min(columns, visible.Length - row * columns);
                float left = (width - rowCount * buttonWidth - (rowCount - 1) * gap) * .5f;
                FruitLabStyle.Place(button.GetButtonRectTransform(), transform, left + column * (buttonWidth + gap),
                    (_rows - row - 1) * (buttonHeight + gap), buttonWidth, buttonHeight);
                button.ApplyLabLayout(PowerUpTypeUtility.IconName(type), PowerUpTypeUtility.Name(type), CountText(type), buttonWidth, buttonHeight);
                index++;
            }
        }

        public void SetUltraActive(bool active){if(_ultraActive==active)return;_ultraActive=active;if(_width>0)ApplyLabLayout(_width,_height,_rows);}

        private TMP_Text CountText(RewardType type) => type == RewardType.Ultra ? ultraPowerUpCount : type == RewardType.Split ? splitPowerUpCount : type == RewardType.Pair ? pairPowerUpCount : type == RewardType.Helper ? helperPowerUpCount
            : type == RewardType.Bomb ? bombPowerUpCount : type == RewardType.Life ? lifePowerUpCount : revealingPowerUpCount;


        
        public IBaseButtonView GetRevealingPowerUpButton()
        {
            return revealingPowerUpButton;
        }
        
        public IBaseButtonView GetLifePowerUpButton()
        {
            return lifePowerUpButton;
        }
        
        public IBaseButtonView GetBombPowerUpButton()
        {
            return bombPowerUpButton;
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }
        
        public void SetRevealingPowerUpCount(string count)
        {
            revealingPowerUpCount.SetText(count);
        }
        
        public void SetLifePowerUpCount(string count)
        {
            lifePowerUpCount.SetText(count);
        }
        
        public void SetBombPowerUpCount(string count)
        {
            bombPowerUpCount.SetText(count);
        }

        public IBaseButtonView GetPowerUpButton(RewardType rewardType)
        {
            EnsureExtraButtons();
            switch (rewardType)
            {
                case RewardType.Ultra: return ultraPowerUpButton;
                case RewardType.Pair: return pairPowerUpButton;
                case RewardType.Split: return splitPowerUpButton;
                case RewardType.Helper: return helperPowerUpButton;
                case RewardType.Revealing:
                    return revealingPowerUpButton;
                case RewardType.Life:
                    return lifePowerUpButton;
                case RewardType.Bomb:
                    return bombPowerUpButton;
                default:
                    return revealingPowerUpButton;
            }
        }

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
    }
}
