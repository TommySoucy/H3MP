using H3MP.Networking;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace H3MP.Scripts
{
    public class ServerListController : MonoBehaviour
    {
        public static ServerListController instance;
        private bool awakened;

        private enum State
        {
            Main, // Server list
            Host, // Host settings
            HostingWaiting, // Confirmed settings, waiting for IS to list us
            Hosting, // Hosting and listed on IS
            Join, // Join settings
            ClientWaiting, // Confirmed settings, waiting for IS to confirm password is correct and that our connection was established
            Client, // Client to a host
        }
        private State state = State.Main;

        // Pages
        public GameObject main;
        public GameObject host;
        public GameObject hosting;
        public GameObject join;
        public GameObject client;

        // Main
        public GameObject mainLoadingAnimation;
        public Transform mainListParent;
        public int mainListPage;
        public GameObject mainPagePrefab;
        public GameObject mainEntryPrefab;
        public GameObject mainHostButton;
        public GameObject mainPrevButton;
        public GameObject mainNextButton;
        public GameObject mainRefreshButton;
        public Text mainInfoText;

        // Host
        public Text hostServerNameLabel;
        public Text hostServerName;
        public Text hostPassword;
        public Text hostLimitLabel;
        public Text hostLimit;
        public Text hostUsernameLabel;
        public Text hostUsername;

        // Hosting
        public GameObject hostingLoadingAnimation;
        public GameObject hostingInfoTextObject;
        public Text hostingInfoText;
        public int hostingListPage;
        public Transform hostingListParent;
        public GameObject hostingPagePrefab;
        public GameObject hostingEntryPrefab;
        public GameObject hostingPrevButton;
        public GameObject hostingNextButton;

        // Client
        public GameObject clientLoadingAnimation;
        public GameObject clientInfoTextObject;
        public Text clientInfoText;
        public int clientListPage;
        public Transform clientListParent;
        public GameObject clientPagePrefab;
        public GameObject clientEntryPrefab;
        public GameObject clientPrevButton;
        public GameObject clientNextButton;
        public GameObject clientDisconnectButton;

        private void Awake()
        {
            TODO: // Subscribe to event to know when we host/join so we can set it to corresponding page
            if(instance != null)
            {
                Destroy(gameObject);
                return;
            }
            else
            {
                instance = this;
            }
            awakened = true;

            ISClient.OnReceiveHostEntries += HostEntriesReceived;
            ISClient.OnDisconnect += ISClientDisconnected;
            ISClient.OnListed += Listed;

            bool init = false;
            if (!ISClient.isConnected)
            {
                // Not connected to index server
                if(Mod.managerObject != null)
                {
                    // But already connected directly
                    init = true;
                    ConnectedInit();
                }
                else // Not already connected directly
                {
                    ISClient.Connect();
                }
            }
            else if(ISClient.gotWelcome)
            {
                // If connected with welcome, it means that we kept connection alive despite there not being a ServerListController
                // Meaning we are hosting already
                // So init UI accordingly
                init = true;
                ConnectedInit();
            }
            // else, connection attempt to index server already in progress, waiting for welcome already, this case should not be possible

            // If did not connect init then we want to init on main page
            if (!init)
            {
                SetMainPage(null);
            }
        }

        private void SetMainPage(List<ISEntry> entries)
        {
            state = State.Main;

            if (entries == null)
            {
                TODO: // Subscribe to event to update once connection to IS is complete
                mainLoadingAnimation.SetActive(true);
                mainListParent.gameObject.SetActive(false);
                mainHostButton.SetActive(false);
                mainPrevButton.SetActive(false);
                mainNextButton.SetActive(false);
                mainRefreshButton.SetActive(false);
                mainInfoText.gameObject.SetActive(true);
                mainInfoText.text = "Waiting for index server";

                // Request latest host entries
                TODO: cont from here//cant make this call until get welcome, need to rewrite this, can probably just check if got welcome and request for list and wait for it and when we doi n handle just call this again with entries
                ISClientSend.RequestHostEntries();
            }
            else
            {
                TODO: // Subscribe to event to update is connection to IS drops
                mainLoadingAnimation.SetActive(false);
                mainListParent.gameObject.SetActive(true);
                mainHostButton.SetActive(true);
                mainPrevButton.SetActive(false);
                mainNextButton.SetActive(false);
                mainRefreshButton.SetActive(true);
                if (entries.Count == 0)
                {
                    mainInfoText.gameObject.SetActive(true);
                    mainInfoText.text = "No servers found";
                }
                else
                {
                    mainInfoText.gameObject.SetActive(false);
                }

                // Destroy any existent pages
                while (mainListParent.childCount > 1)
                {
                    Transform otherChild = mainListParent.GetChild(1);
                    otherChild.parent = null;
                    Destroy(otherChild.gameObject);
                }

                TODO1: // Add entry, and page if necessary, when new server is listed
                // Build new pages
                Transform currentListPage = Instantiate(mainPagePrefab, mainListParent).transform;
                mainListPage = 0;
                currentListPage.gameObject.SetActive(true);
                for (int i = 0; i < entries.Count; ++i)
                {
                    GameObject hostEntry = Instantiate(mainEntryPrefab, currentListPage);
                    hostEntry.SetActive(true);
                    Player player = Server.clients[Server.connectedClients[i]].player;
                    hostEntry.transform.GetChild(0).GetComponent<Text>().text = entries[i].name;
                    hostEntry.transform.GetChild(1).GetComponent<Text>().text = entries[i].playerCount + "/" + entries[i].limit;
                    hostEntry.transform.GetChild(2).gameObject.SetActive(entries[i].locked);
                    int entryID = entries[i].ID;
                    hostEntry.GetComponent<Button>().onClick.AddListener(() => { Join(entryID); });

                    // Start a new page every 7 elements
                    if (i + 1 % 7 == 0 && i != entries.Count - 1)
                    {
                        currentListPage = Instantiate(mainPagePrefab, mainListParent).transform;
                        mainNextButton.SetActive(true);
                    }
                }
            }
        }

        private void OnHostClicked()
        {
            state = State.Host;
            main.SetActive(false);
            host.SetActive(true);

            hostServerNameLabel.color = Color.white;
            hostLimitLabel.color = Color.white;
            hostUsernameLabel.color = Color.white;
        }

        private void OnHostConfirmClicked()
        {
            bool failed = false;
            if(hostServerName.text == "")
            {
                failed = true;
                hostServerNameLabel.color = Color.red;
            }
            if(hostLimit.text == "")
            {
                failed = true;
                hostLimitLabel.color = Color.red;
            }
            if(hostUsername.text == "")
            {
                failed = true;
                hostUsernameLabel.color = Color.red;
            }
            if (failed)
            {
                return;
            }
            SetHostingPage(true);
            ISClientSend.List(hostServerName.text, int.Parse(hostLimit.text), hostPassword.text);
            ISClient.wantListed = true;
            ISClient.listed = false;
        }

        private void OnHostingCloseClicked()
        {
            if (ISClient.listed)
            {
                ISClientSend.Unlist();
            }

            hosting.SetActive(false);
            main.SetActive(true);

            SetMainPage(null);
        }

        private void HostEntriesReceived(List<ISEntry> entries)
        {
            if(state == State.Main)
            {
                SetMainPage(entries);
            }
        }

        private void Listed(int ID)
        {
            if(state == State.HostingWaiting)
            {
                SetHostingPage(false);
                ISClient.listed = true;
            }
            else
            {
                ISClient.wantListed = false;
                ISClient.listed = false;
            }
        }

        private void ISClientDisconnected()
        {
            // TODO: // Set listwanted and listed to false, set UI accordingly
        }

        private void Join(int entryID)
        {
            // TODO: // Implement with NAT punch-through
        }

        private void ConnectedInit()
        {
            if(Mod.managerObject != null)
            {
                main.SetActive(false);
                if (ThreadManager.host)
                {
                    hosting.SetActive(true);
                    SetHostingPage(ISClient.isConnected && ISClient.gotWelcome && ISClient.wantListed && !ISClient.listed);
                }
                else
                {
                    client.SetActive(true);
                    SetClientPage(false);
                }
            }
        }

        private void SetHostingPage(bool waiting)
        {
            if (waiting)
            {
                state = State.HostingWaiting;
                hostingLoadingAnimation.SetActive(true);
                hostingInfoTextObject.SetActive(true);
                hostingListParent.gameObject.SetActive(false);
                hostingInfoText.text = "Awaiting server confirm";
            }
            else
            {
                state = State.Hosting;
                hostingLoadingAnimation.SetActive(false);
                hostingInfoTextObject.SetActive(false);
                hostingListParent.gameObject.SetActive(true);

                hostingListParent.GetChild(0).GetComponent<Text>().text = Mod.config["username"].ToString() + " (Host)";

                // Destroy any existent pages
                while (hostingListParent.childCount > 1)
                {
                    Transform otherChild = hostingListParent.GetChild(1);
                    otherChild.parent = null;
                    Destroy(otherChild.gameObject);
                }

                TODO: // Add entry, and page if necessary, when new player connects
                // Build new pages
                Transform currentListPage = Instantiate(hostingPagePrefab, hostingListParent).transform;
                GameObject playerEntry = Instantiate(hostingEntryPrefab, currentListPage);
                playerEntry.SetActive(true);
                hostingListPage = 0;
                currentListPage.gameObject.SetActive(true);
                playerEntry.transform.GetChild(0).GetComponent<Text>().text = Mod.config["Username"].ToString();
                for (int i = 0; i < Server.connectedClients.Count; ++i)
                {
                    playerEntry = Instantiate(hostingEntryPrefab, currentListPage);
                    playerEntry.SetActive(true);
                    Player player = Server.clients[Server.connectedClients[i]].player;
                    playerEntry.transform.GetChild(0).GetComponent<Text>().text = player.username;
                    GameObject kickButtonObject = playerEntry.transform.GetChild(1).gameObject;
                    kickButtonObject.SetActive(true);
                    int playerID = player.ID;
                    kickButtonObject.GetComponent<Button>().onClick.AddListener(() => { KickPlayer(playerID); });

                    // Start a new page every 7 elements
                    if(i+2 % 7 == 0 && i != Server.connectedClients.Count - 1)
                    {
                        currentListPage = Instantiate(hostingPagePrefab, hostingListParent).transform;
                        hostingNextButton.SetActive(true);
                    }
                }
            }
        }

        private void SetClientPage(bool waiting)
        {
            if (waiting)
            {
                TODO: // Subscribe to event to update UI once we get confirm that password is correct and that we completely established connection to host
                state = State.ClientWaiting;
                clientLoadingAnimation.SetActive(true);
                clientInfoTextObject.SetActive(true);
                clientListParent.gameObject.SetActive(false);
            }
            else
            {
                state = State.Client;
                clientLoadingAnimation.SetActive(false);
                clientInfoTextObject.SetActive(false);
                clientListParent.gameObject.SetActive(true);

                clientListParent.GetChild(0).GetComponent<Text>().text = Mod.config["username"].ToString() + " (Host)";

                // Destroy any existent pages
                while (clientListParent.childCount > 1)
                {
                    Transform otherChild = clientListParent.GetChild(1);
                    otherChild.parent = null;
                    Destroy(otherChild.gameObject);
                }

                TODO: // Add entry, and page if necessary, when new player connects
                // Build new pages
                Transform currentListPage = Instantiate(clientPagePrefab, clientListParent).transform;
                clientListPage = 0;
                GameObject playerEntry = Instantiate(clientEntryPrefab, currentListPage);
                playerEntry.SetActive(true);
                currentListPage.gameObject.SetActive(true);
                playerEntry.transform.GetChild(0).GetComponent<Text>().text = Mod.config["Username"].ToString();
                int i = 1;
                foreach (KeyValuePair<int, PlayerManager> player in GameManager.players)
                {
                    playerEntry = Instantiate(clientEntryPrefab, currentListPage);
                    playerEntry.SetActive(true);
                    playerEntry.transform.GetChild(0).GetComponent<Text>().text = player.Value.username + (player.Key == 0 ? " (Host)" : "");

                    // Start a new page every 7 elements
                    if (i % 7 == 0 && i != GameManager.players.Count)
                    {
                        currentListPage = Instantiate(clientPagePrefab, clientListParent).transform;
                        clientNextButton.SetActive(true);
                    }

                    ++i;
                }
            }
        }

        private void KickPlayer(int ID)
        {
            // TODO
        }

        private void OnDestroy()
        {
            ISClient.OnReceiveHostEntries -= HostEntriesReceived;

            if (!awakened)
            {
                return;
            }

            if(ISClient.isConnected)
            {
                if (ISClient.gotWelcome)
                {
                    if(Mod.managerObject == null)
                    {
                        // Not connected directly, can disconnect from IS
                        ISClient.Disconnect(true, 0);
                    }
                    else // Connected directly as client, no need for IS connection
                    {
                        if (ThreadManager.host)
                        {
                            if (!ISClient.listed)
                            {
                                // Hosting but not listed, we can disconnect from IS
                                ISClient.Disconnect(true, 0);
                            }
                            // else, We are listed host, wanna keep connection to IS alive
                        }
                        else // Connected directly as client, no need for IS connection
                        {
                            ISClient.Disconnect(true, 0);
                        }
                    }
                }
                else
                {
                    ISClient.Disconnect(false, 0);
                }
            }
        }
    }
}
