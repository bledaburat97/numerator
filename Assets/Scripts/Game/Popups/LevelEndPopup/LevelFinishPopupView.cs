using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class LevelFinishPopupView : MonoBehaviour, ILevelFinishPopupView
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private List<LevelFinishButtonModel> buttonModels;
        [SerializeField] private StarImageView[] starList;
        [SerializeField] private ParticleSystem[] starParticleList;
        [SerializeField] private CircleProgressBarView circleProgressBarView;
        [SerializeField] private Image rewardItem;
        [SerializeField] private ParticleSystem rewardParticle;
        [SerializeField] private CanvasGroup starCanvasGroup;

        public void SetStatus(bool status)
        {
            gameObject.SetActive(status);
        }

        public IFadeButtonView GetButton(LevelFinishButtonType buttonType)
        {
            foreach (LevelFinishButtonModel model in buttonModels)
            {
                if (model.buttonType == buttonType)
                {
                    return model.buttonView;
                }
            }
            
            return null;
        }

        public StarImageView[] GetStarList()
        {
            return starList;
        }

        public ParticleSystem[] GetStarParticleList()
        {
            return starParticleList;
        }
        
        public ICircleProgressBarView GetCircleProgressBar()
        {
            return circleProgressBarView;
        }

        public ParticleSystem GetRewardParticle()
        {
            return rewardParticle;
        }
        
        public void SetText(string text)
        {
            this.text.SetText(text);
        }

        public Image GetRewardItem()
        {
            return rewardItem;
        }

        public CanvasGroup GetStarCanvasGroup()
        {
            return starCanvasGroup;
        }
        
        public TMP_Text GetText()
        {
            return text;
        }
    }

    public interface ILevelFinishPopupView
    {
        IFadeButtonView GetButton(LevelFinishButtonType buttonType);
        StarImageView[] GetStarList();
        ParticleSystem[] GetStarParticleList();
        ICircleProgressBarView GetCircleProgressBar();
        ParticleSystem GetRewardParticle();
        void SetText(string text);
        Image GetRewardItem();
        CanvasGroup GetStarCanvasGroup();
        TMP_Text GetText();
        void SetStatus(bool status);
    }
    
    public enum LevelFinishButtonType
    {
        Game,
        Menu,
        Claim
    }

    [Serializable]
    public class LevelFinishButtonModel
    {
        public LevelFinishButtonType buttonType;
        public FadeButtonView buttonView;
    }
}
