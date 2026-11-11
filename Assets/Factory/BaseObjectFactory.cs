using UnityEngine;

namespace Factory
{
    public class BaseObjectFactory<TItem, ITItem> where TItem:Object,ITItem
    {
        // Preserve existing callers. New UI paths can explicitly request local transforms.
        public ITItem Spawn(Transform parent, TItem prefabObject,bool worldPositionStays=true)
        {
            ITItem obj = Create(parent, prefabObject,worldPositionStays);
            return obj;
        }

        private ITItem Create(Transform parent, TItem prefabObject,bool worldPositionStays)
        {
            return Object.Instantiate(prefabObject, parent, worldPositionStays);
        }
    }
}
