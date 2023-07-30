using UnityEngine;

namespace H3MP.Scripts
{
    public class Billboard : MonoBehaviour
    {
        void Update()
        {
            if (!GameManager.spectatorHost && Camera.main != null)
            {
                Vector3 euler = Camera.main.transform.rotation.eulerAngles;
                euler.z = 0;
                transform.rotation = Quaternion.Euler(euler);
            }
        }
    }
}
