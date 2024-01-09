using UnityEngine;

namespace NexPlayerSample
{
    public class ObjectRotator : MonoBehaviour
    {

        [SerializeField] float speed = 10.0f;
        [SerializeField] Vector3 rotationAxes = Vector3.up;

        void Update()
        {
            this.transform.Rotate(rotationAxes * speed * Time.deltaTime);
        }
    }
}