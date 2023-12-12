﻿using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Scripts
{
    public class CardHolderView : MonoBehaviour, ICardHolderView, IPointerClickHandler
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private RectTransform possibleHolderIndicatorHolderTransform;
        [SerializeField] private PossibleHolderIndicatorView possibleHolderIndicatorPrefab;
        [SerializeField] private Image highlightImage;
        [SerializeField] private TMP_Text text;
        private Camera _cam;
        private Action _onClick;
        
        private PossibleHolderIndicatorViewFactory _possibleHolderIndicatorViewFactory;
        public void Init(CardHolderModel model, PossibleHolderIndicatorViewFactory possibleHolderIndicatorViewFactory, Camera cam)
        {
            transform.localScale = Vector3.one;
            _cam = cam;
            SetLocalPosition(model.localPosition);
            transform.localPosition = model.localPosition;
            rectTransform.sizeDelta = model.size;
            _possibleHolderIndicatorViewFactory = possibleHolderIndicatorViewFactory;
            SetText("");
        }

        public void SetOnClick(Action onClick)
        {
            _onClick = onClick;
        }
        
        public void SetText(string cardNumber)
        {
            text.SetText(cardNumber);
        }

        public void SetLocalPosition(Vector2 localPosition)
        {
            transform.localPosition = localPosition;
        }
        
        public IPossibleHolderIndicatorView CreatePossibleHolderIndicatorView()
        {
            return _possibleHolderIndicatorViewFactory.Spawn(possibleHolderIndicatorHolderTransform, possibleHolderIndicatorPrefab);
        }

        public Vector3 GetPosition()
        {
            return _cam.WorldToScreenPoint(transform.position);
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }

        public Vector3 GetSize()
        {
            return rectTransform.sizeDelta;
        }

        public void SetHighlightStatus(bool status)
        {
            highlightImage.gameObject.SetActive(status);
        }

        public void SetStatus(bool status)
        {
            rectTransform.gameObject.SetActive(status);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _onClick.Invoke();
        }
    }

    public interface ICardHolderView
    {
        void Init(CardHolderModel model, PossibleHolderIndicatorViewFactory possibleHolderIndicatorViewFactory, Camera cam);
        Vector3 GetPosition();
        Vector3 GetSize();
        RectTransform GetRectTransform();
        IPossibleHolderIndicatorView CreatePossibleHolderIndicatorView();
        void SetHighlightStatus(bool status);
        void SetLocalPosition(Vector2 localPosition);
        void SetStatus(bool status);
        void SetText(string cardNumber);
        void SetOnClick(Action onClick);
    }
}