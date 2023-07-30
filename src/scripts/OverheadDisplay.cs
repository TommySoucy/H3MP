using UnityEngine;

namespace H3MP.Scripts
{
    public class OverheadDisplay : MonoBehaviour
    {
        public Transform head;
        public Vector3 offset;

        void Update()
        {
            transform.position = head.position + offset;
        }
    }
}