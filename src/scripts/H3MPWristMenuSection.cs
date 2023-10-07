using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using H3MP.Tracking;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace H3MP.Scripts
{
    public class H3MPWristMenuSection : FVRWristMenuSection
    {
        public static bool init;

        delegate void ButtonClick(Text text);
        Dictionary<int, List<KeyValuePair<FVRPointableButton, Vector3>>> pages;
        int currentPage = -1;

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
                if (ThreadManager.host)
                {
                    SetPage(2);
                }
                else
                {
                    SetPage(3);
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
            InitButton(new List<int>() { 0 }, new List<Vector3>() { new Vector3(0, 75, 0) }, new Vector2(1000, 150), new Vector2(140, 70), OnServerListClicked, "Server list", out textOut);
            InitButton(new List<int>() { 0 }, new List<Vector3>() { new Vector3(0, 0, 0) }, new Vector2(1200, 150), new Vector2(140, 70), OnDirectConnectionClicked, "Direct connection", out textOut);
            InitButton(new List<int>() { 1 }, new List<Vector3>() { new Vector3(0, 75, 0) }, new Vector2(500, 240), new Vector2(140, 70), OnHostClicked, "Host", out textOut);
            InitButton(new List<int>() { 1 }, new List<Vector3>() { Vector3.zero }, new Vector2(500, 240), new Vector2(140, 70), OnConnectClicked, "Join", out textOut);
            InitButton(new List<int>() { 0, 1, 2, 3 }, new List<Vector3>() { new Vector3(0, -75, 0), new Vector3(0, -75, 0), new Vector3(0, -75, 0), new Vector3(0, -75, 0) }, new Vector2(500, 150), new Vector2(140, 70), OnOptionsClicked, "Options", out textOut);
            InitButton(new List<int>() { 2 }, new List<Vector3>() { new Vector3(0, 75, 0) }, new Vector2(500, 240), new Vector2(140, 70), OnCloseClicked, "Close\nserver", out textOut);
            InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(0, 75, 0) }, new Vector2(500, 240), new Vector2(140, 70), OnDisconnectClicked, "Disconnect", out textOut);
            InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(-215, 140, 0) }, new Vector2(240, 240), new Vector2(70, 70), OnBackClicked, "Back", out textOut);
            InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(0, 150, 0) }, new Vector2(1200, 150), new Vector2(270, 45), OnReloadConfigClicked, "Reload config", out textOut);
            InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(0, 100, 0) }, new Vector2(1200, 150), new Vector2(270, 45), OnItemInterpolationClicked, "Item interpolation (ON)", out textOut);
            InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(0, 50, 0) }, new Vector2(1200, 150), new Vector2(270, 45), OnTNHReviveClicked, "TNH revive", out textOut);
            InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(0, 0, 0) }, new Vector2(1200, 150), new Vector2(270, 45), OnIFFClicked, "Current IFF: "+GM.CurrentPlayerBody.GetPlayerIFF(), out IFFText);
            InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(155, 0, 0) }, new Vector2(150, 150), new Vector2(45, 45), OnNextIFFClicked, ">", out textOut);
            InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(-155, 0, 0) }, new Vector2(150, 150), new Vector2(45, 45), OnPreviousIFFClicked, "<", out textOut);
            InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(0, -50, 0) }, new Vector2(1200, 150), new Vector2(270, 45), OnColorByIFFClicked, "Color by IFF ("+GameManager.colorByIFF+")", out colorByIFFText);
            InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(0, -100, 0) }, new Vector2(1200, 150), new Vector2(340, 45), OnNameplatesClicked, "Nameplates ("+ (GameManager.nameplateMode == 0 ? "All" : (GameManager.nameplateMode == 1 ? "Friendly Only" : "None"))+ ")", out nameplateText);
            InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(0, -150, 0) }, new Vector2(1200, 150), new Vector2(340, 45), OnHostStartHoldClicked, "Debug: Host start hold", out textOut);

            InitButton(new List<int>() { 5 }, new List<Vector3>() { new Vector3(0, 150, 0) }, new Vector2(1200, 150), new Vector2(340, 45), OnTNHRadarModeClicked, "Radar mode ("+ (GameManager.radarMode == 0 ? "All" : (GameManager.radarMode == 1 ? "Friendly Only" : "None"))+ ")", out radarModeText);
            InitButton(new List<int>() { 5 }, new List<Vector3>() { new Vector3(0, 100, 0) }, new Vector2(1200, 150), new Vector2(340, 45), OnTNHRadarColorClicked, "Radar color IFF ("+ GameManager.radarColor + ")", out radarColorText);
            InitButton(new List<int>() { 5 }, new List<Vector3>() { new Vector3(0, 50, 0) }, new Vector2(1200, 150), new Vector2(340, 45), OnMaxHealthClicked, "Max health: "+ (GameManager.maxHealthIndex == -1 ? "Not set" : GameManager.maxHealths[GameManager.maxHealthIndex].ToString()), out maxHealthText);
            InitButton(new List<int>() { 5 }, new List<Vector3>() { new Vector3(155, 50, 0) }, new Vector2(150, 150), new Vector2(45, 45), OnNextMaxHealthClicked, ">", out textOut);
            InitButton(new List<int>() { 5 }, new List<Vector3>() { new Vector3(-155, 50, 0) }, new Vector2(150, 150), new Vector2(45, 45), OnPreviousMaxHealthClicked, "<", out textOut);
            InitButton(new List<int>() { 5 }, new List<Vector3>() { new Vector3(0, 0, 0) }, new Vector2(1200, 150), new Vector2(340, 45), OnSetRespawnPointClicked, "Set respawn point", out textOut);
            InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(215, -140, 0) }, new Vector2(240, 240), new Vector2(70, 70), OnNextOptionsClicked, "Next", out textOut);
            InitButton(new List<int>() { 5 }, new List<Vector3>() { new Vector3(-215, -140, 0) }, new Vector2(240, 240), new Vector2(70, 70), OnPrevOptionsClicked, "Prev", out textOut);
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

        private void OnServerListClicked(Text textRef)
        {
            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

            if(ServerListController.instance == null)
            {
                Instantiate(Mod.serverListPrefab);
                Vector3 forwardFlat = Vector3.ProjectOnPlane(GM.CurrentPlayerBody.Head.forward, Vector3.up);
                ServerListController.instance.transform.position = GM.CurrentPlayerBody.Head.position + 2 * forwardFlat;
                ServerListController.instance.transform.rotation = Quaternion.LookRotation(forwardFlat);
            }
            else
            {
                Destroy(ServerListController.instance.gameObject);
            }
        }

        private void OnDirectConnectionClicked(Text textRef)
        {
            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);
            SetPage(1);
        }

        private void OnHostClicked(Text textRef)
        {
            if (Mod.managerObject != null)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);

                return;
            }

            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

            Mod.OnHostClicked();

            // Switch page
            SetPage(2);
        }

        private void OnConnectClicked(Text textRef)
        {
            if (Mod.managerObject != null)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);

                return;
            }

            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

            Mod.OnConnectClicked(null);

            // Switch page
            SetPage(3);
        }

        private void OnReloadConfigClicked(Text textRef)
        {
            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

            Mod.modInstance.LoadConfig();
        }

        private void OnCloseClicked(Text textRef)
        {
            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

            Server.Close();

            // Switch page
            SetPage(1);
        }

        private void OnDisconnectClicked(Text textRef)
        {
            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

            Client.singleton.Disconnect(true, 0);

            // Switch page
            SetPage(1);
        }

        private void OnOptionsClicked(Text textRef)
        {
            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

            // Switch page
            SetPage(2);
        }

        private void OnBackClicked(Text textRef)
        {
            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

            if (Mod.managerObject == null)
            {
                SetPage(1);
            }
            else
            {
                SetPage(ThreadManager.host ? 2 : 3);
            }
        }

        private void OnItemInterpolationClicked(Text textRef)
        {
            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

            if (TrackedItem.interpolated)
            {
                TrackedItem.interpolated = false;
                textRef.text = "Item interpolation (OFF)";
            }
            else
            {
                TrackedItem.interpolated = true;
                textRef.text = "Item interpolation (ON)";
            }
        }

        private void OnTNHReviveClicked(Text textRef)
        {
            if (GM.TNH_Manager != null)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

                GM.TNH_Manager.InitPlayerPosition();

                if(Mod.currentTNHInstance != null)
                {
                    Mod.currentTNHInstance.RevivePlayer(GameManager.ID);
                }
            }
            else
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);
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
                GM.CurrentPlayerBody.SetPlayerIFF(-3);
            }
            else if (GM.CurrentPlayerBody.GetPlayerIFF() == -3)
            {
                GM.CurrentPlayerBody.SetPlayerIFF(0);
            }
            else
            {
                GM.CurrentPlayerBody.SetPlayerIFF(GM.CurrentPlayerBody.GetPlayerIFF() + 1);
            }

            // Set canvases visible on all players depending on their IFF and nameplate mode
            foreach(KeyValuePair<int, PlayerManager> entry in GameManager.players)
            {
                if(entry.Value.playerBody != null)
                {
                    entry.Value.playerBody.physicalPlayerBody.SetCanvasesEnabled(GameManager.nameplateMode == 0 || (GameManager.nameplateMode == 1 && GM.CurrentPlayerBody.GetPlayerIFF() == entry.Value.IFF));
                }
            }

            if (ThreadManager.host)
            {
                ServerSend.PlayerIFF(0, GM.CurrentPlayerBody.GetPlayerIFF());
            }
            else
            {
                ClientSend.PlayerIFF(GM.CurrentPlayerBody.GetPlayerIFF());
            }

            if (GameManager.colorByIFF)
            {
                GameManager.SetPlayerColor(GameManager.ID, GM.CurrentPlayerBody.GetPlayerIFF(), false, 0, false);
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

            // Set canvases visible on all players depending on their IFF and nameplate mode
            foreach (KeyValuePair<int, PlayerManager> entry in GameManager.players)
            {
                if (entry.Value.playerBody != null)
                {
                    entry.Value.playerBody.physicalPlayerBody.SetCanvasesEnabled(GameManager.nameplateMode == 0 || (GameManager.nameplateMode == 1 && GM.CurrentPlayerBody.GetPlayerIFF() == entry.Value.IFF));
                }
            }

            if (ThreadManager.host)
            {
                ServerSend.PlayerIFF(0, GM.CurrentPlayerBody.GetPlayerIFF());
            }
            else
            {
                ClientSend.PlayerIFF(GM.CurrentPlayerBody.GetPlayerIFF());
            }

            if (GameManager.colorByIFF)
            {
                GameManager.SetPlayerColor(GameManager.ID, GM.CurrentPlayerBody.GetPlayerIFF(), false, 0, false);
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

            if (ThreadManager.host)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

                GameManager.colorByIFF = !GameManager.colorByIFF;

                textRef.text = "Color by IFF (" + GameManager.colorByIFF + ")";

                if (GameManager.colorByIFF)
                {
                    GameManager.colorIndex = GM.CurrentPlayerBody.GetPlayerIFF() % GameManager.colors.Length;
                    BodyWristMenuSection.colorText.text = "Current color: " + GameManager.colorNames[GameManager.colorIndex];

                    foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
                    {
                        playerEntry.Value.SetColor(playerEntry.Value.IFF);
                    }
                }
                else
                {
                    foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
                    {
                        playerEntry.Value.SetColor(playerEntry.Value.colorIndex);
                    }
                }

                ServerSend.ColorByIFF(GameManager.colorByIFF);
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

            if (ThreadManager.host)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

                GameManager.nameplateMode = (GameManager.nameplateMode + 1 ) % 3;

                switch (GameManager.nameplateMode)
                {
                    case 0:
                        textRef.text = "Nameplates (All)";
                        foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
                        {
                            if (playerEntry.Value.playerBody != null)
                            {
                                playerEntry.Value.playerBody.physicalPlayerBody.SetCanvasesEnabled(true);
                            }
                        }
                        break;
                    case 1:
                        textRef.text = "Nameplates (Friendly only)";
                        foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
                        {
                            if (playerEntry.Value.playerBody != null)
                            {
                                playerEntry.Value.playerBody.physicalPlayerBody.SetCanvasesEnabled(GM.CurrentPlayerBody.GetPlayerIFF() == playerEntry.Value.IFF);
                            }
                        }
                        break;
                    case 2:
                        textRef.text = "Nameplates (None)";
                        foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
                        {
                            if (playerEntry.Value.playerBody != null)
                            {
                                playerEntry.Value.playerBody.physicalPlayerBody.SetCanvasesEnabled(false);
                            }
                        }
                        break;
                }

                ServerSend.NameplateMode(GameManager.nameplateMode);
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

            if (ThreadManager.host)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

                GameManager.radarMode = (GameManager.radarMode + 1 ) % 3;

                switch (GameManager.radarMode)
                {
                    case 0:
                        textRef.text = "Radar mode (All)";
                        if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.manager != null)
                        {
                            // Add all currently playing players to radar
                            foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
                            {
                                if (playerEntry.Value.visible && playerEntry.Value.reticleContact == null && Mod.currentTNHInstance.currentlyPlaying.Contains(playerEntry.Key))
                                {
                                    playerEntry.Value.reticleContact = GM.TNH_Manager.TAHReticle.RegisterTrackedObject(playerEntry.Value.head, (TAH_ReticleContact.ContactType)(GameManager.radarColor ? (playerEntry.Value.IFF == GM.CurrentPlayerBody.GetPlayerIFF() ? -2 : -3) : playerEntry.Value.colorIndex - 4));
                                }
                            }
                        }
                        break;
                    case 1:
                        textRef.text = "Radar mode (Friendly only)";
                        if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.manager != null)
                        {
                            // Add all currently playing friendly players to radar, remove if not friendly
                            foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
                            {
                                if (playerEntry.Value.visible && Mod.currentTNHInstance.currentlyPlaying.Contains(playerEntry.Key) &&
                                    playerEntry.Value.IFF == GM.CurrentPlayerBody.GetPlayerIFF() &&
                                    playerEntry.Value.reticleContact == null)
                                {
                                    playerEntry.Value.reticleContact = GM.TNH_Manager.TAHReticle.RegisterTrackedObject(playerEntry.Value.head, (TAH_ReticleContact.ContactType)(GameManager.radarColor ? (playerEntry.Value.IFF == GM.CurrentPlayerBody.GetPlayerIFF() ? -2 : -3) : playerEntry.Value.colorIndex - 4));
                                }
                                else if ((!playerEntry.Value.visible || !Mod.currentTNHInstance.currentlyPlaying.Contains(playerEntry.Key) || playerEntry.Value.IFF != GM.CurrentPlayerBody.GetPlayerIFF())
                                         && playerEntry.Value.reticleContact != null)
                                {
                                    for (int i = GM.TNH_Manager.TAHReticle.Contacts.Count - 1; i >= 0; i--)
                                    {
                                        if (GM.TNH_Manager.TAHReticle.Contacts[i] == playerEntry.Value.reticleContact)
                                        {
                                            GM.TNH_Manager.TAHReticle.m_trackedTransforms.Remove(GM.TNH_Manager.TAHReticle.Contacts[i].TrackedTransform);
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
                            foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
                            {
                                if (playerEntry.Value.reticleContact != null)
                                {
                                    for (int i = GM.TNH_Manager.TAHReticle.Contacts.Count - 1; i >= 0; i--)
                                    {
                                        if (GM.TNH_Manager.TAHReticle.Contacts[i] == playerEntry.Value.reticleContact)
                                        {
                                            GM.TNH_Manager.TAHReticle.m_trackedTransforms.Remove(GM.TNH_Manager.TAHReticle.Contacts[i].TrackedTransform);
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

                ServerSend.RadarMode(GameManager.radarMode);
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

            if (ThreadManager.host)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

                GameManager.radarColor = !GameManager.radarColor;

                textRef.text = "Radar color IFF (" + GameManager.radarColor + ")";

                // Set color of any active player contacts
                if (GameManager.radarColor)
                {
                    foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
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
                    foreach (KeyValuePair<int, PlayerManager> playerEntry in GameManager.players)
                    {
                        if (playerEntry.Value.reticleContact == null)
                        {
                            playerEntry.Value.reticleContact.R_Arrow.material.color = GameManager.colors[playerEntry.Value.colorIndex];
                            playerEntry.Value.reticleContact.R_Icon.material.color = GameManager.colors[playerEntry.Value.colorIndex];
                        }
                    }
                }

                ServerSend.RadarColor(GameManager.radarColor);
            }
            else
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);
            }
        }

        private void OnHostStartHoldClicked(Text textRef)
        {
            if (Mod.managerObject == null)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);

                return;
            }

            if(Mod.currentTNHInstance != null && !Mod.currentTNHInstance.holdOngoing && Mod.currentTNHInstance.controller != -1)
            {
                if(Mod.currentTNHInstance.controller == GameManager.ID)
                {
                    if (Mod.currentTNHInstance.manager != null)
                    {
                        SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);
                        GM.CurrentMovementManager.TeleportToPoint(Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].SpawnPoint_SystemNode.position, true);
                        Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].m_systemNode.m_hasActivated = true;
                        Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].m_systemNode.m_hasInitiatedHold = true;
                        Mod.currentTNHInstance.manager.HoldPoints[Mod.currentTNHInstance.curHoldIndex].BeginHoldChallenge();
                        return;
                    }
                }
                else
                {
                    if (ThreadManager.host)
                    {
                        ServerSend.TNHHostStartHold(Mod.currentTNHInstance.instance);
                    }
                    else
                    {
                        ClientSend.TNHHostStartHold(Mod.currentTNHInstance.instance);
                    }
                    return;
                }
            }

            SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);
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

            if (ThreadManager.host)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

                ++GameManager.maxHealthIndex;
                if(GameManager.maxHealthIndex >= GameManager.maxHealths.Length)
                {
                    GameManager.maxHealthIndex = -1;
                }

                UpdateMaxHealth(GameManager.scene, GameManager.instance, GameManager.maxHealthIndex, GM.CurrentPlayerBody.GetMaxHealthPlayerRaw());
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

            if (ThreadManager.host)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

                --GameManager.maxHealthIndex;
                if (GameManager.maxHealthIndex < -1)
                {
                    GameManager.maxHealthIndex = GameManager.maxHealths.Length;
                }

                UpdateMaxHealth(GameManager.scene, GameManager.instance, GameManager.maxHealthIndex, GM.CurrentPlayerBody.GetMaxHealthPlayerRaw());
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
            if (index != -2 && ThreadManager.host)
            {
                Mod.LogInfo("\tHost, sending", false);
                ServerSend.MaxHealth(scene, instance, index, original, clientID);
            }

            if (index == -2)
            {
                Mod.LogInfo("\tIndex -2, setting to value in our current scene/instance", false);
                if (!GameManager.overrideMaxHealthSetting &&
                    GameManager.maxHealthByInstanceByScene.TryGetValue(scene, out Dictionary<int, KeyValuePair<float, int>> instanceDict) &&
                    instanceDict.TryGetValue(instance, out KeyValuePair<float, int> entry))
                {
                    Mod.LogInfo("\t\tFound entry: "+entry.Key+":"+entry.Value, false);
                    GameManager.maxHealthIndex = entry.Value;

                    ++SetHealthThresholdPatch.skip;
                    GM.CurrentPlayerBody.SetHealthThreshold(GameManager.maxHealths[GameManager.maxHealthIndex]);
                    --SetHealthThresholdPatch.skip;

                    if (maxHealthText != null)
                    {
                        maxHealthText.text = "Max health: " + GameManager.maxHealths[GameManager.maxHealthIndex];
                    }
                }
                else
                {
                    Mod.LogInfo("\t\tNo entry found", false);
                    GameManager.maxHealthIndex = -1;

                    if (maxHealthText != null)
                    {
                        maxHealthText.text = "Max health: Not set";
                    }
                }
            }
            else if (index == -1)
            {
                Mod.LogInfo("\tIndex -1, unsetting max health entry for given scene/instance", false);
                if (GameManager.scene.Equals(scene) && GameManager.instance == instance)
                {
                    Mod.LogInfo("\t\tCurrent scene/instance, setting index and text", false);
                    GameManager.maxHealthIndex = -1;

                    if (maxHealthText != null)
                    {
                        maxHealthText.text = "Max health: Not set";
                    }
                }

                if (GameManager.maxHealthByInstanceByScene.TryGetValue(scene, out Dictionary<int, KeyValuePair<float, int>> instanceDict) &&
                    instanceDict.TryGetValue(instance, out KeyValuePair<float, int> entry))
                {
                    Mod.LogInfo("\t\tHave entry", false);
                    // Set our own if necessary
                    if (GM.CurrentPlayerBody != null && GameManager.scene.Equals(scene) && GameManager.instance == instance)
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
                        GameManager.maxHealthByInstanceByScene.Remove(scene);
                    }
                }
            }
            else
            {
                Mod.LogInfo("\tIndex > -1, setting max health entry for given scene/instance", false);
                if (GameManager.maxHealthByInstanceByScene.TryGetValue(scene, out Dictionary<int, KeyValuePair<float, int>> instanceDict))
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
                    GameManager.maxHealthByInstanceByScene.Add(scene, newDict);
                    newDict.Add(instance, new KeyValuePair<float, int>(original, index));
                }

                if (GameManager.scene.Equals(scene) && GameManager.instance == instance)
                {
                    Mod.LogInfo("\t\tCurrent scene/instance, setting index", false);
                    GameManager.maxHealthIndex = index;

                    if (maxHealthText != null)
                    {
                        maxHealthText.text = "Max health: " + GameManager.maxHealths[GameManager.maxHealthIndex];
                    }

                    // Set our own if necessary
                    if (GM.CurrentPlayerBody != null)
                    {
                        Mod.LogInfo("\t\t\tWe have a body, setting max health", false);
                        ++SetHealthThresholdPatch.skip;
                        GM.CurrentPlayerBody.SetHealthThreshold(GameManager.maxHealths[GameManager.maxHealthIndex]);
                        --SetHealthThresholdPatch.skip;
                    }
                }
            }
        }

        private void OnSetRespawnPointClicked(Text textRef)
        {
            if (GM.CurrentSceneSettings != null && GM.CurrentPlayerBody != null)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);
                Transform deathResetPoint = GM.CurrentSceneSettings.DeathResetPoint;
                if (deathResetPoint == null)
                {
                    deathResetPoint = new GameObject("DeathResetPoint").transform;
                    GM.CurrentSceneSettings.DeathResetPoint = deathResetPoint;
                }
                deathResetPoint.position = GM.CurrentPlayerBody.transform.position;
                deathResetPoint.rotation = GM.CurrentPlayerBody.transform.rotation;
            }
            else
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);
            }
        }

        private void ProcessPlayerPrefabIndex(int index, Text text)
        {
            if(GameManager.playerPrefabIndex == -1)
            {
                GameManager.playerPrefabID = "None";

                if(GameManager.currentPlayerBody != null)
                {
                    Destroy(GameManager.currentPlayerBody.gameObject);
                }

                text.text = "Body: None";

                return;
            }

            string newID = GameManager.playerPrefabIDs[GameManager.playerPrefabIndex];
            if (!GameManager.playerPrefabID.Equals(newID))
            {
                GameManager.SetPlayerPrefab(newID);
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
