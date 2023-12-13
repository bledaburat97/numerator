using UnityEngine;

namespace Menu
{
    public class SwingingHidge : MonoBehaviour
    {
        [SerializeField] private float speed = 1.5f;
        [SerializeField] private float angle = 10f;

        private float currentAngle = 0f;
        private float timer;

        void Update()
        {
            timer += Time.deltaTime * speed;
            float angle = Mathf.Sin(timer) * this.angle;
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle + currentAngle));
        }
    }
}