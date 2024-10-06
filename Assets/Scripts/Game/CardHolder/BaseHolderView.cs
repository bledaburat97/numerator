using UnityEngine;

namespace Scripts
{
    public interface IBaseHolderView
    {
        RectTransform GetRectTransform();
        void SetLocalPosition(Vector2 localPosition);
        Vector3 GetGlobalPosition();
        void DestroyObject();
        void SetLocalScale();
        void SetSize(Vector2 size);
    }
}