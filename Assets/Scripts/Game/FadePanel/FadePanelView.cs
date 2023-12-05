﻿using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class FadePanelView : MonoBehaviour, IFadePanelView
    {
        [SerializeField] private Image fadeImage;
        public void SetFadeImageStatus(bool status)
        {
            fadeImage.gameObject.SetActive(status);
        }

        public Image GetFadeImage()
        {
            return fadeImage;
        }
    }

    public interface IFadePanelView
    {
        void SetFadeImageStatus(bool status);
        Image GetFadeImage();
    }
}