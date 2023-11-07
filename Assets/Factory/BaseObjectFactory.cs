using UnityEngine;

namespace Factory
{
    public class BaseObjectFactory<TItem, ITItem> where TItem:Object,ITItem
    {
        private TItem _prefabObject;
        public ITItem Spawn(Transform parent, TItem prefabObject)
        {
            _prefabObject = prefabObject;
            ITItem obj = Create(parent);
            return obj;
        }

        private ITItem Create(Transform parent)
        {
            return Object.Instantiate(_prefabObject, parent, true);
        }
    }
}