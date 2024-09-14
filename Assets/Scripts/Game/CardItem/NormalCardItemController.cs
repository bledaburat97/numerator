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
        private Func<int, int> _onDragComplete;
        private Action<int, bool> _onCardClicked;
        private bool _isLocked;
        private Camera _cam;
        private IHapticController _hapticController;
        private bool _isSelected;
        private bool _isAlreadySelected;
        private ITutorialAbilityManager _tutorialAbilityManager;
        private IBoardAreaController _boardAreaController;
        public void Initialize(INormalCardItemView cardItemView, CardItemData cardItemData, ICardItemLocator cardItemLocator, Camera cam, ICardItemInfoManager cardItemInfoManager, IHapticController hapticController, ITutorialAbilityManager tutorialAbilityManager, IBoardAreaController boardAreaController, Action<int> onDragStart)
        {
            _view = cardItemView;
            _cam = cam;
            _cardItemData = cardItemData;
            _hapticController = hapticController;
            _tutorialAbilityManager = tutorialAbilityManager;
            _boardAreaController = boardAreaController;
            _view.Init(cardItemData.cardNumber);
            _view.InitLocalScale();
            _view.SetLocalPosition(Vector3.zero);
            _view.SetOnPointerDown(OnPointerDown);
            _view.SetOnDrag(OnDrag);
            _view.SetOnPointerUp(OnPointerUp);
            _view.SetSize(cardItemData.parent.sizeDelta);
            SetOnDragStart(onDragStart);
            SetOnDragContinue(cardItemLocator.OnDragContinue);
            SetOnDragComplete(cardItemLocator.OnDragComplete);
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

        private void SetOnCardClicked(Action<int, bool> action)
        {
            _onCardClicked += action;
        }
        
        private void SetOnDragStart(Action<int> action)
        {
            _onDragStart += action;
        }

        private void SetOnDragComplete(Func<int, int> func)
        {
            _onDragComplete += func;
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
            _view.SetParent(_cardItemData.tempParent);
            _view.InitLocalScale();
            DOTween.Sequence().Append(_view.ChangePosition(parentTransform.position, 0.3f))
                .OnComplete(() => _view.SetParent(parentTransform));
            _view.SetSize(parentTransform.sizeDelta);
        }
        
        private void OnPointerUp(PointerEventData data)
        {
            if (!_isDragStart)
            {
                if (_tutorialAbilityManager.IsCardSelectable(_cardItemData.cardItemIndex))
                {
                    if (!_isSelected)
                    {

                        _hapticController.Vibrate(HapticType.ButtonClick);
                        _isSelected = true;
                        _onCardClicked.Invoke(_cardItemData.cardItemIndex, _isLocked);
                    }
                    else
                    {
                        _onCardClicked.Invoke(-1, _isLocked);
                    }
                }
            }
            else
            {
                _hapticController.Vibrate(HapticType.CardRelease);
                int boardHolderIndex = _onDragComplete(_cardItemData.cardItemIndex);
                _view.SetParent(_cardItemData.tempParent);
                Vector2 targetPosition;
                RectTransform parentTransform;
                if (boardHolderIndex != -1)
                {
                    targetPosition = _boardAreaController.GetBoardHolderPositionAtIndex(boardHolderIndex);
                    parentTransform = _boardAreaController.GetRectTransformOfBoardHolder(boardHolderIndex);
                }
                else
                {
                    targetPosition = _cardItemData.parent.position;
                    parentTransform = _cardItemData.parent;
                }
                _view.InitLocalScale();
                DOTween.Sequence().Append(_view.ChangePosition(targetPosition, 0.3f))
                    .OnComplete(() => _view.SetParent(parentTransform));
                _view.SetSize(parentTransform.sizeDelta);
            }
        }

        public void MoveCardByClick(int boardCardHolderIndex)
        {
            _onDragStart.Invoke(_cardItemData.cardItemIndex);
            RectTransform cardHolderTransform = _boardAreaController.GetRectTransformOfBoardHolder(boardCardHolderIndex);
            _view.SetParent(_cardItemData.tempParent);
            _view.InitLocalScale();
            DOTween.Sequence().Append(_view.ChangePosition(cardHolderTransform.position, 0.3f))
                .OnComplete(() => _view.SetParent(cardHolderTransform));
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
            _isLocked = isLocked;
        }

        public void BackFlipAnimation(float delayDuration, bool isGuessRight, string correctNumber)
        {
            DOTween.Sequence()
                .AppendInterval(delayDuration)
                .Append(_view.ChangeLocalPosition(new Vector3(0f, 50f, 0f), 0.25f))
                .Append(_view.GetRectTransform().DORotate(new Vector3(0f, 90f, 0f), 0.15f))
                .AppendCallback(() => SetColor(isGuessRight ? ConstantValues.GetProbabilityTypeToColorMapping()[(int)ProbabilityType.Certain] : ConstantValues.GREY_CARD_COLOR))
                .AppendCallback(() => _view.SetTextStatus(false))
                .AppendCallback(() => _view.SetLockImageStatus(false))
                .AppendCallback(isGuessRight ? () => _view.SetBackImageStatus(true) : () => _view.SetBackText(correctNumber))
                .AppendCallback(() => _view.SetNewAnchoredPositionOfRotatedImage())
                .Append(_view.GetRectTransform().DORotate(new Vector3(0f, 180f, 0f), 0.15f))
                .Append(_view.ChangeLocalPosition(new Vector3(0f, 0f, 0f), 0.15f).SetEase(Ease.OutBounce))
                .OnComplete(() => _hapticController.Vibrate(HapticType.CardGrab));
        }
    }

    public interface INormalCardItemController
    {
        void Initialize(INormalCardItemView cardItemView, CardItemData cardItemData, ICardItemLocator cardItemLocator, Camera cam, ICardItemInfoManager cardItemInfoManager, IHapticController hapticController, ITutorialAbilityManager tutorialAbilityManager, IBoardAreaController boardAreaController, Action<int> onDragStart);
        void ResetPosition();
        INormalCardItemView GetView();
        void MoveCardByClick(int boardCardHolderIndex);
        void BackFlipAnimation(float delayDuration, bool isGuessRight, string correctNumber);
        void DeselectCard();
        void SetCardAnimation(bool status);
    }
}