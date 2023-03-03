using FistVR;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace H3MP
{
    public class H3MP_WristMenuSection : FVRWristMenuSection
    {
        public static bool init;

        delegate void ButtonClick(Text text);
        Dictionary<int, List<KeyValuePair<FVRPointableButton, Vector3>>> pages;
        int currentPage = -1;

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
            if (pages != null)
            {
                return;
            }

            pages = new Dictionary<int, List<KeyValuePair<FVRPointableButton, Vector3>>>();

            Image background = gameObject.AddComponent<Image>();
            background.color = new Color(0.1f, 0.1f, 0.1f, 1);

            InitButton(new List<int>() { 0 }, new List<Vector3>() { new Vector3(0, 75, 0) }, new Vector2(500, 240), OnHostClicked, "Host");
            InitButton(new List<int>() { 0 }, new List<Vector3>() { Vector3.zero }, new Vector2(500, 240), OnConnectClicked, "Join");
            InitButton(new List<int>() { 0, 1, 2 }, new List<Vector3>() { new Vector3(0, -75, 0), new Vector3(0, -75, 0), new Vector3(0, -75, 0) }, new Vector2(500, 240), OnOptionsClicked, "Options");
            InitButton(new List<int>() { 1 }, new List<Vector3>() { new Vector3(0, 75, 0) }, new Vector2(500, 240), OnCloseClicked, "Close\nserver");
            InitButton(new List<int>() { 2 }, new List<Vector3>() { new Vector3(0, 75, 0) }, new Vector2(500, 240), OnDisconnectClicked, "Disconnect");
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(-150, 150, 0) }, new Vector2(240, 240), OnBackClicked, "Back");
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(0, 75, 0) }, new Vector2(500, 150), OnReloadConfigClicked, "Reload config");
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(0, -75, 0) }, new Vector2(500, 150), OnItemInterpolationClicked, "Item interpolation (ON)");
        }

        private void InitButton(List<int> pageIndices, List<Vector3> positions, Vector2 sizeDelta, ButtonClick clickMethod, string defaultText)
        {
            GameObject button = Instantiate(this.Menu.BaseButton, transform);
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.GetChild(0).GetComponent<RectTransform>().sizeDelta = sizeDelta;
            button.transform.localPosition = positions[0];
            button.transform.localRotation = Quaternion.identity;
            Destroy(button.GetComponent<FVRWristMenuSectionButton>());
            button.GetComponent<Text>().text = defaultText;
            FVRPointableButton BTN_Ref = button.GetComponent<FVRPointableButton>();
            BTN_Ref.Button.onClick.AddListener(()=>clickMethod(button.GetComponent<Text>()));

            for (int i = 0; i < pageIndices.Count; ++i)
            {
                if (pages.TryGetValue(pageIndices[i], out List<KeyValuePair<FVRPointableButton, Vector3>> buttons))
                {
                    buttons.Add(new KeyValuePair<FVRPointableButton, Vector3>(BTN_Ref, positions[i]));
                }
                else
                {
                    KeyValuePair<FVRPointableButton, Vector3> entry = new KeyValuePair<FVRPointableButton, Vector3>(BTN_Ref, positions[i]);
                    pages.Add(pageIndices[i], new List<KeyValuePair<FVRPointableButton, Vector3>>() { entry });
                }
            }
        }

        private void OnHostClicked(Text textRef)
        {
            if (Mod.managerObject != null)
            {
                return;
            }

            Mod.modInstance.CreateManagerObject(true);

            //H3MP_Server.IP = Mod.config["IP"].ToString();
            H3MP_Server.Start((ushort)Mod.config["MaxClientCount"], (ushort)Mod.config["Port"]);

            if (H3MP_GameManager.scene.Equals("TakeAndHold_Lobby_2"))
            {
                Mod.LogInfo("Just connected in TNH lobby, initializing H3MP menu");
                Mod.modInstance.InitTNHMenu();
            }

            H3MP_GameManager.firstPlayerInSceneInstance = true;

            // Switch page
            SetPage(1);
        }

        private void OnConnectClicked(Text textRef)
        {
            if (Mod.managerObject != null)
            {
                return;
            }

            if (Mod.config["IP"].ToString().Equals(""))
            {
                Mod.LogError("Attempted to connect to server but no IP set in config!");
                return;
            }

            Mod.modInstance.CreateManagerObject();

            H3MP_Client client = Mod.managerObject.AddComponent<H3MP_Client>();
            client.IP = Mod.config["IP"].ToString();
            client.port = (ushort)Mod.config["Port"];

            client.ConnectToServer();

            if (H3MP_GameManager.scene.Equals("TakeAndHold_Lobby_2"))
            {
                Mod.LogInfo("Just connected in TNH lobby, initializing H3MP menu");
                Mod.modInstance.InitTNHMenu();
            }

            // Switch page
            SetPage(2);
        }

        private void OnReloadConfigClicked(Text textRef)
        {
            Mod.modInstance.LoadConfig();
        }

        private void OnCloseClicked(Text textRef)
        {
            H3MP_Server.Close();

            // Switch page
            SetPage(0);
        }

        private void OnDisconnectClicked(Text textRef)
        {
            H3MP_Client.singleton.Disconnect(true, 0);

            // Switch page
            SetPage(0);
        }

        private void OnOptionsClicked(Text textRef)
        {
            // Switch page
            SetPage(3);
        }

        private void OnBackClicked(Text textRef)
        {
            if(Mod.managerObject == null)
            {
                SetPage(0);
            }
            else
            {
                SetPage(H3MP_ThreadManager.host ? 1 : 2);
            }
        }

        private void OnItemInterpolationClicked(Text textRef)
        {
            if (H3MP_TrackedItem.interpolated)
            {
                H3MP_TrackedItem.interpolated = false;
                textRef.text = "Item interpolation (OFF)";
            }
            else
            {
                H3MP_TrackedItem.interpolated = true;
                textRef.text = "Item interpolation (ON)";
            }
        }

        private void SetPage(int index)
        {
            // Disable buttons from previous page if applicable
            if (currentPage != -1 && pages.TryGetValue(currentPage, out List<KeyValuePair<FVRPointableButton, Vector3>> previousButtons))
            {
                for(int i=0; i< previousButtons.Count; ++i)
                {
                    previousButtons[i].Key.gameObject.SetActive(false);
                }
            }

            // Enable buttons of new page and set their positions
            if(pages.TryGetValue(index, out List<KeyValuePair<FVRPointableButton, Vector3>> newButtons))
            {
                for (int i = 0; i < newButtons.Count; ++i)
                {
                    newButtons[i].Key.gameObject.SetActive(true);
                    newButtons[i].Key.GetComponent<RectTransform>().localPosition = newButtons[i].Value;
                }
            }

            currentPage = index;
        }
    }
}
