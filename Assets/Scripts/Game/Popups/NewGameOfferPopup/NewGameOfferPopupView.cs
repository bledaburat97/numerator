using System;
using TMPro;
using UnityEngine;

namespace Scripts
{
    public class NewGameOfferPopupView : MonoBehaviour, INewGameOfferPopupView
    {
        [SerializeField] private TMP_Text text;

        public void Init()
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            text.text = "Opponent offers a new game.";
        }

        public void Close()
        {
            Destroy(gameObject);
        }
        
    }

    public interface INewGameOfferPopupView
    {
        void Init();
        void Close();
    }
}