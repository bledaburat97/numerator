namespace Scripts
{
    public class FirstLevelTutorialController : TutorialController
    {
        protected override void InitializeTutorialAnimationActions()
        {
            AddTutorialAction(() => StartDragAnimation(0, 0));
            AddTutorialAction(() => StartCardClickAnimation(1, _initialCardAreaController.GetNormalCardHolderPositionAtIndex(1), _sizeOfInitialHolder));
            AddTutorialAction(StartCheckButtonClickAnimation);
            AddTutorialAction(() =>
                ShowResultBlock(0, "Any number is not at correct position. One number is at wrong position."));
            AddTutorialAction(() => WaitForATime(5));
            AddTutorialAction(StartResetButtonClickAnimation);
            AddTutorialAction(() => StartDragAnimation(2, 0));
            AddTutorialAction(() => StartDragAnimation(0, 1));
            AddTutorialAction(StartCheckButtonClickAnimation);
            AddTutorialAction(() =>
                ShowResultBlock(1, "Only one number is at correct position, the other one doesn't exist."));
            AddTutorialAction(() => WaitForATime(5));
            AddTutorialAction(() => StartDragAnimation(2, 0, true));
            AddTutorialAction(() => StartDragAnimation(3, 0));
            AddTutorialAction(StartCheckButtonClickAnimation);
            AddTutorialAction(() =>
                ShowResultBlock(2, "Both of the numbers are at correct position. Congratulations!!!"));
            ExecuteNextTutorialAction();
        }
    }
}