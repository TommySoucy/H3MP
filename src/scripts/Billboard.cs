using UnityEngine;

namespace H3MP.Scripts
{
    public class Billboard : MonoBehaviour
    {
        void Update()
        {
            if (!GameManager.spectatorHost && Camera.main != null)
            {
                TODO: // nameplate doesn't update on cahnge of IFF. Nameplate mode on friendlies only, we change IFF to be enemy, is changes for the client's point of view, but the client still has nameplate over their head for us
                Vector3 euler = Camera.main.transform.rotation.eulerAngles;
                euler.z = 0;
                transform.rotation = Quaternion.Euler(euler);
            }
        }
    }
}
