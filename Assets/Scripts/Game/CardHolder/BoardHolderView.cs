using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class BoardHolderView : MonoBehaviour, IBoardHolderView
    {
        [SerializeField] private Button button;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image highlightImage;
        [SerializeField] private Canvas tutorialCanvas;
        [SerializeField] private GraphicRaycaster tutorialRaycaster;
        [SerializeField] private RectTransform garden;
        [SerializeField] private Image successFrameImage;
        
        private Camera _cam;
        private Tween _selectionPulse;
        private Image _selectionSurface;
        private Color _selectionSurfaceColor, _selectionHighlightColor;
        private bool _selectionActive, _highlightWasActive;


        [SerializeField] private RectTransform _dynamiteBadge;
        [SerializeField] private Image _dynamiteSurface, _dynamiteRim;
        [SerializeField] private FruitLabGlyph _dynamiteIcon;
        [SerializeField] private TMP_Text _dynamiteCount;
        private Tween _dynamitePulse;

        public void ShowDynamite(DynamiteRoundState state)
        {
            _dynamitePulse?.Kill();
            if (state == null || state.Exploded || (!state.Disarmed && state.Remaining == 0))
            {
                if (_dynamiteBadge != null) _dynamiteBadge.gameObject.SetActive(false);
                return;
            }
            if (_dynamiteBadge == null)
            {
                _dynamiteBadge = new GameObject("DynamiteBadge",typeof(RectTransform)).GetComponent<RectTransform>();
                _dynamiteBadge.SetParent(transform,false);
                _dynamiteSurface = FruitLabStyle.AddSurface(_dynamiteBadge.gameObject,new Color(.28f,.08f,.09f,.98f));
                _dynamiteRim = FruitLabStyle.Detail(_dynamiteBadge,"Rim",new Color(1f,.64f,.24f,.95f),true);
                FruitLabStyle.Stretch(_dynamiteRim.rectTransform,1f);
                _dynamiteIcon = FruitLabGlyph.Create(_dynamiteBadge,FruitLabGlyph.Symbol.Dynamite,Color.white);
                _dynamiteCount = FruitLabStyle.CreateText("Moves",_dynamiteBadge,13f,FruitLabStyle.Text);
                _dynamiteCount.fontStyle = FontStyles.Bold; _dynamiteCount.alignment = TextAlignmentOptions.Center;
                _dynamiteCount.raycastTarget = false;
            }
            foreach (Transform child in _dynamiteBadge.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = gameObject.layer;
            _dynamiteBadge.gameObject.SetActive(true); _dynamiteBadge.SetAsLastSibling();
            _dynamiteBadge.anchorMin = _dynamiteBadge.anchorMax = new Vector2(.5f,0f);
            _dynamiteBadge.pivot = new Vector2(.5f,1f);
            _dynamiteBadge.anchoredPosition = new Vector2(0f,8f);
            _dynamiteBadge.sizeDelta = new Vector2(state.Disarmed ? 26f : 48f,28f);
            _dynamiteBadge.localScale = Vector3.one;
            _dynamiteIcon.Type = state.Disarmed ? FruitLabGlyph.Symbol.Check : FruitLabGlyph.Symbol.Dynamite;
            _dynamiteIcon.color = state.Disarmed ? FruitLabStyle.Mint : Color.white; _dynamiteIcon.SetVerticesDirty();
            FruitLabStyle.Place(_dynamiteIcon.rectTransform,_dynamiteBadge,state.Disarmed ? 3f : 2f,4f,20f,20f);
            FruitLabStyle.Place(_dynamiteCount.rectTransform,_dynamiteBadge,25f,2f,20f,24f);
            _dynamiteCount.fontSize = 16f;
            _dynamiteCount.color = new Color(1f,.94f,.76f);
            _dynamiteCount.text = state.Disarmed ? "" : state.Remaining.ToString();
            _dynamiteRim.color = state.Disarmed ? FruitLabStyle.Mint : new Color(1f,.64f,.24f,.95f);
            _dynamiteSurface.color = state.Disarmed ? new Color(.08f,.23f,.22f,.98f) : new Color(.28f,.08f,.09f,.98f);
            if (!state.Disarmed && !state.Exploded)
                _dynamitePulse = _dynamiteBadge.DOScale(state.Remaining == 1 ? 1.10f : 1.035f,state.Remaining == 1 ? .35f : .85f)
                    .SetEase(Ease.InOutSine).SetLoops(-1,LoopType.Yoyo).SetLink(gameObject);

        }

#if UNITY_EDITOR
        [ContextMenu("Fruit Lab/Apply Board Style")]
        public void ApplyLabStyle()
        {
            Transform surface = transform.Find("Image");
            if (surface != null)
            {
                FruitLabStyle.Stretch((RectTransform)surface, 3f);
                FruitLabStyle.RecessSurface(surface.GetComponent<Image>());
                FruitLabStyle.Rim((RectTransform)surface);
            }
            FruitLabStyle.Surface(highlightImage, new Color(0.53f, 0.9f, 0.78f, 0.3f), true);
            FruitLabStyle.Surface(successFrameImage, FruitLabStyle.Correct, true);
            highlightImage.sprite = successFrameImage.sprite = FruitLabStyle.ResultFrame;
            highlightImage.pixelsPerUnitMultiplier = successFrameImage.pixelsPerUnitMultiplier = 1.2f;
            highlightImage.raycastTarget = successFrameImage.raycastTarget = false;
        }
#endif
        
        public void SetLocalScale()
        {
            transform.localScale = Vector3.one;
        }

        public void SetLocalPosition(Vector2 localPosition)
        {
            transform.localPosition = localPosition;
        }

        public void SetSize(Vector2 size)
        {
            rectTransform.sizeDelta = size;
        }
        
        public Vector3 GetGlobalPosition()
        {
            return transform.position;
        }
        
        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }
        
        public void DestroyObject()
        {
            if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
        }
        
        public void SetCamera(Camera cam)
        {
            _cam = cam;
        }

        public RectTransform GetGardenRectTransform()
        {
            return garden;
        }
        
        public void SetOnClick(Action onClick)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }
        
        public Vector3 GetPosition()
        {
            return _cam.WorldToScreenPoint(transform.position);
        }
        
        public void SetHighlightStatus(bool status)
        {
            highlightImage.gameObject.SetActive(status);
        }
        
        public void SetupTutorialMode()
        {
            // A separately sorted canvas needs the same event camera as the game UI.
            tutorialCanvas.worldCamera = tutorialCanvas.rootCanvas.worldCamera ?? _cam;
            tutorialCanvas.overrideSorting = true;
            tutorialCanvas.sortingOrder = 2;
            StartRevealSelection();
        }

        public void CleanupTutorialMode()
        {
            tutorialCanvas.overrideSorting = false;
            StopRevealSelection();
        }

        private void StartRevealSelection()
        {
            if (_selectionActive || highlightImage == null) return;
            _selectionActive = true;
            _highlightWasActive = highlightImage.gameObject.activeSelf;
            _selectionHighlightColor = highlightImage.color;
            highlightImage.gameObject.SetActive(true);
            highlightImage.color = new Color(.63f, 1f, .91f, .98f);
            _selectionSurface = transform.Find("Image")?.GetComponent<Image>();
            if (_selectionSurface != null)
            {
                _selectionSurfaceColor = _selectionSurface.color;
                _selectionSurface.color = Color.Lerp(_selectionSurfaceColor, FruitLabStyle.Mint, .32f);
            }
            // One owned tween updates both graphics; a closed selection cannot
            // leave a nested color animation writing over the restored colors.
            _selectionPulse = DOVirtual.Float(0f,1f,.65f, blend =>
                {
                    if (!_selectionActive || highlightImage == null) return;
                    highlightImage.color = new Color(.63f,1f,.91f,Mathf.Lerp(.98f,.52f,blend));
                    if (_selectionSurface != null)
                        _selectionSurface.color = Color.Lerp(_selectionSurfaceColor,FruitLabStyle.Mint,Mathf.Lerp(.32f,.18f,blend));
                })
                .SetEase(Ease.InOutSine).SetLoops(-1,LoopType.Yoyo)
                .SetUpdate(DOTween.defaultUpdateType,true).SetLink(gameObject,LinkBehaviour.KillOnDestroy);
        }

        private void StopRevealSelection()
        {
            if (!_selectionActive) return;
            _selectionActive = false;
            _selectionPulse?.Kill(); _selectionPulse = null;
            if (_selectionSurface != null) _selectionSurface.color = _selectionSurfaceColor;
            if (highlightImage != null)
            {
                highlightImage.color = _selectionHighlightColor;
                highlightImage.gameObject.SetActive(_highlightWasActive);
            }
        }

        private void OnDisable()
        {
            StopRevealSelection();
            _dynamitePulse?.Kill();
            if (tutorialCanvas != null) tutorialCanvas.overrideSorting = false;
        }

        public Sequence PlaySuccessFrameAnimation(float delayDuration)
        {
            Sequence sequence = DOTween.Sequence();
            if (successFrameImage == null) return sequence;

            RectTransform frameTransform = successFrameImage.rectTransform;
            Vector3 targetScale = frameTransform.localScale == Vector3.zero ? Vector3.one : frameTransform.localScale;
            Color frameColor = successFrameImage.color;
            frameColor.a = 0f;
            successFrameImage.color = frameColor;
            frameTransform.localScale = targetScale * 0.85f;

            return sequence
                .AppendInterval(delayDuration)
                .AppendCallback(() => successFrameImage.gameObject.SetActive(true))
                .Append(successFrameImage.DOFade(1f, 0.12f))
                .Join(frameTransform.DOScale(targetScale * 1.08f, 0.12f).SetEase(Ease.OutBack))
                .Append(frameTransform.DOScale(targetScale, 0.08f).SetEase(Ease.OutQuad));
        }

        public void SetSuccessFrameStatus(bool status)
        {
            if (successFrameImage == null) return;

            successFrameImage.gameObject.SetActive(status);
            Color frameColor = successFrameImage.color;
            frameColor.a = status ? 1f : 0f;
            successFrameImage.color = frameColor;
        }

    }

    public interface IBoardHolderView : IBaseHolderView
    {
        void ShowDynamite(DynamiteRoundState state);
        void SetCamera(Camera cam);
        void SetOnClick(Action onClick);
        Vector3 GetPosition();
        void SetHighlightStatus(bool status);
        void SetupTutorialMode();
        void CleanupTutorialMode();
        RectTransform GetGardenRectTransform();
        Sequence PlaySuccessFrameAnimation(float delayDuration);
        void SetSuccessFrameStatus(bool status);
    }
}
