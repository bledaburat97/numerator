using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Scripts
{
    public class NormalCardItemController : DraggableCardItemController, INormalCardItemController
    {
        private INormalCardItemView _view;
        private bool _isDragStart;
        private Action<int> _onDragStart;
        private Func<int, RectTransform> _onDragComplete;
        private Func<int, int, RectTransform> _onPlaceByClick;
        private Action<int> _onCardClicked;
        private bool _isSelectable;
        private Camera _cam;
        private IHapticController _hapticController;
        private bool _isSelected;
        private bool _isAlreadySelected;
        private ITutorialAbilityManager _tutorialAbilityManager;
        public void Initialize(INormalCardItemView cardItemView, CardItemData cardItemData, ICardItemLocator cardItemLocator, Camera cam, ICardItemInfoManager cardItemInfoManager, IHapticController hapticController, ITutorialAbilityManager tutorialAbilityManager)
        {
            _view = cardItemView;
            _cam = cam;
            _cardItemData = cardItemData;
            _hapticController = hapticController;
            _tutorialAbilityManager = tutorialAbilityManager;
            _view.Init(cardItemData.cardNumber);
            _view.InitLocalScale();
            _view.SetLocalPosition(Vector3.zero, 0f);
            _view.SetOnPointerDown(OnPointerDown);
            _view.SetOnDrag(OnDrag);
            _view.SetOnPointerUp(OnPointerUp);
            _view.SetSize(cardItemData.parent.sizeDelta);
            SetOnDragStart(cardItemLocator.OnDragStart);
            SetOnDragContinue(cardItemLocator.OnDragContinue);
            SetOnDragComplete(cardItemLocator.OnDragComplete);
            SetOnPlaceByClick(cardItemLocator.PlaceCardByClick);
            SetOnCardClicked(_cardItemData.onCardClicked);
            CardItemInfo cardItemInfo = cardItemInfoManager.GetCardItemInfoList()[_cardItemData.cardItemIndex];
            SetColor(ConstantValues.GetProbabilityTypeToColorMapping()[(int)cardItemInfo.probabilityType]);
            SetLockStatus(cardItemInfo.isLocked);
            cardItemInfoManager.ProbabilityChanged += OnProbabilityChanged;
        }
        
        private void OnProbabilityChanged(object sender, ProbabilityChangedEventArgs args)
        {
            if (_cardItemData.cardItemIndex == args.cardIndex)
            {
                SetColor(ConstantValues.GetProbabilityTypeToColorMapping()[(int)args.probabilityType]);
                SetLockStatus(args.isLocked);
            }
        }
        
        public INormalCardItemView GetView()
        {
            return _view;
        }

        private void SetOnCardClicked(Action<int> action)
        {
            _onCardClicked += action;
        }
        
        private void SetOnDragStart(Action<int> action)
        {
            _onDragStart += action;
        }

        private void SetOnDragComplete(Func<int, RectTransform> func)
        {
            _onDragComplete += func;
        }

        private void SetOnPlaceByClick(Func<int, int, RectTransform> func)
        {
            _onPlaceByClick += func;
        }

        private void OnDrag(PointerEventData data)
        {
            if(!_tutorialAbilityManager.IsCardDraggable(_cardItemData.cardItemIndex)) return;
            if (!_isDragStart)
            {
                _hapticController.Vibrate(HapticType.CardGrab);
                _onDragStart(_cardItemData.cardItemIndex);
                _view.SetParent(_cardItemData.tempParent);
                _view.SetSize(new Vector2(ConstantValues.BOARD_CARD_HOLDER_WIDTH, ConstantValues.BOARD_CARD_HOLDER_HEIGHT));
            }

            _isDragStart = true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_view.GetParent(), data.position,
                _cam, out Vector2 localPosition);
            _view.SetAnchoredPosition(localPosition);
            _onDragContinue(data.position, _cardItemData.cardItemIndex);
        }

        public void ResetPosition()
        {
            RectTransform parentTransform = _cardItemData.parent;
            _view.SetParent(parentTransform);
            _view.InitLocalScale();
            _view.SetLocalPosition(Vector3.zero, 0.3f);
            _view.SetSize(parentTransform.sizeDelta);
        }
        
        private void OnPointerUp(PointerEventData data)
        {
            if (!_isDragStart)
            {
                if (_isSelectable && _tutorialAbilityManager.IsCardSelectable(_cardItemData.cardItemIndex))
                {
                    if (!_isSelected)
                    {

                        _hapticController.Vibrate(HapticType.ButtonClick);
                        _isSelected = true;
                        _onCardClicked.Invoke(_cardItemData.cardItemIndex);
                    }
                    else
                    {
                        _onCardClicked.Invoke(-1);
                    }
                }
            }
            else
            {
                _hapticController.Vibrate(HapticType.CardRelease);
                RectTransform cardHolderTransform = _onDragComplete(_cardItemData.cardItemIndex);
                RectTransform parentTransform;
                if (cardHolderTransform != null)
                {
                    parentTransform = cardHolderTransform;
                }
                else
                {
                    parentTransform = _cardItemData.parent;
                }
                _view.SetParent(parentTransform);
                _view.InitLocalScale();
                _view.SetLocalPosition(Vector3.zero, 0.3f);
                _view.SetSize(parentTransform.sizeDelta);
            }
        }

        public void MoveCardByClick(int boardCardHolderIndex)
        {
            _onDragStart.Invoke(_cardItemData.cardItemIndex);
            RectTransform cardHolderTransform = _onPlaceByClick(_cardItemData.cardItemIndex, boardCardHolderIndex);
            _view.SetParent(cardHolderTransform);
            _view.InitLocalScale();
            _view.SetLocalPosition(Vector3.zero, 0.3f);
            _view.SetSize(cardHolderTransform.sizeDelta);
        }

        private void OnPointerDown(PointerEventData data)
        {
            _isDragStart = false;
        }

        public void DeselectCard()
        {
            _isSelected = false;
            _hapticController.Vibrate(HapticType.CardRelease);
        }

        public void SetCardAnimation(bool status)
        {
            _view.SetCardAnimation(status);
        }

        private void SetColor(Color color)
        {
            _view.SetColor(color);
        }

        private void SetLockStatus(bool isLocked)
        {
            _view.SetLockImageStatus(isLocked);
            _isSelectable = !isLocked;
        }

        public Sequence BackFlipAnimation(float delayDuration, bool isGuessRight, string correctNumber)
        {
            return DOTween.Sequence()
                .AppendInterval(delayDuration)
                .Append(_view.SetLocalPosition(new Vector3(0f, 50f, 0f), 0.25f))
                .Append(_view.GetRectTransform().DORotate(new Vector3(0f, 90f, 0f), 0.15f))
                .AppendCallback(() => SetColor(isGuessRight ? ConstantValues.GetProbabilityTypeToColorMapping()[(int)ProbabilityType.Certain] : ConstantValues.GREY_CARD_COLOR))
                .AppendCallback(() => _view.SetTextStatus(false))
                .AppendCallback(() => _view.SetLockImageStatus(false))
                .AppendCallback(isGuessRight ? () => _view.SetBackImageStatus(true) : () => _view.SetBackText(correctNumber))
                .AppendCallback(() => _view.SetNewAnchoredPositionOfRotatedImage())
                .Append(_view.GetRectTransform().DORotate(new Vector3(0f, 180f, 0f), 0.15f))
                .Append(_view.SetLocalPosition(new Vector3(0f, 0f, 0f), 0.15f).SetEase(Ease.OutBounce))
                .OnComplete(() => _hapticController.Vibrate(HapticType.CardGrab));
        }
    }

    public interface INormalCardItemController
    {
        void Initialize(INormalCardItemView cardItemView, CardItemData cardItemData, ICardItemLocator cardItemLocator, Camera cam, ICardItemInfoManager cardItemInfoManager, IHapticController hapticController, ITutorialAbilityManager tutorialAbilityManager);
        void ResetPosition();
        INormalCardItemView GetView();
        void MoveCardByClick(int boardCardHolderIndex);
        Sequence BackFlipAnimation(float delayDuration, bool isGuessRight, string correctNumber);
        void DeselectCard();
        void SetCardAnimation(bool status);
    }
}