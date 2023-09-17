using H3MP.Networking;
using UnityEngine;

namespace H3MP.Scripts
{
    public class ServerListController : MonoBehaviour
    {
        private static ServerListController instance;
        private bool awakened;

        // Main
        GameObject loadingAnimation;
        Transform listParent;
        GameObject entryPrefab;
        GameObject hostButton;
        GameObject joinButton;
        GameObject previousButton;
        GameObject nextButton;
        GameObject refreshButton;

        private void Awake()
        {
            if(instance != null)
            {
                Destroy(gameObject);
                return;
            }

            awakened = true;

            if (!ISClient.isConnected)
            {
                ISClient.Connect();
            }
            else
            {
                ConnectedInit();
            }
        }

        private void ConnectedInit()
        {

        }

        private void OnDestroy()
        {
            if (!awakened)
            {
                return;
            }
        }
    }
}
