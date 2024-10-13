namespace Scripts
{
    public class NonDraggableCardItemView : BaseCardItemView, INonDraggableCardItemView
    {
        public void MultiplyPixelsPerUnit()
        {
            image.pixelsPerUnitMultiplier = image.pixelsPerUnitMultiplier * 2;
        }
    }
    
    public interface INonDraggableCardItemView : IBaseCardItemView
    {
        void MultiplyPixelsPerUnit();

    }
}