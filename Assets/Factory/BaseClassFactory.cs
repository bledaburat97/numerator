namespace Factory
{
    public abstract class BaseClassFactory<TItem, ITItem> where TItem:class,ITItem
    {
        public ITItem Spawn()
        {
            ITItem item = Create();
            return item;
        }

        protected abstract ITItem Create();
    }
}