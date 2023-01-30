using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace H3MP
{
    public class H3MP_WristMenuSection : FVRWristMenuSection
    {
        public static bool init;

        // Start page
        private FVRPointableButton BTN_Host;
        private FVRPointableButton BTN_Join;
        private FVRPointableButton BTN_ReloadConfig;

        // Hosting
        private FVRPointableButton BTN_Close;

        // Client
        private FVRPointableButton BTN_Disconnect;

        public override void Enable()
        {
            // Init buttons if not already done
            InitButtons();

            if(Mod.managerObject == null)
            {
                SetPage(0);
            }
            else
            {
                if (H3MP_ThreadManager.host)
                {
                    SetPage(1);
                }
                else
                {
                    SetPage(2);
                }
            }
        }

        private void InitButtons()
        {
            if (BTN_Host != null)
            {
                return;
            }

            Image background = gameObject.AddComponent<Image>();
            background.color = new Color(0.1f, 0.1f, 0.1f, 1);

            GameObject hostButton = Instantiate(this.Menu.BaseButton, transform);
            RectTransform hostRect = hostButton.GetComponent<RectTransform>();
            hostRect.anchorMax = new Vector2(0.5f, 0.5f);
            hostRect.anchorMin = new Vector2(0.5f, 0.5f);
            hostButton.transform.localPosition = new Vector3(-115,0,0);
            hostButton.transform.localRotation = Quaternion.identity;
            Destroy(hostButton.GetComponent<FVRWristMenuSectionButton>());
            hostButton.GetComponent<Text>().text = "Host";
            BTN_Host = hostButton.GetComponent<FVRPointableButton>();
            BTN_Host.Button.onClick.AddListener(OnHostClicked);

            GameObject joinButton = Instantiate(this.Menu.BaseButton, transform);
            RectTransform joinRect = joinButton.GetComponent<RectTransform>();
            joinRect.anchorMax = new Vector2(0.5f, 0.5f);
            joinRect.anchorMin = new Vector2(0.5f, 0.5f);
            joinButton.transform.localPosition = Vector3.zero;
            joinButton.transform.localRotation = Quaternion.identity;
            Destroy(joinButton.GetComponent<FVRWristMenuSectionButton>());
            joinButton.GetComponent<Text>().text = "Join";
            BTN_Join = joinButton.GetComponent<FVRPointableButton>();
            BTN_Join.Button.onClick.AddListener(OnConnectClicked);

            GameObject reloadConfigButton = Instantiate(this.Menu.BaseButton, transform);
            RectTransform reloadConfigRect = reloadConfigButton.GetComponent<RectTransform>();
            reloadConfigRect.anchorMax = new Vector2(0.5f, 0.5f);
            reloadConfigRect.anchorMin = new Vector2(0.5f, 0.5f);
            reloadConfigButton.transform.localPosition = new Vector3(115, 0, 0);
            reloadConfigButton.transform.localRotation = Quaternion.identity;
            Destroy(reloadConfigButton.GetComponent<FVRWristMenuSectionButton>());
            reloadConfigButton.GetComponent<Text>().text = "Reload\nconfig.";
            BTN_ReloadConfig = reloadConfigButton.GetComponent<FVRPointableButton>();
            BTN_ReloadConfig.Button.onClick.AddListener(OnReloadConfigClicked);


            GameObject closeButton = Instantiate(this.Menu.BaseButton, transform);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMax = new Vector2(0.5f, 0.5f);
            closeRect.anchorMin = new Vector2(0.5f, 0.5f);
            closeButton.transform.localPosition = Vector3.zero;
            closeButton.transform.localRotation = Quaternion.identity;
            Destroy(closeButton.GetComponent<FVRWristMenuSectionButton>());
            closeButton.GetComponent<Text>().text = "Close\nserver";
            BTN_Close = closeButton.GetComponent<FVRPointableButton>();
            BTN_Close.Button.onClick.AddListener(OnCloseClicked);


            GameObject disconnectButton = Instantiate(this.Menu.BaseButton, transform);
            RectTransform disconnectRect = disconnectButton.GetComponent<RectTransform>();
            disconnectRect.anchorMax = new Vector2(0.5f, 0.5f);
            disconnectRect.anchorMin = new Vector2(0.5f, 0.5f);
            disconnectButton.transform.localPosition = Vector3.zero;
            disconnectButton.transform.localRotation = Quaternion.identity;
            Destroy(disconnectButton.GetComponent<FVRWristMenuSectionButton>());
            disconnectButton.GetComponent<Text>().text = "Disconnect";
            BTN_Disconnect = disconnectButton.GetComponent<FVRPointableButton>();
            BTN_Disconnect.Button.onClick.AddListener(OnDisconnectClicked);
        }

        private void OnHostClicked()
        {
            Mod.modInstance.CreateManagerObject(true);

            //H3MP_Server.IP = Mod.config["IP"].ToString();
            H3MP_Server.Start((ushort)Mod.config["MaxClientCount"], (ushort)Mod.config["Port"]);

            if (SceneManager.GetActiveScene().name.Equals("TakeAndHold_Lobby_2"))
            {
                Mod.LogInfo("Just connected in TNH lobby, initializing H3MP menu");
                Mod.modInstance.InitTNHMenu();
            }

            // Switch page
            SetPage(1);
        }

        private void OnConnectClicked()
        {
            Mod.modInstance.CreateManagerObject();

            H3MP_Client client = Mod.managerObject.AddComponent<H3MP_Client>();
            client.IP = Mod.config["IP"].ToString();
            client.port = (ushort)Mod.config["Port"];

            client.ConnectToServer();

            if (SceneManager.GetActiveScene().name.Equals("TakeAndHold_Lobby_2"))
            {
                Mod.LogInfo("Just connected in TNH lobby, initializing H3MP menu");
                Mod.modInstance.InitTNHMenu();
            }

            // Switch page
            SetPage(2);
        }

        private void OnReloadConfigClicked()
        {
            Mod.modInstance.LoadConfig();
        }

        private void OnCloseClicked()
        {
            H3MP_Server.Close();

            // Switch page
            SetPage(0);
        }

        private void OnDisconnectClicked()
        {
            H3MP_Client.singleton.Disconnect(true, 0);

            // Switch page
            SetPage(0);
        }

        private void SetPage(int index)
        {
            BTN_Host.gameObject.SetActive(index == 0);
            BTN_Join.gameObject.SetActive(index == 0);
            BTN_ReloadConfig.gameObject.SetActive(index == 0);

            BTN_Close.gameObject.SetActive(index == 1);

            BTN_Disconnect.gameObject.SetActive(index == 2);
        }
    }
}
