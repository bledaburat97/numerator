using UnityEngine;

namespace Factory
{
    public class BaseObjectFactory<TItem, ITItem> where TItem:Object,ITItem
    {
        public ITItem Spawn(Transform parent, TItem prefabObject)
        {
            ITItem obj = Create(parent, prefabObject);
            return obj;
        }

        private ITItem Create(Transform parent, TItem prefabObject)
        {
            return Object.Instantiate(prefabObject, parent, true);
        }
    }
}