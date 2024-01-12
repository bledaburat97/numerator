﻿using TMPro;
using UnityEngine;

namespace Scripts
{
    public class GameUIView : MonoBehaviour, IGameUIView
    {
        [SerializeField] private TMP_Text levelIdText;

        public void SetLevelId(ILevelTracker levelTracker)
        {
            levelIdText.SetText("Level " + (levelTracker.GetLevelId() + 1));
        }

        public void DisableLevelId()
        {
            levelIdText.gameObject.SetActive(false);
        }
    }

    public interface IGameUIView
    {
        void SetLevelId(ILevelTracker levelTracker);
        void DisableLevelId();
    }
}