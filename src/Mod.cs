using BepInEx;
using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using H3MP.Scripts;
using H3MP.Tracking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Valve.Newtonsoft.Json.Linq;
using Valve.VR;

namespace H3MP
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    [BepInDependency("stratum", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("nrgill28.Sodalite", BepInDependency.DependencyFlags.SoftDependency)] // Has WristMenu awake patch, should not interfere
    [BepInDependency("h3vr.otherloader", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("devyndamonster-OtherLoader", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("h3vr.cityrobo.prefab_replacer", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("dll.wfiost.h3vrutilities", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("dll.wfiost.h3vrutilitieslib", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("dll.wfiost.h3vrutilitieslib.vehicles", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("h3vr.cityrobo.OpenScripts", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("h3vr.cityrobo.thermalvision", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("h3vr.OpenScripts2", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("h3vr.andrew_ftw.afcl", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("h3vr.andrew_ftw.bepinexshit", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("nrgill28.Atlas", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("deli", BepInDependency.DependencyFlags.SoftDependency)]
    public class Mod : BaseUnityPlugin
    {
        // BepinEx
        public const string pluginGuid = "VIP.TommySoucy.H3MP";
        public const string pluginName = "H3MP";
        public const string pluginVersion = "1.8.0";

        // Assets
        public static JObject config;
        public static GameObject TNHMenuPrefab;
        public static GameObject TNHStartEquipButtonPrefab;
        public static GameObject playerPrefab;
        public static GameObject keyboardPrefab;
        public static GameObject serverListPrefab;
        public static Material reticleFriendlyContactArrowMat;
        public static Material reticleFriendlyContactIconMat;
        public static Dictionary<string, string> sosigWearableMap;
        public static string H3MPPath;
        public static GameObject glassPrefab;
        public static GameObject glassPFXPrefab;
        public static MatDef glassMatDef;
        public static AudioEvent glassShotEvent;
        public static AudioEvent glassThudHeadEvent;
        public static AudioEvent glassThudTailEvent;
        public static AudioEvent glassTotalMediumEvent;
        public static AudioEvent glassGroundShatterEvent;
        public static Dictionary<FVRSoundEnvironment, AudioEvent> distantShotSets;

        // Menu refs
        public static Text mainStatusText;
        public static Text statusLocationText;
        public static Text statusPlayerCountText;
        public GameObject hostButton;
        public GameObject connectButton;
        public GameObject joinButton;

        // TNH Menu refs
        public static GameObject TNHMenu;
        public static GameObject[] TNHMenuPages; // Main, Host, Join_Options, Join_Instance, Instance, RequestHost_Options, RequestHost_Waiting
        public static Text TNHStatusText;
        public static GameObject TNHInstanceList;
        public static GameObject TNHInstancePrefab;
        public static GameObject TNHInstanceListScrollUpArrow;
        public static GameObject TNHInstanceListScrollDownArrow;
        public static Scrollbar TNHInstanceListScrollBar;
        public static GameObject TNHHostButton;
        public static GameObject TNHJoinButton;
        public static GameObject TNHLPJCheck;
        public static GameObject TNHHostOnDeathSpectateRadio;
        public static GameObject TNHHostOnDeathLeaveRadio;
        public static GameObject TNHHostConfirmButton;
        public static GameObject TNHHostCancelButton;
        public static GameObject TNHJoinCancelButton;
        public static GameObject TNHJoinInstanceCancelButton;
        public static GameObject TNHJoinOptionsCancelButton;
        public static GameObject TNHJoinOnDeathSpectateRadio;
        public static GameObject TNHJoinOnDeathLeaveRadio;
        public static GameObject TNHJoinConfirmButton;
        public static GameObject TNHPlayerList;
        public static GameObject TNHPlayerPrefab;
        public static GameObject TNHPlayerListScrollUpArrow;
        public static GameObject TNHPlayerListScrollDownArrow;
        public static Scrollbar TNHPlayerListScrollBar;
        public static GameObject TNHLPJCheckMark;
        public static GameObject TNHHostOnDeathSpectateCheckMark;
        public static GameObject TNHHostOnDeathLeaveCheckMark;
        public static GameObject TNHJoinOnDeathSpectateCheckMark;
        public static GameObject TNHJoinOnDeathLeaveCheckMark;
        public static GameObject TNHRequestHostButton;
        public static GameObject TNHRequestHostConfirmButton;
        public static GameObject TNHRequestHostCancelButton;
        public static GameObject TNHRequestHostWaitingCancelButton;
        public static GameObject TNHRequestHostOnDeathSpectateRadio;
        public static GameObject TNHRequestHostOnDeathLeaveRadio;
        public static GameObject TNHRequestHostOnDeathSpectateCheckMark;
        public static GameObject TNHRequestHostOnDeathLeaveCheckMark;
        public static Text TNHInstanceTitle;

        // Live
        public static Mod modInstance;
        public static GameObject managerObject;
        public static int skipNextFires = 0;
        public static int skipAllInstantiates = 0;
        public static AudioEvent sosigFootstepAudioEvent;
        public static bool TNHMenuLPJ;
        public static bool TNHOnDeathSpectate; // If false, leave
        public static bool TNHRequestHostOnDeathSpectate;
        public static bool TNHSpectating;
        public static bool setLatestInstance; // Whether to set instance screen according to new instance index when we receive server response
        public static TNHInstance currentTNHInstance;
        public static bool currentlyPlayingTNH;
        public static Dictionary<int, GameObject> joinTNHInstances;
        public static Dictionary<int, GameObject> currentTNHInstancePlayers;
        public static TNH_UIManager currentTNHUIManager;
        public static SceneLoader currentTNHSceneLoader;
        public static bool waitingForTNHHost;
        public static int TNHHostedInstance;
        public static GameObject TNHStartEquipButton;
        public static Dictionary<Type, List<Type>> trackedObjectTypes;
        public static Dictionary<string, Type> trackedObjectTypesByName;
        public delegate void CustomPacketHandler(int clientID, Packet packet);
        public static CustomPacketHandler[] customPacketHandlers = new CustomPacketHandler[10];
        public static List<int> availableCustomPacketIndices = new List<int>() { 0,1,2,3,4,5,6,7,8,9 };
        public static Dictionary<string, int> registeredCustomPacketIDs = new Dictionary<string, int>();
        public static bool spectatorHostWaitingForTNHInstance;
        public static bool spectatorHostWaitingForTNHSetup;
        public static bool waitingForTNHGameStart;
        public static Scene DDOLScene;

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the CustomPacketHandlerReceived event
        /// </summary>
        /// <param name="ID">The identifier that was used for the new packet ID</param>
        /// <param name="index">The new packet ID</param>
        public delegate void CustomPacketHandlerReceivedDelegate(string ID, int index);

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when a new custom packet is added to the network
        /// </summary>
        public static event CustomPacketHandlerReceivedDelegate CustomPacketHandlerReceived;

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the GenericCustomPacketReceived event
        /// </summary>
        /// <param name="clientID">The index of the client this packet comes from</param>
        /// <param name="ID">The packet's identifier</param>
        /// <param name="packet">The packet</param>
        public delegate void GenericCustomPacketReceivedDelegate(int clientID, string ID, Packet packet);

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when a new unregistered/generic custom packet is received
        /// </summary>
        public static event GenericCustomPacketReceivedDelegate GenericCustomPacketReceived;

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnGetBestPotentialObjectHost event
        /// </summary>
        /// <param name="currentController">The object's current controller (If applicable). -1 means to find the best host in current context without restrictions.</param>
        /// <param name="forUs">Whether the call was made to find new host for something controlled by another player</param>
        /// <param name="hasWhiteList">Whether a whitelist was included</param>
        /// <param name="whiteList">The whitelist. Contains the IDs of only the players who can possibly take control.</param>
        /// <param name="sceneOverride">The override for which scene we want to find best host in. null will use current scene.</param>
        /// <param name="instanceOverride">The override for which instance we want to find best host in. -1 will use current instance.</param>
        /// <param name="bestPotentialObjectHost">Custom override for the ID of the best host</param>
        public delegate void OnGetBestPotentialObjectHostDelegate(int currentController, bool forUs, bool hasWhiteList , List<int> whiteList, string sceneOverride, int instanceOverride, ref int bestPotentialObjectHost);

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when we try to get the best potential host for an object or context
        /// </summary>
        public static event OnGetBestPotentialObjectHostDelegate OnGetBestPotentialObjectHost;

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnRemovePlayerFromSpecificLists event
        /// </summary>
        /// <param name="player">The player we are getting rid of</param>
        public delegate void OnRemovePlayerFromSpecificListsDelegate(PlayerManager player);

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when we remove a player from the network
        /// </summary>
        public static event OnRemovePlayerFromSpecificListsDelegate OnRemovePlayerFromSpecificLists;

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnSpectatorHostReceived event
        /// </summary>
        /// <param name="confirmed">Custom override for whether we are going to use this host or not</param>
        public delegate void OnSpectatorHostReceivedDelegate(int host, ref bool confirmed);

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when we receive a host that was assigned to us
        /// </summary>
        public static event OnSpectatorHostReceivedDelegate OnSpectatorHostReceived;

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnSpectatorHostGiveUp event
        /// </summary>
        public delegate void OnSpectatorHostGiveUpDelegate();
        
        /// <summary>
        /// CUSTOMIZATION
        /// Event called when we receive order to give up current spectator host
        /// </summary>
        public static event OnSpectatorHostGiveUpDelegate OnSpectatorHostGiveUp;

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnInstantiationTrack event
        /// </summary>
        /// <param name="gameObject">The GameObject we are about to attempt tracking</param>
        public delegate void OnInstantiationTrackDelegate(GameObject gameObject);

        /// <summary>
        /// CUSTOMIZATION
        /// Event called right before we track an object upon instantiation
        /// </summary>
        public static event OnInstantiationTrackDelegate OnInstantiationTrack;

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnConnection event
        /// </summary>
        public delegate void OnConnectionDelegate();

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when H3MP connection/hosting starts
        /// </summary>
        public static event OnConnectionDelegate OnConnection;

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnPlayerRemoved event
        /// </summary>
        /// <param name="player">The PlayerManager of the player that was removed</param>
        public delegate void OnPlayerRemovedDelegate(PlayerManager player);

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when a player is removed from GameManager.Players dictionary
        /// </summary>
        public static event OnPlayerRemovedDelegate OnPlayerRemoved;

        // Debug
        public static bool waitingForDebugCode;
        public static string debugCode;
        public static Vector3 TNHSpawnPoint;
        public static bool nullDriverControls;
        public static Vector3 nullDriverMovement = Vector3.zero;
        public static float defaultNullDriverMovementMultiplier = 0.2f;
        public static float defaultNullDriverRotationMultiplier = 0.2f;
        public static float nullDriverVerticalRot = 0;
        public static float nullDriverHorzontalRot = 0;
        public static int testCustomPacketID;

        private void Start()
        {
            Logger.LogInfo("H3MP Started");

            modInstance = this;

            Init();
        }

        public static void Reset()
        {
            Mod.LogInfo("Mod reset called", false);
            skipNextFires = 0;
            skipAllInstantiates = 0;
            TNHMenuLPJ = true;
            TNHOnDeathSpectate = true;
            TNHSpectating = false;
            if (GM.CurrentPlayerBody != null && GM.CurrentPlayerBody.RightHand != null && GM.CurrentPlayerBody.LeftHand != null)
            {
                GM.CurrentPlayerBody.EnableHands();
            }
            setLatestInstance = false;
            currentTNHInstance = null;
            currentlyPlayingTNH = false;
            customPacketHandlers = new CustomPacketHandler[10];
            registeredCustomPacketIDs.Clear();
            availableCustomPacketIndices = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            spectatorHostWaitingForTNHSetup = false;

            DestroyImmediate(Mod.managerObject);
            Mod.managerObject = null;
        }

        private void Update()
        {
#if DEBUG 
            if (waitingForDebugCode)
            {
                if (Input.GetKeyDown(KeyCode.Keypad0))
                {
                    debugCode += "0";
                    Mod.LogInfo("DebugCode: " + debugCode);
                }
                else if(Input.GetKeyDown(KeyCode.Keypad1))
                {
                    debugCode += "1";
                    Mod.LogInfo("DebugCode: " + debugCode);
                }
                else if(Input.GetKeyDown(KeyCode.Keypad2))
                {
                    debugCode += "2";
                    Mod.LogInfo("DebugCode: " + debugCode);
                }
                else if(Input.GetKeyDown(KeyCode.Keypad3))
                {
                    debugCode += "3";
                    Mod.LogInfo("DebugCode: " + debugCode);
                }
                else if(Input.GetKeyDown(KeyCode.Keypad4))
                {
                    debugCode += "4";
                    Mod.LogInfo("DebugCode: " + debugCode);
                }
                else if(Input.GetKeyDown(KeyCode.Keypad5))
                {
                    debugCode += "5";
                    Mod.LogInfo("DebugCode: " + debugCode);
                }
                else if(Input.GetKeyDown(KeyCode.Keypad6))
                {
                    debugCode += "6";
                    Mod.LogInfo("DebugCode: " + debugCode);
                }
                else if(Input.GetKeyDown(KeyCode.Keypad7))
                {
                    debugCode += "7";
                    Mod.LogInfo("DebugCode: " + debugCode);
                }
                else if(Input.GetKeyDown(KeyCode.Keypad8))
                {
                    debugCode += "8";
                    Mod.LogInfo("DebugCode: " + debugCode);
                }
                else if(Input.GetKeyDown(KeyCode.Keypad9))
                {
                    debugCode += "9";
                    Mod.LogInfo("DebugCode: " + debugCode);
                }
            }
            if(Input.GetKeyDown(KeyCode.F11))
            {
                waitingForDebugCode = !waitingForDebugCode;
                if (waitingForDebugCode)
                {
                    debugCode = string.Empty;
                }
                else
                {
                    Mod.LogInfo("Activating DebugCode: " + debugCode);
                    if (debugCode != string.Empty && int.TryParse(debugCode, out int code))
                    {
                        switch (code)
                        {
                            case 0: // Start hosting
                                Mod.LogInfo("\tDebug: Start hosting");
                                OnHostClicked();
                                break;
                            case 1: // Connect
                                Mod.LogInfo("\tDebug: Connect");
                                OnConnectClicked(null);
                                break;
                            case 2: // Load config
                                Mod.LogInfo("\tDebug: Load config");
                                LoadConfig();
                                break;
                            case 3: // Disconnect/Close server
                                Mod.LogInfo("\tDebug: Disconnect/Close server");
                                if (Mod.managerObject != null)
                                {
                                    if (ThreadManager.host)
                                    {
                                        Server.Close();
                                    }
                                    else
                                    {
                                        Client.singleton.Disconnect(true, 0);
                                    }
                                }
                                break;
                            case 4: // Load to main menu
                                Mod.LogInfo("\tDebug: Load to main menu");
                                SteamVR_LoadLevel.Begin("MainMenu3", false, 0.5f, 0f, 0f, 0f, 1f);
                                break;
                            case 5: // Load to proving grounds
                                Mod.LogInfo("\tDebug: Load to proving grounds");
                                SteamVR_LoadLevel.Begin("ProvingGround", false, 0.5f, 0f, 0f, 0f, 1f);
                                break;
                            case 6: // Build sosig wearable map
                                Mod.LogInfo("\tDebug: Build sosig wearable map");
                                Dictionary<string, string> map = new Dictionary<string, string>();
                                foreach (KeyValuePair<string, FVRObject> o in IM.OD)
                                {
                                    Mod.LogInfo("Checking " + o.Key);
                                    GameObject prefab = null;
                                    try
                                    {
                                        prefab = o.Value.GetGameObject();

                                    }
                                    catch (Exception)
                                    {
                                        Mod.LogError("There was an error trying to retrieve prefab with ID: " + o.Key);
                                        continue;
                                    }
                                    try
                                    {
                                        SosigWearable wearable = prefab.GetComponent<SosigWearable>();
                                        if (wearable != null)
                                        {
                                            if (map.ContainsKey(prefab.name))
                                            {
                                                Mod.LogWarning("Sosig wearable with name: " + prefab.name + " is already in the map with value: " + map[prefab.name] + " and wewanted to add value: " + o.Key);
                                            }
                                            else
                                            {
                                                map.Add(prefab.name, o.Key);
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        Mod.LogError("There was an error trying to check if prefab with ID: " + o.Key + " is wearable or adding it to the list");
                                        continue;
                                    }
                                }
                                Mod.LogInfo("\tGot wearables, writing...");
                                JObject jDict = JObject.FromObject(map);
                                File.WriteAllText(H3MPPath + "/Debug/SosigWearableMap.json", jDict.ToString());
                                Mod.LogInfo("\tNew wearables map written");
                                break;
                            case 7: // Load to TNH lobby
                                Mod.LogInfo("\tDebug: Load to TNH lobby");
                                SteamVR_LoadLevel.Begin("TakeAndHold_Lobby_2", false, 0.5f, 0f, 0f, 0f, 1f);
                                break;
                            case 8: // Join first TNH instance
                                Mod.LogInfo("\tDebug: Join first TNH instance");
                                if (Mod.currentTNHInstance == null)
                                {
                                    OnTNHJoinClicked();
                                    OnTNHJoinConfirmClicked();
                                    OnTNHInstanceClicked(1);
                                }
                                break;
                            case 9: // Create new TNH instance
                                Mod.LogInfo("\tDebug: Create new TNH instance");
                                if (Mod.currentTNHInstance == null)
                                {
                                    OnTNHHostClicked();
                                    OnTNHHostConfirmClicked();
                                }
                                break;
                            case 10: // Begin TNH hold
                                Mod.LogInfo("\tDebug: Begin TNH hold");
                                GM.TNH_Manager.m_curHoldPoint.m_systemNode.m_hasActivated = true;
                                GM.TNH_Manager.m_curHoldPoint.m_systemNode.m_hasInitiatedHold = true;
                                GM.TNH_Manager.m_curHoldPoint.BeginHoldChallenge();
                                break;
                            case 11: // Load into TNH game (Trigger first scene loader we can find in the scene)
                                Mod.LogInfo("\tDebug: Trigger first scene loader we can find in the scene");
                                GameObject.FindObjectOfType<SceneLoader>().LoadMG();
                                break;
                            case 12: // Write PatchHashes
                                Mod.LogInfo("\tDebug: Write PatchHashes");
                                string dest = H3MPPath + "/PatchHashes" + DateTimeOffset.Now.ToString().Replace("/", ".").Replace(":", ".") + ".json";
                                File.Copy(H3MPPath + "/PatchHashes.json", dest);
                                Mod.LogWarning("Writing new hashes to file!");
                                File.WriteAllText(H3MPPath + "/PatchHashes.json", JObject.FromObject(PatchController.hashes).ToString());
                                break;
                            case 13: // Load to friendly 45
                                Mod.LogInfo("\tDebug: Load to friendly 45");
                                SteamVR_LoadLevel.Begin("Friendly45_New", false, 0.5f, 0f, 0f, 0f, 1f);
                                break;
                            case 14: // Send packet with size > MTU
                                Mod.LogInfo("\tDebug: Send packet with size > MTU");
                                using (Packet packet = new Packet(ThreadManager.host ? (int)ServerPackets.MTUTest : (int)ClientPackets.MTUTest))
                                {
                                    byte[] testArr = new byte[2048];
                                    packet.Write(testArr);
                                    if (ThreadManager.host)
                                    {
                                        ServerSend.SendTCPDataToAll(packet);
                                    }
                                    else
                                    {
                                        ClientSend.SendTCPData(packet);
                                    }
                                }
                                break;
                            case 15: // Spawn items
                                Mod.LogInfo("\tDebug: Spawning items");
                                //ModulAR300LowerFDEdupe
                                //AR15UpperM4FDE

                                GameObject lowerPrefab = IM.OD["ModulAR300LowerFDEdupe"].GetGameObject();
                                Instantiate(lowerPrefab, GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.forward * 0.5f, Quaternion.identity);
                                GameObject upperPrefab = IM.OD["AR15UpperM4FDE"].GetGameObject();
                                Instantiate(upperPrefab, GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.forward, Quaternion.identity);
                                break;
                            case 16: // Dump item IDs
                                Mod.LogInfo("\tDebug: Dumping item IDs");
                                foreach(KeyValuePair<string, FVRObject> entry in IM.OD)
                                {
                                    Mod.LogInfo(entry.Key);
                                }
                                break;
                            case 17: // Toggle null driver debug controls
                                nullDriverControls = !nullDriverControls;
                                Mod.LogInfo("\tDebug: Null driver controls: "+nullDriverControls);
                                if (nullDriverControls)
                                {
                                    nullDriverVerticalRot = 90;
                                    nullDriverHorzontalRot = 0;
                                }
                                break;
                            case 18: // Resolve sub domain
                                Mod.LogInfo("\tDebug: Sub domain h3mp.tommysoucy.vip resolved to: "+ Dns.GetHostAddresses("h3mp.tommysoucy.vip")[0].ToString());
                                break;
                            case 19: // Load to grillhouse
                                Mod.LogInfo("\tDebug: Load to grillhouse");
                                SteamVR_LoadLevel.Begin("Grillhouse_2Story", false, 0.5f, 0f, 0f, 0f, 1f);
                                break;
                            case 20: // Spawn SMG AutoMeater
                                Mod.LogInfo("\tDebug: Spawn SMG AutoMeater");
                                SpawnItem("Turburgert_SMG");
                                break;
                            case 21: // Spawn Flak AutoMeater
                                Mod.LogInfo("\tDebug: Spawn Flak AutoMeater");
                                SpawnItem("Turburgert_Flak");
                                break;
                            case 22: // Spawn Flamethrower AutoMeater
                                Mod.LogInfo("\tDebug: Spawn Flamethrower AutoMeater");
                                SpawnItem("Turburgert_Flamethrower");
                                break;
                            case 23: // Spawn MachineGun AutoMeater
                                Mod.LogInfo("\tDebug: Spawn MachineGun AutoMeater");
                                SpawnItem("Turburgert_MachineGun");
                                break;
                            case 24: // Spawn Suppression AutoMeater
                                Mod.LogInfo("\tDebug: Spawn Suppression AutoMeater");
                                SpawnItem("Turburgert_Suppression");
                                break;
                            case 25: // Spawn blue MF AutoMeater
                                Mod.LogInfo("\tDebug: Spawn blue MF AutoMeater");
                                SpawnItem("TurburgertMFBlue");
                                break;
                            case 26: // Spawn red MF AutoMeater
                                Mod.LogInfo("\tDebug: Spawn red MF AutoMeater");
                                SpawnItem("TurburgertMFRed");
                                break;
                            case 27: // Load to SamplerPlatter
                                Mod.LogInfo("\tDebug: Load to SamplerPlatter");
                                SteamVR_LoadLevel.Begin("SamplerPlatter", false, 0.5f, 0f, 0f, 0f, 1f);
                                break;
                            case 28: // Register test custom packet
                                Mod.LogInfo("\tDebug: Register test custom packet");
                                if (ThreadManager.host)
                                {
                                    if(Mod.registeredCustomPacketIDs.TryGetValue("TestCustomPacketID", out int customPacketID))
                                    {
                                        testCustomPacketID = customPacketID;
                                    }
                                    else
                                    {
                                        testCustomPacketID = Server.RegisterCustomPacketType("TestCustomPacketID");
                                    }
                                    Mod.customPacketHandlers[testCustomPacketID] = TestCustomPacketIDServerHandler;
                                }
                                else
                                {
                                    if (Mod.registeredCustomPacketIDs.TryGetValue("TestCustomPacketID", out int customPacketID))
                                    {
                                        testCustomPacketID = customPacketID;
                                        Mod.customPacketHandlers[testCustomPacketID] = TestCustomPacketIDClientHandler;
                                    }
                                    else
                                    {
                                        ClientSend.RegisterCustomPacketType("TestCustomPacketID");
                                        Mod.CustomPacketHandlerReceived += TestCustomPacketIDReceived;
                                    }
                                }
                                break;
                            case 29: // Send test custom packet
                                Mod.LogInfo("\tDebug: Send test custom packet");
                                if (ThreadManager.host)
                                {
                                    using (Packet packet = new Packet(testCustomPacketID))
                                    {
                                        ServerSend.SendTCPDataToAll(packet, true);
                                    }
                                }
                                else
                                {
                                    using (Packet packet = new Packet(testCustomPacketID))
                                    {
                                        ClientSend.SendTCPData(packet, true);
                                    }
                                }
                                break;
                            case 30: // Toggle server list
                                Mod.LogInfo("\tDebug: Toggle server list");
                                if (ServerListController.instance == null)
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
                                break;
                            case 31: // Server list, host
                                Mod.LogInfo("\tDebug: Server list, host");
                                if (ServerListController.instance != null)
                                {
                                    ServerListController.instance.OnHostClicked();
                                    ServerListController.instance.hostServerName.text = "Debug server";
                                    ServerListController.instance.hostLimit.text = "5";
                                    ServerListController.instance.hostUsername.text = "Debug host";
                                    ServerListController.instance.OnHostConfirmClicked();
                                }
                                break;
                            case 32: // Server list, join first host
                                Mod.LogInfo("\tDebug: Server list, join first host");
                                if (ServerListController.instance != null)
                                {
                                    ServerListController.instance.main.transform.GetChild(2).GetChild(1).GetChild(1).GetComponent<Button>().onClick.Invoke();
                                    ServerListController.instance.joinUsername.text = "Debug client";
                                    ServerListController.instance.OnJoinConfirmClicked();
                                }
                                break;
                        }
                    }
                }
            }
            if (nullDriverControls)
            {
                nullDriverMovement = Vector3.zero;
                if (Input.GetKey(KeyCode.W))
                {
                    nullDriverMovement += GM.CurrentPlayerBody.Head.forward * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.S))
                {
                    nullDriverMovement -= GM.CurrentPlayerBody.Head.forward * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.D))
                {
                    nullDriverMovement += GM.CurrentPlayerBody.Head.right * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.A))
                {
                    nullDriverMovement -= GM.CurrentPlayerBody.Head.right * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.Space))
                {
                    nullDriverMovement += GM.CurrentPlayerBody.Head.up * Time.deltaTime;
                }
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    nullDriverMovement -= GM.CurrentPlayerBody.Head.up * Time.deltaTime;
                }
                nullDriverMovement.Normalize();

                nullDriverMovement *= defaultNullDriverMovementMultiplier;
                float nullDriverRotationMultiplier = defaultNullDriverRotationMultiplier;
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    nullDriverMovement *= 5;
                    nullDriverRotationMultiplier = 2;
                }

                if (Input.GetKey(KeyCode.UpArrow))
                {
                    nullDriverVerticalRot -= 90 * Time.deltaTime * nullDriverRotationMultiplier;
                    if(nullDriverVerticalRot < -90)
                    {
                        nullDriverVerticalRot = -90;
                    }
                }
                else if (Input.GetKey(KeyCode.DownArrow))
                {
                    nullDriverVerticalRot += 90 * Time.deltaTime * nullDriverRotationMultiplier;
                    if (nullDriverVerticalRot > 90)
                    {
                        nullDriverVerticalRot = 90;
                    }
                }
                if (Input.GetKey(KeyCode.RightArrow))
                {
                    nullDriverHorzontalRot += 90 * Time.deltaTime * nullDriverRotationMultiplier;
                }
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    nullDriverHorzontalRot -= 90 * Time.deltaTime * nullDriverRotationMultiplier;
                }
                GM.CurrentPlayerBody.transform.rotation = Quaternion.Euler(nullDriverVerticalRot, nullDriverHorzontalRot, 0);

                GM.CurrentPlayerBody.transform.position += nullDriverMovement;

                bool leftClick = Input.GetMouseButtonDown(0);
                bool rightClick = Input.GetMouseButtonDown(1);
                bool processed = false;
                if (leftClick)
                {
                    FVRViveHand leftHand = GM.CurrentPlayerBody.LeftHand.GetComponent<FVRViveHand>();
                    if (leftHand.CurrentInteractable != null)
                    {
                        TrackedItem trackedItem = leftHand.CurrentInteractable.GetComponent<TrackedItem>();
                        if(trackedItem == null)
                        {
                            leftHand.CurrentInteractable.ForceBreakInteraction();
                        }
                        else
                        {
                            if(trackedItem.fireFunc != null)
                            {
                                trackedItem.fireFunc(0);
                            }
                            else
                            {
                                leftHand.CurrentInteractable.ForceBreakInteraction();
                            }
                        }
                    }
                    processed = true;
                }
                if (rightClick)
                {
                    FVRViveHand rightHand = GM.CurrentPlayerBody.RightHand.GetComponent<FVRViveHand>();
                    if (rightHand.CurrentInteractable != null)
                    {
                        TrackedItem trackedItem = rightHand.CurrentInteractable.GetComponent<TrackedItem>();
                        if(trackedItem == null)
                        {
                            rightHand.CurrentInteractable.ForceBreakInteraction();
                        }
                        else
                        {
                            if(trackedItem.fireFunc != null)
                            {
                                trackedItem.fireFunc(0);
                            }
                            else
                            {
                                rightHand.CurrentInteractable.ForceBreakInteraction();
                            }
                        }
                    }
                    processed = true;
                }
                if (!processed && (leftClick || rightClick))
                {
                    RaycastHit[] hits = Physics.RaycastAll(Camera.main.ScreenPointToRay(Input.mousePosition), 10);
                    if (hits != null && hits.Length > 0)
                    {
                        for(int i=0; i < hits.Length; ++i)
                        {
                            FVRInteractiveObject interactive = hits[i].collider.GetComponent<FVRInteractiveObject>();
                            if (interactive != null)
                            {
                                if (leftClick)
                                {
                                    FVRViveHand leftHand = GM.CurrentPlayerBody.LeftHand.GetComponent<FVRViveHand>();
                                    interactive.BeginInteraction(leftHand);
                                    leftHand.ForceSetInteractable(interactive);
                                }
                                else if (rightClick)
                                {
                                    FVRViveHand rightHand = GM.CurrentPlayerBody.RightHand.GetComponent<FVRViveHand>();
                                    interactive.BeginInteraction(rightHand);
                                    rightHand.ForceSetInteractable(interactive);
                                }
                            }
                        }
                    }
                }
            }
#endif
        }

#if DEBUG
        public static void TestCustomPacketIDServerHandler(int clientID, Packet packet)
        {
            Mod.LogInfo("Custom packet received from client " + clientID);
        }

        public static void TestCustomPacketIDClientHandler(int clientID, Packet packet)
        {
            Mod.LogInfo("Custom packet received from server");
        }

        public static void TestCustomPacketIDReceived(string identifier, int ID)
        {
            Mod.LogInfo("Client received ID " + ID + " for custom packet ID " + identifier);
            if (identifier.Equals("TestCustomPacketID"))
            {
                testCustomPacketID = ID;
                Mod.customPacketHandlers[testCustomPacketID] = TestCustomPacketIDClientHandler;
                Mod.CustomPacketHandlerReceived -= TestCustomPacketIDReceived;
            }
        }

        public GameObject SpawnItem(string itemID)
        {
            if(IM.OD.TryGetValue(itemID, out FVRObject obj))
            {
                return Instantiate(obj.GetGameObject(), Camera.main.transform.position + Camera.main.transform.forward, Quaternion.identity);
            }
            return null;
        }
#endif

        public static void CustomPacketHandlerReceivedInvoke(string handlerID, int index)
        {
            if (CustomPacketHandlerReceived != null)
            {
                CustomPacketHandlerReceived(handlerID, index);
            }
        }

        public static void GenericCustomPacketReceivedInvoke(int clientID, string ID, Packet packet)
        {
            if (GenericCustomPacketReceived != null)
            {
                GenericCustomPacketReceived(clientID, ID, packet);
            }
        }

        public static void OnInstantiationTrackInvoke(GameObject gameObject)
        {
            if (OnInstantiationTrack != null)
            {
                OnInstantiationTrack(gameObject);
            }
        }

        public static void OnConnectionInvoke()
        {
            if (OnConnection != null)
            {
                OnConnection();
            }
        }

        private void SpawnDummyPlayer()
        {
            GameObject player = Instantiate(playerPrefab);

            PlayerManager playerManager = player.GetComponent<PlayerManager>();
            playerManager.ID = -1;
            playerManager.username = "Dummy";
            playerManager.scene = GameManager.scene;
            playerManager.instance = GameManager.instance;
            playerManager.SetIFF(GM.CurrentPlayerBody.GetPlayerIFF());
        }

        public void LoadConfig()
        {
            Logger.LogInfo("Loading config...");
            config = JObject.Parse(File.ReadAllText(H3MPPath + "/Config.json"));
            Logger.LogInfo("Config loaded");
        }

        public static void WriteConfig()
        {
            TODO1: // Make Direct connection use a similar UI to server list where player can enter username, IP (if client), and port
            TODO2: // Add periodic UDP ping as well as UDP punchthrough
            File.WriteAllText(H3MPPath + "/Config.json", JObject.FromObject(config).ToString());
        }

        public static void InitTNHMenu()
        {
            TNHMenu = Instantiate(TNHMenuPrefab, new Vector3(-2.4418f, 1.04f, 6.2977f), Quaternion.Euler(0, 270, 0));

            joinTNHInstances = new Dictionary<int, GameObject>();
            currentTNHInstancePlayers = new Dictionary<int, GameObject>();

            // Add background pointable
            FVRPointable backgroundPointable = TNHMenu.transform.GetChild(0).gameObject.AddComponent<FVRPointable>();
            backgroundPointable.MaxPointingRange = 5;

            // Init refs
            TNHMenuPages = new GameObject[7];
            TNHMenuPages[0] = TNHMenu.transform.GetChild(1).gameObject;
            TNHMenuPages[1] = TNHMenu.transform.GetChild(2).gameObject;
            TNHMenuPages[2] = TNHMenu.transform.GetChild(3).gameObject;
            TNHMenuPages[3] = TNHMenu.transform.GetChild(4).gameObject;
            TNHMenuPages[4] = TNHMenu.transform.GetChild(5).gameObject;
            TNHMenuPages[5] = TNHMenu.transform.GetChild(6).gameObject;
            TNHMenuPages[6] = TNHMenu.transform.GetChild(7).gameObject;
            TNHStatusText = TNHMenu.transform.GetChild(8).GetChild(0).GetComponent<Text>();
            TNHInstanceList = TNHMenu.transform.GetChild(4).GetChild(2).GetChild(0).GetChild(0).gameObject;
            TNHInstancePrefab = TNHMenu.transform.GetChild(4).GetChild(2).GetChild(0).GetChild(0).GetChild(0).gameObject;
            TNHInstanceListScrollBar = TNHMenu.transform.GetChild(4).GetChild(2).GetChild(1).GetComponent<Scrollbar>();
            TNHPlayerList = TNHMenu.transform.GetChild(5).GetChild(2).GetChild(0).GetChild(0).gameObject;
            TNHPlayerPrefab = TNHMenu.transform.GetChild(5).GetChild(2).GetChild(0).GetChild(0).GetChild(0).gameObject;
            TNHPlayerListScrollBar = TNHMenu.transform.GetChild(5).GetChild(2).GetChild(1).GetComponent<Scrollbar>();
            TNHLPJCheckMark = TNHMenu.transform.GetChild(2).GetChild(1).GetChild(0).GetChild(0).gameObject;
            TNHHostOnDeathSpectateCheckMark = TNHMenu.transform.GetChild(2).GetChild(2).GetChild(1).GetChild(0).gameObject;
            TNHHostOnDeathLeaveCheckMark = TNHMenu.transform.GetChild(2).GetChild(2).GetChild(2).GetChild(0).gameObject;
            TNHJoinOnDeathSpectateCheckMark = TNHMenu.transform.GetChild(3).GetChild(1).GetChild(1).GetChild(0).gameObject;
            TNHJoinOnDeathLeaveCheckMark = TNHMenu.transform.GetChild(3).GetChild(1).GetChild(2).GetChild(0).gameObject;
            TNHInstanceTitle = TNHMenu.transform.GetChild(5).GetChild(0).GetComponent<Text>();
            TNHRequestHostOnDeathSpectateCheckMark = TNHMenu.transform.GetChild(6).GetChild(1).GetChild(1).GetChild(0).gameObject;
            TNHRequestHostOnDeathLeaveCheckMark = TNHMenu.transform.GetChild(6).GetChild(1).GetChild(2).GetChild(0).gameObject;

            // Init buttons
            TNHHostButton = TNHMenu.transform.GetChild(1).GetChild(1).gameObject;
            FVRPointableButton currentButton = TNHHostButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHHostClicked);
            TNHJoinButton = TNHMenu.transform.GetChild(1).GetChild(2).gameObject;
            currentButton = TNHJoinButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHJoinClicked);
            TNHRequestHostButton = TNHMenu.transform.GetChild(1).GetChild(3).gameObject;
            currentButton = TNHRequestHostButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHRequestHostClicked);
            TNHRequestHostButton.SetActive(GameManager.spectatorHosts.Count > 0);
            TNHLPJCheck = TNHMenu.transform.GetChild(2).GetChild(1).GetChild(0).gameObject;
            currentButton = TNHLPJCheck.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHLPJCheckClicked);
            TNHHostOnDeathSpectateRadio = TNHMenu.transform.GetChild(2).GetChild(2).GetChild(1).gameObject;
            currentButton = TNHHostOnDeathSpectateRadio.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHHostOnDeathSpectateClicked);
            TNHHostOnDeathLeaveRadio = TNHMenu.transform.GetChild(2).GetChild(2).GetChild(2).gameObject;
            currentButton = TNHHostOnDeathLeaveRadio.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHHostOnDeathLeaveClicked);
            TNHHostConfirmButton = TNHMenu.transform.GetChild(2).GetChild(3).gameObject;
            currentButton = TNHHostConfirmButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHHostConfirmClicked);
            TNHHostCancelButton = TNHMenu.transform.GetChild(2).GetChild(4).gameObject;
            currentButton = TNHHostCancelButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHHostCancelClicked);
            TNHJoinCancelButton = TNHMenu.transform.GetChild(3).GetChild(3).gameObject;
            currentButton = TNHJoinCancelButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHJoinCancelClicked);
            TNHJoinInstanceCancelButton = TNHMenu.transform.GetChild(4).GetChild(3).gameObject;
            currentButton = TNHJoinInstanceCancelButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHJoinCancelClicked);
            TNHInstanceListScrollUpArrow = TNHMenu.transform.GetChild(4).GetChild(2).GetChild(2).gameObject;
            TNHInstanceListScrollDownArrow = TNHMenu.transform.GetChild(4).GetChild(2).GetChild(3).gameObject;
            HoverScroll upScroll = TNHInstanceListScrollUpArrow.AddComponent<HoverScroll>();
            HoverScroll downScroll = TNHInstanceListScrollDownArrow.AddComponent<HoverScroll>();
            upScroll.MaxPointingRange = 5;
            upScroll.scrollbar = TNHInstanceListScrollBar;
            upScroll.other = downScroll;
            upScroll.up = true;
            upScroll.rate = 0.25f;
            downScroll.MaxPointingRange = 5;
            downScroll.scrollbar = TNHInstanceListScrollBar;
            downScroll.other = upScroll;
            downScroll.rate = 0.25f;
            TNHJoinOnDeathSpectateRadio = TNHMenu.transform.GetChild(3).GetChild(1).GetChild(1).gameObject;
            currentButton = TNHJoinOnDeathSpectateRadio.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHJoinOnDeathSpectateClicked);
            TNHJoinOnDeathLeaveRadio = TNHMenu.transform.GetChild(3).GetChild(1).GetChild(2).gameObject;
            currentButton = TNHJoinOnDeathLeaveRadio.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHJoinOnDeathLeaveClicked);
            TNHJoinConfirmButton = TNHMenu.transform.GetChild(3).GetChild(2).gameObject;
            currentButton = TNHJoinConfirmButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHJoinConfirmClicked);
            TNHJoinOptionsCancelButton = TNHMenu.transform.GetChild(3).GetChild(3).gameObject;
            currentButton = TNHJoinOptionsCancelButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHJoinCancelClicked);
            TNHPlayerListScrollUpArrow = TNHMenu.transform.GetChild(5).GetChild(2).GetChild(2).gameObject;
            TNHPlayerListScrollDownArrow = TNHMenu.transform.GetChild(5).GetChild(2).GetChild(3).gameObject;
            upScroll = TNHPlayerListScrollUpArrow.AddComponent<HoverScroll>();
            downScroll = TNHPlayerListScrollDownArrow.AddComponent<HoverScroll>();
            upScroll.MaxPointingRange = 5;
            upScroll.scrollbar = TNHInstanceListScrollBar;
            upScroll.other = downScroll;
            upScroll.up = true;
            upScroll.rate = 0.25f;
            downScroll.MaxPointingRange = 5;
            downScroll.scrollbar = TNHInstanceListScrollBar;
            downScroll.other = upScroll;
            downScroll.rate = 0.25f;
            TNHJoinOptionsCancelButton = TNHMenu.transform.GetChild(5).GetChild(3).gameObject;
            currentButton = TNHJoinOptionsCancelButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHDisconnectClicked);
            TNHRequestHostOnDeathSpectateRadio = TNHMenu.transform.GetChild(6).GetChild(1).GetChild(1).gameObject;
            currentButton = TNHJoinOnDeathSpectateRadio.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHRequestHostOnDeathSpectateClicked);
            TNHRequestHostOnDeathLeaveRadio = TNHMenu.transform.GetChild(6).GetChild(1).GetChild(2).gameObject;
            currentButton = TNHJoinOnDeathLeaveRadio.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHRequestHostOnDeathLeaveClicked);
            TNHRequestHostConfirmButton = TNHMenu.transform.GetChild(6).GetChild(2).gameObject;
            currentButton = TNHRequestHostConfirmButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHRequestHostConfirmClicked);
            TNHRequestHostCancelButton = TNHMenu.transform.GetChild(6).GetChild(3).gameObject;
            currentButton = TNHRequestHostCancelButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHRequestHostCancelClicked);
            TNHRequestHostWaitingCancelButton = TNHMenu.transform.GetChild(7).GetChild(2).gameObject;
            currentButton = TNHRequestHostWaitingCancelButton.AddComponent<FVRPointableButton>();
            currentButton.SetButton();
            currentButton.MaxPointingRange = 5;
            currentButton.Button.onClick.AddListener(OnTNHRequestHostWaitingCancelClicked);

            // Get ref to the UI Manager
            Mod.currentTNHUIManager = GameObject.FindObjectOfType<TNH_UIManager>();
            Mod.currentTNHSceneLoader = GameObject.FindObjectOfType<SceneLoader>();

            // If already in a TNH instance, which could be the case if we are coming back from being in game
            if (currentTNHInstance != null)
            {
                // Set vanilla TNH settings corresponding to TNH instance's
                InitTNHUIManager(currentTNHInstance);

                // Set our TNH menu accordingly
                // Set instance page
                TNHMenuPages[0].SetActive(false);
                TNHMenuPages[1].SetActive(false);
                TNHMenuPages[2].SetActive(false);
                TNHMenuPages[3].SetActive(false);
                TNHMenuPages[4].SetActive(true);

                // Set details
                if (currentTNHInstance.playerIDs[0] == GameManager.ID)
                {
                    TNHStatusText.text = "Hosting TNH instance";
                }
                else
                {
                    TNHStatusText.text = "Client in TNH game";
                }
                TNHStatusText.color = Color.green;

                // Populate player list
                for (int i = 0; i < currentTNHInstance.playerIDs.Count; ++i)
                {
                    GameObject newPlayer = Instantiate<GameObject>(TNHPlayerPrefab, TNHPlayerList.transform);
                    if (GameManager.players.ContainsKey(currentTNHInstance.playerIDs[i]))
                    {
                        newPlayer.transform.GetChild(0).GetComponent<Text>().text = GameManager.players[currentTNHInstance.playerIDs[i]].username + (i == 0 ? " (Host)" : "");
                    }
                    else
                    {
                        newPlayer.transform.GetChild(0).GetComponent<Text>().text = config["Username"].ToString() + (i == 0 ? " (Host)" : "");
                    }
                    newPlayer.SetActive(true);

                    currentTNHInstancePlayers.Add(currentTNHInstance.playerIDs[i], newPlayer);
                }
            }
            else
            {
                // Set option defaults
                TNHMenuLPJ = true;
                TNHOnDeathSpectate = true;
            }
        }

        public static void InitTNHUIManager(TNHInstance instance)
        {
            Mod.currentTNHUIManager.OBS_Progression.SetSelectedButton(instance.progressionTypeSetting);
            GM.TNHOptions.ProgressionTypeSetting = (TNHSetting_ProgressionType)instance.progressionTypeSetting;
            Mod.currentTNHUIManager.OBS_EquipmentMode.SetSelectedButton(instance.equipmentModeSetting);
            GM.TNHOptions.EquipmentModeSetting = (TNHSetting_EquipmentMode)instance.equipmentModeSetting;
            Mod.currentTNHUIManager.OBS_HealthMode.SetSelectedButton(instance.healthModeSetting);
            GM.TNHOptions.HealthModeSetting = (TNHSetting_HealthMode)instance.healthModeSetting;
            Mod.currentTNHUIManager.OBS_TargetMode.SetSelectedButton(instance.targetModeSetting);
            GM.TNHOptions.TargetModeSetting = (TNHSetting_TargetMode)instance.targetModeSetting;
            Mod.currentTNHUIManager.OBS_AIDifficulty.SetSelectedButton(instance.AIDifficultyModifier);
            GM.TNHOptions.AIDifficultyModifier = (TNHModifier_AIDifficulty)instance.AIDifficultyModifier;
            Mod.currentTNHUIManager.OBS_AIRadarMode.SetSelectedButton(instance.radarModeModifier);
            GM.TNHOptions.RadarModeModifier = (TNHModifier_RadarMode)instance.radarModeModifier;
            Mod.currentTNHUIManager.OBS_ItemSpawner.SetSelectedButton(instance.itemSpawnerMode);
            GM.TNHOptions.ItemSpawnerMode = (TNH_ItemSpawnerMode)instance.itemSpawnerMode;
            Mod.currentTNHUIManager.OBS_Backpack.SetSelectedButton(instance.backpackMode);
            GM.TNHOptions.BackpackMode = (TNH_BackpackMode)instance.backpackMode;
            Mod.currentTNHUIManager.OBS_HealthMult.SetSelectedButton(instance.healthMult);
            GM.TNHOptions.HealthMult = (TNH_HealthMult)instance.healthMult;
            Mod.currentTNHUIManager.OBS_SosiggunReloading.SetSelectedButton(instance.sosiggunShakeReloading);
            GM.TNHOptions.SosiggunShakeReloading = (TNH_SosiggunShakeReloading)instance.sosiggunShakeReloading;
            Mod.currentTNHUIManager.OBS_RunSeed.SetSelectedButton(instance.TNHSeed + 1);
            GM.TNHOptions.TNHSeed = instance.TNHSeed;

            // Find level
            bool found = false;
            for (int i = 0; i < Mod.currentTNHUIManager.Levels.Count; ++i)
            {
                if (Mod.currentTNHUIManager.Levels[i].LevelID.Equals(instance.levelID))
                {
                    found = true;
                    Mod.currentTNHUIManager.m_currentLevelIndex = i;
                    Mod.currentTNHUIManager.CurLevelID = instance.levelID;
                    Mod.currentTNHUIManager.UpdateLevelSelectDisplayAndLoader();
                    Mod.currentTNHUIManager.UpdateTableBasedOnOptions();
                    Mod.currentTNHSceneLoader.gameObject.SetActive(true);
                    break;
                }
            }
            if (!found)
            {
                Mod.currentTNHSceneLoader.gameObject.SetActive(false);
                Mod.LogError("Missing TNH level: " + instance.levelID + "! Make sure you have it installed.");
            }
        }

        private void LoadAssets()
        {
            AssetBundle assetBundle = AssetBundle.LoadFromFile(H3MPPath + "/H3MP.ab");

            TNHMenuPrefab = assetBundle.LoadAsset<GameObject>("TNHMenu");
            reticleFriendlyContactArrowMat = assetBundle.LoadAsset<Material>("ReticleFriendlyContactArrowMat");
            reticleFriendlyContactIconMat = assetBundle.LoadAsset<Material>("ReticleFriendlyContactIconMat");
            keyboardPrefab = assetBundle.LoadAsset<GameObject>("KeyBoard");
            serverListPrefab = assetBundle.LoadAsset<GameObject>("ServerList");

            playerPrefab = assetBundle.LoadAsset<GameObject>("Player");
            GameManager.playerPrefabs.Add("Default", playerPrefab);
            GameManager.playerPrefabIDs.Add("Default");

            sosigWearableMap = JObject.Parse(File.ReadAllText(H3MPPath + "/SosigWearableMap.json")).ToObject<Dictionary<string, string>>();

            TNHStartEquipButtonPrefab = assetBundle.LoadAsset<GameObject>("TNHStartEquipButton");
            FVRPointableButton startEquipButton = TNHStartEquipButtonPrefab.transform.GetChild(0).gameObject.AddComponent<FVRPointableButton>();
            startEquipButton.SetButton();
            startEquipButton.MaxPointingRange = 1;

            // Build glass MatDef
            glassMatDef = ScriptableObject.CreateInstance<MatDef>();
            glassMatDef.BallisticType = MatBallisticType.GlassThin;
            glassMatDef.SoundType = MatSoundType.Tile;
            glassMatDef.ImpactEffectType = BallisticImpactEffectType.Generic;
            glassMatDef.BulletHoleType = BulletHoleDecalType.None;
            glassMatDef.BulletImpactSound = BulletImpactSoundType.GlassWindshield;

            // Build glass audio events
            glassShotEvent = new AudioEvent();
            glassThudHeadEvent = new AudioEvent();
            glassThudTailEvent = new AudioEvent();
            glassGroundShatterEvent = new AudioEvent();
            glassTotalMediumEvent = new AudioEvent();
            for (int i = 1; i <= 3; ++i) 
            {
                glassShotEvent.Clips.Add(assetBundle.LoadAsset<AudioClip>("SheetBreak_Shot_Head_0" + i + ".wav"));
            }
            for (int i = 1; i <= 4; ++i) 
            {
                glassThudHeadEvent.Clips.Add(assetBundle.LoadAsset<AudioClip>("SheetBreak_Thud_Head_0" + i + ".wav"));
                glassThudTailEvent.Clips.Add(assetBundle.LoadAsset<AudioClip>("SheetBreak_Thud_Tail_0" + i + ".wav"));
                glassGroundShatterEvent.Clips.Add(assetBundle.LoadAsset<AudioClip>("SmallShard_GroundImpactShatter_0" + i + ".wav"));
            }
            for (int i = 1; i <= 8; ++i)
            {
                glassTotalMediumEvent.Clips.Add(assetBundle.LoadAsset<AudioClip>("SheetBreak_Total_Medium_0" + i + ".wav"));
            }

            // Build glass prefabs
            glassPrefab = assetBundle.LoadAsset<GameObject>("GlassPrefab");
            BreakableGlassDamager damager = glassPrefab.AddComponent<BreakableGlassDamager>();
            damager.Glass = glassPrefab.AddComponent<BreakableGlass>();
            damager.AudEvent_Head_Projectile = Mod.glassShotEvent;
            damager.AudEvent_Head_Melee = Mod.glassThudHeadEvent;
            damager.AudEvent_Tail = Mod.glassThudTailEvent;
            damager.AudEvent_Shatter_BlowOut = Mod.glassTotalMediumEvent;
            damager.AudEvent_GroundShatter = Mod.glassGroundShatterEvent;
            glassPrefab.AddComponent<PMat>().MatDef = Mod.glassMatDef;
            glassPFXPrefab = assetBundle.LoadAsset<GameObject>("GlassPFXPlaceholder");
            glassPFXPrefab.AddComponent<KillAfter>().DieTime = 5;
            damager.DestructionPFX_Directional = glassPFXPrefab;
            damager.DestructionPFX_Omni = glassPFXPrefab;

            // Build distant shot set
            // TODO: Improvement: These sounds are currently only using pistol distant sounds, should be dependent on FireArmRoundType
            AudioEvent[] shotSets = new AudioEvent[4];
            for(int i=0; i < 3; ++i)
            {
                shotSets[i] = new AudioEvent();
                shotSets[i].PitchRange = new Vector2(1.3f, 1.33f);
                shotSets[i].VolumeRange = new Vector2(0.4f, 0.4f);
                string name = "";
                switch (i)
                {
                    case 0:
                        name = "InsideLarge";
                        break;
                    case 1:
                        name = "InsideSmall";
                        break;
                    case 2:
                        name = "OutsideOpen";
                        break;
                }
                for (int j=1; j < 4; ++j)
                {
                    shotSets[i].Clips.Add(assetBundle.LoadAsset<AudioClip>("DistantShot_" + name + "_"+j+".wav"));
                }
            }
            distantShotSets = new Dictionary<FVRSoundEnvironment, AudioEvent>();
            foreach (FVRSoundEnvironment env in Enum.GetValues(typeof(FVRSoundEnvironment)))
            {
                switch (env)
                {
                    case FVRSoundEnvironment.None:
                    case FVRSoundEnvironment.InsideNarrow:
                    case FVRSoundEnvironment.InsideWarehouse:
                    case FVRSoundEnvironment.InsideWarehouseSmall:
                    case FVRSoundEnvironment.InsideLargeHighCeiling:
                    case FVRSoundEnvironment.SniperRange:
                    case FVRSoundEnvironment.ShootingRange:
                        distantShotSets.Add(env, shotSets[0]);
                        break;
                    case FVRSoundEnvironment.InsideSmall:
                    case FVRSoundEnvironment.InsideNarrowSmall:
                    case FVRSoundEnvironment.InsideMedium:
                    case FVRSoundEnvironment.InsideLarge:
                        distantShotSets.Add(env, shotSets[1]);
                        break;
                    case FVRSoundEnvironment.OutsideEnclosed:
                    case FVRSoundEnvironment.OutsideEnclosedNarrow:
                    case FVRSoundEnvironment.OutsideOpen:
                    case FVRSoundEnvironment.Forest:
                        distantShotSets.Add(env, shotSets[2]);
                        break;
                }
            }
        }

        private void GetTrackedObjectTypes()
        {
            // The idea here is that when we check which tracked type a certain object corresponds to
            // we want to check tracked types taht are as specific as possible first
            // So we need to store them in such a way that they can be sorted in order of most to least specific
            // just so we can check them in order
            trackedObjectTypes = new Dictionary<Type, List<Type>>();
            trackedObjectTypesByName = new Dictionary<string, Type>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; ++i)
            {
                try
                {
                    Type[] types = assemblies[i].GetTypes();
                    for (int j = 0; j < types.Length; ++j)
                    {
                        if (IsTypeTrackedObject(types[j]))
                        {
                            AddTrackedType(types[j]);
                        }
                    }
                }
                catch
                {
                    Mod.LogWarning("Unable to read types from assembly: "+ assemblies[i].FullName);

                    if (assemblies[i].FullName.Split(',')[0].Equals("H3MP"))
                    {
                        AddTrackedType(typeof(TrackedItemData));
                        AddTrackedType(typeof(TrackedSosigData));
                        AddTrackedType(typeof(TrackedEncryptionData));
                        AddTrackedType(typeof(TrackedAutoMeaterData));
                        AddTrackedType(typeof(TrackedBreakableGlassData));
                    }
                }
            }
        }

        public void AddTrackedType(Type type)
        {
            // Add to dict of all tracked types
            trackedObjectTypesByName.Add(type.Name, type);

            // Note: Key of entry is the most general subtype of TrackedObjectData

            // Just add if no other type yet
            if (trackedObjectTypes.Count == 0)
            {
                trackedObjectTypes.Add(type, new List<Type>() { type });
                return;
            }

            // Check if sub/supertype of any preexisting entry
            foreach (KeyValuePair<Type, List<Type>> entry in trackedObjectTypes)
            {
                if (type.IsSubclassOf(entry.Key))
                {
                    // It is subtype of preexisting entry, must add to list of types in order
                    // We want to insert after the last type it is subtype of
                    int currentLastIndex = 0;
                    for(int i=1; i < entry.Value.Count; ++i) // Note: we start at 1 because 0 is key and if we are here it is because we are subtype of key
                    {
                        if (type.IsSubclassOf(entry.Value[i]))
                        {
                            currentLastIndex = i;
                        }
                    }
                    entry.Value.Insert(currentLastIndex + 1, type);
                    return;
                }
                else if (entry.Key.IsSubclassOf(type))
                {
                    // It is supertype of preexisting entry, must remove current entry and
                    // insert type at start of list since it is the most generic type
                    entry.Value.Insert(0, type);
                    trackedObjectTypes.Add(type, entry.Value);
                    trackedObjectTypes.Remove(entry.Key);
                    return;
                }
            }

            // If got all the way here, it is because it didn't match any preexisting entry, just add
            trackedObjectTypes.Add(type, new List<Type>() { type });
        }

        public bool IsTypeTrackedObject(Type type)
        {
            if(type.BaseType != null)
            {
                if (type.IsSubclassOf(typeof(TrackedObjectData)))
                {
                    MethodInfo isOfTypeMethod = type.GetMethod("IsOfType", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    MethodInfo makeTrackedMethod = type.GetMethod("MakeTracked", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    if (isOfTypeMethod != null && isOfTypeMethod.ReturnType == typeof(bool) && isOfTypeMethod.GetParameters()[0].ParameterType == typeof(Transform) &&
                        makeTrackedMethod != null && makeTrackedMethod.ReturnType.IsSubclassOf(typeof(TrackedObject)) && makeTrackedMethod.GetParameters()[0].ParameterType == typeof(Transform) && makeTrackedMethod.GetParameters()[1].ParameterType == typeof(TrackedObjectData))
                    {
                        return true;
                    }
                    else
                    {
                        Mod.LogError("TrackedObjectData inheriting type \""+ type.Name+ "\" is missing implementation for one of the following methods:\n" +
                            "\tstatic bool IsOfType(Transform)\n" +
                            "\tstatic TrackedObject MakeTracked(Transform, TrackedObjectData)\n" +
                            "\tThis type will not be tracked.");
                    }
                }
            }

            return false;
        }

        private void Init()
        {
            Logger.LogInfo("H3MP Init called");

            H3MPPath = Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(typeof(Mod)).Location);
            H3MPPath.Replace('\\', '/');
            Mod.LogInfo("H3MP path found: "+ H3MPPath, false);

            GetTrackedObjectTypes();

            PatchController.DoPatching();

            LoadConfig();

            LoadAssets();

            SteamVR_Events.Loading.Listen(GameManager.OnSceneLoadedVR);
            SceneManager.sceneLoaded += OnSceneLoaded;

            SteamVR_Events.Loading.Listen(TrackedBreakableGlassData.ClearWrapperDicts);

            GameManager.OnSpectatorHostsChanged += OnSpectatorHostsChanged;
        }

        private void OnSpectatorHostsChanged()
        {
            if (TNHRequestHostButton != null)
            {
                TNHRequestHostButton.SetActive(GameManager.spectatorHosts.Count > 0);
            }
        }

        public static void OnHostClicked()
        {
            if(managerObject != null)
            {
                return;
            }

            //Server.IP = config["IP"].ToString();
            CreateManagerObject(true);

            Server.Start((ushort)config["MaxClientCount"], (ushort)config["Port"]);

            if (GameManager.scene.Equals("TakeAndHold_Lobby_2"))
            {
                LogInfo("Just connected in TNH lobby, initializing H3MP menu");
                InitTNHMenu();
            }

            ForceDefaultPlayerBody();

            GameManager.firstPlayerInSceneInstance = true; 
            
            if(OnConnection != null)
            {
                OnConnection();
            }

            //mainStatusText.text = "Starting...";
            //mainStatusText.color = Color.white;
        }

        public static void OnConnectClicked(IPEndPoint endPointOverride)
        {
            if (managerObject != null)
            {
                return;
            }

            if (config["IP"].ToString().Equals(""))
            {
                LogError("Attempted to connect to server but no IP set in config!");
                return;
            }

            CreateManagerObject();

            Client client = managerObject.AddComponent<Client>();
            if(endPointOverride == null)
            {
                client.IP = config["IP"].ToString();
                client.port = (ushort)config["Port"];
            }
            else
            {
                client.IP = endPointOverride.Address.ToString();
                client.port = (ushort)endPointOverride.Port;
            }

            client.ConnectToServer();

            if (GameManager.scene.Equals("TakeAndHold_Lobby_2"))
            {
                LogInfo("Just connected in TNH lobby, initializing H3MP menu");
                InitTNHMenu();
            }

            //mainStatusText.text = "Connecting...";
            //mainStatusText.color = Color.white;
        }

        public static void ForceDefaultPlayerBody()
        {
            if ((GameManager.playerPrefabIndex == -1 || GameManager.playerPrefabID.Equals("None"))
                && GameManager.currentPlayerBody == null
                && !GameManager.playerModelAwaitingInstantiation)
            {
                GameManager.playerPrefabID = "Default";
                for (int i = 0; i < GameManager.playerPrefabIDs.Count; ++i)
                {
                    if (GameManager.playerPrefabIDs[i].Equals(GameManager.playerPrefabID))
                    {
                        GameManager.playerPrefabIndex = i;
                        break;
                    }
                }

                GameManager.currentPlayerBody = Instantiate(GameManager.playerPrefabs[GameManager.playerPrefabID] as GameObject).GetComponent<PlayerBody>();

                if (BodyWristMenuSection.playerBodyText != null)
                {
                    BodyWristMenuSection.playerBodyText.text = "Body: Default";
                }
            }
        }

        public static void OnTNHHostClicked()
        {
            TNHMenuPages[0].SetActive(false);
            TNHMenuPages[1].SetActive(true);
            TNHStatusText.text = "Setting up as Host";
            TNHStatusText.color = Color.blue;
        }

        private static void OnTNHJoinClicked()
        {
            TNHMenuPages[0].SetActive(false);
            TNHMenuPages[2].SetActive(true);
            TNHStatusText.text = "Setting up as Client";
            TNHStatusText.color = Color.blue;
        }

        private static void OnTNHLPJCheckClicked()
        {
            TNHLPJCheckMark.SetActive(!TNHLPJCheckMark.activeSelf);

            TNHMenuLPJ = TNHLPJCheckMark.activeSelf;
        }

        private static void OnTNHHostOnDeathSpectateClicked()
        {
            TNHHostOnDeathSpectateCheckMark.SetActive(!TNHHostOnDeathSpectateCheckMark.activeSelf);
            TNHHostOnDeathLeaveCheckMark.SetActive(!TNHHostOnDeathSpectateCheckMark.activeSelf);

            TNHOnDeathSpectate = TNHHostOnDeathSpectateCheckMark.activeSelf;
        }

        private static void OnTNHHostOnDeathLeaveClicked()
        {
            TNHHostOnDeathLeaveCheckMark.SetActive(!TNHHostOnDeathLeaveCheckMark.activeSelf);
            TNHHostOnDeathSpectateCheckMark.SetActive(!TNHHostOnDeathLeaveCheckMark.activeSelf);

            TNHOnDeathSpectate = TNHHostOnDeathSpectateCheckMark.activeSelf;
        }

        public static void OnTNHHostConfirmClicked()
        {
            TNHMenuPages[1].SetActive(false);
            TNHMenuPages[4].SetActive(true);

            setLatestInstance = true;

            TNHInstance newTNHInstance = GameManager.AddNewTNHInstance(GameManager.ID, TNHMenuLPJ, (int)GM.TNHOptions.ProgressionTypeSetting,
                                              (int)GM.TNHOptions.HealthModeSetting, (int)GM.TNHOptions.EquipmentModeSetting, (int)GM.TNHOptions.TargetModeSetting,
                                              (int)GM.TNHOptions.AIDifficultyModifier, (int)GM.TNHOptions.RadarModeModifier, (int)GM.TNHOptions.ItemSpawnerMode,
                                              (int)GM.TNHOptions.BackpackMode, (int)GM.TNHOptions.HealthMult, (int)GM.TNHOptions.SosiggunShakeReloading, (int)GM.TNHOptions.TNHSeed,
                                              Mod.currentTNHUIManager.CurLevelID);
            if (ThreadManager.host)
            {
                ServerSend.AddTNHInstance(newTNHInstance);
            }

            TNHStatusText.text = "Hosting TNH instance";
            TNHStatusText.color = Color.green;
        }

        private static void OnTNHHostCancelClicked()
        {
            TNHMenuPages[0].SetActive(true);
            TNHMenuPages[1].SetActive(false);

            TNHStatusText.text = "Solo";
            TNHStatusText.color = Color.red;
        }

        private static void OnTNHJoinCancelClicked()
        {
            TNHMenuPages[0].SetActive(true);
            TNHMenuPages[2].SetActive(false);
            TNHMenuPages[3].SetActive(false);

            TNHStatusText.text = "Solo";
            TNHStatusText.color = Color.red;
        }

        private static void OnTNHJoinOnDeathSpectateClicked()
        {
            TNHJoinOnDeathSpectateCheckMark.SetActive(!TNHJoinOnDeathSpectateCheckMark.activeSelf);
            TNHJoinOnDeathLeaveCheckMark.SetActive(!TNHJoinOnDeathSpectateCheckMark.activeSelf);

            TNHOnDeathSpectate = TNHJoinOnDeathSpectateCheckMark.activeSelf;
        }

        private static void OnTNHJoinOnDeathLeaveClicked()
        {
            TNHJoinOnDeathLeaveCheckMark.SetActive(!TNHJoinOnDeathLeaveCheckMark.activeSelf);
            TNHJoinOnDeathSpectateCheckMark.SetActive(!TNHJoinOnDeathLeaveCheckMark.activeSelf);

            TNHOnDeathSpectate = TNHJoinOnDeathSpectateCheckMark.activeSelf;
        }

        private static void OnTNHJoinConfirmClicked()
        {
            if (waitingForTNHHost)
            {
                TNHMenuPages[2].SetActive(false);

                // Handle joining instance success/fail
                if (SetTNHInstance(GameManager.TNHInstances[TNHHostedInstance]))
                {
                    TNHMenuPages[4].SetActive(true);

                    TNHStatusText.text = "Client in TNH game";
                    TNHStatusText.color = Color.green;
                }
                else
                {
                    TNHMenuPages[3].SetActive(true);

                    PopulateInstancesList();

                    joinTNHInstances[TNHHostedInstance].transform.GetChild(0).GetComponent<Text>().color = Color.red;
                }

                waitingForTNHHost = false;
            }
            else
            {
                TNHMenuPages[2].SetActive(false);
                TNHMenuPages[3].SetActive(true);

                PopulateInstancesList();
            }
        }

        private static void PopulateInstancesList()
        {
            if (joinTNHInstances == null)
            {
                joinTNHInstances = new Dictionary<int, GameObject>();
            }
            else
            {
                foreach (KeyValuePair<int, GameObject> entry in joinTNHInstances)
                {
                    Destroy(entry.Value);
                }
            }
            joinTNHInstances.Clear();

            // Populate instance list
            foreach (KeyValuePair<int, TNHInstance> TNHInstance in GameManager.TNHInstances)
            {
                if (TNHInstance.Value.letPeopleJoin || TNHInstance.Value.currentlyPlaying.Count == 0)
                {
                    GameObject newInstance = Instantiate<GameObject>(TNHInstancePrefab, TNHInstanceList.transform);
                    newInstance.transform.GetChild(0).GetComponent<Text>().text = "Instance " + TNHInstance.Key;
                    newInstance.SetActive(true);

                    int instanceID = TNHInstance.Key;
                    FVRPointableButton instanceButton = newInstance.AddComponent<FVRPointableButton>();
                    instanceButton.SetButton();
                    instanceButton.MaxPointingRange = 5;
                    instanceButton.Button.onClick.AddListener(() => { OnTNHInstanceClicked(instanceID); });

                    joinTNHInstances.Add(TNHInstance.Key, newInstance);
                }
            }
        }

        public static void OnTNHInstanceClicked(int instance)
        {
            // Handle joining instance success/fail
            if (SetTNHInstance(GameManager.TNHInstances[instance]))
            {
                TNHMenuPages[3].SetActive(false);
                TNHMenuPages[4].SetActive(true);

                TNHStatusText.text = "Client in TNH game";
                TNHStatusText.color = Color.green;
            }
            else
            {
                joinTNHInstances[instance].transform.GetChild(0).GetComponent<Text>().color = Color.red;
            }
        }

        private static void OnTNHDisconnectClicked()
        {
            TNHMenuPages[4].SetActive(false);
            TNHMenuPages[0].SetActive(true);

            GameManager.SetInstance(0);
            if (Mod.currentlyPlayingTNH)
            {
                Mod.currentTNHInstance.RemoveCurrentlyPlaying(true, GameManager.ID, ThreadManager.host);
                Mod.currentlyPlayingTNH = false;
            }
            Mod.currentTNHInstance = null;
            Mod.TNHSpectating = false;
            if (GM.CurrentPlayerBody != null && GM.CurrentPlayerBody.RightHand != null && GM.CurrentPlayerBody.LeftHand != null)
            {
                GM.CurrentPlayerBody.EnableHands();
            }

            TNHStatusText.text = "Solo";
            TNHStatusText.color = Color.red;
        }

        private static void OnTNHRequestHostClicked()
        {
            TNHMenuPages[0].SetActive(false);
            TNHMenuPages[5].SetActive(true);
            TNHStatusText.text = "Setting up host request";
            TNHStatusText.color = Color.blue;
        }

        private static void OnTNHRequestHostOnDeathSpectateClicked()
        {
            TNHRequestHostOnDeathSpectateCheckMark.SetActive(!TNHRequestHostOnDeathSpectateCheckMark.activeSelf);
            TNHRequestHostOnDeathLeaveCheckMark.SetActive(!TNHRequestHostOnDeathSpectateCheckMark.activeSelf);

            TNHRequestHostOnDeathSpectate = TNHRequestHostOnDeathSpectateCheckMark.activeSelf;
        }

        private static void OnTNHRequestHostOnDeathLeaveClicked()
        {
            TNHRequestHostOnDeathLeaveCheckMark.SetActive(!TNHRequestHostOnDeathLeaveCheckMark.activeSelf);
            TNHRequestHostOnDeathSpectateCheckMark.SetActive(!TNHRequestHostOnDeathLeaveCheckMark.activeSelf);

            TNHRequestHostOnDeathSpectate = TNHRequestHostOnDeathSpectateCheckMark.activeSelf;
        }

        private static void OnTNHRequestHostConfirmClicked()
        {
            TNHMenuPages[5].SetActive(false);
            TNHMenuPages[6].SetActive(true);
            TNHStatusText.text = "Waiting for host";
            TNHStatusText.color = Color.blue;

            if (ThreadManager.host)
            {
                if (Server.availableSpectatorHosts.Count > 0)
                {
                    GameManager.controlledSpectatorHost = Server.availableSpectatorHosts[Server.availableSpectatorHosts.Count - 1];
                    Server.spectatorHostByController.Add(0, GameManager.controlledSpectatorHost);
                    Server.spectatorHostControllers.Add(GameManager.controlledSpectatorHost, 0);
                    Server.availableSpectatorHosts.RemoveAt(Server.availableSpectatorHosts.Count - 1);

                    ServerSend.SpectatorHostAssignment(GameManager.controlledSpectatorHost, 0);

                    ServerSend.SpectatorHostOrderTNHHost(GameManager.controlledSpectatorHost, TNHRequestHostOnDeathSpectate);

                    waitingForTNHHost = true;
                }
                else
                {
                    TNHRequestHostButton.SetActive(false);

                    TNHMenuPages[0].SetActive(true);
                    TNHMenuPages[6].SetActive(false);
                }
            }
            else
            {
                ClientSend.RequestSpectatorHost();
                OnSpectatorHostReceived += OnReceiveTNHHost;

                waitingForTNHHost = true;
            }
        }

        private static void OnReceiveTNHHost(int host, ref bool confirmed)
        {
            if (confirmed)
            {
                Mod.LogError("Spectator host was already confirmed!");
                return;
            }

            if (waitingForTNHHost)
            {
                GameManager.controlledSpectatorHost = host;
                confirmed = true;
                OnSpectatorHostReceived -= OnReceiveTNHHost;
                OnSpectatorHostGiveUp += OnSpectatorHostGiveUpOrdered;

                ClientSend.SpectatorHostOrderTNHHost(TNHRequestHostOnDeathSpectate);
            }
        }

        private static void OnSpectatorHostGiveUpOrdered()
        {
            if (waitingForTNHHost)
            {
                TNHRequestHostButton.SetActive(false);

                TNHMenuPages[0].SetActive(true);
                TNHMenuPages[6].SetActive(false);

                waitingForTNHHost = false;

                OnSpectatorHostGiveUp -= OnSpectatorHostGiveUpOrdered;
            }
        }

        public static void OnSpectatorHostReceivedInvoke(int host, bool reassignment = false)
        {
            bool confirmed = false;
            if (OnSpectatorHostReceived != null)
            {
                OnSpectatorHostReceived(host, ref confirmed);
            }

            if (reassignment)
            {
                // Will not necessarily get confirmation so set controlled
                GameManager.controlledSpectatorHost = host;
            }
            else
            {
                if (!confirmed)
                {
                    // Note: If received a spectator host, we must be client
                    GameManager.controlledSpectatorHost = -1;
                    ClientSend.UnassignSpectatorHost();
                }
            }
        }

        public static void OnSpectatorHostGiveUpInvoke()
        {
            if (OnSpectatorHostGiveUp != null)
            {
                OnSpectatorHostGiveUp();
            }

            GameManager.controlledSpectatorHost = -1;
        }

        private static void OnTNHRequestHostCancelClicked()
        {
            TNHMenuPages[0].SetActive(true);
            TNHMenuPages[5].SetActive(false);

            TNHStatusText.text = "Solo";
            TNHStatusText.color = Color.red;
        }

        private static void OnTNHRequestHostWaitingCancelClicked()
        {
            if (waitingForTNHHost)
            {
                TNHMenuPages[0].SetActive(true);
                TNHMenuPages[6].SetActive(false);

                TNHStatusText.text = "Solo";
                TNHStatusText.color = Color.red;

                waitingForTNHHost = false;

                if (ThreadManager.host)
                {
                    Server.spectatorHostByController.Remove(0);
                    Server.spectatorHostControllers.Remove(GameManager.controlledSpectatorHost);

                    if (GameManager.spectatorHosts.Contains(GameManager.controlledSpectatorHost))
                    {
                        Server.availableSpectatorHosts.Add(GameManager.controlledSpectatorHost);
                    }
                    GameManager.controlledSpectatorHost = -1;
                }
                else
                {
                    ClientSend.UnassignSpectatorHost();
                }
            }
            else if(waitingForTNHGameStart)
            {
                TNHMenuPages[6].SetActive(false);
                TNHMenuPages[4].SetActive(true);

                waitingForTNHGameStart = false;
            }
        }

        public static void OnTNHSpawnStartEquipClicked()
        {
            Mod.LogInfo("OnTNHSpawnStartEquipClicked, TNHTweakerASM = " + PatchController.TNHTweakerAsmIdx, false);
            int debugStep = 0;
            try
            {
                if (GM.TNH_Manager != null)
                {
                    ++debugStep;
                    if (Mod.managerObject != null)
                    {
                        Mod.currentTNHInstance.spawnedStartEquip = true;
                    }

                    TNH_Manager M = GM.TNH_Manager;
                    TNH_CharacterDef C = M.C;
                    Vector3 projectedForward = Vector3.ProjectOnPlane(GM.CurrentPlayerBody.Head.forward, Vector3.up);
                    if (M.ItemSpawnerMode == TNH_ItemSpawnerMode.On)
                    {
                        M.ItemSpawner.transform.position = GM.CurrentPlayerBody.Head.position + projectedForward.normalized * 2;
                        M.ItemSpawner.transform.rotation = Quaternion.LookRotation(-projectedForward, Vector3.up);
                        M.ItemSpawner.SetActive(true);
                    }
                    Vector3 target = GM.CurrentPlayerBody.Head.position + Vector3.down * 0.5f;
                    Vector3 projectedRight = Vector3.ProjectOnPlane(GM.CurrentPlayerBody.Head.right, Vector3.up);
                    Vector3 largeCaseSpawnPos = target + projectedForward * 0.8f;
                    Vector3 smallCaseSpawnPos = target + projectedRight * 0.8f;
                    Vector3 shieldSpawnPos = target - projectedRight * 0.8f;

                    if (PatchController.TNHTweakerAsmIdx == -1)
                    {
                        ++debugStep;
                        if (C.Has_Weapon_Primary)
                        {
                            TNH_CharacterDef.LoadoutEntry weapon_Primary = C.Weapon_Primary;
                            int minAmmo = -1;
                            int maxAmmo = -1;
                            FVRObject weapon;
                            if (weapon_Primary.ListOverride != null && weapon_Primary.ListOverride.Count > 0)
                            {
                                weapon = weapon_Primary.ListOverride[UnityEngine.Random.Range(0, weapon_Primary.ListOverride.Count)];
                            }
                            else
                            {
                                ObjectTableDef objectTableDef = weapon_Primary.TableDefs[UnityEngine.Random.Range(0, weapon_Primary.TableDefs.Count)];
                                ObjectTable objectTable = new ObjectTable();
                                objectTable.Initialize(objectTableDef);
                                Mod.LogInfo("Initialized object table for character: " + C.name + ", objectTable.objs null?: " + (objectTable.Objs == null));
                                weapon = objectTable.GetRandomObject();
                                minAmmo = objectTableDef.MinAmmoCapacity;
                                maxAmmo = objectTableDef.MaxAmmoCapacity;
                            }
                            Mod.LogInfo("Instantiating weapon case, prefab null?: " + (M.Prefab_WeaponCaseLarge == null) + ", weapon null?: " + (weapon == null));
                            GameObject gameObject = M.SpawnWeaponCase(M.Prefab_WeaponCaseLarge, largeCaseSpawnPos, -projectedForward, weapon, weapon_Primary.Num_Mags_SL_Clips, weapon_Primary.Num_Rounds, minAmmo, maxAmmo, weapon_Primary.AmmoObjectOverride);
                            gameObject.GetComponent<TNH_WeaponCrate>().M = M;
                            gameObject.AddComponent<TimerDestroyer>();
                        }
                        ++debugStep;
                        if (C.Has_Weapon_Secondary)
                        {
                            TNH_CharacterDef.LoadoutEntry weapon_Secondary = C.Weapon_Secondary;
                            int minAmmo2 = -1;
                            int maxAmmo2 = -1;
                            FVRObject weapon2;
                            if (weapon_Secondary.ListOverride != null && weapon_Secondary.ListOverride.Count > 0)
                            {
                                weapon2 = weapon_Secondary.ListOverride[UnityEngine.Random.Range(0, weapon_Secondary.ListOverride.Count)];
                            }
                            else
                            {
                                ObjectTableDef objectTableDef2 = weapon_Secondary.TableDefs[UnityEngine.Random.Range(0, weapon_Secondary.TableDefs.Count)];
                                ObjectTable objectTable2 = new ObjectTable();
                                objectTable2.Initialize(objectTableDef2);
                                weapon2 = objectTable2.GetRandomObject();
                                minAmmo2 = objectTableDef2.MinAmmoCapacity;
                                maxAmmo2 = objectTableDef2.MaxAmmoCapacity;
                            }
                            GameObject gameObject2 = M.SpawnWeaponCase(M.Prefab_WeaponCaseSmall, smallCaseSpawnPos, -projectedRight, weapon2, weapon_Secondary.Num_Mags_SL_Clips, weapon_Secondary.Num_Rounds, minAmmo2, maxAmmo2, weapon_Secondary.AmmoObjectOverride);
                            gameObject2.GetComponent<TNH_WeaponCrate>().M = M;
                            gameObject2.AddComponent<TimerDestroyer>();
                        }
                        ++debugStep;
                        if (C.Has_Weapon_Tertiary)
                        {
                            TNH_CharacterDef.LoadoutEntry weapon_Tertiary = C.Weapon_Tertiary;
                            FVRObject fvrobject;
                            if (weapon_Tertiary.ListOverride != null && weapon_Tertiary.ListOverride.Count > 0)
                            {
                                fvrobject = weapon_Tertiary.ListOverride[UnityEngine.Random.Range(0, weapon_Tertiary.ListOverride.Count)];
                            }
                            else
                            {
                                ObjectTableDef d = weapon_Tertiary.TableDefs[UnityEngine.Random.Range(0, weapon_Tertiary.TableDefs.Count)];
                                ObjectTable objectTable3 = new ObjectTable();
                                objectTable3.Initialize(d);
                                fvrobject = objectTable3.GetRandomObject();
                            }
                            GameObject g = UnityEngine.Object.Instantiate<GameObject>(fvrobject.GetGameObject(), smallCaseSpawnPos + Vector3.up * 0.5f, UnityEngine.Random.rotation);
                            M.AddObjectToTrackedList(g);
                        }
                        ++debugStep;
                        if (C.Has_Item_Primary)
                        {
                            TNH_CharacterDef.LoadoutEntry item_Primary = C.Item_Primary;
                            FVRObject fvrobject2;
                            if (item_Primary.ListOverride != null && item_Primary.ListOverride.Count > 0)
                            {
                                fvrobject2 = item_Primary.ListOverride[UnityEngine.Random.Range(0, item_Primary.ListOverride.Count)];
                            }
                            else
                            {
                                ObjectTableDef d2 = item_Primary.TableDefs[UnityEngine.Random.Range(0, item_Primary.TableDefs.Count)];
                                ObjectTable objectTable4 = new ObjectTable();
                                objectTable4.Initialize(d2);
                                fvrobject2 = objectTable4.GetRandomObject();
                            }
                            GameObject g2 = UnityEngine.Object.Instantiate<GameObject>(fvrobject2.GetGameObject(), largeCaseSpawnPos + Vector3.up * 0.3f + Vector3.left * 0.3f, UnityEngine.Random.rotation);
                            M.AddObjectToTrackedList(g2);
                        }
                        ++debugStep;
                        if (C.Has_Item_Secondary)
                        {
                            TNH_CharacterDef.LoadoutEntry item_Secondary = C.Item_Secondary;
                            FVRObject fvrobject3;
                            if (item_Secondary.ListOverride != null && item_Secondary.ListOverride.Count > 0)
                            {
                                fvrobject3 = item_Secondary.ListOverride[UnityEngine.Random.Range(0, item_Secondary.ListOverride.Count)];
                            }
                            else
                            {
                                ObjectTableDef d3 = item_Secondary.TableDefs[UnityEngine.Random.Range(0, item_Secondary.TableDefs.Count)];
                                ObjectTable objectTable5 = new ObjectTable();
                                objectTable5.Initialize(d3);
                                fvrobject3 = objectTable5.GetRandomObject();
                            }
                            GameObject g3 = UnityEngine.Object.Instantiate<GameObject>(fvrobject3.GetGameObject(), largeCaseSpawnPos + Vector3.up * 0.3f + Vector3.right * 0.3f, UnityEngine.Random.rotation);
                            M.AddObjectToTrackedList(g3);
                        }
                        ++debugStep;
                        if (C.Has_Item_Tertiary)
                        {
                            TNH_CharacterDef.LoadoutEntry item_Tertiary = C.Item_Tertiary;
                            FVRObject fvrobject4;
                            if (item_Tertiary.ListOverride != null && item_Tertiary.ListOverride.Count > 0)
                            {
                                fvrobject4 = item_Tertiary.ListOverride[UnityEngine.Random.Range(0, item_Tertiary.ListOverride.Count)];
                            }
                            else
                            {
                                ObjectTableDef d4 = item_Tertiary.TableDefs[UnityEngine.Random.Range(0, item_Tertiary.TableDefs.Count)];
                                ObjectTable objectTable6 = new ObjectTable();
                                objectTable6.Initialize(d4);
                                fvrobject4 = objectTable6.GetRandomObject();
                            }
                            GameObject g4 = UnityEngine.Object.Instantiate<GameObject>(fvrobject4.GetGameObject(), largeCaseSpawnPos + Vector3.up * 0.3f, UnityEngine.Random.rotation);
                            M.AddObjectToTrackedList(g4);
                        }
                        ++debugStep;
                        if (C.Has_Item_Shield)
                        {
                            TNH_CharacterDef.LoadoutEntry item_Shield = C.Item_Shield;
                            FVRObject fvrobject5;
                            if (item_Shield.ListOverride != null && item_Shield.ListOverride.Count > 0)
                            {
                                fvrobject5 = item_Shield.ListOverride[UnityEngine.Random.Range(0, item_Shield.ListOverride.Count)];
                            }
                            else
                            {
                                ObjectTableDef d5 = item_Shield.TableDefs[UnityEngine.Random.Range(0, item_Shield.TableDefs.Count)];
                                ObjectTable objectTable7 = new ObjectTable();
                                objectTable7.Initialize(d5);
                                fvrobject5 = objectTable7.GetRandomObject();
                            }
                            GameObject g5 = UnityEngine.Object.Instantiate<GameObject>(fvrobject5.GetGameObject(), shieldSpawnPos, Quaternion.Euler(Vector3.up));
                            M.AddObjectToTrackedList(g5);
                        }
                        ++debugStep;
                    }
                    else // Got TNH tweaker, must spawn init equip from custom character instead
                    {
                        ++debugStep; // 2
                        Mod.LogInfo("\tSpawning init equip as TNH tweaker char", false);
                        object character = ((IDictionary)PatchController.TNHTweaker_LoadedTemplateManager_LoadedCharactersDict.GetValue(PatchController.TNHTweaker_LoadedTemplateManager))[C];

                        ++debugStep;
                        if ((bool)PatchController.TNHTweaker_CustomCharacter_HasPrimaryWeapon.GetValue(character))
                        {
                            object selectedGroup = PatchController.TNHTweaker_LoadoutEntry_PrimaryGroup.GetValue(PatchController.TNHTweaker_CustomCharacter_PrimaryWeapon.GetValue(character));
                            if (selectedGroup == null)
                            {
                                selectedGroup = PatchController.TNHTweaker_LoadoutEntry_BackupGroup.GetValue(PatchController.TNHTweaker_CustomCharacter_PrimaryWeapon.GetValue(character));
                            }

                            if (selectedGroup != null)
                            {
                                IList list = (IList)PatchController.TNHTweaker_EquipmentGroup_GetSpawnedEquipmentGroups.Invoke(selectedGroup, null);
                                selectedGroup = list[UnityEngine.Random.Range(0, list.Count)];
                                list = (IList)PatchController.TNHTweaker_EquipmentGroup_GetObjects.Invoke(selectedGroup, null);
                                FVRObject selectedItem = IM.OD[(string)list[UnityEngine.Random.Range(0, list.Count)]];
                                GameObject weaponCase = M.SpawnWeaponCase(M.Prefab_WeaponCaseLarge, largeCaseSpawnPos, -projectedForward, selectedItem, (int)PatchController.TNHTweaker_EquipmentGroup_NumMagsSpawned.GetValue(selectedGroup), (int)PatchController.TNHTweaker_EquipmentGroup_NumRoundsSpawned.GetValue(selectedGroup), (int)PatchController.TNHTweaker_EquipmentGroup_MinAmmoCapacity.GetValue(selectedGroup), (int)PatchController.TNHTweaker_EquipmentGroup_MaxAmmoCapacity.GetValue(selectedGroup));
                                weaponCase.GetComponent<TNH_WeaponCrate>().M = M;
                                weaponCase.AddComponent<TimerDestroyer>();
                            }
                        }
                        ++debugStep;

                        if ((bool)PatchController.TNHTweaker_CustomCharacter_HasSecondaryWeapon.GetValue(character))
                        {
                            object selectedGroup = PatchController.TNHTweaker_LoadoutEntry_PrimaryGroup.GetValue(PatchController.TNHTweaker_CustomCharacter_SecondaryWeapon.GetValue(character));
                            if (selectedGroup == null)
                            {
                                selectedGroup = PatchController.TNHTweaker_LoadoutEntry_BackupGroup.GetValue(PatchController.TNHTweaker_CustomCharacter_SecondaryWeapon.GetValue(character));
                            }

                            if (selectedGroup != null)
                            {
                                IList list = (IList)PatchController.TNHTweaker_EquipmentGroup_GetSpawnedEquipmentGroups.Invoke(selectedGroup, null);
                                selectedGroup = list[UnityEngine.Random.Range(0, list.Count)];
                                list = (IList)PatchController.TNHTweaker_EquipmentGroup_GetObjects.Invoke(selectedGroup, null);
                                FVRObject selectedItem = IM.OD[(string)list[UnityEngine.Random.Range(0, list.Count)]];
                                GameObject weaponCase = M.SpawnWeaponCase(M.Prefab_WeaponCaseLarge, smallCaseSpawnPos, -projectedForward, selectedItem, (int)PatchController.TNHTweaker_EquipmentGroup_NumMagsSpawned.GetValue(selectedGroup), (int)PatchController.TNHTweaker_EquipmentGroup_NumRoundsSpawned.GetValue(selectedGroup), (int)PatchController.TNHTweaker_EquipmentGroup_MinAmmoCapacity.GetValue(selectedGroup), (int)PatchController.TNHTweaker_EquipmentGroup_MaxAmmoCapacity.GetValue(selectedGroup));
                                weaponCase.GetComponent<TNH_WeaponCrate>().M = M;
                                weaponCase.AddComponent<TimerDestroyer>();
                            }
                        }

                        ++debugStep; // 5
                        if ((bool)PatchController.TNHTweaker_CustomCharacter_HasTertiaryWeapon.GetValue(character))
                        {
                            object selectedGroup = PatchController.TNHTweaker_LoadoutEntry_PrimaryGroup.GetValue(PatchController.TNHTweaker_CustomCharacter_TertiaryWeapon.GetValue(character));
                            if (selectedGroup == null)
                            {
                                selectedGroup = PatchController.TNHTweaker_LoadoutEntry_BackupGroup.GetValue(PatchController.TNHTweaker_CustomCharacter_TertiaryWeapon.GetValue(character));
                            }

                            if (selectedGroup != null)
                            {
                                // This is usually a coroutine that uses a callback to get spawnedObject
                                IList list = (IList)PatchController.TNHTweaker_EquipmentGroup_GetSpawnedEquipmentGroups.Invoke(selectedGroup, null);
                                for(int j=0; j < list.Count; ++j)
                                {
                                    int itemsToSpawn = (int)PatchController.TNHTweaker_EquipmentGroup_ItemsToSpawn.GetValue(selectedGroup);
                                    for (int i = 0; i < itemsToSpawn; i++)
                                    {
                                        FVRObject selectedFVR;
                                        IList finalList = (IList)PatchController.TNHTweaker_EquipmentGroup_GetObjects.Invoke(selectedGroup, null);
                                        string randomObject = (string)finalList[UnityEngine.Random.Range(0, finalList.Count)];
                                        if (IM.OD.TryGetValue(randomObject, out selectedFVR))
                                        {
                                            GameObject gameObject = selectedFVR.GetGameObject();

                                            GameObject spawnedObject = UnityEngine.GameObject.Instantiate(gameObject, smallCaseSpawnPos + Vector3.up * 0.5f, UnityEngine.Random.rotation);
                                            M.AddObjectToTrackedList(spawnedObject);
                                        }
                                    }
                                }
                            }
                        }

                        ++debugStep;
                        if ((bool)PatchController.TNHTweaker_CustomCharacter_HasPrimaryItem.GetValue(character))
                        {
                            object selectedGroup = PatchController.TNHTweaker_LoadoutEntry_PrimaryGroup.GetValue(PatchController.TNHTweaker_CustomCharacter_PrimaryItem.GetValue(character));
                            if (selectedGroup == null)
                            {
                                selectedGroup = PatchController.TNHTweaker_LoadoutEntry_BackupGroup.GetValue(PatchController.TNHTweaker_CustomCharacter_PrimaryItem.GetValue(character));
                            }

                            if (selectedGroup != null)
                            {
                                // This is usually a coroutine that uses a callback to get spawnedObject
                                IList list = (IList)PatchController.TNHTweaker_EquipmentGroup_GetSpawnedEquipmentGroups.Invoke(selectedGroup, null);
                                for(int j=0; j < list.Count; ++j)
                                {
                                    int itemsToSpawn = (int)PatchController.TNHTweaker_EquipmentGroup_ItemsToSpawn.GetValue(selectedGroup);
                                    for (int i = 0; i < itemsToSpawn; i++)
                                    {
                                        FVRObject selectedFVR;
                                        IList finalList = (IList)PatchController.TNHTweaker_EquipmentGroup_GetObjects.Invoke(selectedGroup, null);
                                        string randomObject = (string)finalList[UnityEngine.Random.Range(0, finalList.Count)];
                                        if (IM.OD.TryGetValue(randomObject, out selectedFVR))
                                        {
                                            GameObject gameObject = selectedFVR.GetGameObject();

                                            GameObject spawnedObject = UnityEngine.GameObject.Instantiate(gameObject, smallCaseSpawnPos + Vector3.up * 0.5f, UnityEngine.Random.rotation);
                                            M.AddObjectToTrackedList(spawnedObject);
                                        }
                                    }
                                }
                            }
                        }

                        ++debugStep;
                        if ((bool)PatchController.TNHTweaker_CustomCharacter_HasSecondaryItem.GetValue(character))
                        {
                            object selectedGroup = PatchController.TNHTweaker_LoadoutEntry_PrimaryGroup.GetValue(PatchController.TNHTweaker_CustomCharacter_SecondaryItem.GetValue(character));
                            if (selectedGroup == null)
                            {
                                selectedGroup = PatchController.TNHTweaker_LoadoutEntry_BackupGroup.GetValue(PatchController.TNHTweaker_CustomCharacter_SecondaryItem.GetValue(character));
                            }

                            if (selectedGroup != null)
                            {
                                // This is usually a coroutine that uses a callback to get spawnedObject
                                IList list = (IList)PatchController.TNHTweaker_EquipmentGroup_GetSpawnedEquipmentGroups.Invoke(selectedGroup, null);
                                for(int j=0; j < list.Count; ++j)
                                {
                                    int itemsToSpawn = (int)PatchController.TNHTweaker_EquipmentGroup_ItemsToSpawn.GetValue(selectedGroup);
                                    for (int i = 0; i < itemsToSpawn; i++)
                                    {
                                        FVRObject selectedFVR;
                                        IList finalList = (IList)PatchController.TNHTweaker_EquipmentGroup_GetObjects.Invoke(selectedGroup, null);
                                        string randomObject = (string)finalList[UnityEngine.Random.Range(0, finalList.Count)];
                                        if (IM.OD.TryGetValue(randomObject, out selectedFVR))
                                        {
                                            GameObject gameObject = selectedFVR.GetGameObject();

                                            GameObject spawnedObject = UnityEngine.GameObject.Instantiate(gameObject, smallCaseSpawnPos + Vector3.up * 0.5f, UnityEngine.Random.rotation);
                                            M.AddObjectToTrackedList(spawnedObject);
                                        }
                                    }
                                }
                            }
                        }

                        ++debugStep;
                        if ((bool)PatchController.TNHTweaker_CustomCharacter_HasTertiaryItem.GetValue(character))
                        {
                            object selectedGroup = PatchController.TNHTweaker_LoadoutEntry_PrimaryGroup.GetValue(PatchController.TNHTweaker_CustomCharacter_TertiaryItem.GetValue(character));
                            if (selectedGroup == null)
                            {
                                selectedGroup = PatchController.TNHTweaker_LoadoutEntry_BackupGroup.GetValue(PatchController.TNHTweaker_CustomCharacter_TertiaryItem.GetValue(character));
                            }

                            if (selectedGroup != null)
                            {
                                // This is usually a coroutine that uses a callback to get spawnedObject
                                IList list = (IList)PatchController.TNHTweaker_EquipmentGroup_GetSpawnedEquipmentGroups.Invoke(selectedGroup, null);
                                for(int j=0; j < list.Count; ++j)
                                {
                                    int itemsToSpawn = (int)PatchController.TNHTweaker_EquipmentGroup_ItemsToSpawn.GetValue(selectedGroup);
                                    for (int i = 0; i < itemsToSpawn; i++)
                                    {
                                        FVRObject selectedFVR;
                                        IList finalList = (IList)PatchController.TNHTweaker_EquipmentGroup_GetObjects.Invoke(selectedGroup, null);
                                        string randomObject = (string)finalList[UnityEngine.Random.Range(0, finalList.Count)];
                                        if (IM.OD.TryGetValue(randomObject, out selectedFVR))
                                        {
                                            GameObject gameObject = selectedFVR.GetGameObject();

                                            GameObject spawnedObject = UnityEngine.GameObject.Instantiate(gameObject, smallCaseSpawnPos + Vector3.up * 0.5f, UnityEngine.Random.rotation);
                                            M.AddObjectToTrackedList(spawnedObject);
                                        }
                                    }
                                }
                            }
                        }

                        ++debugStep;
                        if ((bool)PatchController.TNHTweaker_CustomCharacter_HasShield.GetValue(character))
                        {
                            object selectedGroup = PatchController.TNHTweaker_LoadoutEntry_PrimaryGroup.GetValue(PatchController.TNHTweaker_CustomCharacter_Shield.GetValue(character));
                            if (selectedGroup == null)
                            {
                                selectedGroup = PatchController.TNHTweaker_LoadoutEntry_BackupGroup.GetValue(PatchController.TNHTweaker_CustomCharacter_Shield.GetValue(character));
                            }

                            if (selectedGroup != null)
                            {
                                // This is usually a coroutine that uses a callback to get spawnedObject
                                IList list = (IList)PatchController.TNHTweaker_EquipmentGroup_GetSpawnedEquipmentGroups.Invoke(selectedGroup, null);
                                for(int j=0; j < list.Count; ++j)
                                {
                                    int itemsToSpawn = (int)PatchController.TNHTweaker_EquipmentGroup_ItemsToSpawn.GetValue(selectedGroup);
                                    for (int i = 0; i < itemsToSpawn; i++)
                                    {
                                        FVRObject selectedFVR;
                                        IList finalList = (IList)PatchController.TNHTweaker_EquipmentGroup_GetObjects.Invoke(selectedGroup, null);
                                        string randomObject = (string)finalList[UnityEngine.Random.Range(0, finalList.Count)];
                                        if (IM.OD.TryGetValue(randomObject, out selectedFVR))
                                        {
                                            GameObject gameObject = selectedFVR.GetGameObject();

                                            GameObject spawnedObject = UnityEngine.GameObject.Instantiate(gameObject, smallCaseSpawnPos + Vector3.up * 0.5f, UnityEngine.Random.rotation);
                                            M.AddObjectToTrackedList(spawnedObject);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Mod.LogError("Error spawning initial equipment at step "+debugStep+": " + ex.Message + ":\n" + ex.StackTrace);
            }
            Destroy(TNHStartEquipButton);
            TNHStartEquipButton = null;
        }

        public void OnTNHInstanceReceived(TNHInstance instance)
        {
            if (setLatestInstance)
            {
                setLatestInstance = false;

                SetTNHInstance(instance);

                if (spectatorHostWaitingForTNHInstance)
                {
                    ClientSend.TNHSpectatorHostReady(GameManager.instance);
                    spectatorHostWaitingForTNHInstance = false;
                }
            }
        }

        public void OnInstanceReceived(int instance)
        {
            if (setLatestInstance)
            {
                setLatestInstance = false;

                GameManager.SetInstance(instance);
            }
        }

        private static bool SetTNHInstance(TNHInstance instance)
        {
            if (currentTNHUIManager != null)
            {
                currentTNHUIManager.SetOBS_Progression(instance.progressionTypeSetting);
                currentTNHUIManager.SetOBS_EquipmentMode(instance.equipmentModeSetting);
                currentTNHUIManager.SetOBS_TargetMode(instance.targetModeSetting);
                currentTNHUIManager.SetOBS_HealthMode(instance.healthModeSetting);
                currentTNHUIManager.SetOBS_RunSeed(instance.TNHSeed);
                currentTNHUIManager.SetOBS_AIDifficulty(instance.AIDifficultyModifier);
                currentTNHUIManager.SetOBS_AIRadarMode(instance.radarModeModifier);
                currentTNHUIManager.SetOBS_HealthMult(instance.healthMult);
                currentTNHUIManager.SetOBS_ItemSpawner(instance.itemSpawnerMode);
                currentTNHUIManager.SetOBS_Backpack(instance.backpackMode);
                currentTNHUIManager.SetOBS_SosiggunShakeReloading(instance.sosiggunShakeReloading);

                // Find level
                bool found = false;
                for (int i = 0; i < Mod.currentTNHUIManager.Levels.Count; ++i)
                {
                    if (Mod.currentTNHUIManager.Levels[i].LevelID.Equals(instance.levelID))
                    {
                        found = true;

                        Mod.currentTNHUIManager.m_currentLevelIndex = i;
                        Mod.currentTNHUIManager.CurLevelID = instance.levelID;
                        Mod.currentTNHUIManager.UpdateLevelSelectDisplayAndLoader();
                        Mod.currentTNHUIManager.UpdateTableBasedOnOptions();
                        Mod.currentTNHUIManager.PlayButtonSound(2);
                        Mod.currentTNHSceneLoader.gameObject.SetActive(true);
                        break;
                    }
                }
                if (!found)
                {
                    Mod.currentTNHSceneLoader.gameObject.SetActive(false);
                    Mod.LogError("Missing TNH level: " + instance.levelID + "! Make sure you have it installed.");
                }
            }

            GameManager.SetInstance(instance.instance);

            TNHInstanceTitle.text = "Instance " + instance.instance;

            if (currentTNHInstancePlayers == null)
            {
                currentTNHInstancePlayers = new Dictionary<int, GameObject>();
            }
            else
            {
                foreach (KeyValuePair<int, GameObject> entry in currentTNHInstancePlayers)
                {
                    Destroy(entry.Value);
                }
            }
            currentTNHInstancePlayers.Clear();

            // Populate player list
            for (int i = 0; i < instance.playerIDs.Count; ++i)
            {
                GameObject newPlayer = Instantiate<GameObject>(TNHPlayerPrefab, TNHPlayerList.transform);
                if (GameManager.players.ContainsKey(instance.playerIDs[i]))
                {
                    newPlayer.transform.GetChild(0).GetComponent<Text>().text = GameManager.players[instance.playerIDs[i]].username + (i == 0 ? " (Host)" : "");
                }
                else
                {
                    newPlayer.transform.GetChild(0).GetComponent<Text>().text = config["Username"].ToString() + (i == 0 ? " (Host)" : "");
                }
                newPlayer.SetActive(true);

                currentTNHInstancePlayers.Add(instance.playerIDs[i], newPlayer);
            }

            currentTNHInstance = instance;

            return true;
        }

        public static void CreateManagerObject(bool host = false)
        {
            if (managerObject == null)
            {
                managerObject = new GameObject();

                ThreadManager threadManager = managerObject.AddComponent<ThreadManager>();
                ThreadManager.host = host;

                GameManager gameManager = managerObject.AddComponent<GameManager>();

                DontDestroyOnLoad(managerObject);

                DDOLScene = managerObject.scene;
            }
        }

        private void OnSceneLoaded(Scene loadedScene, LoadSceneMode loadedSceneMode)
        {
            if (loadedScene.name.Equals("TakeAndHold_Lobby_2"))
            {
                Logger.LogInfo("TNH lobby loaded, initializing H3MP menu if possible");
                if (managerObject != null)
                {
                    InitTNHMenu();
                }
            }
        }

        public static void DropAllItems()
        {
            if (GM.CurrentMovementManager.Hands[0].CurrentInteractable != null)
            {
                GM.CurrentMovementManager.Hands[0].ForceSetInteractable(null);
            }
            if (GM.CurrentMovementManager.Hands[1].CurrentInteractable != null)
            {
                GM.CurrentMovementManager.Hands[1].ForceSetInteractable(null);
            }
            foreach (FVRQuickBeltSlot slot in GM.CurrentPlayerBody.QuickbeltSlots)
            {
                if (slot.CurObject != null)
                {
                    slot.CurObject.ClearQuickbeltState();
                }
            }
            foreach (FVRQuickBeltSlot slot in GM.CurrentPlayerBody.QBSlots_Internal)
            {
                if (slot.CurObject != null)
                {
                    slot.CurObject.ClearQuickbeltState();
                }
            }
            foreach (FVRQuickBeltSlot slot in GM.CurrentPlayerBody.QBSlots_Added)
            {
                if (slot.CurObject != null)
                {
                    slot.CurObject.ClearQuickbeltState();
                }
            }
        }

        public static int GetBestPotentialObjectHost(int currentController, bool forUs = true, bool hasWhiteList = false, List<int> whiteList = null, string sceneOverride = null, int instanceOverride = -1)
        {
            if(hasWhiteList && whiteList == null)
            {
                whiteList = new List<int>();
            }

            int bestPotentialObjectHost = -1;
            if (OnGetBestPotentialObjectHost != null)
            {
                OnGetBestPotentialObjectHost(currentController, forUs, hasWhiteList, whiteList, sceneOverride, instanceOverride, ref bestPotentialObjectHost);
            }
            if(bestPotentialObjectHost != -1)
            {
                return bestPotentialObjectHost;
            }

            if (forUs)
            {
                if (Mod.currentTNHInstance != null)
                {
                    if (currentController == -1) // This means the potential host could also be us
                    {
                        // Going through each like this, we will go through the host of the instance before any other
                        foreach (int playerID in Mod.currentTNHInstance.playerIDs)
                        {
                            if (Mod.currentTNHInstance.currentlyPlaying.Contains(playerID) &&
                                (!hasWhiteList || whiteList.Contains(playerID)))
                            {
                                return playerID;
                            }
                        }
                    }
                    else
                    {
                        // Going through each like this, we will go through the host of the instance before any other
                        foreach (int playerID in Mod.currentTNHInstance.playerIDs)
                        {
                            if (playerID != currentController && 
                                Mod.currentTNHInstance.currentlyPlaying.Contains(playerID) &&
                                (!hasWhiteList || whiteList.Contains(playerID)))
                            {
                                return playerID;
                            }
                        }
                    }
                }
                else
                {
                    string sceneToUse = sceneOverride == null ? (GameManager.sceneLoading ? GameManager.sceneAtSceneLoadStart : GameManager.scene) : sceneOverride;
                    int instanceToUse = instanceOverride == -1 ? (GameManager.sceneLoading ? GameManager.instanceAtSceneLoadStart : GameManager.instance) : instanceOverride;
                    if (currentController == -1) // This means the potential host could also be us
                    {
                        if (GameManager.playersByInstanceByScene.TryGetValue(sceneToUse, out Dictionary<int, List<int>> instances) &&
                            instances.TryGetValue(instanceToUse, out List<int> otherPlayers))
                        {
                            if (otherPlayers.Count == 0)
                            {
                                return GameManager.ID;
                            }
                            else
                            {
                                bool found = false;
                                int smallest = int.MaxValue;
                                for (int i = 0; i < otherPlayers.Count; ++i)
                                {
                                    if (otherPlayers[i] < smallest &&
                                        (!hasWhiteList || whiteList.Contains(otherPlayers[i])))
                                    {
                                        found = true;
                                        smallest = otherPlayers[i];
                                    }
                                }
                                if (GameManager.ID < smallest &&
                                    (!hasWhiteList || whiteList.Contains(GameManager.ID)))
                                {
                                    found = true;
                                    smallest = GameManager.ID;
                                }
                                return found ? smallest : -1;
                            }
                        }
                    }
                    else
                    {
                        if (GameManager.playersByInstanceByScene.TryGetValue(sceneToUse, out Dictionary<int, List<int>> instances) &&
                            instances.TryGetValue(instanceToUse, out List<int> otherPlayers) && otherPlayers.Count > 0)
                        {
                            // Just take first one
                            for(int i=0; i < otherPlayers.Count; ++i)
                            {
                                if(!hasWhiteList || whiteList.Contains(otherPlayers[i]))
                                {
                                    return otherPlayers[i];
                                }
                            }
                        }
                    }
                }
            }
            else if(ThreadManager.host) // Not for us, this was called to find a best potential host for something controlled by someone else
            {
                string scene = Server.clients[currentController].player.scene;
                int instance = Server.clients[currentController].player.instance;

                if(scene.Equals(GameManager.sceneLoading ? LoadLevelBeginPatch.loadingLevel : GameManager.scene) && instance == GameManager.instance && (!hasWhiteList || whiteList.Contains(0)))
                {
                    return 0;
                }

                if (GameManager.playersByInstanceByScene.TryGetValue(scene, out Dictionary<int, List<int>> instances) &&
                    instances.TryGetValue(instance, out List<int> otherPlayers) && otherPlayers.Count > 0)
                {
                    bool found = false;
                    int smallest = int.MaxValue;
                    for (int i = 0; i < otherPlayers.Count; ++i)
                    {
                        if (otherPlayers[i] < smallest &&
                            (!hasWhiteList || whiteList.Contains(otherPlayers[i])))
                        {
                            found = true;
                            smallest = otherPlayers[i];
                        }
                    }
                    return found ? smallest : -1;
                }
            }
            return -1;
        }

        public static void SetKinematicRecursive(Transform root, bool value)
        {
            // TODO: Review: We make the assumption that a rigidbody that is not active does not need to be managed
            //               This is because of UberShatterable shards being inactive until the core is shattered
            //               If we managed the shards' rigidbodies we would have set them as kinematic if not controlled
            //               and once activated on shatter, that would remain because the game doesn't set kinematic or recover a rigidbody
            //               to make them physical, it just enables them. By only managing active objects, we won't set them as kinematic
            //               even if uncontrolled, then, once shattered, they will be activ as the game intends.
            //               Will need to review to make sure this doesn't interfere with another process.
            if (root.gameObject.activeSelf)
            {
                Rigidbody rb = root.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    KinematicMarker marker = rb.GetComponent<KinematicMarker>();

                    // If we want to make kinematic we can just set it and mark
                    if (value)
                    {
                        if (marker == null)
                        {
                            marker = rb.gameObject.AddComponent<KinematicMarker>();
                        }
                        ++KinematicPatch.skip;
                        rb.isKinematic = value;
                        --KinematicPatch.skip;
                    }
                    else // If we don't want it kinematic, we only want to unset it on marked children, because unmarked were not set by us
                    {
                        // For example, a piece of an item that is not a tracked item itself but is a child of a tracked item
                        // got set to kinematic by the game, will have its marker destroyed so we don't set it to non kinematic
                        if (marker != null)
                        {
                            ++KinematicPatch.skip;
                            rb.isKinematic = value;
                            --KinematicPatch.skip;
                            Destroy(marker);
                        }
                    }
                }
            }

            foreach (Transform child in root.transform)
            {
                SetKinematicRecursive(child, value);
            }
        }

        public static void RemovePlayerFromLists(int playerID)
        {
            PlayerManager player = GameManager.players[playerID];

            // Manage instance
            if (GameManager.activeInstances.ContainsKey(player.instance))
            {
                --GameManager.activeInstances[player.instance];
                if (GameManager.activeInstances[player.instance] == 0)
                {
                    GameManager.activeInstances.Remove(player.instance);
                }
            }

            // Manage scene/instance
            GameManager.playersByInstanceByScene[player.scene][player.instance].Remove(player.ID);
            if (GameManager.playersByInstanceByScene[player.scene][player.instance].Count == 0)
            {
                GameManager.playersByInstanceByScene[player.scene].Remove(player.instance);
            }
            if(GameManager.playersByInstanceByScene[player.scene].Count == 0)
            {
                GameManager.playersByInstanceByScene.Remove(player.scene);
            }

            // Manage players present
            if (player.scene.Equals(GameManager.scene) && !GameManager.nonSynchronizedScenes.ContainsKey(player.scene) && GameManager.instance == player.instance)
            {
                GameManager.playersPresent.Remove(playerID);
            }

            RemovePlayerFromSpecificLists(player);

            GameObject.Destroy(GameManager.players[playerID].gameObject);
            GameManager.players.Remove(playerID);

            if(OnPlayerRemoved != null)
            {
                OnPlayerRemoved(player);
            }
        }

        public static void RemovePlayerFromSpecificLists(PlayerManager player)
        {
            if (GameManager.TNHInstances.TryGetValue(player.instance, out TNHInstance currentInstance))
            {
                int preHost = currentInstance.playerIDs[0];
                currentInstance.playerIDs.Remove(player.ID);
                if (currentInstance.playerIDs.Count == 0)
                {
                    GameManager.TNHInstances.Remove(player.instance);

                    if (Mod.TNHInstanceList != null && Mod.joinTNHInstances.ContainsKey(player.instance))
                    {
                        GameObject.Destroy(Mod.joinTNHInstances[player.instance]);
                        Mod.joinTNHInstances.Remove(player.instance);
                    }

                    if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == player.instance)
                    {
                        Mod.TNHSpectating = false;
                        if (GM.CurrentPlayerBody != null && GM.CurrentPlayerBody.RightHand != null && GM.CurrentPlayerBody.LeftHand != null)
                        {
                            GM.CurrentPlayerBody.EnableHands();
                        }
                    }
                }
                else
                {
                    // Remove player from active TNH player list
                    if (Mod.TNHMenu != null && Mod.TNHPlayerList != null && Mod.TNHPlayerPrefab != null &&
                        Mod.currentTNHInstancePlayers != null && Mod.currentTNHInstancePlayers.ContainsKey(player.ID))
                    {
                        Destroy(Mod.currentTNHInstancePlayers[player.ID]);
                        Mod.currentTNHInstancePlayers.Remove(player.ID);

                        // Switch host if necessary
                        if (preHost != currentInstance.playerIDs[0])
                        {
                            Mod.currentTNHInstancePlayers[currentInstance.playerIDs[0]].transform.GetChild(0).GetComponent<Text>().text += " (Host)";
                        }
                    }

                    // Remove from other active lists
                    currentInstance.currentlyPlaying.Remove(player.ID);
                    currentInstance.played.Remove(player.ID);
                    currentInstance.dead.Remove(player.ID);
                }
            }

            if (OnRemovePlayerFromSpecificLists != null)
            {
                OnRemovePlayerFromSpecificLists(player);
            }
        }

        public static void LogInfo(string message, bool debug = true)
        {
            if (debug)
            {
#if DEBUG
                modInstance.Logger.LogInfo(message);
#endif
            }
            else
            {
                modInstance.Logger.LogInfo(message);
            }
        }

        public static void LogWarning(string message)
        {
            modInstance.Logger.LogWarning(message);
        }

        public static void LogError(string message)
        {
            modInstance.Logger.LogError(message);
        }
    }
}