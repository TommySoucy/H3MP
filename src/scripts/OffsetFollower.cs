using UnityEngine;

namespace H3MP.Scripts
{
    public class OffsetFollower : MonoBehaviour
    {
        public Transform transformToFollow;
        public Vector3 offset;
        public Vector3 rotationAxes;

        public void Update()
        {
            transform.position = transformToFollow.position;
            transform.localPosition += offset;
            Vector3 actualRotation = new Vector3(transformToFollow.rotation.eulerAngles.x * rotationAxes.x,
                                                 transformToFollow.rotation.eulerAngles.y * rotationAxes.y,
                                                 transformToFollow.rotation.eulerAngles.z * rotationAxes.z);
            transform.rotation = Quaternion.Euler(actualRotation);
        }
    }
}
