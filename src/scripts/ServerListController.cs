using BepInEx.Bootstrap;
using H3MP.Networking;
using Open.Nat;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Valve.Newtonsoft.Json.Linq;

namespace H3MP.Scripts
{
    public class ServerListController : MonoBehaviour
    {
        public static ServerListController instance;
        public static Dictionary<int, List<string>> modlists = new Dictionary<int, List<string>>();
        public static string minimumVersion = null;
        public static string failedConnectionReason = null;
        private bool awakened;
        private bool skipCloseClicked;
        private bool skipDisconnectClicked;
        private bool directConnection;

        public enum State
        {
            ISSelect, // Selecting / Adding an IS
            MainWaiting, // Waiting for host entries from IS
            Main, // Server list
            Host, // Host settings
            HostingWaiting, // Confirmed settings, waiting for IS to list us
            Hosting, // Hosting and listed on IS
            Join, // Join settings
            ClientWaiting, // Confirmed settings, waiting for IS to confirm password is correct and that our connection was established
            Client, // Client to a host
            Modlist, // In modlist page
        }
        public State state = State.Main;

        // Pages
        public GameObject ISSelect;
        public GameObject ISAdd;
        public GameObject main;
        public GameObject host;
        public GameObject hosting;
        public GameObject join;
        public GameObject client;
        public GameObject modlistPage;

        // ISSelect
        public Transform ISSelectListParent;
        public int ISSelectListPage;
        public GameObject ISSelectPagePrefab;
        public GameObject ISSelectEntryPrefab;
        public GameObject ISSelectPreviousButton;
        public GameObject ISSelectNextButton;

        // ISAdd
        public Text ISAddServerNameLabel;
        public Text ISAddServerNameText;
        public TextField ISAddServerNameField;
        public Text ISAddIPLabel;
        public Text ISAddIPText;
        public TextField ISAddIPField;
        public Text ISAddPortLabel;
        public Text ISAddPortText;
        public TextField ISAddPortField;

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
        public float mainRefreshTimer;
        public Text mainInfoText;

        // Host
        public GameObject hostPage0;
        public GameObject hostPage1;
        public GameObject hostPreviousButton;
        public GameObject hostNextButton;
        public Text hostServerNameLabel;
        public Text hostServerName;
        public TextField hostServerNameField;
        public GameObject hostListToggle;
        public GameObject hostListToggleCheck;
        public Text hostPassword;
        public Text hostLimitLabel;
        public Text hostLimit;
        public TextField hostLimitField;
        public Text hostUsernameLabel;
        public Text hostUsername;
        public TextField hostUsernameField;
        public GameObject hostPortFieldObject;
        public Text hostPortLabel;
        public Text hostPort;
        public TextField hostPortField;
        public GameObject hostPortForwardedToggleCheck;
        public GameObject hostModlistEnforcedToggleCheck;
        public GameObject hostModlistMinimumToggleCheck;
        public GameObject hostModlistUnenforcedToggleCheck;

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
        public GameObject hostingListButtonObject;
        public Button hostingListButton;
        public Text hostingListButtonText;

        // Join
        public Text joinUsernameLabel;
        public Text joinUsername;
        public TextField joinUsernameField;
        public int joiningEntry;
        public int entryModlistEnforcement;
        public List<string> modlist;
        public bool gotEndPoint;
        public GameObject joinPasswordFieldObject;
        public Text joinPassword;
        public GameObject joinIPFieldObject;
        public Text joinIPLabel;
        public Text joinIP;
        public TextField joinIPField;
        public GameObject joinPortFieldObject;
        public Text joinPortLabel;
        public Text joinPort;
        public TextField joinPortField;
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

        // Modlist
        public int modlistPageIndex;
        public Transform modlistParent;
        public GameObject modlistPagePrefab;
        public GameObject modlistEntryPrefab;
        public GameObject modlistPrevButton;
        public GameObject modlistNextButton;

        private void Awake()
        {
            // Check if there already is a server list opened
            if(instance != null)
            {
                instance.transform.position = transform.position;
                instance.transform.rotation = transform.rotation;
                Destroy(gameObject);
                return;
            }
            else
            {
                instance = this;
            }

            // We unload OnDestroy, but only want to do that if we didn't get destroyed above
            awakened = true;

            // Subscribe to relevant events, anything we will need to keep track of to update our UI accordingly
            ISClient.OnReceiveHostEntries += HostEntriesReceived;
            ISClient.OnDisconnect += ISClientDisconnected;
            ISClient.OnListed += Listed;
            Mod.OnConnection += Connected;
            GameManager.OnPlayerAdded += PlayerAdded;
            Mod.OnPlayerRemoved += PlayerRemoved;
            Server.OnServerClose += OnHostingCloseClicked;
            Client.OnDisconnect += OnClientDisconnectClicked;

            // Initialize UI based on connection state
            Init();
        }

        public void Update()
        {
            if(mainRefreshTimer > 0)
            {
                mainRefreshTimer -= Time.deltaTime;
                if(mainRefreshTimer <= 0)
                {
                    mainRefreshButton.SetActive(true);
                }
            }
        }

        public void Init()
        {
            if (Mod.managerObject == null)
            {
                if (ISClient.isConnected)
                {
                    SetMainPage(null);
                }
                else
                {
                    SetISSelectPage();
                }
            }
            else
            {
                ISSelect.SetActive(false);
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

        public void SetISSelectPage()
        {
            if (ISClient.isConnected)
            {
                Mod.LogError("ServerListController.SetISSelectPage called but we were connected to IS:\n" + Environment.StackTrace);
                ISClient.Disconnect(true, 0);
            }

            state = State.ISSelect;

            ISSelectListParent.gameObject.SetActive(true);
            ISSelectPreviousButton.SetActive(false);
            ISSelectNextButton.SetActive(false);
            JArray ISList = null;
            if (Mod.config["ISList"] == null)
            {
                ISList = new JArray();
                Mod.config["ISList"] = ISList;
                JObject defaultISEntry = new JObject();
                defaultISEntry["Name"] = "Official";
                defaultISEntry["IP"] = "h3mp.tommysoucy.vip";
                defaultISEntry["Port"] = 7862;
                ISList.Add(defaultISEntry);
                Mod.WriteConfig();
            }
            else
            {
                ISList = Mod.config["ISList"] as JArray;
            }

            // Destroy any existent pages
            while (ISSelectListParent.childCount > 1)
            {
                Transform otherChild = ISSelectListParent.GetChild(1);
                otherChild.SetParent(null);
                Destroy(otherChild.gameObject);
            }

            // Build new pages
            Transform currentListPage = Instantiate(ISSelectPagePrefab, ISSelectListParent).transform;
            for (int i = 0; i < ISList.Count; ++i)
            {
                GameObject ISEntry = Instantiate(ISSelectEntryPrefab, currentListPage);
                ISEntry.SetActive(true);
                ISEntry.transform.GetChild(0).GetComponent<Text>().text = ISList[i]["Name"].ToString();
                string IP = ISList[i]["IP"].ToString();
                ushort port = (ushort)ISList[i]["Port"];
                ISEntry.GetComponent<Button>().onClick.AddListener(() => { 
                    ISClient.Connect(IP, port);
                    ISSelect.SetActive(false);
                    if (Mod.managerObject == null)
                    {
                        main.SetActive(true);
                        SetMainPage(null);
                    }
                    else
                    {
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
                });
                int index = i;
                ISEntry.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(() => {
                    JArray currentISList = Mod.config["ISList"] as JArray;
                    currentISList.RemoveAt(index);
                    SetISSelectPage();
                });

                // Start a new page every 7 elements
                if (i + 1 % 7 == 0 && i != ISList.Count - 1)
                {
                    currentListPage = Instantiate(ISSelectPagePrefab, ISSelectListParent).transform;
                    ISSelectNextButton.SetActive(true);
                }
            }
            ISSelectListPage = Mathf.Min(ISSelectListPage, ISSelectListParent.childCount - 2);
            ISSelectListParent.GetChild(ISSelectListPage + 1).gameObject.SetActive(true);
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
                // else, don't yet have welcome, we're going to call a request when we do
            }
            else
            {
                state = State.Main;

                mainLoadingAnimation.SetActive(false);
                mainListParent.gameObject.SetActive(true);
                mainHostButton.SetActive(true);
                mainPrevButton.SetActive(false);
                mainNextButton.SetActive(false);
                mainRefreshButton.SetActive(false);
                mainRefreshTimer = 10;
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
                    otherChild.SetParent(null);
                    Destroy(otherChild.gameObject);
                }

                // Build new pages
                Transform currentListPage = Instantiate(mainPagePrefab, mainListParent).transform;
                for (int i = 0; i < entries.Count; ++i)
                {
                    GameObject hostEntry = Instantiate(mainEntryPrefab, currentListPage);
                    hostEntry.SetActive(true);
                    hostEntry.transform.GetChild(0).GetComponent<Text>().text = entries[i].name;
                    hostEntry.transform.GetChild(1).GetComponent<Text>().text = entries[i].playerCount + "/" + entries[i].limit;
                    hostEntry.transform.GetChild(2).gameObject.SetActive(entries[i].locked);
                    int entryID = entries[i].ID;
                    bool hasPassword = entries[i].locked;
                    int modlistEnforcement = entries[i].modlistEnforcement;
                    string serverName = entries[i].name;
                    hostEntry.GetComponent<Button>().onClick.AddListener(() => { Join(false, entryID, hasPassword, modlistEnforcement, serverName); });

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
            ISSelect.SetActive(false);
            main.SetActive(false);
            host.SetActive(true);

            hostServerNameLabel.color = Color.white;
            if(Mod.config["ServerName"] != null)
            {
                hostServerName.text = Mod.config["ServerName"].ToString();
                hostServerNameField.clearButton.SetActive(true);
            }
            hostLimitLabel.color = Color.white;
            if(Mod.config["MaxClientCount"] != null)
            {
                hostLimit.text = Mod.config["MaxClientCount"].ToString();
                hostLimitField.clearButton.SetActive(true);
            }
            hostUsernameLabel.color = Color.white;
            if(Mod.config["Username"] != null)
            {
                hostUsername.text = Mod.config["Username"].ToString();
                hostUsernameField.clearButton.SetActive(true);
            }
            hostPortLabel.color = Color.white;
            if(Mod.config["Port"] != null)
            {
                hostPort.text = Mod.config["Port"].ToString();
                hostPortField.clearButton.SetActive(true);
            }
            if (ISClient.isConnected)
            {
                hostListToggle.SetActive(true);
                if (Mod.config["Public"] != null)
                {
                    hostListToggleCheck.SetActive((bool)Mod.config["Public"]);
                }
            }
            else
            {
                hostListToggle.SetActive(false);
                hostListToggleCheck.SetActive(false);
            }
            if(Mod.config["UPnP"] != null)
            {
                hostPortForwardedToggleCheck.SetActive((bool)Mod.config["UPnP"]);
            }
            if(Mod.config["ModlistEnforcement"] != null)
            {
                int enforcement = (int)Mod.config["ModlistEnforcement"];
                hostModlistEnforcedToggleCheck.SetActive(enforcement == 0);
                hostModlistMinimumToggleCheck.SetActive(enforcement == 1);
                hostModlistUnenforcedToggleCheck.SetActive(enforcement == 2);
            }
        }

        public void OnJoinClicked()
        {
            Join(true, -1, true, 2, "Direct connection");
            state = State.Join;

            // IP and Port label colors are set in Join() only if necessary
            if(Mod.config["IP"] != null)
            {
                joinIP.text = Mod.config["IP"].ToString();
                joinIPField.clearButton.SetActive(true);
            }
            joinUsernameLabel.color = Color.white;
            if(Mod.config["Username"] != null)
            {
                joinUsername.text = Mod.config["Username"].ToString();
                joinUsernameField.clearButton.SetActive(true);
            }
            if(Mod.config["Port"] != null)
            {
                joinPort.text = Mod.config["Port"].ToString();
                joinPortField.clearButton.SetActive(true);
            }
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
            Mod.config["Public"] = hostListToggleCheck.activeSelf;
            Mod.config["UPnP"] = hostPortForwardedToggleCheck.activeSelf;
            int modlistEnforcement = 2;
            if (hostModlistEnforcedToggleCheck.activeSelf)
            {
                Mod.config["ModlistEnforcement"] = 0;
                modlistEnforcement = 0;
            }
            else if (hostModlistMinimumToggleCheck.activeSelf)
            {
                Mod.config["ModlistEnforcement"] = 1;
                modlistEnforcement = 1;
            }
            else if (hostModlistUnenforcedToggleCheck.activeSelf)
            {
                Mod.config["ModlistEnforcement"] = 2;
                modlistEnforcement = 2;
            }
            Mod.WriteConfig();
            host.SetActive(false);
            hosting.SetActive(true);
            directConnection = !hostListToggleCheck.activeSelf;
            SetHostingPage(Mod.managerObject == null);
            if (!directConnection)
            {
                ISClientSend.List(hostServerName.text, int.Parse(hostLimit.text), hostPassword.text, ushort.Parse(hostPort.text), modlistEnforcement);
                ISClient.wantListed = true;
            }
            ISClient.listed = false;
        }

        public void OnHostingCloseClicked()
        {
            if (skipCloseClicked)
            {
                return;
            }

            // Make sure we are not listed
            if (ISClient.isConnected)
            {
                if (ISClient.listed || ISClient.wantListed)
                {
                    ISClient.wantListed = false;
                    ISClient.listed = false;
                    ISClientSend.Unlist();
                }
            }

            // Close server if necessary
            if(Mod.managerObject != null && ThreadManager.host)
            {
                skipCloseClicked = true;
                Server.Close();
                skipCloseClicked = false;
            }

            hosting.SetActive(false);
            if (ISClient.isConnected)
            {
                main.SetActive(true);
                SetMainPage(null);
            }
            else
            {
                ISSelect.SetActive(true);
                SetISSelectPage();
            }
        }

        public void OnHostingListClicked()
        {
            if (ISClient.isConnected)
            {
                if (!ISClient.listed && !ISClient.wantListed)
                {
                    state = State.Host;
                    hosting.SetActive(false);
                    host.SetActive(true);
                    directConnection = false;
                    hostListToggleCheck.SetActive(true);

                    hostServerNameLabel.color = Color.white;
                    if (Mod.config["ServerName"] != null)
                    {
                        hostServerName.text = Mod.config["ServerName"].ToString();
                        hostServerNameField.clearButton.SetActive(true);
                    }
                    hostLimitLabel.color = Color.white;
                    if (Mod.config["MaxClientCount"] != null)
                    {
                        hostLimit.text = Mod.config["MaxClientCount"].ToString();
                        hostLimitField.clearButton.SetActive(true);
                    }
                    hostUsernameLabel.color = Color.white;
                    if (Mod.config["Username"] != null)
                    {
                        hostUsername.text = Mod.config["Username"].ToString();
                        hostUsernameField.clearButton.SetActive(true);
                    }
                    hostPortLabel.color = Color.white;
                    if (Mod.config["Port"] != null)
                    {
                        hostPort.text = Mod.config["Port"].ToString();
                        hostPortField.clearButton.SetActive(true);
                    }
                }
                else if (ISClient.listed)
                {
                    ISClientSend.Unlist();
                    ISClient.wantListed = false;
                }
            }
            else
            {
                hosting.SetActive(false);
                ISSelect.SetActive(true);
                SetISSelectPage();
            }
        }

        public void OnHostPortForwardedClicked()
        {
            hostPortForwardedToggleCheck.SetActive(!hostPortForwardedToggleCheck.activeSelf);
        }

        public void OnHostModlistEnforcedClicked()
        {
            hostModlistEnforcedToggleCheck.SetActive(true);
            hostModlistMinimumToggleCheck.SetActive(false);
            hostModlistUnenforcedToggleCheck.SetActive(false);
        }

        public void OnHostModlistMinimumClicked()
        {
            hostModlistEnforcedToggleCheck.SetActive(false);
            hostModlistMinimumToggleCheck.SetActive(true);
            hostModlistUnenforcedToggleCheck.SetActive(false);
        }

        public void OnHostModlistUnenforcedClicked()
        {
            hostModlistEnforcedToggleCheck.SetActive(false);
            hostModlistMinimumToggleCheck.SetActive(false);
            hostModlistUnenforcedToggleCheck.SetActive(true);
        }

        public void OnHostListClicked()
        {
            hostListToggleCheck.SetActive(!hostListToggleCheck.activeSelf);
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
            // Could already be at hosting if we were already hosting and manually listed
            // in which case we still want to call SetHostingPage to refresh the listed button
            if (state == State.HostingWaiting || state == State.Hosting)
            {
                ISClient.listed = true;
                SetHostingPage(false);
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
            // If in hosting, if listed, just make sure we set the corresponding vars
            // In hosting/client, we want to remain on that page

            if(state == State.Hosting)
            {
                ISClient.wantListed = false;
                ISClient.listed = false;
                hostingListButtonText.color = Color.yellow;
                hostingListButtonText.text = "Select IS";
                hostingListButton.interactable = true;
            }
            else if(state != State.Client && state != State.ISSelect)
            {
                main.SetActive(true);
                host.SetActive(false);
                hosting.SetActive(false);
                join.SetActive(false);
                client.SetActive(false);
                SetMainPage(null);

                mainLoadingAnimation.SetActive(false);
                mainInfoText.color = Color.red;
                if(failedConnectionReason == null)
                {
                    mainInfoText.text = "Connection to index server failed";
                }
                else
                {
                    mainInfoText.text = failedConnectionReason;
                    failedConnectionReason = null;
                }
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

        private void Join(bool direct, int entryID, bool hasPassword, int modlistEnforcement, string name)
        {
            directConnection = direct;
            joiningEntry = entryID;
            entryModlistEnforcement = modlistEnforcement;
            gotEndPoint = false;
            ISSelect.SetActive(false);
            main.SetActive(false);
            join.SetActive(true);
            joinPasswordFieldObject.SetActive(hasPassword);
            joinServerName.text = "Server:\n" + name;

            if (direct)
            {
                joinIPLabel.color = Color.white;
                joinIPFieldObject.SetActive(true);
                joinPortLabel.color = Color.white;
                joinPortFieldObject.SetActive(true);
            }
            else
            {
                joinIPFieldObject.SetActive(false);
                joinPortFieldObject.SetActive(false);
            }
        }

        private void SetHostingPage(bool waiting)
        {
            if (waiting && !directConnection)
            {
                state = State.HostingWaiting;
                hostingLoadingAnimation.SetActive(true);
                hostingInfoTextObject.SetActive(true);
                hostingListParent.gameObject.SetActive(false);
                hostingListButtonObject.SetActive(false);
                hostingInfoText.text = "Awaiting server confirm";
            }
            else
            {
                state = State.Hosting;
                hostingLoadingAnimation.SetActive(false);
                hostingInfoTextObject.SetActive(false);
                hostingListParent.gameObject.SetActive(true);
                hostingListButtonObject.SetActive(true);
                if (ISClient.listed)
                {
                    hostingListButtonText.color = Color.green;
                    hostingListButtonText.text = "Public";
                    hostingListButton.interactable = true;
                }
                else
                {
                    if (ISClient.wantListed)
                    {
                        hostingListButtonText.color = Color.cyan;
                        hostingListButtonText.text = "Listing";
                        hostingListButton.interactable = false;
                    }
                    else
                    {
                        hostingListButtonText.color = Color.yellow;
                        hostingListButtonText.text = "Private";
                        hostingListButton.interactable = true;
                    }
                }

                // Destroy any existent pages
                while (hostingListParent.childCount > 1)
                {
                    Transform otherChild = hostingListParent.GetChild(1);
                    otherChild.SetParent(null);
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

                if(Mod.managerObject == null)
                {
                    // Actually start hosting if not already are
                    if (hostPortForwardedToggleCheck.activeSelf)
                    {
                        // Must first make sure we forward the port through UPnP if necessary
                        try
                        {
                            CreateMappings();
                        }
                        catch (Exception ex)
                        {
                            Mod.LogError("Failed to create UPnP mappings. Ensure UPnP is supported and enabled in your router: " + ex.Message + "\n" + ex.StackTrace);
                        }
                    }
                    Mod.OnHostClicked();
                }
            }
        }

        private void CreateMappings()
        {
            int port = int.Parse(Mod.config["Port"].ToString());
            Mod.LogInfo("Creating UPnP mappings for "+port);
            NatDiscoverer discoverer = new NatDiscoverer();
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(10000);
            System.Threading.Tasks.Task<NatDevice> deviceTask = discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);
            deviceTask.Wait();
            NatDevice device = deviceTask.Result;

            // Mappings with lifetime 0 are permanent
            System.Threading.Tasks.Task mapTask = device.CreatePortMapAsync(new Mapping(Protocol.Tcp, port, port, 0, "H3MP - UPnP TCP mapping"));
            mapTask.Wait();
            mapTask = device.CreatePortMapAsync(new Mapping(Protocol.Udp, port, port, 0, "H3MP - UPnP UDP mapping"));
            mapTask.Wait();
            System.Threading.Tasks.Task<IPAddress> IPTask = device.GetExternalIPAsync();
            IPTask.Wait();
            IPAddress IP = IPTask.Result;
            Mod.LogInfo("Mappings created on device: "+IP);
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

                if (directConnection)
                {
                    clientInfoText.color = Color.cyan;
                    clientInfoText.text = "Attempting to join server";
                    Mod.OnConnectClicked(null);
                }
                else
                {
                    if (joiningEntry == -1)
                    {
                        clientInfoText.color = Color.red;
                        clientInfoText.text = "Error joining server";
                    }
                    else if (gotEndPoint)
                    {
                        clientInfoText.color = Color.cyan;
                        clientInfoText.text = "Attempting to join server";
                    }
                    else
                    {
                        if (entryModlistEnforcement != 2)
                        {
                            if (modlist == null)
                            {
                                clientInfoText.color = Color.white;
                                clientInfoText.text = "Getting mod list";
                                ISClientSend.RequestModlist(joiningEntry, Mod.GetSHA256Hash(joinPassword.text));
                            }
                            else
                            {
                                List<string> missing = new List<string>();
                                List<string> surplus = new List<string>(Chainloader.PluginInfos.Keys);
                                for(int i=0; i < modlist.Count; ++i)
                                {
                                    if (Chainloader.PluginInfos.ContainsKey(modlist[i]))
                                    {
                                        surplus.Remove(modlist[i]);
                                    }
                                    else
                                    {
                                        missing.Add(modlist[i]);
                                    }
                                }

                                if(missing.Count != 0 || (entryModlistEnforcement == 0 && surplus.Count != 0))
                                {
                                    joiningEntry = -1;
                                    client.SetActive(false);
                                    modlistPage.SetActive(true);
                                    SetModlistPage(missing, surplus);
                                }
                                else
                                {
                                    clientInfoText.color = Color.white;
                                    clientInfoText.text = "Awaiting server confirm";
                                    ISClientSend.Join(joiningEntry, Mod.GetSHA256Hash(joinPassword.text));
                                }
                            }
                        }
                        else
                        {
                            clientInfoText.color = Color.white;
                            clientInfoText.text = "Awaiting server confirm";
                            ISClientSend.Join(joiningEntry, Mod.GetSHA256Hash(joinPassword.text));
                        }
                    }
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
                    otherChild.SetParent(null);
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

        public void SetModlistPage(List<string> missing, List<string> surplus)
        {
            state = State.Modlist;

            // Destroy any existent pages
            while (modlistParent.childCount > 1)
            {
                Transform otherChild = modlistParent.GetChild(1);
                otherChild.SetParent(null);
                Destroy(otherChild.gameObject);
            }

            // Build new pages
            Transform currentListPage = Instantiate(modlistPagePrefab, modlistParent).transform;
            currentListPage.gameObject.SetActive(true);
            modlistPageIndex = 0;
            for (int i = 0; i < missing.Count; ++i)
            {
                // Start a new page if necessary
                if (currentListPage.childCount == 8)
                {
                    currentListPage = Instantiate(modlistPagePrefab, modlistParent).transform;
                    clientNextButton.SetActive(true);
                }

                GameObject modEntry = Instantiate(modlistEntryPrefab, currentListPage);
                modEntry.SetActive(true);
                Text text = modEntry.transform.GetChild(0).GetComponent<Text>();
                text.text = missing[i];
                text.color = Color.red;

            }
            if (entryModlistEnforcement == 1)
            {
                for (int i = 0; i < surplus.Count; ++i)
                {
                    // Start a new page if necessary
                    if (currentListPage.childCount == 8)
                    {
                        currentListPage = Instantiate(modlistPagePrefab, modlistParent).transform;
                        clientNextButton.SetActive(true);
                    }

                    GameObject modEntry = Instantiate(modlistEntryPrefab, currentListPage);
                    modEntry.SetActive(true);
                    Text text = modEntry.transform.GetChild(0).GetComponent<Text>();
                    text.text = surplus[i];
                    text.color = Color.yellow;
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

        public void OnISSelectAddClicked()
        {
            ISSelect.SetActive(false);
            ISAdd.SetActive(true);

            ISAddIPLabel.color = Color.white;
            ISAddIPText.text = "";
            ISAddIPField.clearButton.SetActive(false);
            ISAddPortLabel.color = Color.white;
            ISAddPortText.text = "";
            ISAddPortField.clearButton.SetActive(false);
            ISAddServerNameLabel.color = Color.white;
            ISAddServerNameText.text = "";
            ISAddServerNameField.clearButton.SetActive(false);
        }

        public void OnISSelectPreviousClicked()
        {
            ISSelectListParent.GetChild(ISSelectListPage + 1).gameObject.SetActive(false);

            --ISSelectListPage;

            ISSelectListParent.GetChild(ISSelectListPage + 1).gameObject.SetActive(true);

            if (ISSelectListPage == 0)
            {
                ISSelectPreviousButton.SetActive(false);
            }
            ISSelectNextButton.SetActive(true);
        }

        public void OnISSelectNextClicked()
        {
            ISSelectListParent.GetChild(ISSelectListPage + 1).gameObject.SetActive(false);

            ++ISSelectListPage;

            ISSelectListParent.GetChild(ISSelectListPage + 1).gameObject.SetActive(true);

            if (ISSelectListPage == ISSelectListParent.childCount - 2)
            {
                ISSelectNextButton.SetActive(false);
            }
            ISSelectPreviousButton.SetActive(true);
        }

        public void OnISAddBackClicked()
        {
            ISSelect.SetActive(true);
            ISAdd.SetActive(false);

            SetISSelectPage();
        }

        public void OnISAddConfirmClicked()
        {
            bool failed = false;
            if (ISAddServerNameText.text == "")
            {
                failed = true;
                ISAddServerNameLabel.color = Color.red;
            }
            if (ISAddIPText.text == "")
            {
                failed = true;
                ISAddIPLabel.color = Color.red;
            }
            else
            {
                try
                {
                    Dns.GetHostAddresses(ISAddIPText.text);
                }
                catch (Exception)
                {
                    failed = true;
                    ISAddIPLabel.color = Color.red;
                }
            }
            ushort parsedPort = 0;
            if (ISAddPortText.text == "" || !ushort.TryParse(ISAddPortText.text, out parsedPort))
            {
                failed = true;
                ISAddPortLabel.color = Color.red;
            }
            if (failed)
            {
                return;
            }

            JObject newISEntry = new JObject();
            newISEntry["Name"] = ISAddServerNameText.text;
            newISEntry["IP"] = ISAddIPText.text;
            newISEntry["Port"] = parsedPort;
            (Mod.config["ISList"] as JArray).Add(newISEntry);
            Mod.WriteConfig();

            ISSelect.SetActive(true);
            ISAdd.SetActive(false);
            SetISSelectPage();
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
                mainNextButton.SetActive(false);
            }
            mainPrevButton.SetActive(true);
        }

        public void OnMainRefreshClicked()
        {
            ISClientSend.RequestHostEntries();
            mainRefreshButton.SetActive(false);
            mainRefreshTimer = 10;
        }

        public void OnMainBackClicked()
        {
            ISClient.Disconnect(true, 0);

            main.SetActive(false);
            ISSelect.SetActive(true);
            SetISSelectPage();
        }

        public void OnHostNextClicked()
        {
            hostPage0.SetActive(false);
            hostPage1.SetActive(true);
            hostPreviousButton.SetActive(true);
            hostNextButton.SetActive(false);
        }

        public void OnHostPreviousClicked()
        {
            hostPage0.SetActive(true);
            hostPage1.SetActive(false);
            hostPreviousButton.SetActive(false);
            hostNextButton.SetActive(true);
        }

        public void OnHostCancelClicked()
        {
            host.SetActive(false);
            if (ISClient.isConnected)
            {
                main.SetActive(true);
                SetMainPage(null);
            }
            else
            {
                ISSelect.SetActive(true);
                SetISSelectPage();
            }
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
            hostingNextButton.SetActive(true);
        }

        public void OnHostingNextClicked()
        {
            hostingListParent.GetChild(hostingListPage + 1).gameObject.SetActive(false);

            ++hostingListPage;

            hostingListParent.GetChild(hostingListPage + 1).gameObject.SetActive(true);

            if (hostingListPage == hostingListParent.childCount - 2)
            {
                hostingNextButton.SetActive(false);
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
            if (directConnection)
            {
                if (joinIP.text == "")
                {
                    failed = true;
                    joinIPLabel.color = Color.red;
                }
                if (joinPort.text == "" || !ushort.TryParse(joinPort.text, out ushort parsedPort))
                {
                    failed = true;
                    joinPortLabel.color = Color.red;
                }
            }
            if (failed)
            {
                return;
            }
            Mod.config["Username"] = joinUsername.text;
            if (directConnection)
            {
                Mod.config["IP"] = joinIP.text;
                Mod.config["Port"] = ushort.Parse(joinPort.text);
            }
            Mod.WriteConfig();
            join.SetActive(false);
            client.SetActive(true);
            modlists.TryGetValue(joiningEntry, out modlist);
            SetClientPage(true);
        }

        public void OnJoinCancelClicked()
        {
            join.SetActive(false);
            if (ISClient.isConnected)
            {
                main.SetActive(true);
                SetMainPage(null);
            }
            else
            {
                ISSelect.SetActive(true);
                SetISSelectPage();
            }
        }

        public void OnClientDisconnectClicked()
        {
            if (skipDisconnectClicked)
            {
                return;
            }

            if (Mod.managerObject != null && !ThreadManager.host && Client.singleton.isConnected)
            {
                skipDisconnectClicked = true;
                Client.singleton.Disconnect(true, 0);
                skipDisconnectClicked = false;
            }
            client.SetActive(false);
            if (ISClient.isConnected)
            {
                main.SetActive(true);
                SetMainPage(null);
            }
            else
            {
                ISSelect.SetActive(true);
                SetISSelectPage();
            }
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
            clientNextButton.SetActive(true);
        }

        public void OnClientNextClicked()
        {
            clientListParent.GetChild(clientListPage + 1).gameObject.SetActive(false);

            ++clientListPage;

            clientListParent.GetChild(clientListPage + 1).gameObject.SetActive(true);

            if (clientListPage == clientListParent.childCount - 2)
            {
                clientNextButton.SetActive(false);
            }
            clientPrevButton.SetActive(true);
        }

        public void OnModlistPrevClicked()
        {
            modlistParent.GetChild(modlistPageIndex + 1).gameObject.SetActive(false);

            --modlistPageIndex;

            modlistParent.GetChild(modlistPageIndex + 1).gameObject.SetActive(true);

            if (modlistPageIndex == 0)
            {
                modlistPrevButton.SetActive(false);
            }
            modlistNextButton.SetActive(true);
        }

        public void OnModlistNextClicked()
        {
            modlistParent.GetChild(modlistPageIndex + 1).gameObject.SetActive(false);

            ++modlistPageIndex;

            modlistParent.GetChild(modlistPageIndex + 1).gameObject.SetActive(true);

            if (modlistPageIndex == modlistParent.childCount - 2)
            {
                modlistNextButton.SetActive(false);
            }
            modlistPrevButton.SetActive(true);
        }

        public void OnModlistBackClicked()
        {
            modlistPage.SetActive(false);
            main.SetActive(true);
            SetMainPage(null);
        }

        private void OnDestroy()
        {
            if (!awakened)
            {
                return;
            }

            ISClient.OnReceiveHostEntries -= HostEntriesReceived;
            ISClient.OnDisconnect -= ISClientDisconnected;
            ISClient.OnListed -= Listed;
            Mod.OnConnection -= Connected;
            GameManager.OnPlayerAdded -= PlayerAdded;
            Mod.OnPlayerRemoved -= PlayerRemoved;
            Server.OnServerClose -= OnHostingCloseClicked;
            Client.OnDisconnect -= OnClientDisconnectClicked;

            if(ISClient.isConnected)
            {
                if (ISClient.gotWelcome)
                {
                    if(Mod.managerObject == null)
                    {
                        // Not connected directly, can disconnect from IS
                        ISClient.Disconnect(true, 0);
                    }
                    else // Connected directly
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
