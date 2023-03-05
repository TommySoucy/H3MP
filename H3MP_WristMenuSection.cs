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

        public static Text colorText;
        public static Text colorByIFFText;
        public static Text IFFText;
        public static Text nameplateText;

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

            Text textOut = null;
            InitButton(new List<int>() { 0 }, new List<Vector3>() { new Vector3(0, 75, 0) }, new Vector2(500, 240), new Vector2(140, 70), OnHostClicked, "Host", out textOut);
            InitButton(new List<int>() { 0 }, new List<Vector3>() { Vector3.zero }, new Vector2(500, 240), new Vector2(140, 70), OnConnectClicked, "Join", out textOut);
            InitButton(new List<int>() { 0, 1, 2 }, new List<Vector3>() { new Vector3(0, -75, 0), new Vector3(0, -75, 0), new Vector3(0, -75, 0) }, new Vector2(500, 240), new Vector2(140, 70), OnOptionsClicked, "Options", out textOut);
            InitButton(new List<int>() { 1 }, new List<Vector3>() { new Vector3(0, 75, 0) }, new Vector2(500, 240), new Vector2(140, 70), OnCloseClicked, "Close\nserver", out textOut);
            InitButton(new List<int>() { 2 }, new List<Vector3>() { new Vector3(0, 75, 0) }, new Vector2(500, 240), new Vector2(140, 70), OnDisconnectClicked, "Disconnect", out textOut);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(-150, 150, 0) }, new Vector2(240, 240), new Vector2(70, 70), OnBackClicked, "Back", out textOut);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(0, 150, 0) }, new Vector2(1000, 150), new Vector2(270, 45), OnReloadConfigClicked, "Reload config", out textOut);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(0, 100, 0) }, new Vector2(1000, 150), new Vector2(270, 45), OnItemInterpolationClicked, "Item interpolation (ON)", out textOut);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(0, 50, 0) }, new Vector2(1000, 150), new Vector2(270, 45), OnTNHReviveClicked, "TNH revive", out textOut);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(0, 0, 0) }, new Vector2(1000, 150), new Vector2(270, 45), OnColorClicked, "Current color: " + H3MP_GameManager.colorNames[H3MP_GameManager.colorIndex], out colorText);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(130, 0, 0) }, new Vector2(150, 150), new Vector2(45, 45), OnNextColorClicked, ">", out textOut);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(-130, 0, 0) }, new Vector2(150, 150), new Vector2(45, 45), OnPreviousColorClicked, "<", out textOut);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(0, -50, 0) }, new Vector2(1000, 150), new Vector2(270, 45), OnIFFClicked, "Current IFF: "+GM.CurrentPlayerBody.GetPlayerIFF(), out IFFText);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(130, -50, 0) }, new Vector2(150, 150), new Vector2(45, 45), OnNextIFFClicked, ">", out textOut);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(-130, -50, 0) }, new Vector2(150, 150), new Vector2(45, 45), OnPreviousIFFClicked, "<", out textOut);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(0, -100, 0) }, new Vector2(1000, 150), new Vector2(270, 45), OnColorByIFFClicked, "Color by IFF ("+H3MP_GameManager.colorByIFF+")", out colorByIFFText);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(0, -150, 0) }, new Vector2(1200, 150), new Vector2(340, 45), OnNameplatesClicked, "Nameplates ("+ (H3MP_GameManager.nameplateMode == 0 ? "All" : (H3MP_GameManager.nameplateMode == 1 ? "Friendly Only" : "None"))+ ")", out nameplateText);
        }

        private void InitButton(List<int> pageIndices, List<Vector3> positions, Vector2 sizeDelta, Vector2 boxSize, ButtonClick clickMethod, string defaultText, out Text textOut)
        {
            GameObject button = Instantiate(this.Menu.BaseButton, transform);
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = sizeDelta;
            buttonRect.GetChild(0).GetComponent<RectTransform>().sizeDelta = sizeDelta;
            BoxCollider boxCollider = button.GetComponent<BoxCollider>();
            boxCollider.size = new Vector3(boxSize.x, boxSize.y, boxCollider.size.z);
            button.transform.localPosition = positions[0];
            button.transform.localRotation = Quaternion.identity;
            Destroy(button.GetComponent<FVRWristMenuSectionButton>());
            button.GetComponent<Text>().text = defaultText;
            FVRPointableButton BTN_Ref = button.GetComponent<FVRPointableButton>();
            Text buttonText = button.GetComponent<Text>();
            textOut = buttonText;
            BTN_Ref.Button.onClick.AddListener(()=>clickMethod(buttonText));

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

        private void OnTNHReviveClicked(Text textRef)
        {
            if(GM.TNH_Manager != null)
            {
                Mod.TNH_Manager_InitPlayerPosition.Invoke(GM.TNH_Manager, null);

                if(Mod.currentTNHInstance != null)
                {
                    Mod.currentTNHInstance.RevivePlayer(H3MP_GameManager.ID);
                }
            }
        }

        private void OnColorClicked(Text textRef)
        {
            // Place holder
        }

        private void OnNextColorClicked(Text textRef)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            if (!H3MP_GameManager.colorByIFF && Mod.managerObject != null)
            {
                ++H3MP_GameManager.colorIndex;
                if(H3MP_GameManager.colorIndex >= H3MP_GameManager.colors.Length)
                {
                    H3MP_GameManager.colorIndex = 0;
                }

                H3MP_GameManager.SetPlayerColor(H3MP_GameManager.ID, H3MP_GameManager.colorIndex, false, 0);
            }
        }

        private void OnPreviousColorClicked(Text textRef)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            if (!H3MP_GameManager.colorByIFF && Mod.managerObject != null)
            {
                --H3MP_GameManager.colorIndex;
                if(H3MP_GameManager.colorIndex < 0)
                {
                    H3MP_GameManager.colorIndex = H3MP_GameManager.colors.Length - 1;
                }

                H3MP_GameManager.SetPlayerColor(H3MP_GameManager.ID, H3MP_GameManager.colorIndex, false, 0);
            }
        }

        private void OnIFFClicked(Text textRef)
        {
            // Place holder
        }

        private void OnNextIFFClicked(Text textRef)
        {
            if(Mod.managerObject == null)
            {
                return;
            }

            if (GM.CurrentPlayerBody.GetPlayerIFF() == 31)
            {
                GM.CurrentPlayerBody.SetPlayerIFF(0);
            }
            else
            {
                GM.CurrentPlayerBody.SetPlayerIFF(GM.CurrentPlayerBody.GetPlayerIFF() + 1);
            }

            if (H3MP_ThreadManager.host)
            {
                H3MP_ServerSend.PlayerIFF(0, GM.CurrentPlayerBody.GetPlayerIFF());
            }
            else
            {
                H3MP_ClientSend.PlayerIFF(GM.CurrentPlayerBody.GetPlayerIFF());
            }

            if (H3MP_GameManager.colorByIFF)
            {
                H3MP_GameManager.SetPlayerColor(H3MP_GameManager.ID, GM.CurrentPlayerBody.GetPlayerIFF(), false, 0, false);
            }

            IFFText.text = "Current IFF: " + GM.CurrentPlayerBody.GetPlayerIFF();
        }

        private void OnPreviousIFFClicked(Text textRef)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            if (GM.CurrentPlayerBody.GetPlayerIFF() == 0)
            {
                GM.CurrentPlayerBody.SetPlayerIFF(31);
            }
            else
            {
                GM.CurrentPlayerBody.SetPlayerIFF(GM.CurrentPlayerBody.GetPlayerIFF() - 1);
            }

            if (H3MP_ThreadManager.host)
            {
                H3MP_ServerSend.PlayerIFF(0, GM.CurrentPlayerBody.GetPlayerIFF());
            }
            else
            {
                H3MP_ClientSend.PlayerIFF(GM.CurrentPlayerBody.GetPlayerIFF());
            }

            if (H3MP_GameManager.colorByIFF)
            {
                H3MP_GameManager.SetPlayerColor(H3MP_GameManager.ID, GM.CurrentPlayerBody.GetPlayerIFF(), false, 0, false);
            }

            IFFText.text = "Current IFF: " + GM.CurrentPlayerBody.GetPlayerIFF();
        }

        private void OnColorByIFFClicked(Text textRef)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            if (H3MP_ThreadManager.host)
            {
                H3MP_GameManager.colorByIFF = !H3MP_GameManager.colorByIFF;

                textRef.text = "Color by IFF (" + H3MP_GameManager.colorByIFF + ")";

                if (H3MP_GameManager.colorByIFF)
                {
                    H3MP_GameManager.colorIndex = GM.CurrentPlayerBody.GetPlayerIFF() % H3MP_GameManager.colors.Length;
                    H3MP_WristMenuSection.colorText.text = "Current color: " + H3MP_GameManager.colorNames[H3MP_GameManager.colorIndex];

                    foreach (KeyValuePair<int, H3MP_PlayerManager> playerEntry in H3MP_GameManager.players)
                    {
                        playerEntry.Value.SetColor(playerEntry.Value.IFF);
                    }
                }
                else
                {
                    foreach (KeyValuePair<int, H3MP_PlayerManager> playerEntry in H3MP_GameManager.players)
                    {
                        playerEntry.Value.SetColor(playerEntry.Value.colorIndex);
                    }
                }

                H3MP_ServerSend.ColorByIFF(H3MP_GameManager.colorByIFF);
            }
        }

        private void OnNameplatesClicked(Text textRef)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            if (H3MP_ThreadManager.host)
            {
                H3MP_GameManager.nameplateMode = (H3MP_GameManager.nameplateMode + 1 ) % 3;

                switch (H3MP_GameManager.nameplateMode)
                {
                    case 0:
                        textRef.text = "Nameplates (All)";
                        foreach (KeyValuePair<int, H3MP_PlayerManager> playerEntry in H3MP_GameManager.players)
                        {
                            playerEntry.Value.overheadDisplayBillboard.gameObject.SetActive(playerEntry.Value.visible);
                        }
                        break;
                    case 1:
                        textRef.text = "Nameplates (Friendly only)";
                        foreach (KeyValuePair<int, H3MP_PlayerManager> playerEntry in H3MP_GameManager.players)
                        {
                            playerEntry.Value.overheadDisplayBillboard.gameObject.SetActive(playerEntry.Value.visible && GM.CurrentPlayerBody.GetPlayerIFF() == playerEntry.Value.IFF);
                        }
                        break;
                    case 2:
                        textRef.text = "Nameplates (None)";
                        foreach (KeyValuePair<int, H3MP_PlayerManager> playerEntry in H3MP_GameManager.players)
                        {
                            playerEntry.Value.overheadDisplayBillboard.gameObject.SetActive(false);
                        }
                        break;
                }

                H3MP_ServerSend.NameplateMode(H3MP_GameManager.nameplateMode);
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
