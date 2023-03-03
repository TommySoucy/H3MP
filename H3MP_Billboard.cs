using UnityEngine;

namespace H3MP
{
    public class H3MP_Billboard : MonoBehaviour
    {
        void Update()
        {
            if (!Mod.spectatorHost && Camera.main != null)
            {
                Vector3 euler = Camera.main.transform.rotation.eulerAngles;
                euler.z = 0;
                transform.rotation = Quaternion.Euler(euler);
            }
        }
    }
}
