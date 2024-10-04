using System.Collections.Generic;
using Game;
using Scripts;

public class RevealingPowerUpController : BasePowerUpController
{
    private IUnmaskServiceAreaView _unmaskServiceAreaView;
    private IBoardAreaController _boardAreaController;
    private ITargetNumberCreator _targetNumberCreator;
    private IInitialCardAreaController _initialCardAreaController;

    private List<IBoardCardHolderController> _shinyBoardCardHolderControllers;

    public RevealingPowerUpController(IHapticController hapticController, IPowerUpMessagePopupView powerUpMessagePopupView, IFadePanelController fadePanelController) : base(hapticController, powerUpMessagePopupView, fadePanelController)
    {
        _shinyBoardCardHolderControllers = new List<IBoardCardHolderController>();
    }

    public override void Activate(IBoardAreaController boardAreaController, ITargetNumberCreator targetNumberCreator, IInitialCardAreaController initialCardAreaController, IGuessManager guessManager, IBaseButtonController closeButton, IBaseButtonController continueButton)
    {
        base.Activate(boardAreaController, targetNumberCreator, initialCardAreaController, guessManager, closeButton, continueButton);
        continueButton.SetButtonStatus(false);
        closeButton.SetAction(Close);
        _shinyBoardCardHolderControllers = boardAreaController.GetEmptyBoardHolders();
        foreach (IBoardCardHolderController boardCardHolder in _shinyBoardCardHolderControllers)
        {
            boardCardHolder.GetView().SetupTutorialMode();
        }
        boardAreaController.BoardHolderClickedEvent += OnBoardClicked;
        _powerUpMessagePopupView.SetTitle("Revealing Power Up");
        _powerUpMessagePopupView.SetText("Select the place you want to reveal.");
        _boardAreaController = boardAreaController;
        _targetNumberCreator = targetNumberCreator;
        _initialCardAreaController = initialCardAreaController;
    }

    protected override void Close()
    {
        base.Close();

        foreach (IBoardCardHolderController boardCardHolder in _shinyBoardCardHolderControllers)
        {
            boardCardHolder.GetView().CleanupTutorialMode();
        }
    }

    private void OnBoardClicked(object sender, int boardHolderIndex)
    {
        _hapticController.Vibrate(HapticType.CardRelease);
        int cardNumber = _targetNumberCreator.GetTargetCardsList()[boardHolderIndex];
        int cardIndex = cardNumber - 1;
        _boardAreaController.SetCardIndex(boardHolderIndex, cardIndex);
        LockedCardInfo lockedCardInfo = new LockedCardInfo(){boardHolderIndex = boardHolderIndex, targetCardIndex = cardIndex};
        _initialCardAreaController.SetLockedCardController(lockedCardInfo);
        Close();
        _boardAreaController.BoardHolderClickedEvent -= OnBoardClicked;
    }
}
