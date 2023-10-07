using H3MP.Networking;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

namespace H3MP.Scripts
{
    public class ServerListController : MonoBehaviour
    {
        public static ServerListController instance;
        private bool awakened;

        public enum State
        {
            MainWaiting, // Waiting for host entires from IS
            Main, // Server list
            Host, // Host settings
            HostingWaiting, // Confirmed settings, waiting for IS to list us
            Hosting, // Hosting and listed on IS
            Join, // Join settings
            ClientWaiting, // Confirmed settings, waiting for IS to confirm password is correct and that our connection was established
            Client, // Client to a host
        }
        public State state = State.Main;

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
        public Text hostPortLabel;
        public Text hostPort;

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

        // Join
        public Text joinUsernameLabel;
        public Text joinUsername;
        public Text joinPassword;
        public int joiningEntry;
        public bool gotEndPoint;
        public GameObject joinPasswordField;
        public Text joinServerName;

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
            Mod.OnConnection += Connected;
            GameManager.OnPlayerAdded += PlayerAdded;
            Mod.OnPlayerRemoved += PlayerRemoved;

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

        public void SetMainPage(List<ISEntry> entries)
        {
            if (entries == null)
            {
                state = State.MainWaiting;

                mainLoadingAnimation.SetActive(true);
                mainListParent.gameObject.SetActive(false);
                mainHostButton.SetActive(false);
                mainPrevButton.SetActive(false);
                mainNextButton.SetActive(false);
                mainRefreshButton.SetActive(false);
                mainInfoText.gameObject.SetActive(true);
                mainInfoText.color = Color.white;
                mainInfoText.text = "Waiting for index server";

                // Request latest host entries if possible
                if (ISClient.gotWelcome)
                {
                    ISClientSend.RequestHostEntries();
                }
                // else, don't yet have welcome, we're going to call this again when we do
            }
            else
            {
                state = State.Main;

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

                // Build new pages
                Transform currentListPage = Instantiate(mainPagePrefab, mainListParent).transform;
                for (int i = 0; i < entries.Count; ++i)
                {
                    GameObject hostEntry = Instantiate(mainEntryPrefab, currentListPage);
                    hostEntry.SetActive(true);
                    Player player = Server.clients[Server.connectedClients[i]].player;
                    hostEntry.transform.GetChild(0).GetComponent<Text>().text = entries[i].name;
                    hostEntry.transform.GetChild(1).GetComponent<Text>().text = entries[i].playerCount + "/" + entries[i].limit;
                    hostEntry.transform.GetChild(2).gameObject.SetActive(entries[i].locked);
                    int entryID = entries[i].ID;
                    bool hasPassword = entries[i].locked;
                    string serverName = entries[i].name;
                    hostEntry.GetComponent<Button>().onClick.AddListener(() => { Join(entryID, hasPassword, serverName); });

                    // Start a new page every 7 elements
                    if (i + 1 % 7 == 0 && i != entries.Count - 1)
                    {
                        currentListPage = Instantiate(mainPagePrefab, mainListParent).transform;
                        mainNextButton.SetActive(true);
                    }
                }
                mainListPage = Mathf.Min(mainListPage, mainListParent.childCount - 2);
                mainListParent.GetChild(mainListPage + 1).gameObject.SetActive(true);
            }
        }

        public void OnHostClicked()
        {
            state = State.Host;
            main.SetActive(false);
            host.SetActive(true);

            hostServerNameLabel.color = Color.white;
            hostLimitLabel.color = Color.white;
            hostUsernameLabel.color = Color.white;
            hostPortLabel.color = Color.white;
        }

        public void OnHostConfirmClicked()
        {
            bool failed = false;
            if(hostServerName.text == "")
            {
                failed = true;
                hostServerNameLabel.color = Color.red;
            }
            if(hostLimit.text == "" || !uint.TryParse(hostLimit.text, out uint parsedLimit))
            {
                failed = true;
                hostLimitLabel.color = Color.red;
            }
            if(hostPort.text == "" || !ushort.TryParse(hostPort.text, out ushort parsedPort))
            {
                failed = true;
                hostPortLabel.color = Color.red;
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
            Mod.config["ServerName"] = hostServerName.text;
            Mod.config["MaxClientCount"] = uint.Parse(hostLimit.text);
            Mod.config["Username"] = hostUsername.text;
            Mod.config["Port"] = ushort.Parse(hostPort.text);
            Mod.WriteConfig();
            host.SetActive(false);
            hosting.SetActive(true);
            SetHostingPage(true);
            ISClientSend.List(hostServerName.text, int.Parse(hostLimit.text), hostPassword.text, ushort.Parse(hostPort.text));
            ISClient.wantListed = true;
            ISClient.listed = false;
        }

        public void OnHostingCloseClicked()
        {
            if (ISClient.listed || ISClient.wantListed)
            {
                ISClient.wantListed = false;
                ISClient.listed = false;
                ISClientSend.Unlist();
            }
            else // If were on hosting page but not listed and don't want, means we were hosting directly
            {
                Server.Close();
            }

            hosting.SetActive(false);
            main.SetActive(true);

            SetMainPage(null);
        }

        private void HostEntriesReceived(List<ISEntry> entries)
        {
            if (state == State.Main || state == State.MainWaiting)
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

            if (!ISClient.wantListed)
            {
                ISClientSend.Unlist();
            }
        }

        private void ISClientDisconnected()
        {
            // If got disconnected while in other page than Hosting/Client, we for sure just want to go back to main page
            // because connection to IS dropped unexpectedly, and we will attempt to reconnect
            // If in hosting, if listed, just make sure we set the corersponding vars
            // In hosting/client, we want to remain on that page

            if(state == State.Hosting)
            {
                if (ISClient.listed)
                {
                    ISClient.wantListed = false;
                    ISClient.listed = false;
                }
            }
            else if(state != State.Client)
            {
                main.SetActive(true);
                host.SetActive(false);
                hosting.SetActive(false);
                join.SetActive(false);
                client.SetActive(false);
                SetMainPage(null);

                mainLoadingAnimation.SetActive(false);
                mainInfoText.color = Color.red;
                mainInfoText.text = "Connection to index server failed";
            }
        }

        private void Connected()
        {
            main.SetActive(false);
            host.SetActive(false);
            join.SetActive(false);
            if (ThreadManager.host)
            {
                hosting.SetActive(true);
                client.SetActive(false);
                SetHostingPage(false);
            }
            else // Client
            {
                hosting.SetActive(false);
                client.SetActive(true);
                SetClientPage(false);
            }
        }

        private void Join(int entryID, bool hasPassword, string name)
        {
            joiningEntry = entryID;
            gotEndPoint = false;
            main.SetActive(false);
            join.SetActive(true);
            joinPasswordField.SetActive(hasPassword);
            joinServerName.text = "Server:\n" + name;
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
            TODO: // Check if local endpoint is the same that the IS sees
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

                // Destroy any existent pages
                while (hostingListParent.childCount > 1)
                {
                    Transform otherChild = hostingListParent.GetChild(1);
                    otherChild.parent = null;
                    Destroy(otherChild.gameObject);
                }

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

                // Actually start hosting if not already are
                Mod.OnHostClicked();
            }
        }

        private void PlayerAdded(PlayerManager player)
        {
            if(state == State.Client)
            {
                Transform currentListPage = clientListParent.GetChild(clientListParent.childCount - 1);
                if(currentListPage.childCount == 8)
                {
                    currentListPage = Instantiate(clientPagePrefab, clientListParent).transform;
                    currentListPage.gameObject.SetActive(true);
                    if (clientListPage == clientListParent.childCount - 2)
                    {
                        clientNextButton.SetActive(true);
                    }
                }
                GameObject playerEntry = Instantiate(clientEntryPrefab, currentListPage);
                playerEntry.SetActive(true);
                playerEntry.transform.GetChild(0).GetComponent<Text>().text = player.username;
            }
            else if(state == State.Hosting)
            {
                Transform currentListPage = hostingListParent.GetChild(hostingListParent.childCount - 1);
                if (currentListPage.childCount == 8)
                {
                    currentListPage = Instantiate(hostingPagePrefab, hostingListParent).transform;
                    currentListPage.gameObject.SetActive(true);
                    if (hostingListPage == hostingListParent.childCount - 2)
                    {
                        hostingNextButton.SetActive(true);
                    }
                }
                GameObject playerEntry = Instantiate(hostingEntryPrefab, currentListPage);
                playerEntry.SetActive(true);
                playerEntry.transform.GetChild(0).GetComponent<Text>().text = player.username;
                GameObject kickButtonObject = playerEntry.transform.GetChild(1).gameObject;
                kickButtonObject.SetActive(true);
                int playerID = player.ID;
                kickButtonObject.GetComponent<Button>().onClick.AddListener(() => { KickPlayer(playerID); });
            }
        }

        private void PlayerRemoved(PlayerManager player)
        {
            if(state == State.Client)
            {
                int page = clientListPage;
                SetClientPage(false);
                if(clientListParent.childCount - 1 <= page)
                {
                    page = clientListParent.childCount - 2;
                }

                clientListPage = page;
                clientListParent.GetChild(1).gameObject.SetActive(false);
                clientListParent.GetChild(clientListPage + 1).gameObject.SetActive(true);
                clientPrevButton.SetActive(clientListPage > 0);
                clientNextButton.SetActive(clientListPage < clientListParent.childCount - 2);
            }
            else if(state == State.Hosting)
            {
                int page = hostingListPage;
                SetHostingPage(false);
                if (hostingListParent.childCount - 1 <= page)
                {
                    page = hostingListParent.childCount - 2;
                }

                hostingListPage = page;
                hostingListParent.GetChild(1).gameObject.SetActive(false);
                hostingListParent.GetChild(hostingListPage + 1).gameObject.SetActive(true);
                hostingPrevButton.SetActive(hostingListPage > 0);
                hostingNextButton.SetActive(hostingListPage < hostingListParent.childCount - 2);
            }
        }

        public void SetClientPage(bool waiting)
        {
            if (waiting)
            {
                state = State.ClientWaiting;
                clientLoadingAnimation.SetActive(true);
                clientInfoTextObject.SetActive(true);
                clientListParent.gameObject.SetActive(false);

                if(joiningEntry == -1)
                {
                    clientInfoText.color = Color.red;
                    clientInfoText.text = "Error joining server";
                }
                else if (gotEndPoint)
                {
                    clientInfoText.color = Color.white;
                    clientInfoText.text = "Attempting to join server";
                }
                else
                {
                    clientInfoText.color = Color.white;
                    clientInfoText.text = "Awaiting server confirm";
                    ISClientSend.Join(joiningEntry, joinPassword.text.GetDeterministicHashCode());
                }
            }
            else
            {
                state = State.Client;
                clientLoadingAnimation.SetActive(false);
                clientInfoTextObject.SetActive(false);
                clientListParent.gameObject.SetActive(true);

                // Destroy any existent pages
                while (clientListParent.childCount > 1)
                {
                    Transform otherChild = clientListParent.GetChild(1);
                    otherChild.parent = null;
                    Destroy(otherChild.gameObject);
                }

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
            if (ThreadManager.host)
            {
                Server.clients[ID].Disconnect(2);
            }
        }

        public void OnExitClicked()
        {
            Destroy(gameObject);
        }

        public void OnMainPrevClicked()
        {
            mainListParent.GetChild(mainListPage + 1).gameObject.SetActive(false);

            --mainListPage;

            mainListParent.GetChild(mainListPage + 1).gameObject.SetActive(true);

            if(mainListPage == 0)
            {
                mainPrevButton.SetActive(false);
            }
            mainNextButton.SetActive(true);
        }

        public void OnMainNextClicked()
        {
            mainListParent.GetChild(mainListPage + 1).gameObject.SetActive(false);

            ++mainListPage;

            mainListParent.GetChild(mainListPage + 1).gameObject.SetActive(true);

            if (mainListPage == mainListParent.childCount - 2)
            {
                mainPrevButton.SetActive(false);
            }
            mainPrevButton.SetActive(true);
        }

        public void OnMainRefreshClicked()
        {
            ISClientSend.RequestHostEntries();
        }

        public void OnHostCancelClicked()
        {
            host.SetActive(false);
            main.SetActive(true);
            SetMainPage(null);
        }

        public void OnHostingPrevClicked()
        {
            hostingListParent.GetChild(hostingListPage + 1).gameObject.SetActive(false);

            --hostingListPage;

            hostingListParent.GetChild(hostingListPage + 1).gameObject.SetActive(true);

            if (hostingListPage == 0)
            {
                hostingPrevButton.SetActive(false);
            }
            hostingPrevButton.SetActive(true);
        }

        public void OnHostingNextClicked()
        {
            hostingListParent.GetChild(hostingListPage + 1).gameObject.SetActive(false);

            ++hostingListPage;

            hostingListParent.GetChild(hostingListPage + 1).gameObject.SetActive(true);

            if (hostingListPage == hostingListParent.childCount - 2)
            {
                hostingPrevButton.SetActive(false);
            }
            hostingPrevButton.SetActive(true);
        }

        public void OnJoinConfirmClicked()
        {
            bool failed = false;
            if (joinUsername.text == "")
            {
                failed = true;
                joinUsernameLabel.color = Color.red;
            }
            if (failed)
            {
                return;
            }
            Mod.config["Username"] = joinUsername.text;
            Mod.WriteConfig();
            join.SetActive(false);
            client.SetActive(true);
            SetClientPage(true);
        }

        public void OnJoinCancelClicked()
        {
            join.SetActive(false);
            main.SetActive(true);
            SetMainPage(null);
        }

        public void OnClientDisconnectClicked()
        {
            Client.singleton.Disconnect(true, 0);
            client.SetActive(false);
            main.SetActive(true);
            SetMainPage(null);
        }

        public void OnClientPrevClicked()
        {
            clientListParent.GetChild(clientListPage + 1).gameObject.SetActive(false);

            --clientListPage;

            clientListParent.GetChild(clientListPage + 1).gameObject.SetActive(true);

            if (clientListPage == 0)
            {
                clientPrevButton.SetActive(false);
            }
            clientPrevButton.SetActive(true);
        }

        public void OnClientNextClicked()
        {
            clientListParent.GetChild(clientListPage + 1).gameObject.SetActive(false);

            ++clientListPage;

            clientListParent.GetChild(clientListPage + 1).gameObject.SetActive(true);

            if (clientListPage == clientListParent.childCount - 2)
            {
                clientPrevButton.SetActive(false);
            }
            clientPrevButton.SetActive(true);
        }

        private void OnDestroy()
        {
            ISClient.OnReceiveHostEntries -= HostEntriesReceived;
            ISClient.OnDisconnect -= ISClientDisconnected;
            ISClient.OnListed -= Listed;
            Mod.OnConnection -= Connected;
            GameManager.OnPlayerAdded -= PlayerAdded;
            Mod.OnPlayerRemoved -= PlayerRemoved;

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
