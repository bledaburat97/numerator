using System;

namespace Scripts
{
public class CardClickHandler : ICardClickHandler
{
    private readonly IHapticController _hapticController;
    private readonly ITutorialAbilityManager _tutorialAbilityManager;
    private readonly ICardViewHandler _cardViewHandler;
    private readonly ICardItemInfoManager _cardItemInfoManager;


    private CardItemData _cardItemData;
    private Action<int, bool> _onCardClicked;
    private bool _isSelected = false;
    private bool _isLocked = false;

    public CardClickHandler(IHapticController hapticController, ITutorialAbilityManager tutorialAbilityManager, 
        ICardViewHandler cardViewHandler, ICardItemInfoManager cardItemInfoManager, Action<int, bool> onCardClicked)
    {
        _hapticController = hapticController;
        _tutorialAbilityManager = tutorialAbilityManager;
        _cardViewHandler = cardViewHandler;
        _cardItemInfoManager = cardItemInfoManager;
        _onCardClicked = onCardClicked;
    }

    public void Initialize(CardItemData cardItemData)
    {
        _isSelected = false;
        _cardItemData = cardItemData;
        CardItemInfo cardItemInfo = _cardItemInfoManager.GetCardItemInfoList()[_cardItemData.cardItemIndex];
        SetLockStatus(cardItemInfo.isLocked);
    }
    
    public void TryClickCard()
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

    public void SetLockStatus(bool status)
    {
        _isLocked = status;
    }

    public void DeselectCard()
    {
        _isSelected = false;
        _hapticController.Vibrate(HapticType.CardRelease);
    }
}

public interface ICardClickHandler
{
    void Initialize(CardItemData cardItemData);
    void TryClickCard();
    void SetLockStatus(bool status);
    void DeselectCard();
}
}