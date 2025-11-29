using UnityEngine;

namespace Game
{
    public class MovingRewardItemView : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        private bool _isRotating;
        
        private float _orbitRadius;
        public float orbitSpeed = 30f;

        private float currentAngle = 0f;

        void Update()
        {
            if (_isRotating)
            {
                currentAngle += orbitSpeed * Time.deltaTime;
                float angleInRadians = currentAngle * Mathf.Deg2Rad;
                float x = Mathf.Cos(angleInRadians) * _orbitRadius;
                float y = Mathf.Sin(angleInRadians) * _orbitRadius;
                transform.localPosition = new Vector2(x, y);
            }
        }

        public void SetIsRotating(bool isRotating)
        {
            _isRotating = isRotating;
        }

        public void SetOrbitRadius(float orbitRadius)
        {
            _orbitRadius = orbitRadius;
        }

        public void SetSize(Vector2 size)
        {
            rectTransform.sizeDelta = size;
        }

        public void Init()
        {
            rectTransform.localScale = Vector3.one;
        }
        
        public void SetStatus(bool status)
        {
            gameObject.SetActive(status);
        }
        
        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }

        public void DestroyObject()
        {
            Destroy(gameObject);
        }
    }
}