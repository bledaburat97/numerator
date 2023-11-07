using UnityEngine;

namespace Views
{
    public class ExistenceButtonView : MonoBehaviour, IExistenceButtonView
    {
        public void Init()
        {
            transform.localScale = Vector3.one;
        }
        
        public void SetStatus(bool status)
        {
            transform.gameObject.SetActive(status);
        }
        
        public void SetParent(RectTransform parent)
        {
            transform.parent = parent;
        }
    }

    public interface IExistenceButtonView
    {
        void Init();
        void SetStatus(bool status);
        void SetParent(RectTransform parent);
    }
}