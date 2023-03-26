using FistVR;
using System.Collections.Generic;
using UnityEngine;
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
        public static Text radarModeText;
        public static Text radarColorText;
        public static Text maxHealthText;

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
            background.rectTransform.sizeDelta = new Vector2(500, 350);
            background.color = new Color(0.1f, 0.1f, 0.1f, 1);

            Text textOut = null;
            InitButton(new List<int>() { 0 }, new List<Vector3>() { new Vector3(0, 75, 0) }, new Vector2(500, 240), new Vector2(140, 70), OnHostClicked, "Host", out textOut);
            InitButton(new List<int>() { 0 }, new List<Vector3>() { Vector3.zero }, new Vector2(500, 240), new Vector2(140, 70), OnConnectClicked, "Join", out textOut);
            InitButton(new List<int>() { 0, 1, 2 }, new List<Vector3>() { new Vector3(0, -75, 0), new Vector3(0, -75, 0), new Vector3(0, -75, 0) }, new Vector2(500, 240), new Vector2(140, 70), OnOptionsClicked, "Options", out textOut);
            InitButton(new List<int>() { 1 }, new List<Vector3>() { new Vector3(0, 75, 0) }, new Vector2(500, 240), new Vector2(140, 70), OnCloseClicked, "Close\nserver", out textOut);
            InitButton(new List<int>() { 2 }, new List<Vector3>() { new Vector3(0, 75, 0) }, new Vector2(500, 240), new Vector2(140, 70), OnDisconnectClicked, "Disconnect", out textOut);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(-215, 140, 0) }, new Vector2(240, 240), new Vector2(70, 70), OnBackClicked, "Back", out textOut);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(0, 150, 0) }, new Vector2(1200, 150), new Vector2(270, 45), OnReloadConfigClicked, "Reload config", out textOut);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(0, 100, 0) }, new Vector2(1200, 150), new Vector2(270, 45), OnItemInterpolationClicked, "Item interpolation (ON)", out textOut);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(0, 50, 0) }, new Vector2(1200, 150), new Vector2(270, 45), OnTNHReviveClicked, "TNH revive", out textOut);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(0, 0, 0) }, new Vector2(1200, 150), new Vector2(270, 45), OnColorClicked, "Current color: " + H3MP_GameManager.colorNames[H3MP_GameManager.colorIndex], out colorText);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(155, 0, 0) }, new Vector2(150, 150), new Vector2(45, 45), OnNextColorClicked, ">", out textOut);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(-155, 0, 0) }, new Vector2(150, 150), new Vector2(45, 45), OnPreviousColorClicked, "<", out textOut);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(0, -50, 0) }, new Vector2(1200, 150), new Vector2(270, 45), OnIFFClicked, "Current IFF: "+GM.CurrentPlayerBody.GetPlayerIFF(), out IFFText);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(155, -50, 0) }, new Vector2(150, 150), new Vector2(45, 45), OnNextIFFClicked, ">", out textOut);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(-155, -50, 0) }, new Vector2(150, 150), new Vector2(45, 45), OnPreviousIFFClicked, "<", out textOut);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(0, -100, 0) }, new Vector2(1200, 150), new Vector2(270, 45), OnColorByIFFClicked, "Color by IFF ("+H3MP_GameManager.colorByIFF+")", out colorByIFFText);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(0, -150, 0) }, new Vector2(1200, 150), new Vector2(340, 45), OnNameplatesClicked, "Nameplates ("+ (H3MP_GameManager.nameplateMode == 0 ? "All" : (H3MP_GameManager.nameplateMode == 1 ? "Friendly Only" : "None"))+ ")", out nameplateText);
            InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(0, 150, 0) }, new Vector2(1200, 150), new Vector2(340, 45), OnTNHRadarModeClicked, "Radar mode ("+ (H3MP_GameManager.radarMode == 0 ? "All" : (H3MP_GameManager.radarMode == 1 ? "Friendly Only" : "None"))+ ")", out radarModeText);
            InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(0, 100, 0) }, new Vector2(1200, 150), new Vector2(340, 45), OnTNHRadarColorClicked, "Radar color IFF ("+ H3MP_GameManager.radarColor + ")", out radarColorText);
            InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(0, 50, 0) }, new Vector2(1200, 150), new Vector2(340, 45), OnMaxHealthClicked, "Max health: "+ (H3MP_GameManager.maxHealthIndex == -1 ? "Not set" : H3MP_GameManager.maxHealths[H3MP_GameManager.maxHealthIndex].ToString()), out maxHealthText);
            InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(155, 50, 0) }, new Vector2(150, 150), new Vector2(45, 45), OnNextMaxHealthClicked, ">", out textOut);
            InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(-155, 50, 0) }, new Vector2(150, 150), new Vector2(45, 45), OnPreviousMaxHealthClicked, "<", out textOut);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(215, -140, 0) }, new Vector2(240, 240), new Vector2(70, 70), OnNextOptionsClicked, "Next", out textOut);
            InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(-215, -140, 0) }, new Vector2(240, 240), new Vector2(70, 70), OnPrevOptionsClicked, "Prev", out textOut);
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
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);

                return;
            }

            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

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
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);

                return;
            }

            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

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
            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

            Mod.modInstance.LoadConfig();
        }

        private void OnCloseClicked(Text textRef)
        {
            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

            H3MP_Server.Close();

            // Switch page
            SetPage(0);
        }

        private void OnDisconnectClicked(Text textRef)
        {
            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

            H3MP_Client.singleton.Disconnect(true, 0);

            // Switch page
            SetPage(0);
        }

        private void OnOptionsClicked(Text textRef)
        {
            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

            // Switch page
            SetPage(3);
        }

        private void OnBackClicked(Text textRef)
        {
            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

            if (Mod.managerObject == null)
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
            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

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
            if (GM.TNH_Manager != null)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

                Mod.TNH_Manager_InitPlayerPosition.Invoke(GM.TNH_Manager, null);

                if(Mod.currentTNHInstance != null)
                {
                    Mod.currentTNHInstance.RevivePlayer(H3MP_GameManager.ID);
                }
            }
            else
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);
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
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);

                return;
            }

            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

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
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);

                return;
            }

            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

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
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);

                return;
            }

            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

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
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);

                return;
            }

            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

            if (GM.CurrentPlayerBody.GetPlayerIFF() == 0)
            {
                GM.CurrentPlayerBody.SetPlayerIFF(-3);
            }
            else if (GM.CurrentPlayerBody.GetPlayerIFF() == -3)
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
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);

                return;
            }

            if (H3MP_ThreadManager.host)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

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
            else
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);
            }
        }

        private void OnNameplatesClicked(Text textRef)
        {
            if (Mod.managerObject == null)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);

                return;
            }

            if (H3MP_ThreadManager.host)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

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
            else
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);
            }
        }

        private void OnTNHRadarModeClicked(Text textRef)
        {
            if (Mod.managerObject == null)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);

                return;
            }

            if (H3MP_ThreadManager.host)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

                H3MP_GameManager.radarMode = (H3MP_GameManager.radarMode + 1 ) % 3;

                switch (H3MP_GameManager.radarMode)
                {
                    case 0:
                        textRef.text = "Radar mode (All)";
                        if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.manager != null)
                        {
                            // Add all currently playing players to radar
                            foreach (KeyValuePair<int, H3MP_PlayerManager> playerEntry in H3MP_GameManager.players)
                            {
                                if (playerEntry.Value.visible && playerEntry.Value.reticleContact == null && Mod.currentTNHInstance.currentlyPlaying.Contains(playerEntry.Key))
                                {
                                    playerEntry.Value.reticleContact = GM.TNH_Manager.TAHReticle.RegisterTrackedObject(playerEntry.Value.head, (TAH_ReticleContact.ContactType)(H3MP_GameManager.radarColor ? (playerEntry.Value.IFF == GM.CurrentPlayerBody.GetPlayerIFF() ? -2 : -3) : playerEntry.Value.colorIndex - 4));
                                }
                            }
                        }
                        break;
                    case 1:
                        textRef.text = "Radar mode (Friendly only)";
                        if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.manager != null)
                        {
                            // Add all currently playing friendly players to radar, remove if not friendly
                            foreach (KeyValuePair<int, H3MP_PlayerManager> playerEntry in H3MP_GameManager.players)
                            {
                                if (playerEntry.Value.visible && Mod.currentTNHInstance.currentlyPlaying.Contains(playerEntry.Key) &&
                                    playerEntry.Value.IFF == GM.CurrentPlayerBody.GetPlayerIFF() &&
                                    playerEntry.Value.reticleContact == null)
                                {
                                    playerEntry.Value.reticleContact = GM.TNH_Manager.TAHReticle.RegisterTrackedObject(playerEntry.Value.head, (TAH_ReticleContact.ContactType)(H3MP_GameManager.radarColor ? (playerEntry.Value.IFF == GM.CurrentPlayerBody.GetPlayerIFF() ? -2 : -3) : playerEntry.Value.colorIndex - 4));
                                }
                                else if ((!playerEntry.Value.visible || !Mod.currentTNHInstance.currentlyPlaying.Contains(playerEntry.Key) || playerEntry.Value.IFF != GM.CurrentPlayerBody.GetPlayerIFF())
                                         && playerEntry.Value.reticleContact != null)
                                {
                                    for (int i = GM.TNH_Manager.TAHReticle.Contacts.Count - 1; i >= 0; i--)
                                    {
                                        if (GM.TNH_Manager.TAHReticle.Contacts[i] == playerEntry.Value.reticleContact)
                                        {
                                            HashSet<Transform> ts = (HashSet<Transform>)Mod.TAH_Reticle_m_trackedTransforms.GetValue(GM.TNH_Manager.TAHReticle);
                                            ts.Remove(GM.TNH_Manager.TAHReticle.Contacts[i].TrackedTransform);
                                            UnityEngine.Object.Destroy(GM.TNH_Manager.TAHReticle.Contacts[i].gameObject);
                                            GM.TNH_Manager.TAHReticle.Contacts.RemoveAt(i);
                                            playerEntry.Value.reticleContact = null;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case 2:
                        textRef.text = "Radar mode (None)";
                        if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.manager != null)
                        {
                            // Remove all player contacts
                            foreach (KeyValuePair<int, H3MP_PlayerManager> playerEntry in H3MP_GameManager.players)
                            {
                                if (playerEntry.Value.reticleContact != null)
                                {
                                    for (int i = GM.TNH_Manager.TAHReticle.Contacts.Count - 1; i >= 0; i--)
                                    {
                                        if (GM.TNH_Manager.TAHReticle.Contacts[i] == playerEntry.Value.reticleContact)
                                        {
                                            HashSet<Transform> ts = (HashSet<Transform>)Mod.TAH_Reticle_m_trackedTransforms.GetValue(GM.TNH_Manager.TAHReticle);
                                            ts.Remove(GM.TNH_Manager.TAHReticle.Contacts[i].TrackedTransform);
                                            UnityEngine.Object.Destroy(GM.TNH_Manager.TAHReticle.Contacts[i].gameObject);
                                            GM.TNH_Manager.TAHReticle.Contacts.RemoveAt(i);
                                            playerEntry.Value.reticleContact = null;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }

                H3MP_ServerSend.RadarMode(H3MP_GameManager.radarMode);
            }
            else
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);
            }
        }

        private void OnTNHRadarColorClicked(Text textRef)
        {
            if (Mod.managerObject == null)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);

                return;
            }

            if (H3MP_ThreadManager.host)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

                H3MP_GameManager.radarColor = !H3MP_GameManager.radarColor;

                textRef.text = "Radar color IFF (" + H3MP_GameManager.radarColor + ")";

                // Set color of any active player contacts
                if (H3MP_GameManager.radarColor)
                {
                    foreach (KeyValuePair<int, H3MP_PlayerManager> playerEntry in H3MP_GameManager.players)
                    {
                        if (playerEntry.Value.reticleContact == null)
                        {
                            playerEntry.Value.reticleContact.R_Arrow.material.color = playerEntry.Value.IFF == GM.CurrentPlayerBody.GetPlayerIFF() ? Color.green : Color.red;
                            playerEntry.Value.reticleContact.R_Icon.material.color = playerEntry.Value.IFF == GM.CurrentPlayerBody.GetPlayerIFF() ? Color.green : Color.red;
                        }
                    }
                }
                else
                {
                    foreach (KeyValuePair<int, H3MP_PlayerManager> playerEntry in H3MP_GameManager.players)
                    {
                        if (playerEntry.Value.reticleContact == null)
                        {
                            playerEntry.Value.reticleContact.R_Arrow.material.color = H3MP_GameManager.colors[playerEntry.Value.colorIndex];
                            playerEntry.Value.reticleContact.R_Icon.material.color = H3MP_GameManager.colors[playerEntry.Value.colorIndex];
                        }
                    }
                }

                H3MP_ServerSend.RadarColor(H3MP_GameManager.radarColor);
            }
            else
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);
            }
        }

        private void OnNextOptionsClicked(Text textRef)
        {
            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

            SetPage(currentPage + 1);
        }

        private void OnPrevOptionsClicked(Text textRef)
        {
            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

            SetPage(currentPage - 1);
        }

        private void OnMaxHealthClicked(Text textRef)
        {
            // Place holder
        }

        private void OnNextMaxHealthClicked(Text textRef)
        {
            if (Mod.managerObject == null)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);

                return;
            }

            if (H3MP_ThreadManager.host)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

                ++H3MP_GameManager.maxHealthIndex;
                if(H3MP_GameManager.maxHealthIndex >= H3MP_GameManager.maxHealths.Length)
                {
                    H3MP_GameManager.maxHealthIndex = -1;
                }

                UpdateMaxHealth(H3MP_GameManager.scene, H3MP_GameManager.instance, H3MP_GameManager.maxHealthIndex, GM.CurrentPlayerBody.GetMaxHealthPlayerRaw());
            }
            else
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);
            }
        }

        private void OnPreviousMaxHealthClicked(Text textRef)
        {
            if (Mod.managerObject == null)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);

                return;
            }

            if (H3MP_ThreadManager.host)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

                --H3MP_GameManager.maxHealthIndex;
                if (H3MP_GameManager.maxHealthIndex < -1)
                {
                    H3MP_GameManager.maxHealthIndex = H3MP_GameManager.maxHealths.Length;
                }

                UpdateMaxHealth(H3MP_GameManager.scene, H3MP_GameManager.instance, H3MP_GameManager.maxHealthIndex, GM.CurrentPlayerBody.GetMaxHealthPlayerRaw());
            }
            else
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);
            }
        }

        public static void UpdateMaxHealth(string scene, int instance, int index, float original, int clientID = 0)
        {
            Mod.LogInfo("UpdateMaxHealth: " + scene+"/"+instance+", to index: "+index+" with original: "+original+" and clientID: "+clientID, false);
            // Tell others if necessary
            if (index != -2 && H3MP_ThreadManager.host)
            {
                Mod.LogInfo("\tHost, sending", false);
                H3MP_ServerSend.MaxHealth(scene, instance, index, original, clientID);
            }

            if (index == -2)
            {
                Mod.LogInfo("\tIndex -2, setting to value in our current scene/instance", false);
                if (!H3MP_GameManager.overrideMaxHealthSetting &&
                    H3MP_GameManager.maxHealthByInstanceByScene.TryGetValue(scene, out Dictionary<int, KeyValuePair<float, int>> instanceDict) &&
                    instanceDict.TryGetValue(instance, out KeyValuePair<float, int> entry))
                {
                    Mod.LogInfo("\t\tFound entry: "+entry.Key+":"+entry.Value, false);
                    H3MP_GameManager.maxHealthIndex = entry.Value;

                    ++SetHealthThresholdPatch.skip;
                    GM.CurrentPlayerBody.SetHealthThreshold(H3MP_GameManager.maxHealths[H3MP_GameManager.maxHealthIndex]);
                    --SetHealthThresholdPatch.skip;

                    if (maxHealthText != null)
                    {
                        maxHealthText.text = "Max health: " + H3MP_GameManager.maxHealths[H3MP_GameManager.maxHealthIndex];
                    }
                }
                else
                {
                    Mod.LogInfo("\t\tNo entry found", false);
                    H3MP_GameManager.maxHealthIndex = -1;

                    if (maxHealthText != null)
                    {
                        maxHealthText.text = "Max health: Not set";
                    }
                }
            }
            else if (index == -1)
            {
                Mod.LogInfo("\tIndex -1, unsetting max health entry for given scene/instance", false);
                if (H3MP_GameManager.scene.Equals(scene) && H3MP_GameManager.instance == instance)
                {
                    Mod.LogInfo("\t\tCurrent scene/instance, setting index and text", false);
                    H3MP_GameManager.maxHealthIndex = -1;

                    if (maxHealthText != null)
                    {
                        maxHealthText.text = "Max health: Not set";
                    }
                }

                if (H3MP_GameManager.maxHealthByInstanceByScene.TryGetValue(scene, out Dictionary<int, KeyValuePair<float, int>> instanceDict) &&
                    instanceDict.TryGetValue(instance, out KeyValuePair<float, int> entry))
                {
                    Mod.LogInfo("\t\tHave entry", false);
                    // Set our own if necessary
                    if (GM.CurrentPlayerBody != null && H3MP_GameManager.scene.Equals(scene) && H3MP_GameManager.instance == instance)
                    {
                        Mod.LogInfo("\t\t\tCurrent scene/instance and we have body, resetting our health to scene default: "+entry.Key, false);
                        ++SetHealthThresholdPatch.skip;
                        GM.CurrentPlayerBody.SetHealthThreshold(entry.Key);
                        --SetHealthThresholdPatch.skip;
                    }

                    // Remove instance
                    instanceDict.Remove(instance);

                    // Remove scene if no more entries for it
                    if (instanceDict.Count == 0)
                    {
                        H3MP_GameManager.maxHealthByInstanceByScene.Remove(scene);
                    }
                }
            }
            else
            {
                Mod.LogInfo("\tIndex > -1, setting max health entry for given scene/instance", false);
                if (H3MP_GameManager.maxHealthByInstanceByScene.TryGetValue(scene, out Dictionary<int, KeyValuePair<float, int>> instanceDict))
                {
                    if (instanceDict.TryGetValue(instance, out KeyValuePair<float, int> entry))
                    {
                        Mod.LogInfo("\t\tReplacing existing entry", false);
                        // Replace existing entry
                        instanceDict[instance] = new KeyValuePair<float, int>(entry.Key, index);
                    }
                    else
                    {
                        Mod.LogInfo("\t\tAdding entry in new instance", false);
                        // Add new entry
                        instanceDict.Add(instance, new KeyValuePair<float, int>(original, index));
                    }
                }
                else
                {
                    Mod.LogInfo("\t\tAdding entry in new scene", false);
                    // Add new entry
                    Dictionary<int, KeyValuePair<float, int>> newDict = new Dictionary<int, KeyValuePair<float, int>>();
                    H3MP_GameManager.maxHealthByInstanceByScene.Add(scene, newDict);
                    newDict.Add(instance, new KeyValuePair<float, int>(original, index));
                }

                if (H3MP_GameManager.scene.Equals(scene) && H3MP_GameManager.instance == instance)
                {
                    Mod.LogInfo("\t\tCurrent scene/instance, setting index", false);
                    H3MP_GameManager.maxHealthIndex = index;

                    if (maxHealthText != null)
                    {
                        maxHealthText.text = "Max health: " + H3MP_GameManager.maxHealths[H3MP_GameManager.maxHealthIndex];
                    }

                    // Set our own if necessary
                    if (GM.CurrentPlayerBody != null)
                    {
                        Mod.LogInfo("\t\t\tWe have a body, setting max health", false);
                        ++SetHealthThresholdPatch.skip;
                        GM.CurrentPlayerBody.SetHealthThreshold(H3MP_GameManager.maxHealths[H3MP_GameManager.maxHealthIndex]);
                        --SetHealthThresholdPatch.skip;
                    }
                }
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
