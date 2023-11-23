namespace Scripts
{
    public class NonDraggableCardItemView : BaseCardItemView, INonDraggableCardItemView
    {
        public void MultiplyPixelsPerUnit()
        {
            outerBGImage.pixelsPerUnitMultiplier = outerBGImage.pixelsPerUnitMultiplier * 2;
            innerBGImage.pixelsPerUnitMultiplier = innerBGImage.pixelsPerUnitMultiplier * 2;
            shadowImage.pixelsPerUnitMultiplier = shadowImage.pixelsPerUnitMultiplier * 2;
        }
    }
    
    public interface INonDraggableCardItemView : IBaseCardItemView
    {
        void MultiplyPixelsPerUnit();

    }
}