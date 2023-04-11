using FistVR;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP
{
    public class H3MP_TrackedItem : MonoBehaviour
    {
        public static float interpolationSpeed = 12f;
        public static bool interpolated = true;

        public H3MP_TrackedItemData data;
        public bool awoken;
        public bool sendOnAwake;

        // Unknown tracked ID queues
        public static Dictionary<uint, KeyValuePair<uint, bool>> unknownTrackedIDs = new Dictionary<uint, KeyValuePair<uint, bool>>();
        public static Dictionary<uint, List<uint>> unknownParentWaitList = new Dictionary<uint, List<uint>>();
        public static Dictionary<uint, List<int>> unknownParentTrackedIDs = new Dictionary<uint, List<int>>();
        public static Dictionary<uint, int> unknownControlTrackedIDs = new Dictionary<uint, int>();
        public static List<uint> unknownDestroyTrackedIDs = new List<uint>();
        public static Dictionary<uint, byte> unknownCrateHolding = new Dictionary<uint, byte>();
        public static Dictionary<SosigWeapon, KeyValuePair<H3MP_TrackedSosigData, int>> unknownSosigInventoryObjects = new Dictionary<SosigWeapon, KeyValuePair<H3MP_TrackedSosigData, int>>();
        public static Dictionary<uint, KeyValuePair<H3MP_TrackedSosigData, int>> unknownSosigInventoryItems = new Dictionary<uint, KeyValuePair<H3MP_TrackedSosigData, int>>();

        // Update
        public delegate bool UpdateData(); // The updateFunc and updateGivenFunc should return a bool indicating whether data has been modified
        public delegate bool UpdateDataWithGiven(byte[] newData);
        public delegate bool FireFirearm(int chamberIndex);
        public delegate void FirearmUpdateOverrideSetter(FireArmRoundType roundType, FireArmRoundClass roundClass, int chamberIndex);
        public delegate bool FireSosigGun(float recoilMult);
        public delegate void FireAttachableFirearm(bool firedFromInterface);
        public delegate void FireAttachableFirearmChamberRound(FireArmRoundType roundType, FireArmRoundClass roundClass);
        public delegate FVRFireArmChamber FireAttachableFirearmGetChamber();
        public delegate void UpdateParent();
        public delegate void UpdateAttachmentInterface(FVRFireArmAttachment att, ref bool modified);
        public delegate void UpdateAttachmentInterfaceWithGiven(FVRFireArmAttachment att, byte[] newData, ref bool modified);
        public delegate int GetChamberIndex(FVRFireArmChamber chamber);
        public delegate void ChamberRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex);
        public UpdateData updateFunc; // Update the item's data based on its physical state since we are the controller
        public UpdateDataWithGiven updateGivenFunc; // Update the item's data and state based on data provided by another client
        public FireFirearm fireFunc; // Fires the corresponding firearm type
        public FirearmUpdateOverrideSetter setFirearmUpdateOverride; // Set fire update override data
        public FireAttachableFirearm attachableFirearmFireFunc; // Fires the corresponding attachable firearm type
        public FireAttachableFirearmChamberRound attachableFirearmChamberRoundFunc; // Loads the chamber of the attachable firearm with round of class
        public FireAttachableFirearmGetChamber attachableFirearmGetChamberFunc; // Returns the chamber of the corresponding attachable firearm
        public FireSosigGun sosigWeaponfireFunc; // Fires the corresponding sosig weapon
        public UpdateParent updateParentFunc; // Update the item's state depending on current parent
        public UpdateAttachmentInterface attachmentInterfaceUpdateFunc; // Update the attachment's attachment interface
        public UpdateAttachmentInterfaceWithGiven attachmentInterfaceUpdateGivenFunc; // Update the attachment's attachment interface
        public GetChamberIndex getChamberIndex; // Get the index of the given chamber on the item
        public ChamberRound chamberRound; // Set round of chamber at gicen index of this item
        public byte currentMountIndex = 255; // Used by attachment, TODO: This limits number of mounts to 255, if necessary could make index into a short
        public UnityEngine.Object dataObject;
        public FVRPhysicalObject physicalObject;
        public int attachmentInterfaceDataSize;

        // StingerLauncher specific
        public StingerMissile stingerMissile;

        // TrackedItemReferences array
        // Used by certain item subtypes who need to get access to their TrackedItem very often (On Update for example)
        // This is used to bypass having to find the item in a datastructure too often
        public static GameObject[] trackedItemRefObjects = new GameObject[100];
        public static H3MP_TrackedItem[] trackedItemReferences = new H3MP_TrackedItem[100];
        public static List<int> availableTrackedItemRefIndices = new List<int>() {  1,2,3,4,5,6,7,8,9,
                                                                                    10,11,12,13,14,15,16,17,18,19,
                                                                                    20,21,22,23,24,25,26,27,28,29,
                                                                                    30,31,32,33,34,35,36,37,38,39,
                                                                                    40,41,42,43,44,45,46,47,48,49,
                                                                                    50,51,52,53,54,55,56,57,58,59,
                                                                                    60,61,62,63,64,65,66,67,68,69,
                                                                                    70,71,72,73,74,75,76,77,78,79,
                                                                                    80,81,82,83,84,85,86,87,88,89,
                                                                                    90,91,92,93,94,95,96,97,98,99};

        public bool sendDestroy = true; // To prevent feeback loops
        public bool skipDestroyProcessing;
        public bool skipFullDestroy;
        public bool dontGiveControl;

        private void Awake()
        {
            InitItemType();

            if(updateFunc != null && data != null)
            {
                updateFunc();
            }

            awoken = true;
            if (sendOnAwake)
            {
                Mod.LogInfo(gameObject.name + " awoken");
                if (H3MP_ThreadManager.host)
                {
                    // This will also send a packet with the item to be added in the client's global item list
                    H3MP_Server.AddTrackedItem(data, 0);
                }
                else
                {
                    // Tell the server we need to add this item to global tracked items
                    data.localWaitingIndex = H3MP_Client.localItemCounter++;
                    H3MP_Client.waitingLocalItems.Add(data.localWaitingIndex, data);
                    H3MP_ClientSend.TrackedItem(data);
                }
            }
        }

        // MOD: This will check which type this item is so we can keep track of its data more efficiently
        //      A mod with a custom item type which has custom data should postfix this to check if this item is of custom type
        //      to keep a ref to the object itself and set delegate update functions
        private void InitItemType()
        {
            FVRPhysicalObject physObj = GetComponent<FVRPhysicalObject>();

            // For each relevant type for which we may want to store additional data, we set a specific update function and the object ref
            // NOTE: We want to handle a subtype before its parent type (ex.: sblpCell before FVRFireArmMagazine) 
            // TODO: Optimization: Maybe instead of having a big if statement like this, put all of them in a dictionnary for faster lookup
            if (physObj is sblpCell)
            {
                updateFunc = UpdateSBLPCell;
                updateGivenFunc = UpdateGivenSBLPCell;
                dataObject = physObj as sblpCell;
            }
            else if (physObj is FVRFireArmMagazine)
            {
                updateFunc = UpdateMagazine;
                updateGivenFunc = UpdateGivenMagazine;
                dataObject = physObj as FVRFireArmMagazine;
            }
            else if(physObj is FVRFireArmClip)
            {
                updateFunc = UpdateClip;
                updateGivenFunc = UpdateGivenClip;
                dataObject = physObj as FVRFireArmClip;
            }
            else if(physObj is Speedloader)
            {
                updateFunc = UpdateSpeedloader;
                updateGivenFunc = UpdateGivenSpeedloader;
                dataObject = physObj as Speedloader;
            }
            else if (physObj is ClosedBoltWeapon)
            {
                ClosedBoltWeapon asCBW = (ClosedBoltWeapon)physObj;
                updateFunc = UpdateClosedBoltWeapon;
                updateGivenFunc = UpdateGivenClosedBoltWeapon;
                dataObject = asCBW;
                fireFunc = FireCBW;
                setFirearmUpdateOverride = SetCBWUpdateOverride;
                getChamberIndex = GetFirstChamberIndex;
                chamberRound = ChamberClosedBoltWeaponRound;
            }
            else if (physObj is OpenBoltReceiver)
            {
                OpenBoltReceiver asOBR = (OpenBoltReceiver)physObj;
                updateFunc = UpdateOpenBoltReceiver;
                updateGivenFunc = UpdateGivenOpenBoltReceiver;
                dataObject = asOBR;
                fireFunc = FireOBR;
                setFirearmUpdateOverride = SetOBRUpdateOverride;
                getChamberIndex = GetFirstChamberIndex;
                chamberRound = ChamberOpenBoltReceiverRound;
            }
            else if (physObj is BoltActionRifle)
            {
                BoltActionRifle asBAR = (BoltActionRifle)physObj;
                updateFunc = UpdateBoltActionRifle;
                updateGivenFunc = UpdateGivenBoltActionRifle;
                dataObject = asBAR;
                fireFunc = FireBAR;
                setFirearmUpdateOverride = SetBARUpdateOverride;
                getChamberIndex = GetFirstChamberIndex;
                chamberRound = ChamberBoltActionRifleRound;
            }
            else if (physObj is Handgun)
            {
                Handgun asHandgun = (Handgun)physObj;
                updateFunc = UpdateHandgun;
                updateGivenFunc = UpdateGivenHandgun;
                dataObject = asHandgun;
                fireFunc = FireHandgun;
                setFirearmUpdateOverride = SetHandgunUpdateOverride;
                getChamberIndex = GetFirstChamberIndex;
                chamberRound = ChamberHandgunRound;
            }
            else if (physObj is TubeFedShotgun)
            {
                TubeFedShotgun asTFS = (TubeFedShotgun)physObj;
                updateFunc = UpdateTubeFedShotgun;
                updateGivenFunc = UpdateGivenTubeFedShotgun;
                dataObject = asTFS;
                fireFunc = FireTFS;
                setFirearmUpdateOverride = SetTFSUpdateOverride;
                getChamberIndex = GetFirstChamberIndex;
                chamberRound = ChamberTubeFedShotgunRound;
            }
            else if (physObj is Revolver)
            {
                Revolver asRevolver = (Revolver)physObj;
                updateFunc = UpdateRevolver;
                updateGivenFunc = UpdateGivenRevolver;
                dataObject = asRevolver; 
                getChamberIndex = GetRevolverChamberIndex;
                chamberRound = ChamberRevolverRound;
            }
            else if (physObj is SingleActionRevolver)
            {
                SingleActionRevolver asSAR = (SingleActionRevolver)physObj;
                updateFunc = UpdateSingleActionRevolver;
                updateGivenFunc = UpdateGivenSingleActionRevolver;
                dataObject = asSAR;
                getChamberIndex = GetSingleActionRevolverChamberIndex;
                chamberRound = ChamberSingleActionRevolverRound;
            }
            else if (physObj is RevolvingShotgun)
            {
                RevolvingShotgun asRS = (RevolvingShotgun)physObj;
                updateFunc = UpdateRevolvingShotgun;
                updateGivenFunc = UpdateGivenRevolvingShotgun;
                dataObject = asRS;
                getChamberIndex = GetRevolvingShotgunChamberIndex;
                chamberRound = ChamberRevolvingShotgunRound;
            }
            else if (physObj is BAP)
            {
                BAP asBAP = (BAP)physObj;
                updateFunc = UpdateBAP;
                updateGivenFunc = UpdateGivenBAP;
                dataObject = asBAP;
                fireFunc = FireBAP;
                setFirearmUpdateOverride = SetBAPUpdateOverride;
                getChamberIndex = GetFirstChamberIndex;
                chamberRound = ChamberBAPRound;
            }
            else if (physObj is BreakActionWeapon)
            {
                updateFunc = UpdateBreakActionWeapon;
                updateGivenFunc = UpdateGivenBreakActionWeapon;
                dataObject = physObj as BreakActionWeapon;
                getChamberIndex = GetBreakActionWeaponChamberIndex;
                chamberRound = ChamberBreakActionWeaponRound;
            }
            else if (physObj is LeverActionFirearm)
            {
                LeverActionFirearm LAF = (LeverActionFirearm)physObj;
                updateFunc = UpdateLeverActionFirearm;
                updateGivenFunc = UpdateGivenLeverActionFirearm;
                dataObject = LAF;
                getChamberIndex = GetLeverActionFirearmChamberIndex;
                chamberRound = ChamberLeverActionFirearmRound;
            }
            else if (physObj is CarlGustaf)
            {
                CarlGustaf asCG = (CarlGustaf)physObj;
                updateFunc = UpdateCarlGustaf;
                updateGivenFunc = UpdateGivenCarlGustaf;
                dataObject = asCG;
                fireFunc = FireCarlGustaf;
                setFirearmUpdateOverride = SetCarlGustafUpdateOverride;
                getChamberIndex = GetFirstChamberIndex;
                chamberRound = ChamberCarlGustafRound;
            }
            else if (physObj is Molotov)
            {
                Molotov asMolotov = (Molotov)physObj;
                updateFunc = UpdateMolotov;
                updateGivenFunc = UpdateGivenMolotov;
                dataObject = asMolotov;
            }
            else if(physObj is PinnedGrenade)
            {
                PinnedGrenade asPG = (PinnedGrenade)physObj;
                updateFunc = UpdatePinnedGrenade;
                updateGivenFunc = UpdateGivenPinnedGrenade;
                dataObject = asPG;
                if (asPG.SpawnOnSplode == null)
                {
                    asPG.SpawnOnSplode = new List<GameObject>();
                }
                GameObject trackedItemRef = new GameObject();
                H3MP_TrackedItemReference refScript = trackedItemRef.AddComponent<H3MP_TrackedItemReference>();
                trackedItemRef.SetActive(false);
                if (availableTrackedItemRefIndices.Count == 0)
                {
                    GameObject[] tempRefs = trackedItemRefObjects;
                    trackedItemRefObjects = new GameObject[tempRefs.Length + 100];
                    for (int i = 0; i < tempRefs.Length; ++i)
                    {
                        trackedItemRefObjects[i] = tempRefs[i];
                    }
                    H3MP_TrackedItem[] tempItems = trackedItemReferences;
                    trackedItemReferences = new H3MP_TrackedItem[tempItems.Length + 100];
                    for (int i = 0; i < tempItems.Length; ++i)
                    {
                        trackedItemReferences[i] = tempItems[i];
                    }
                    for (int i = tempItems.Length; i < trackedItemReferences.Length; ++i)
                    {
                        availableTrackedItemRefIndices.Add(i);
                    }
                }
                int refIndex = availableTrackedItemRefIndices[availableTrackedItemRefIndices.Count - 1];
                availableTrackedItemRefIndices.RemoveAt(availableTrackedItemRefIndices.Count - 1);
                trackedItemRefObjects[refIndex] = trackedItemRef;
                trackedItemReferences[refIndex] = this;
                trackedItemRef.name = refIndex.ToString();
                refScript.refIndex = refIndex;
                asPG.SpawnOnSplode.Add(trackedItemRef);
            }
            else if(physObj is FVRGrenade)
            {
                FVRGrenade asGrenade = (FVRGrenade)physObj;
                updateFunc = UpdateGrenade;
                updateGivenFunc = UpdateGivenGrenade;
                dataObject = asGrenade;
                Dictionary<int, float> timings = Mod.FVRGrenade_FuseTimings.GetValue(asGrenade) as Dictionary<int, float>;
                if (timings == null)
                {
                    timings = new Dictionary<int, float>();
                    Mod.FVRGrenade_FuseTimings.SetValue(asGrenade, timings);
                }
                if (availableTrackedItemRefIndices.Count == 0)
                {
                    GameObject[] tempRefs = trackedItemRefObjects;
                    trackedItemRefObjects = new GameObject[tempRefs.Length + 100];
                    for (int i = 0; i < tempRefs.Length; ++i)
                    {
                        trackedItemRefObjects[i] = tempRefs[i];
                    }
                    H3MP_TrackedItem[] tempItems = trackedItemReferences;
                    trackedItemReferences = new H3MP_TrackedItem[tempItems.Length + 100];
                    for (int i = 0; i < tempItems.Length; ++i)
                    {
                        trackedItemReferences[i] = tempItems[i];
                    }
                    for (int i = tempItems.Length; i < trackedItemReferences.Length; ++i)
                    {
                        availableTrackedItemRefIndices.Add(i);
                    }
                }
                int refIndex = availableTrackedItemRefIndices[availableTrackedItemRefIndices.Count - 1];
                availableTrackedItemRefIndices.RemoveAt(availableTrackedItemRefIndices.Count - 1);
                timings.Add(-1, refIndex);
                trackedItemReferences[refIndex] = this;

                // Just make an object that will handle readding ref index to available list when it gets destroyed by scene change
                GameObject trackedItemRef = new GameObject("GrenadeRefIndex");
                H3MP_TrackedItemReference refScript = trackedItemRef.AddComponent<H3MP_TrackedItemReference>();
                refScript.refIndex = refIndex;
                trackedItemRef.SetActive(false);
            }
            else if(physObj is C4)
            {
                C4 asC4 = (C4)physObj;
                updateFunc = UpdateC4;
                updateGivenFunc = UpdateGivenC4;
                dataObject = asC4;
            }
            else if(physObj is ClaymoreMine)
            {
                ClaymoreMine asCM = (ClaymoreMine)physObj;
                updateFunc = UpdateClaymoreMine;
                updateGivenFunc = UpdateGivenClaymoreMine;
                dataObject = asCM;
            }
            else if(physObj is SLAM)
            {
                SLAM asSLAM = (SLAM)physObj;
                updateFunc = UpdateSLAM;
                updateGivenFunc = UpdateGivenSLAM;
                dataObject = asSLAM;
            }
            else if (physObj is Derringer)
            {
                updateFunc = UpdateDerringer;
                updateGivenFunc = UpdateGivenDerringer;
                dataObject = physObj as Derringer;
                getChamberIndex = GetDerringerChamberIndex;
            }
            else if (physObj is FlameThrower)
            {
                updateFunc = UpdateFlameThrower;
                updateGivenFunc = UpdateGivenFlameThrower;
                dataObject = physObj as FlameThrower;
            }
            else if (physObj is sblp)
            {
                updateFunc = UpdateLaserGun;
                updateGivenFunc = UpdateGivenLaserGun;
                dataObject = physObj as sblp;
            }
            else if (physObj is Flaregun)
            {
                Flaregun asFG = physObj as Flaregun;
                updateFunc = UpdateFlaregun;
                updateGivenFunc = UpdateGivenFlaregun;
                dataObject = asFG;
                fireFunc = FireFlaregun;
                setFirearmUpdateOverride = SetFlaregunUpdateOverride;
                getChamberIndex = GetFirstChamberIndex;
            }
            else if (physObj is Airgun)
            {
                Airgun asAG = (Airgun)physObj;
                updateFunc = UpdateAirgun;
                updateGivenFunc = UpdateGivenAirgun;
                dataObject = asAG;
                fireFunc = FireAirgun;
                setFirearmUpdateOverride = SetAirgunUpdateOverride;
                getChamberIndex = GetFirstChamberIndex;
            }
            else if (physObj is FlintlockWeapon)
            {
                updateFunc = UpdateFlintlockWeapon;
                updateGivenFunc = UpdateGivenFlintlockWeapon;
                dataObject = physObj.GetComponentInChildren<FlintlockBarrel>();
            }
            else if (physObj is GBeamer)
            {
                updateFunc = UpdateGBeamer;
                updateGivenFunc = UpdateGivenGBeamer;
                dataObject = physObj as GBeamer;
            }
            else if (physObj is GrappleGun)
            {
                updateFunc = UpdateGrappleGun;
                updateGivenFunc = UpdateGivenGrappleGun;
                dataObject = physObj as GrappleGun;
                getChamberIndex = GetGrappleGunChamberIndex;
            }
            else if (physObj is HCB)
            {
                updateFunc = UpdateHCB;
                updateGivenFunc = UpdateGivenHCB;
                dataObject = physObj as HCB;
                getChamberIndex = GetFirstChamberIndex;
            }
            else if (physObj is M72)
            {
                M72 asM72 = physObj as M72;
                updateFunc = UpdateM72;
                updateGivenFunc = UpdateGivenM72;
                dataObject = asM72;
                fireFunc = FireM72;
                setFirearmUpdateOverride = SetM72UpdateOverride;
                getChamberIndex = GetFirstChamberIndex;
            }
            else if (physObj is Minigun)
            {
                updateFunc = UpdateMinigun;
                updateGivenFunc = UpdateGivenMinigun;
                dataObject = physObj as Minigun;
            }
            else if (physObj is PotatoGun)
            {
                PotatoGun asPG = physObj as PotatoGun;
                updateFunc = UpdatePotatoGun;
                updateGivenFunc = UpdateGivenPotatoGun;
                dataObject = asPG;
                fireFunc = FirePotatoGun;
                setFirearmUpdateOverride = SetPotatoGunUpdateOverride;
                getChamberIndex = GetFirstChamberIndex;
            }
            else if (physObj is RemoteMissileLauncher)
            {
                RemoteMissileLauncher asRML = physObj as RemoteMissileLauncher;
                updateFunc = UpdateRemoteMissileLauncher;
                updateGivenFunc = UpdateGivenRemoteMissileLauncher;
                dataObject = asRML;
                fireFunc = FireRemoteMissileLauncher;
                setFirearmUpdateOverride = SetRemoteMissileLauncherUpdateOverride;
                getChamberIndex = GetFirstChamberIndex;
            }
            else if (physObj is StingerLauncher)
            {
                updateFunc = UpdateStingerLauncher;
                updateGivenFunc = UpdateGivenStingerLauncher;
                dataObject = physObj as StingerLauncher;
            }
            else if (physObj is RGM40)
            {
                RGM40 asRGM40 = physObj as RGM40;
                updateFunc = UpdateRGM40;
                updateGivenFunc = UpdateGivenRGM40;
                dataObject = asRGM40;
                fireFunc = FireRGM40;
                setFirearmUpdateOverride = SetRGM40UpdateOverride;
                getChamberIndex = GetFirstChamberIndex;
            }
            else if (physObj is RollingBlock)
            {
                RollingBlock asRB = physObj as RollingBlock;
                updateFunc = UpdateRollingBlock;
                updateGivenFunc = UpdateGivenRollingBlock;
                dataObject = asRB;
                fireFunc = FireRollingBlock;
                setFirearmUpdateOverride = SetRollingBlockUpdateOverride;
                getChamberIndex = GetFirstChamberIndex;
            }
            else if (physObj is RPG7)
            {
                RPG7 asRPG7 = physObj as RPG7;
                updateFunc = UpdateRPG7;
                updateGivenFunc = UpdateGivenRPG7;
                dataObject = asRPG7;
                fireFunc = FireRPG7;
                setFirearmUpdateOverride = SetRPG7UpdateOverride;
                getChamberIndex = GetFirstChamberIndex;
            }
            else if (physObj is SimpleLauncher)
            {
                SimpleLauncher asSimpleLauncher = physObj as SimpleLauncher;
                updateFunc = UpdateSimpleLauncher;
                updateGivenFunc = UpdateGivenSimpleLauncher;
                dataObject = asSimpleLauncher;
                fireFunc = FireSimpleLauncher;
                setFirearmUpdateOverride = SetSimpleLauncherUpdateOverride;
                getChamberIndex = GetFirstChamberIndex;
            }
            else if (physObj is SimpleLauncher2)
            {
                SimpleLauncher2 asSimpleLauncher = physObj as SimpleLauncher2;
                updateFunc = UpdateSimpleLauncher2;
                updateGivenFunc = UpdateGivenSimpleLauncher2;
                dataObject = asSimpleLauncher;
                fireFunc = FireSimpleLauncher2;
                setFirearmUpdateOverride = SetSimpleLauncher2UpdateOverride;
                getChamberIndex = GetFirstChamberIndex;
            }
            else if (physObj is MF2_RL)
            {
                MF2_RL asMF2_RL = physObj as MF2_RL;
                updateFunc = UpdateMF2_RL;
                updateGivenFunc = UpdateGivenMF2_RL;
                dataObject = asMF2_RL;
                fireFunc = FireMF2_RL;
                setFirearmUpdateOverride = SetMF2_RLUpdateOverride;
                getChamberIndex = GetFirstChamberIndex;
            }
            else if (physObj is LAPD2019)
            {
                updateFunc = UpdateLAPD2019;
                updateGivenFunc = UpdateGivenLAPD2019;
                dataObject = physObj as LAPD2019;
                getChamberIndex = GetLAPD2019ChamberIndex;
            }
            else if (physObj is LAPD2019Battery)
            {
                updateFunc = UpdateLAPD2019Battery;
                updateGivenFunc = UpdateGivenLAPD2019Battery;
                dataObject = physObj as LAPD2019Battery;
            }
            else if (physObj is AttachableFirearmPhysicalObject)
            {
                AttachableFirearmPhysicalObject asAttachableFirearmPhysicalObject = (AttachableFirearmPhysicalObject)physObj;
                if(asAttachableFirearmPhysicalObject.FA is AttachableBreakActions)
                {
                    updateFunc = UpdateAttachableBreakActions;
                    updateGivenFunc = UpdateGivenAttachableBreakActions;
                    attachableFirearmFireFunc = (asAttachableFirearmPhysicalObject.FA as AttachableBreakActions).Fire;
                    attachableFirearmChamberRoundFunc = AttachableBreakActionsChamberRound;
                    attachableFirearmGetChamberFunc = AttachableBreakActionsGetChamber;
                    getChamberIndex = GetFirstChamberIndex;
                    chamberRound = ChamberAttachableBreakActionsRound;
                }
                else if(asAttachableFirearmPhysicalObject.FA is AttachableClosedBoltWeapon)
                {
                    updateFunc = UpdateAttachableClosedBoltWeapon;
                    updateGivenFunc = UpdateGivenAttachableClosedBoltWeapon;
                    attachableFirearmFireFunc = (asAttachableFirearmPhysicalObject.FA as AttachableClosedBoltWeapon).Fire;
                    attachableFirearmChamberRoundFunc = AttachableClosedBoltWeaponChamberRound;
                    attachableFirearmGetChamberFunc = AttachableClosedBoltWeaponGetChamber;
                    getChamberIndex = GetFirstChamberIndex;
                    chamberRound = ChamberAttachableClosedBoltWeaponRound;
                }
                else if(asAttachableFirearmPhysicalObject.FA is AttachableTubeFed)
                {
                    updateFunc = UpdateAttachableTubeFed;
                    updateGivenFunc = UpdateGivenAttachableTubeFed;
                    attachableFirearmFireFunc = (asAttachableFirearmPhysicalObject.FA as AttachableTubeFed).Fire;
                    attachableFirearmChamberRoundFunc = AttachableTubeFedChamberRound;
                    attachableFirearmGetChamberFunc = AttachableTubeFedGetChamber;
                    getChamberIndex = GetFirstChamberIndex;
                    chamberRound = ChamberAttachableTubeFedRound;
                }
                else if(asAttachableFirearmPhysicalObject.FA is GP25)
                {
                    updateFunc = UpdateGP25;
                    updateGivenFunc = UpdateGivenGP25;
                    attachableFirearmFireFunc = (asAttachableFirearmPhysicalObject.FA as GP25).Fire;
                    attachableFirearmChamberRoundFunc = GP25ChamberRound;
                    attachableFirearmGetChamberFunc = GP25GetChamber;
                    getChamberIndex = GetFirstChamberIndex;
                    chamberRound = ChamberGP25Round;
                }
                else if(asAttachableFirearmPhysicalObject.FA is M203)
                {
                    updateFunc = UpdateM203;
                    updateGivenFunc = UpdateGivenM203;
                    attachableFirearmFireFunc = (asAttachableFirearmPhysicalObject.FA as M203).Fire;
                    attachableFirearmChamberRoundFunc = M203ChamberRound;
                    attachableFirearmGetChamberFunc = M203GetChamber;
                    getChamberIndex = GetFirstChamberIndex;
                    chamberRound = ChamberM203Round;
                }
                updateParentFunc = UpdateAttachableFirearmParent;
                dataObject = asAttachableFirearmPhysicalObject.FA;
            }
            else if (physObj is Suppressor)
            {
                Suppressor asAttachment = (Suppressor)physObj;
                updateFunc = UpdateSuppressor;
                updateGivenFunc = UpdateGivenSuppressor;
                updateParentFunc = UpdateAttachmentParent;
                dataObject = asAttachment;
            }
            else if (physObj is FVRFireArmAttachment)
            {
                FVRFireArmAttachment asAttachment = (FVRFireArmAttachment)physObj;
                updateFunc = UpdateAttachment;
                updateGivenFunc = UpdateGivenAttachment;
                updateParentFunc = UpdateAttachmentParent;
                dataObject = asAttachment;

                // Init interface
                // TODO: Future: Add support for the following if necessary
                //       Amplifier
                //       AttachableForegrip
                //       AttachableMeleeWeaponInterface
                //       AttachableStock
                //       HandgunRailAdapter
                //       HoloSight
                //       MuzzleDeviceInterface
                //       RailCam
                //       RedDotSight
                //       SmartTrigger
                if (asAttachment.AttachmentInterface != null)
                {
                    if(asAttachment.AttachmentInterface is AttachableBipodInterface)
                    {
                        attachmentInterfaceUpdateFunc = UpdateAttachableBipod;
                        attachmentInterfaceUpdateGivenFunc = UpdateGivenAttachableBipod;
                        attachmentInterfaceDataSize = 1;
                    }
                    else if(asAttachment.AttachmentInterface is FlagPoseSwitcher)
                    {
                        attachmentInterfaceUpdateFunc = UpdateFlagPoseSwitcher;
                        attachmentInterfaceUpdateGivenFunc = UpdateGivenFlagPoseSwitcher;
                        attachmentInterfaceDataSize = 2;
                    }
                    else if(asAttachment.AttachmentInterface is FlipSight)
                    {
                        attachmentInterfaceUpdateFunc = UpdateFlipSight;
                        attachmentInterfaceUpdateGivenFunc = UpdateGivenFlipSight;
                        attachmentInterfaceDataSize = 1;
                    }
                    else if(asAttachment.AttachmentInterface is FlipSightY)
                    {
                        attachmentInterfaceUpdateFunc = UpdateFlipSightY;
                        attachmentInterfaceUpdateGivenFunc = UpdateGivenFlipSightY;
                        attachmentInterfaceDataSize = 1;
                    }
                    else if(asAttachment.AttachmentInterface is LAM)
                    {
                        attachmentInterfaceUpdateFunc = UpdateLAM;
                        attachmentInterfaceUpdateGivenFunc = UpdateGivenLAM;
                        attachmentInterfaceDataSize = 1;
                    }
                    else if(asAttachment.AttachmentInterface is LaserPointer)
                    {
                        attachmentInterfaceUpdateFunc = UpdateLaserPointer;
                        attachmentInterfaceUpdateGivenFunc = UpdateGivenLaserPointer;
                        attachmentInterfaceDataSize = 1;
                    }
                    else if(asAttachment.AttachmentInterface is TacticalFlashlight)
                    {
                        attachmentInterfaceUpdateFunc = UpdateTacticalFlashlight;
                        attachmentInterfaceUpdateGivenFunc = UpdateGivenTacticalFlashlight;
                        attachmentInterfaceDataSize = 1;
                    }
                }
            }
            else if (physObj is SosigWeaponPlayerInterface)
            {
                SosigWeaponPlayerInterface asInterface = (SosigWeaponPlayerInterface)physObj;
                updateFunc = UpdateSosigWeaponInterface;
                updateGivenFunc = UpdateGivenSosigWeaponInterface;
                dataObject = asInterface;
                sosigWeaponfireFunc = asInterface.W.FireGun;
            }
            else if(physObj is FVRFireArm)
            {
                FVRFireArm asFA = physObj as FVRFireArm;
                updateFunc = UpdateFireArm;
                updateGivenFunc = UpdateGivenFireArm;
                dataObject = asFA;
                fireFunc = FireFireArm;
                setFirearmUpdateOverride = SetFireArmUpdateOverride;
                getChamberIndex = GetFireArmChamberIndex;
                chamberRound = ChamberFireArmRound;
            }
        }

        public bool UpdateItemData(byte[] newData = null)
        {
            if(dataObject != null)
            {
                if(newData != null)
                {
                    return updateGivenFunc(newData);
                }
                else
                {
                    return updateFunc();
                }
            }

            return false;
        }

        #region Type Updates
        private bool UpdateFireArm()
        {
            FVRFireArm asFA = dataObject as FVRFireArm;
            bool modified = false;

            int necessarySize = asFA.GetChambers().Count * 4;

            if (data.data == null || data.data.Length < necessarySize)
            {
                data.data = new byte[necessarySize];
                modified = true;
            }

            // Write chambered rounds
            byte preval0;
            byte preval1;
            byte preval2;
            byte preval3;
            for (int i = 0; i < asFA.GetChambers().Count; ++i)
            {
                int firstIndex = i * 4;
                preval0 = data.data[firstIndex];
                preval1 = data.data[firstIndex + 1];
                preval2 = data.data[firstIndex + 2];
                preval3 = data.data[firstIndex + 3];

                if (asFA.GetChambers()[i].GetRound() == null || asFA.GetChambers()[i].GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(data.data, firstIndex + 2);
                }
                else
                {
                    BitConverter.GetBytes((short)asFA.GetChambers()[i].GetRound().RoundType).CopyTo(data.data, firstIndex);
                    BitConverter.GetBytes((short)asFA.GetChambers()[i].GetRound().RoundClass).CopyTo(data.data, firstIndex + 2);
                }

                modified |= (preval0 != data.data[firstIndex] || preval1 != data.data[firstIndex + 1] || preval2 != data.data[firstIndex + 2] || preval3 != data.data[firstIndex + 3]);
            }

            return modified;
        }

        private bool UpdateGivenFireArm(byte[] newData)
        {
            bool modified = false;
            FVRFireArm asFA = dataObject as FVRFireArm;

            // Set chambers
            for (int i = 0; i < asFA.GetChambers().Count; ++i)
            {
                int firstIndex = i * 4;

                if(firstIndex >= asFA.GetChambers().Count)
                {
                    break;
                }

                short chamberTypeIndex = BitConverter.ToInt16(newData, firstIndex);
                short chamberClassIndex = BitConverter.ToInt16(newData, firstIndex + 2);
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (asFA.GetChambers()[i].GetRound() != null)
                    {
                        ++ChamberPatch.chamberSkip;
                        asFA.GetChambers()[i].SetRound(null, false);
                        --ChamberPatch.chamberSkip;
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (asFA.GetChambers()[i].GetRound() == null || asFA.GetChambers()[i].GetRound().RoundClass != roundClass)
                    {
                        if (asFA.GetChambers()[i].RoundType == roundType)
                        {
                            ++ChamberPatch.chamberSkip;
                            asFA.GetChambers()[i].SetRound(roundClass, asFA.GetChambers()[i].transform.position, asFA.GetChambers()[i].transform.rotation);
                            --ChamberPatch.chamberSkip;
                        }
                        else
                        {
                            FireArmRoundType prevRoundType = asFA.GetChambers()[i].RoundType;
                            asFA.GetChambers()[i].RoundType = roundType;
                            ++ChamberPatch.chamberSkip;
                            asFA.GetChambers()[i].SetRound(roundClass, asFA.GetChambers()[i].transform.position, asFA.GetChambers()[i].transform.rotation);
                            --ChamberPatch.chamberSkip;
                            asFA.GetChambers()[i].RoundType = prevRoundType;
                        }
                        modified = true;
                    }
                }
            }

            data.data = newData;

            return modified;
        }

        private bool FireFireArm(int chamberIndex)
        {
            if(chamberIndex == -1)
            {
                return false;
            }

            FVRFireArm asFA = dataObject as FVRFireArm;
            asFA.Fire(asFA.GetChambers()[chamberIndex], asFA.GetMuzzle(), false);
            return true;
        }

        private void SetFireArmUpdateOverride(FireArmRoundType roundType, FireArmRoundClass roundClass, int chamberIndex)
        {
            if(chamberIndex == -1)
            {
                return;
            }

            FVRFireArm asFA = dataObject as FVRFireArm;
            FireArmRoundType prevRoundType = asFA.GetChambers()[chamberIndex].RoundType;
            asFA.GetChambers()[chamberIndex].RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asFA.GetChambers()[chamberIndex].SetRound(roundClass, asFA.GetChambers()[chamberIndex].transform.position, asFA.GetChambers()[chamberIndex].transform.rotation);
            --ChamberPatch.chamberSkip;
            asFA.GetChambers()[chamberIndex].RoundType = prevRoundType;
        }

        private int GetFireArmChamberIndex(FVRFireArmChamber chamber)
        {
            if(chamber == null)
            {
                return -1;
            }

            FVRFireArm asFA = dataObject as FVRFireArm;
            List<FVRFireArmChamber> chambers = asFA.GetChambers();
            for (int i=0; i < chambers.Count; ++i)
            {
                if (chambers[i] == chamber)
                {
                    return i;
                }
            }

            return -1;
        }

        private void ChamberFireArmRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            FVRFireArm asFA = dataObject as FVRFireArm;

            ++ChamberPatch.chamberSkip;
            asFA.GetChambers()[chamberIndex].SetRound(roundClass, asFA.GetChambers()[chamberIndex].transform.position, asFA.GetChambers()[chamberIndex].transform.rotation);
            --ChamberPatch.chamberSkip;
        }

        private bool UpdateSBLPCell()
        {
            bool modified = false;
            sblpCell asCell = dataObject as sblpCell;

            if (data.data == null)
            {
                data.data = new byte[9];
                modified = true;
            }

            // Write loaded into firearm
            byte preval0 = data.data[0];
            data.data[0] = asCell.FireArm != null ? (byte)1 : (byte)0;
            modified |= preval0 != data.data[0];

            // Write secondary slot index, TODO: Having to look through each secondary slot for equality every update is obviously not optimal
            // We might want to look into patching (Attachable)Firearm's LoadMagIntoSecondary and eject from secondary to keep track of this instead
            preval0 = data.data[1];
            if (asCell.FireArm == null)
            {
                data.data[1] = (byte)255;
            }
            else
            {
                bool found = false;
                for (int i = 0; i < asCell.FireArm.SecondaryMagazineSlots.Length; ++i)
                {
                    if (asCell.FireArm.SecondaryMagazineSlots[i].Magazine == asCell)
                    {
                        found = true;
                        data.data[1] = (byte)i;
                        break;
                    }
                }
                if (!found)
                {
                    data.data[1] = (byte)255;
                }
            }
            modified |= preval0 != data.data[1];

            // Write loaded into AttachableFirearm
            preval0 = data.data[2];
            data.data[2] = asCell.AttachableFireArm != null ? (byte)1 : (byte)0;
            modified |= preval0 != data.data[2];

            // Write secondary slot index, TODO: Having to look through each secondary slot for equality every update is obviously not optimal
            // We might want to look into patching (Attachable)Firearm's LoadMagIntoSecondary and eject from secondary to keep track of this instead
            preval0 = data.data[3];
            if (asCell.AttachableFireArm == null)
            {
                data.data[3] = (byte)255;
            }
            else
            {
                bool found = false;
                for (int i = 0; i < asCell.AttachableFireArm.SecondaryMagazineSlots.Length; ++i)
                {
                    if (asCell.AttachableFireArm.SecondaryMagazineSlots[i].Magazine == asCell)
                    {
                        data.data[3] = (byte)i;
                        break;
                    }
                }
                if (!found)
                {
                    data.data[3] = (byte)255;
                }
            }
            modified |= preval0 != data.data[3];

            // Write fuel amount left
            preval0 = data.data[4];
            byte preval1 = data.data[5];
            byte preval2 = data.data[6];
            byte preval3 = data.data[7];
            BitConverter.GetBytes(asCell.FuelAmountLeft).CopyTo(data.data, 4);
            modified |= (preval0 != data.data[4] || preval1 != data.data[5] || preval2 != data.data[6] || preval3 != data.data[7]);

            // Write PL
            preval0 = data.data[8];
            data.data[8] = (byte)asCell.PL;
            modified |= preval0 != data.data[8];

            return modified;
        }

        private bool UpdateGivenSBLPCell(byte[] newData)
        {
            bool modified = false;
            sblpCell asCell = dataObject as sblpCell;

            if (data.data == null)
            {
                modified = true;
            }

            // Load into firearm if necessary
            if (newData[0] == 1)
            {
                if (data.parent != -1)
                {
                    H3MP_TrackedItemData parentTrackedItemData = null;
                    if (H3MP_ThreadManager.host)
                    {
                        parentTrackedItemData = H3MP_Server.items[data.parent];
                    }
                    else
                    {
                        parentTrackedItemData = H3MP_Client.items[data.parent];
                    }

                    if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null && parentTrackedItemData.physicalItem.dataObject is FVRFireArm)
                    {
                        // We want to be loaded in a firearm, we have a parent, it is a firearm
                        if (asCell.FireArm != null)
                        {
                            if (asCell.FireArm != parentTrackedItemData.physicalItem.dataObject)
                            {
                                // Unload from current, load into new firearm
                                if (asCell.FireArm.Magazine == asCell)
                                {
                                    asCell.FireArm.EjectMag(true);
                                }
                                else
                                {
                                    for (int i = 0; i < asCell.FireArm.SecondaryMagazineSlots.Length; ++i)
                                    {
                                        if (asCell.FireArm.SecondaryMagazineSlots[i].Magazine == asCell)
                                        {
                                            asCell.FireArm.EjectSecondaryMagFromSlot(i, true);
                                            break;
                                        }
                                    }
                                }
                                if (newData[1] == 255)
                                {
                                    ++MagazinePatch.loadSkip;
                                    asCell.Load(parentTrackedItemData.physicalItem.dataObject as FVRFireArm);
                                    --MagazinePatch.loadSkip;
                                }
                                else
                                {
                                    ++MagazinePatch.loadSkip;
                                    asCell.LoadIntoSecondary(parentTrackedItemData.physicalItem.dataObject as FVRFireArm, newData[1]);
                                    --MagazinePatch.loadSkip;
                                }
                                modified = true;
                            }
                        }
                        else if (asCell.AttachableFireArm != null)
                        {
                            // Unload from current, load into new firearm
                            if (asCell.AttachableFireArm.Magazine == asCell)
                            {
                                asCell.AttachableFireArm.EjectMag(true);
                            }
                            else
                            {
                                for (int i = 0; i < asCell.AttachableFireArm.SecondaryMagazineSlots.Length; ++i)
                                {
                                    if (asCell.AttachableFireArm.SecondaryMagazineSlots[i].Magazine == asCell)
                                    {
                                        // TODO: Future: When H3 adds support for secondary slots on attachable firearm uncomment the following:
                                        //asMag.AttachableFireArm.EjectSecondaryMagFromSlot(i, true);
                                        break;
                                    }
                                }
                            }
                            if (newData[1] == 255)
                            {
                                ++MagazinePatch.loadSkip;
                                asCell.Load(parentTrackedItemData.physicalItem.dataObject as FVRFireArm);
                                --MagazinePatch.loadSkip;
                            }
                            else
                            {
                                ++MagazinePatch.loadSkip;
                                asCell.LoadIntoSecondary(parentTrackedItemData.physicalItem.dataObject as FVRFireArm, newData[1]);
                                --MagazinePatch.loadSkip;
                            }
                            modified = true;
                        }
                        else
                        {
                            // Load into firearm
                            if (newData[1] == 255)
                            {
                                ++MagazinePatch.loadSkip;
                                asCell.Load(parentTrackedItemData.physicalItem.dataObject as FVRFireArm);
                                --MagazinePatch.loadSkip;
                            }
                            else
                            {
                                ++MagazinePatch.loadSkip;
                                asCell.LoadIntoSecondary(parentTrackedItemData.physicalItem.dataObject as FVRFireArm, newData[1]);
                                --MagazinePatch.loadSkip;
                            }
                            modified = true;
                        }
                    }
                }
            }
            else if (newData[2] == 1)
            {
                if (data.parent != -1)
                {
                    H3MP_TrackedItemData parentTrackedItemData = null;
                    if (H3MP_ThreadManager.host)
                    {
                        parentTrackedItemData = H3MP_Server.items[data.parent];
                    }
                    else
                    {
                        parentTrackedItemData = H3MP_Client.items[data.parent];
                    }

                    if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null && parentTrackedItemData.physicalItem.dataObject is AttachableFirearmPhysicalObject)
                    {
                        // We want to be loaded in a AttachableFireArm, we have a parent, it is a AttachableFireArm
                        if (asCell.AttachableFireArm != null)
                        {
                            if (asCell.AttachableFireArm != (parentTrackedItemData.physicalItem.dataObject as AttachableFirearmPhysicalObject).FA)
                            {
                                // Unload from current, load into new AttachableFireArm
                                if (asCell.AttachableFireArm.Magazine == asCell)
                                {
                                    asCell.AttachableFireArm.EjectMag(true);
                                }
                                else
                                {
                                    for (int i = 0; i < asCell.AttachableFireArm.SecondaryMagazineSlots.Length; ++i)
                                    {
                                        if (asCell.AttachableFireArm.SecondaryMagazineSlots[i].Magazine == asCell)
                                        {
                                            // TODO: Future: When H3 adds support for secondary slots on attachable firearm uncomment the following:
                                            //asMag.AttachableFireArm.EjectSecondaryMagFromSlot(i, true);
                                            break;
                                        }
                                    }
                                }
                                if (newData[3] == 255)
                                {
                                    ++MagazinePatch.loadSkip;
                                    asCell.Load((parentTrackedItemData.physicalItem.dataObject as AttachableFirearmPhysicalObject).FA);
                                    --MagazinePatch.loadSkip;
                                }
                                else
                                {
                                    // TODO: Future: When H3 adds support for secondary slots on attachable firearm uncomment the following:
                                    //asMag.LoadIntoSecondary((parentTrackedItemData.physicalItem.dataObject as AttachableFirearmPhysicalObject).FA, newData[newData.Length - 1]);
                                }
                                modified = true;
                            }
                        }
                        else if (asCell.FireArm != null)
                        {
                            // Unload from current firearm, load into new AttachableFireArm
                            if (asCell.FireArm.Magazine == asCell)
                            {
                                asCell.FireArm.EjectMag(true);
                            }
                            else
                            {
                                for (int i = 0; i < asCell.FireArm.SecondaryMagazineSlots.Length; ++i)
                                {
                                    if (asCell.FireArm.SecondaryMagazineSlots[i].Magazine == asCell)
                                    {
                                        asCell.FireArm.EjectSecondaryMagFromSlot(i, true);
                                        break;
                                    }
                                }
                            }
                            if (newData[3] == 255)
                            {
                                ++MagazinePatch.loadSkip;
                                asCell.Load((parentTrackedItemData.physicalItem.dataObject as AttachableFirearmPhysicalObject).FA);
                                --MagazinePatch.loadSkip;
                            }
                            else
                            {
                                // TODO: Future: When H3 adds support for secondary slots on attachable firearm uncomment the following:
                                //asMag.LoadIntoSecondary((parentTrackedItemData.physicalItem.dataObject as AttachableFirearmPhysicalObject).FA, newData[newData.Length - 1]);
                            }
                            modified = true;
                        }
                        else
                        {
                            // Load into AttachableFireArm
                            if (newData[3] == 255)
                            {
                                ++MagazinePatch.loadSkip;
                                asCell.Load((parentTrackedItemData.physicalItem.dataObject as AttachableFirearmPhysicalObject).FA);
                                --MagazinePatch.loadSkip;
                            }
                            else
                            {
                                // TODO: Future: When H3 adds support for secondary slots on attachable firearm uncomment the following:
                                //asMag.LoadIntoSecondary((parentTrackedItemData.physicalItem.dataObject as AttachableFirearmPhysicalObject).FA, newData[newData.Length - 1]);
                            }
                            modified = true;
                        }
                    }
                }
            }
            else
            {
                if (asCell.FireArm != null)
                {
                    // Don't want to be loaded, but we are loaded, unload
                    if (asCell.FireArm.Magazine == asCell)
                    {
                        asCell.FireArm.EjectMag(true);
                    }
                    else
                    {
                        for (int i = 0; i < asCell.FireArm.SecondaryMagazineSlots.Length; ++i)
                        {
                            if (asCell.FireArm.SecondaryMagazineSlots[i].Magazine == asCell)
                            {
                                asCell.FireArm.EjectSecondaryMagFromSlot(i, true);
                                break;
                            }
                        }
                    }
                    modified = true;
                }
                else if (asCell.AttachableFireArm != null)
                {
                    if (asCell.AttachableFireArm.Magazine == asCell)
                    {
                        asCell.AttachableFireArm.EjectMag(true);
                    }
                    else
                    {
                        for (int i = 0; i < asCell.AttachableFireArm.SecondaryMagazineSlots.Length; ++i)
                        {
                            if (asCell.AttachableFireArm.SecondaryMagazineSlots[i].Magazine == asCell)
                            {
                                // TODO: Future: When H3 adds support for secondary slots on attachable firearm uncomment the following:
                                //asMag.AttachableFireArm.EjectSecondaryMagFromSlot(i, true);
                                break;
                            }
                        }
                    }
                    modified = true;
                }
            }

            float preAmount = asCell.FuelAmountLeft;

            asCell.FuelAmountLeft = BitConverter.ToSingle(newData, 4);

            modified |= preAmount != asCell.FuelAmountLeft;

            sblpCell.PLevel preLevel = asCell.PL;

            asCell.PL = (sblpCell.PLevel)newData[8];

            modified |= preLevel != asCell.PL;

            data.data = newData;

            return modified;
        }

        private bool UpdateLaserGun()
        {
            sblp asLG = dataObject as sblp;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[3];
                modified = true;
            }

            byte preval = data.data[0];

            // Write m_isShotEngaged
            data.data[0] = (bool)Mod.sblp_m_isShotEngaged.GetValue(asLG) ? (byte)1 : (byte)0;

            modified |= preval != data.data[0];

            return modified;
        }

        private bool UpdateGivenLaserGun(byte[] newData)
        {
            bool modified = false;
            sblp asLG = dataObject as sblp;

            if (data.data == null)
            {
                modified = true;

                // Set m_isShotEngaged
                if (newData[0] == 1 && !(bool)Mod.sblp_m_isShotEngaged.GetValue(asLG))
                {
                    Mod.sblp_TryToEngageShot.Invoke(asLG, null);
                }
                else if (newData[0] == 0 && (bool)Mod.sblp_m_isShotEngaged.GetValue(asLG))
                {
                    Mod.sblp_TryToDisengageShot.Invoke(asLG, null);
                }
            }
            else
            {
                // Set m_isShotEngaged
                if (newData[0] == 1 && !(bool)Mod.sblp_m_isShotEngaged.GetValue(asLG))
                {
                    Mod.sblp_TryToEngageShot.Invoke(asLG, null);
                    modified = true;
                }
                else if (newData[0] == 0 && (bool)Mod.sblp_m_isShotEngaged.GetValue(asLG))
                {
                    Mod.sblp_TryToDisengageShot.Invoke(asLG, null);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool UpdateAirgun()
        {
            Airgun asAG = dataObject as Airgun;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[3];
                modified = true;
            }

            byte preval = data.data[0];

            // Write hammer state
            data.data[0] = asAG.IsHammerCocked ? (byte)1 : (byte)0;

            modified |= preval != data.data[0];

            preval = data.data[1];
            byte preval0 = data.data[2];

            // Write chambered round class
            if (asAG.Chamber.GetRound() == null || asAG.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 1);
            }
            else
            {
                BitConverter.GetBytes((short)asAG.Chamber.GetRound().RoundClass).CopyTo(data.data, 1);
            }

            modified |= (preval != data.data[1] || preval0 != data.data[2]);

            return modified;
        }

        private bool UpdateGivenAirgun(byte[] newData)
        {
            bool modified = false;
            Airgun asAG = dataObject as Airgun;

            if (data.data == null)
            {
                modified = true;

                // Set hammer
                if (newData[0] == 1 && !asAG.IsHammerCocked)
                {
                    asAG.CockHammer();
                }
                else if (newData[0] == 0 && asAG.IsHammerCocked)
                {
                    Mod.Airgun_m_isHammerCocked.SetValue(asAG, false);
                }
            }
            else
            {
                // Set hammer
                if (newData[0] == 1 && !asAG.IsHammerCocked)
                {
                    asAG.CockHammer();
                    modified = true;
                }
                else if (newData[0] == 0 && asAG.IsHammerCocked)
                {
                    Mod.Airgun_m_isHammerCocked.SetValue(asAG, false);
                    modified = true;
                }
            }

            // Set chamber
            short chamberClassIndex = BitConverter.ToInt16(newData, 1);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asAG.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asAG.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asAG.Chamber.GetRound() == null || asAG.Chamber.GetRound().RoundClass != roundClass)
                {
                    ++ChamberPatch.chamberSkip;
                    asAG.Chamber.SetRound(roundClass, asAG.Chamber.transform.position, asAG.Chamber.transform.rotation);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool FireAirgun(int chamberIndex)
        {
            Airgun asAG = dataObject as Airgun;
            Mod.Airgun_DropHammer.Invoke(asAG, null);
            return true;
        }

        private void SetAirgunUpdateOverride(FireArmRoundType roundType, FireArmRoundClass roundClass, int chamberIndex)
        {
            Airgun asAG = dataObject as Airgun;

            asAG.CockHammer();
            FireArmRoundType prevRoundType = asAG.Chamber.RoundType;
            asAG.Chamber.RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asAG.Chamber.SetRound(roundClass, asAG.Chamber.transform.position, asAG.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
            asAG.Chamber.RoundType = prevRoundType;
        }

        private int GetFirstChamberIndex(FVRFireArmChamber chamber)
        {
            return 0;
        }

        private bool UpdateSLAM()
        {
            SLAM asSLAM = (SLAM)dataObject;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[1];
                modified = true;
            }

            // Write armed
            byte preval = data.data[0];

            data.data[0] = (byte)asSLAM.Mode;

            modified |= preval != data.data[0];

            return modified;
        }

        private bool UpdateGivenSLAM(byte[] newData)
        {
            bool modified = false;
            SLAM asSLAM = (SLAM)dataObject;

            if (data.data == null)
            {
                modified = true;

                // Set mode
                asSLAM.SetMode((SLAM.SLAMMode)newData[0]);
            }
            else
            {
                if (data.data[0] != newData[0])
                {
                    // Set mode
                    asSLAM.SetMode((SLAM.SLAMMode)newData[0]);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool UpdateClaymoreMine()
        {
            ClaymoreMine asCM = (ClaymoreMine)dataObject;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[2];
                modified = true;
            }

            // Write armed
            byte preval = data.data[0];

            data.data[0] = (bool)Mod.ClaymoreMine_m_isArmed.GetValue(asCM) ? (byte)1 : (byte)0;

            modified |= preval != data.data[0];

            // Write planted
            preval = data.data[1];

            data.data[1] = (bool)Mod.ClaymoreMine_m_isPlanted.GetValue(asCM) ? (byte)1 : (byte)0;

            modified |= preval != data.data[1];

            return modified;
        }

        private bool UpdateGivenClaymoreMine(byte[] newData)
        {
            bool modified = false;
            ClaymoreMine asCM = (ClaymoreMine)dataObject;

            if (data.data == null)
            {
                modified = true;

                // Set armed
                Mod.ClaymoreMine_m_isArmed.SetValue(asCM, newData[0] == 1);

                // Set planted
                Mod.ClaymoreMine_m_isPlanted.SetValue(asCM, newData[1] == 1);
            }
            else
            {
                if (data.data[0] != newData[0])
                {
                    // Set armed
                    Mod.ClaymoreMine_m_isArmed.SetValue(asCM, newData[0] == 1);
                    modified = true;
                }
                if (data.data[1] != newData[1])
                {
                    // Set planted
                    Mod.ClaymoreMine_m_isPlanted.SetValue(asCM, newData[1] == 1);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool UpdateC4()
        {
            C4 asC4 = (C4)dataObject;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[1];
                modified = true;
            }

            // Write armed
            byte preval = data.data[0];

            data.data[0] = (bool)Mod.C4_m_isArmed.GetValue(asC4) ? (byte)1 : (byte)0;

            modified |= preval != data.data[0];

            return modified;
        }

        private bool UpdateGivenC4(byte[] newData)
        {
            bool modified = false;
            C4 asC4 = (C4)dataObject;

            if (data.data == null)
            {
                modified = true;

                // Set armed
                asC4.SetArmed(newData[0] == 1);
            }
            else
            {
                if (data.data[0] != newData[0])
                {
                    // Set armed
                    asC4.SetArmed(newData[0] == 1);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool UpdateGrenade()
        {
            FVRGrenade asGrenade = (FVRGrenade)dataObject;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[asGrenade.Uses2ndPin ? 3 : 2];
                modified = true;
            }

            byte preval = data.data[0];

            data.data[0] = (bool)Mod.FVRGrenade_m_isLeverReleased.GetValue(asGrenade) ? (byte)1 : (byte)0;

            modified |= preval != data.data[0];

            preval = data.data[1];

            data.data[1] = (bool)Mod.FVRGrenadePin_m_hasBeenPulled.GetValue(asGrenade.Pin) ? (byte)1 : (byte)0;

            modified |= preval != data.data[1];

            if (asGrenade.Uses2ndPin)
            {
                preval = data.data[2];

                data.data[2] = (bool)Mod.FVRGrenadePin_m_hasBeenPulled.GetValue(asGrenade.Pin2) ? (byte)1 : (byte)0;

                modified |= preval != data.data[2];
            }

            return modified;
        }

        private bool UpdateGivenGrenade(byte[] newData)
        {
            bool modified = false;
            FVRGrenade asGrenade = (FVRGrenade)dataObject;

            if (data.data == null)
            {
                modified = true;

                // Set lever released
                if (newData[0] == 1 && !(bool)Mod.FVRGrenade_m_isLeverReleased.GetValue(asGrenade))
                {
                    asGrenade.ReleaseLever();
                }

                // Set pin
                if (newData[1] == 1 && !(bool)Mod.FVRGrenadePin_m_hasBeenPulled.GetValue(asGrenade.Pin))
                {
                    Mod.FVRGrenadePin_m_hasBeenPulled.SetValue(asGrenade.Pin, true);
                    asGrenade.Pin.transform.SetParent(null);
                    asGrenade.Pin.PinPiece.transform.SetParent(asGrenade.Pin.transform);
                    Rigidbody rigidbody = asGrenade.Pin.PinPiece.AddComponent<Rigidbody>();
                    rigidbody.mass = 0.01f;
                    HingeJoint component = asGrenade.Pin.GetComponent<HingeJoint>();
                    component.connectedBody = rigidbody;
                    asGrenade.Pin.Grenade.PullPin();
                    Mod.FVRGrenadePin_m_isDying.SetValue(asGrenade.Pin, true);
                    if (asGrenade.Pin.UXGeo_Held != null)
                    {
                        UnityEngine.Object.Destroy(asGrenade.Pin.UXGeo_Held);
                    }
                }

                if (asGrenade.Uses2ndPin)
                {
                    // Set pin2
                    if (newData[2] == 1 && !(bool)Mod.FVRGrenadePin_m_hasBeenPulled.GetValue(asGrenade.Pin2))
                    {
                        Mod.FVRGrenadePin_m_hasBeenPulled.SetValue(asGrenade.Pin2, true);
                        asGrenade.Pin2.transform.SetParent(null);
                        asGrenade.Pin2.PinPiece.transform.SetParent(asGrenade.Pin2.transform);
                        Rigidbody rigidbody = asGrenade.Pin2.PinPiece.AddComponent<Rigidbody>();
                        rigidbody.mass = 0.01f;
                        HingeJoint component = asGrenade.Pin2.GetComponent<HingeJoint>();
                        component.connectedBody = rigidbody;
                        asGrenade.Pin2.Grenade.PullPin2();
                        Mod.FVRGrenadePin_m_isDying.SetValue(asGrenade.Pin2, true);
                        if (asGrenade.Pin2.UXGeo_Held != null)
                        {
                            UnityEngine.Object.Destroy(asGrenade.Pin2.UXGeo_Held);
                        }
                    }
                }
            }
            else
            {
                // Set lever released
                if (newData[0] == 1 && !(bool)Mod.FVRGrenade_m_isLeverReleased.GetValue(asGrenade))
                {
                    asGrenade.ReleaseLever();
                    modified = true;
                }
                // Set pin
                if (newData[1] == 1 && !(bool)Mod.FVRGrenadePin_m_hasBeenPulled.GetValue(asGrenade.Pin))
                {
                    Mod.FVRGrenadePin_m_hasBeenPulled.SetValue(asGrenade.Pin, true);
                    asGrenade.Pin.transform.SetParent(null);
                    asGrenade.Pin.PinPiece.transform.SetParent(asGrenade.Pin.transform);
                    Rigidbody rigidbody = asGrenade.Pin.PinPiece.AddComponent<Rigidbody>();
                    rigidbody.mass = 0.01f;
                    HingeJoint component = asGrenade.Pin.GetComponent<HingeJoint>();
                    component.connectedBody = rigidbody;
                    asGrenade.Pin.Grenade.PullPin();
                    Mod.FVRGrenadePin_m_isDying.SetValue(asGrenade.Pin, true);
                    if (asGrenade.Pin.UXGeo_Held != null)
                    {
                        UnityEngine.Object.Destroy(asGrenade.Pin.UXGeo_Held);
                    }
                    modified = true;
                }

                if (asGrenade.Uses2ndPin)
                {
                    // Set pin2
                    if (newData[2] == 1 && !(bool)Mod.FVRGrenadePin_m_hasBeenPulled.GetValue(asGrenade.Pin2))
                    {
                        Mod.FVRGrenadePin_m_hasBeenPulled.SetValue(asGrenade.Pin2, true);
                        asGrenade.Pin2.transform.SetParent(null);
                        asGrenade.Pin2.PinPiece.transform.SetParent(asGrenade.Pin2.transform);
                        Rigidbody rigidbody = asGrenade.Pin2.PinPiece.AddComponent<Rigidbody>();
                        rigidbody.mass = 0.01f;
                        HingeJoint component = asGrenade.Pin2.GetComponent<HingeJoint>();
                        component.connectedBody = rigidbody;
                        asGrenade.Pin2.Grenade.PullPin2();
                        Mod.FVRGrenadePin_m_isDying.SetValue(asGrenade.Pin2, true);
                        if (asGrenade.Pin2.UXGeo_Held != null)
                        {
                            UnityEngine.Object.Destroy(asGrenade.Pin2.UXGeo_Held);
                        }
                        modified = true;
                    }
                }
            }

            data.data = newData;

            return modified;
        }

        private bool UpdatePinnedGrenade()
        {
            PinnedGrenade asPG = (PinnedGrenade)dataObject;
            List<PinnedGrenadeRing> rings = Mod.PinnedGrenade_m_rings.GetValue(asPG) as List<PinnedGrenadeRing>;
            bool modified = false;

            int neededSize = rings.Count + 1;
            if (data.data == null || data.data.Length != neededSize)
            {
                data.data = new byte[neededSize];
                modified = true;
            }

            byte preval = data.data[0];

            data.data[0] = asPG.IsLeverReleased() ? (byte)1 : (byte)0;

            modified |= preval != data.data[0];

            if (rings != null)
            {
                for (int i = 0; i < rings.Count; ++i)
                {
                    preval = data.data[i + 1];

                    data.data[i + 1] = rings[i].HasPinDetached() ? (byte)1 : (byte)0;

                    modified |= preval != data.data[i + 1];
                }
            }

            return modified;
        }

        private bool UpdateGivenPinnedGrenade(byte[] newData)
        {
            bool modified = false;
            PinnedGrenade asPG = (PinnedGrenade)dataObject;
            List<PinnedGrenadeRing> rings = Mod.PinnedGrenade_m_rings.GetValue(asPG) as List<PinnedGrenadeRing>;

            if (data.data == null)
            {
                modified = true;

                // Set lever released
                if (newData[0] == 1 && !asPG.IsLeverReleased())
                {
                    asPG.ReleaseLever();
                }
            }
            else
            {
                // Set lever released
                if (newData[0] == 1 && !asPG.IsLeverReleased())
                {
                    asPG.ReleaseLever();
                    modified = true;
                }
            }

            if (rings != null)
            {
                bool pinPulled = (bool)Mod.PinnedGrenade_m_isPinPulled.GetValue(asPG);
                bool newPinPulled = true;
                for (int i = 0; i < rings.Count; ++i)
                {
                    if (!rings[i].HasPinDetached())
                    {
                        if(newData[i + 1] == 1)
                        {
                            Mod.PinnedGrenadeRing_m_hasPinDetached.SetValue(rings[i], true);
                            rings[i].Pin.RootRigidbody = rings[i].Pin.gameObject.AddComponent<Rigidbody>();
                            rings[i].Pin.RootRigidbody.mass = 0.02f;
                            rings[i].ForceBreakInteraction();
                            rings[i].transform.SetParent(rings[i].Pin.transform);
                            rings[i].Pin.enabled = true;
                            SM.PlayCoreSound(FVRPooledAudioType.GenericClose, rings[i].G.AudEvent_Pinpull, rings[i].transform.position);
                            rings[i].GetComponent<Collider>().enabled = false;
                            rings[i].enabled = false;
                            modified = true;
                        }
                        else
                        {
                            newPinPulled = false;
                        }
                    }
                }

                if(pinPulled != newPinPulled)
                {
                    Mod.PinnedGrenade_m_isPinPulled.SetValue(asPG, newPinPulled);
                }
            }

            data.data = newData;

            return modified;
        }

        private bool UpdateMolotov()
        {
            Molotov asMolotov = (Molotov)dataObject;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[1];
                modified = true;
            }

            byte preval = data.data[0];

            data.data[0] = asMolotov.Igniteable.IsOnFire() ? (byte)1 : (byte)0;

            modified |= preval != data.data[0];

            return modified;
        }

        private bool UpdateGivenMolotov(byte[] newData)
        {
            bool modified = false;
            Molotov asMolotov = (Molotov)dataObject;

            if (data.data == null)
            {
                modified = true;

                // Set ignited
                if (newData[0] == 1 && !asMolotov.Igniteable.IsOnFire())
                {
                    asMolotov.RemoteIgnite();
                }
            }
            else
            {
                // Set ignited
                if (newData[0] == 1 && !asMolotov.Igniteable.IsOnFire())
                {
                    asMolotov.RemoteIgnite();
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool UpdateMF2_RL()
        {
            MF2_RL asMF2_RL = (MF2_RL)dataObject;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[4];
                modified = true;
            }

            byte preval = data.data[0];
            byte preval0 = data.data[1];
            byte preval1 = data.data[2];
            byte preval2 = data.data[3];

            // Write chambered round class
            if (asMF2_RL.Chamber.GetRound() == null || asMF2_RL.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 2);
            }
            else
            {
                BitConverter.GetBytes((short)asMF2_RL.Chamber.GetRound().RoundType).CopyTo(data.data, 0);
                BitConverter.GetBytes((short)asMF2_RL.Chamber.GetRound().RoundClass).CopyTo(data.data, 2);
            }

            modified |= (preval != data.data[0] || preval0 != data.data[1] || preval1 != data.data[2] || preval2 != data.data[3]);

            return modified;
        }

        private bool UpdateGivenMF2_RL(byte[] newData)
        {
            bool modified = false;
            MF2_RL asMF2_RL = (MF2_RL)dataObject;

            // Set chamber
            short chamberTypeIndex = BitConverter.ToInt16(newData, 0);
            short chamberClassIndex = BitConverter.ToInt16(newData, 2);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asMF2_RL.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asMF2_RL.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asMF2_RL.Chamber.GetRound() == null || asMF2_RL.Chamber.GetRound().RoundClass != roundClass)
                {
                    if (asMF2_RL.Chamber.RoundType == roundType)
                    {
                        ++ChamberPatch.chamberSkip;
                        asMF2_RL.Chamber.SetRound(roundClass, asMF2_RL.Chamber.transform.position, asMF2_RL.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                    else
                    {
                        FireArmRoundType prevRoundType = asMF2_RL.Chamber.RoundType;
                        asMF2_RL.Chamber.RoundType = roundType;
                        ++ChamberPatch.chamberSkip;
                        asMF2_RL.Chamber.SetRound(roundClass, asMF2_RL.Chamber.transform.position, asMF2_RL.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                        asMF2_RL.Chamber.RoundType = prevRoundType;
                    }
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool FireMF2_RL(int chamberIndex)
        {
            MF2_RL asMF2_RL = (MF2_RL)dataObject;
            asMF2_RL.Fire();
            return true;
        }

        private void SetMF2_RLUpdateOverride(FireArmRoundType roundType, FireArmRoundClass roundClass, int chamberIndex)
        {
            MF2_RL asMF2_RL = (MF2_RL)dataObject;
            FireArmRoundType prevRoundType = asMF2_RL.Chamber.RoundType;
            asMF2_RL.Chamber.RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asMF2_RL.Chamber.SetRound(roundClass, asMF2_RL.Chamber.transform.position, asMF2_RL.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
            asMF2_RL.Chamber.RoundType = prevRoundType;
        }

        private bool UpdateStingerLauncher()
        {
            StingerLauncher asSL = dataObject as StingerLauncher;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[1];
                modified = true;
            }

            byte preval0 = data.data[0];

            // Write has missile
            data.data[0] = (bool)Mod.StingerLauncher_m_hasMissile.GetValue(asSL) ? (byte)1 : (byte)0;

            modified |= preval0 != data.data[0];

            return modified;
        }

        private bool UpdateGivenStingerLauncher(byte[] newData)
        {
            bool modified = false;
            StingerLauncher asSL = dataObject as StingerLauncher;

            if (data.data == null)
            {
                modified = true;

                // Set has missile
                Mod.StingerLauncher_m_hasMissile.SetValue(asSL, newData[0] == 1);
            }
            else
            {
                if (data.data[0] != newData[0])
                {
                    // Set has missile
                    Mod.StingerLauncher_m_hasMissile.SetValue(asSL, newData[0] == 1);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool UpdateSingleActionRevolver()
        {
            SingleActionRevolver asRevolver = dataObject as SingleActionRevolver;
            bool modified = false;

            int necessarySize = asRevolver.Cylinder.NumChambers * 4 + 2;

            if (data.data == null)
            {
                data.data = new byte[necessarySize];
                modified = true;
            }

            byte preval0 = data.data[0];

            // Write cur chamber
            data.data[0] = (byte)asRevolver.CurChamber;

            modified |= preval0 != data.data[0];

            preval0 = data.data[1];

            // Write hammer cocked
            data.data[1] = (bool)Mod.SingleActionRevolver_m_isHammerCocked.GetValue(asRevolver) ? (byte)1 : (byte)0;

            modified |= preval0 != data.data[1];

            // Write chambered rounds
            byte preval1;
            byte preval2;
            byte preval3;
            for (int i = 0; i < asRevolver.Cylinder.Chambers.Length; ++i)
            {
                int firstIndex = i * 4 + 2;
                preval0 = data.data[firstIndex];
                preval1 = data.data[firstIndex + 1];
                preval2 = data.data[firstIndex + 2];
                preval3 = data.data[firstIndex + 3];

                if (asRevolver.Cylinder.Chambers[i].GetRound() == null || asRevolver.Cylinder.Chambers[i].GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(data.data, firstIndex + 2);
                }
                else
                {
                    BitConverter.GetBytes((short)asRevolver.Cylinder.Chambers[i].GetRound().RoundType).CopyTo(data.data, firstIndex);
                    BitConverter.GetBytes((short)asRevolver.Cylinder.Chambers[i].GetRound().RoundClass).CopyTo(data.data, firstIndex + 2);
                }

                modified |= (preval0 != data.data[firstIndex] || preval1 != data.data[firstIndex + 1] || preval2 != data.data[firstIndex + 2] || preval3 != data.data[firstIndex + 3]);
            }

            return modified;
        }

        private bool UpdateGivenSingleActionRevolver(byte[] newData)
        {
            bool modified = false;
            SingleActionRevolver asRevolver = dataObject as SingleActionRevolver;

            if (data.data == null)
            {
                modified = true;

                // Set cur chamber
                asRevolver.CurChamber = newData[0];

                // Set hammer cocked
                Mod.SingleActionRevolver_m_isHammerCocked.SetValue(asRevolver, newData[1] == 1);
            }
            else
            {
                if (data.data[0] != newData[0])
                {
                    // Set cur chamber
                    asRevolver.CurChamber = newData[0];
                    modified = true;
                }
                if (data.data[1] != newData[1])
                {
                    // Set hammer cocked
                    Mod.SingleActionRevolver_m_isHammerCocked.SetValue(asRevolver, newData[1] == 1);
                    modified = true;
                }
            }

            // Set chambers
            for (int i = 0; i < asRevolver.Cylinder.Chambers.Length; ++i)
            {
                int firstIndex = i * 4 + 2;
                short chamberTypeIndex = BitConverter.ToInt16(newData, firstIndex);
                short chamberClassIndex = BitConverter.ToInt16(newData, firstIndex + 2);
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (asRevolver.Cylinder.Chambers[i].GetRound() != null)
                    {
                        ++ChamberPatch.chamberSkip;
                        asRevolver.Cylinder.Chambers[i].SetRound(null, false);
                        --ChamberPatch.chamberSkip;
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (asRevolver.Cylinder.Chambers[i].GetRound() == null || asRevolver.Cylinder.Chambers[i].GetRound().RoundClass != roundClass)
                    {
                        if (asRevolver.Cylinder.Chambers[i].RoundType == roundType)
                        {
                            ++ChamberPatch.chamberSkip;
                            asRevolver.Cylinder.Chambers[i].SetRound(roundClass, asRevolver.Cylinder.Chambers[i].transform.position, asRevolver.Cylinder.Chambers[i].transform.rotation);
                            --ChamberPatch.chamberSkip;
                        }
                        else
                        {
                            FireArmRoundType prevRoundType = asRevolver.Cylinder.Chambers[i].RoundType;
                            asRevolver.Cylinder.Chambers[i].RoundType = roundType;
                            ++ChamberPatch.chamberSkip;
                            asRevolver.Cylinder.Chambers[i].SetRound(roundClass, asRevolver.Cylinder.Chambers[i].transform.position, asRevolver.Cylinder.Chambers[i].transform.rotation);
                            --ChamberPatch.chamberSkip;
                            asRevolver.Cylinder.Chambers[i].RoundType = prevRoundType;
                        }
                        modified = true;
                    }
                }
            }

            data.data = newData;

            return modified;
        }

        public int GetSingleActionRevolverChamberIndex(FVRFireArmChamber chamber)
        {
            SingleActionRevolver asRevolver = dataObject as SingleActionRevolver;
            List< FVRFireArmChamber> chambers = asRevolver.GetChambers();

            if(chamber == null)
            {
                return -1;
            }

            for(int i=0; i < chambers.Count; ++i)
            {
                if (chambers[i] == chamber)
                {
                    return i;
                }
            }

            return -1;
        }

        private void ChamberSingleActionRevolverRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            SingleActionRevolver asRevolver = dataObject as SingleActionRevolver;

            ++ChamberPatch.chamberSkip;
            asRevolver.GetChambers()[chamberIndex].SetRound(roundClass, asRevolver.GetChambers()[chamberIndex].transform.position, asRevolver.GetChambers()[chamberIndex].transform.rotation);
            --ChamberPatch.chamberSkip;
        }

        private bool UpdateSimpleLauncher2()
        {
            SimpleLauncher2 asSimpleLauncher = (SimpleLauncher2)dataObject;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[5];
                modified = true;
            }

            byte preval = data.data[0];

            // Write mode
            data.data[0] = (byte)(int)asSimpleLauncher.Mode;

            modified |= preval != data.data[0];

            preval = data.data[1];
            byte preval0 = data.data[2];
            byte preval1 = data.data[3];
            byte preval2 = data.data[4];

            // Write chambered round class
            if (asSimpleLauncher.Chamber.GetRound() == null || asSimpleLauncher.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 3);
            }
            else
            {
                BitConverter.GetBytes((short)asSimpleLauncher.Chamber.GetRound().RoundType).CopyTo(data.data, 1);
                BitConverter.GetBytes((short)asSimpleLauncher.Chamber.GetRound().RoundClass).CopyTo(data.data, 3);
            }

            modified |= (preval != data.data[1] || preval0 != data.data[2] || preval1 != data.data[3] || preval2 != data.data[4]);

            return modified;
        }

        private bool UpdateGivenSimpleLauncher2(byte[] newData)
        {
            bool modified = false;
            SimpleLauncher2 asSimpleLauncher = (SimpleLauncher2)dataObject;

            if (data.data == null)
            {
                modified = true;

                // Set mode
                asSimpleLauncher.Mode = (SimpleLauncher2.fMode)newData[0];
                if(asSimpleLauncher.Mode == SimpleLauncher2.fMode.dr)
                {
                    // Dont want it to go into DR mode if not in control
                    asSimpleLauncher.Mode = SimpleLauncher2.fMode.tr;
                }
            }
            else
            {
                if (data.data[0] != newData[0])
                {
                    // Set mode
                    asSimpleLauncher.Mode = (SimpleLauncher2.fMode)newData[0];
                    if (asSimpleLauncher.Mode == SimpleLauncher2.fMode.dr)
                    {
                        // Dont want it to go into DR mode if not in control
                        asSimpleLauncher.Mode = SimpleLauncher2.fMode.tr;
                    }
                    modified = true;
                }
            }

            // Set chamber
            short chamberTypeIndex = BitConverter.ToInt16(newData, 1);
            short chamberClassIndex = BitConverter.ToInt16(newData, 3);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asSimpleLauncher.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asSimpleLauncher.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asSimpleLauncher.Chamber.GetRound() == null || asSimpleLauncher.Chamber.GetRound().RoundClass != roundClass)
                {
                    if (asSimpleLauncher.Chamber.RoundType == roundType)
                    {
                        ++ChamberPatch.chamberSkip;
                        asSimpleLauncher.Chamber.SetRound(roundClass, asSimpleLauncher.Chamber.transform.position, asSimpleLauncher.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                    else
                    {
                        FireArmRoundType prevRoundType = asSimpleLauncher.Chamber.RoundType;
                        asSimpleLauncher.Chamber.RoundType = roundType;
                        ++ChamberPatch.chamberSkip;
                        asSimpleLauncher.Chamber.SetRound(roundClass, asSimpleLauncher.Chamber.transform.position, asSimpleLauncher.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                        asSimpleLauncher.Chamber.RoundType = prevRoundType;
                    }
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool FireSimpleLauncher2(int chamberIndex)
        {
            SimpleLauncher2 asSimpleLauncher = (SimpleLauncher2)dataObject;
            bool wasOnSA = false;
            if(asSimpleLauncher.Mode == SimpleLauncher2.fMode.sa)
            {
                wasOnSA = true;
                asSimpleLauncher.Mode = SimpleLauncher2.fMode.tr;
            }
            asSimpleLauncher.Fire();
            if (wasOnSA)
            {
                asSimpleLauncher.Mode = SimpleLauncher2.fMode.sa;
            }
            return true;
        }

        private void SetSimpleLauncher2UpdateOverride(FireArmRoundType roundType, FireArmRoundClass roundClass, int chamberIndex)
        {
            SimpleLauncher2 asSimpleLauncher = (SimpleLauncher2)dataObject;
            FireArmRoundType prevRoundType = asSimpleLauncher.Chamber.RoundType;
            asSimpleLauncher.Chamber.RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asSimpleLauncher.Chamber.SetRound(roundClass, asSimpleLauncher.Chamber.transform.position, asSimpleLauncher.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
            asSimpleLauncher.Chamber.RoundType = prevRoundType;
        }
        
        private bool UpdateSimpleLauncher()
        {
            SimpleLauncher asSimpleLauncher = (SimpleLauncher)dataObject;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[4];
                modified = true;
            }

            byte preval = data.data[0];
            byte preval0 = data.data[1];
            byte preval1 = data.data[2];
            byte preval2 = data.data[3];

            // Write chambered round class
            if (asSimpleLauncher.Chamber.GetRound() == null || asSimpleLauncher.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 2);
            }
            else
            {
                BitConverter.GetBytes((short)asSimpleLauncher.Chamber.GetRound().RoundType).CopyTo(data.data, 0);
                BitConverter.GetBytes((short)asSimpleLauncher.Chamber.GetRound().RoundClass).CopyTo(data.data, 2);
            }

            modified |= (preval != data.data[0] || preval0 != data.data[1] || preval1 != data.data[2] || preval2 != data.data[3]);

            return modified;
        }

        private bool UpdateGivenSimpleLauncher(byte[] newData)
        {
            bool modified = false;
            SimpleLauncher asSimpleLauncher = (SimpleLauncher)dataObject;

            // Set chamber
            short chamberTypeIndex = BitConverter.ToInt16(newData, 0);
            short chamberClassIndex = BitConverter.ToInt16(newData, 2);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asSimpleLauncher.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asSimpleLauncher.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asSimpleLauncher.Chamber.GetRound() == null || asSimpleLauncher.Chamber.GetRound().RoundClass != roundClass)
                {
                    if (asSimpleLauncher.Chamber.RoundType == roundType)
                    {
                        ++ChamberPatch.chamberSkip;
                        asSimpleLauncher.Chamber.SetRound(roundClass, asSimpleLauncher.Chamber.transform.position, asSimpleLauncher.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                    else
                    {
                        FireArmRoundType prevRoundType = asSimpleLauncher.Chamber.RoundType;
                        asSimpleLauncher.Chamber.RoundType = roundType;
                        ++ChamberPatch.chamberSkip;
                        asSimpleLauncher.Chamber.SetRound(roundClass, asSimpleLauncher.Chamber.transform.position, asSimpleLauncher.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                        asSimpleLauncher.Chamber.RoundType = prevRoundType;
                    }
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool FireSimpleLauncher(int chamberIndex)
        {
            SimpleLauncher asSimpleLauncher = (SimpleLauncher)dataObject;
            asSimpleLauncher.Fire();
            return true;
        }

        private void SetSimpleLauncherUpdateOverride(FireArmRoundType roundType, FireArmRoundClass roundClass, int chamberIndex)
        {
            SimpleLauncher asSimpleLauncher = (SimpleLauncher)dataObject;
            FireArmRoundType prevRoundType = asSimpleLauncher.Chamber.RoundType;
            asSimpleLauncher.Chamber.RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asSimpleLauncher.Chamber.SetRound(roundClass, asSimpleLauncher.Chamber.transform.position, asSimpleLauncher.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
            asSimpleLauncher.Chamber.RoundType = prevRoundType;
        }

        private bool UpdateRPG7()
        {
            RPG7 asRPG7 = (RPG7)dataObject;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[5];
                modified = true;
            }

            byte preval = data.data[0];

            // Write hammer state
            data.data[0] = (bool)Mod.RPG7_m_isHammerCocked.GetValue(asRPG7) ? (byte)1 : (byte)0;

            modified |= preval != data.data[0];

            preval = data.data[1];
            byte preval0 = data.data[2];
            byte preval1 = data.data[3];
            byte preval2 = data.data[4];

            // Write chambered round class
            if (asRPG7.Chamber.GetRound() == null || asRPG7.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 3);
            }
            else
            {
                BitConverter.GetBytes((short)asRPG7.Chamber.GetRound().RoundType).CopyTo(data.data, 1);
                BitConverter.GetBytes((short)asRPG7.Chamber.GetRound().RoundClass).CopyTo(data.data, 3);
            }

            modified |= (preval != data.data[1] || preval0 != data.data[2] || preval1 != data.data[3] || preval2 != data.data[4]);

            return modified;
        }

        private bool UpdateGivenRPG7(byte[] newData)
        {
            bool modified = false;
            RPG7 asRPG7 = (RPG7)dataObject;

            if (data.data == null)
            {
                modified = true;

                // Set hammer state
                Mod.RPG7_m_isHammerCocked.SetValue(asRPG7, newData[0] == 1);
            }
            else
            {
                if (data.data[0] != newData[0])
                {
                    // Set hammer state
                    Mod.RPG7_m_isHammerCocked.SetValue(asRPG7, newData[0] == 1);
                    modified = true;
                }
            }

            // Set chamber
            short chamberTypeIndex = BitConverter.ToInt16(newData, 1);
            short chamberClassIndex = BitConverter.ToInt16(newData, 3);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asRPG7.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asRPG7.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asRPG7.Chamber.GetRound() == null || asRPG7.Chamber.GetRound().RoundClass != roundClass)
                {
                    if (asRPG7.Chamber.RoundType == roundType)
                    {
                        ++ChamberPatch.chamberSkip;
                        asRPG7.Chamber.SetRound(roundClass, asRPG7.Chamber.transform.position, asRPG7.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                    else
                    {
                        FireArmRoundType prevRoundType = asRPG7.Chamber.RoundType;
                        asRPG7.Chamber.RoundType = roundType;
                        ++ChamberPatch.chamberSkip;
                        asRPG7.Chamber.SetRound(roundClass, asRPG7.Chamber.transform.position, asRPG7.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                        asRPG7.Chamber.RoundType = prevRoundType;
                    }
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool FireRPG7(int chamberIndex)
        {
            RPG7 asRPG7 = (RPG7)dataObject;
            asRPG7.Fire();
            return true;
        }

        private void SetRPG7UpdateOverride(FireArmRoundType roundType, FireArmRoundClass roundClass, int chamberIndex)
        {
            RPG7 asRPG7 = (RPG7)dataObject;
            Mod.RPG7_m_isHammerCocked.SetValue(asRPG7, true);
            FireArmRoundType prevRoundType = asRPG7.Chamber.RoundType;
            asRPG7.Chamber.RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asRPG7.Chamber.SetRound(roundClass, asRPG7.Chamber.transform.position, asRPG7.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
            asRPG7.Chamber.RoundType = prevRoundType;
        }

        private bool UpdateRollingBlock()
        {
            RollingBlock asRB = (RollingBlock)dataObject;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[5];
                modified = true;
            }

            byte preval = data.data[0];

            // Write block state
            data.data[0] = (byte)(int)Mod.RollingBlock_m_state.GetValue(asRB);

            modified |= preval != data.data[0];

            preval = data.data[1];
            byte preval0 = data.data[2];
            byte preval1 = data.data[3];
            byte preval2 = data.data[4];

            // Write chambered round class
            if (asRB.Chamber.GetRound() == null || asRB.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 3);
            }
            else
            {
                BitConverter.GetBytes((short)asRB.Chamber.GetRound().RoundType).CopyTo(data.data, 1);
                BitConverter.GetBytes((short)asRB.Chamber.GetRound().RoundClass).CopyTo(data.data, 3);
            }

            modified |= (preval != data.data[1] || preval0 != data.data[2] || preval != data.data[3] || preval0 != data.data[4]);

            return modified;
        }

        private bool UpdateGivenRollingBlock(byte[] newData)
        {
            bool modified = false;
            RollingBlock asRB = (RollingBlock)dataObject;

            if (data.data == null)
            {
                modified = true;

                // Set block state
                Mod.RollingBlock_m_state.SetValue(asRB, (RollingBlock.RollingBlockState)newData[0]);
            }
            else
            {
                if (data.data[0] != newData[0])
                {
                    // Set block state
                    Mod.RollingBlock_m_state.SetValue(asRB, (RollingBlock.RollingBlockState)newData[0]);
                    modified = true;
                }
            }

            // Set chamber
            short chamberTypeIndex = BitConverter.ToInt16(newData, 1);
            short chamberClassIndex = BitConverter.ToInt16(newData, 3);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asRB.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asRB.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asRB.Chamber.GetRound() == null || asRB.Chamber.GetRound().RoundClass != roundClass)
                {
                    if (asRB.Chamber.RoundType == roundType)
                    {
                        ++ChamberPatch.chamberSkip;
                        asRB.Chamber.SetRound(roundClass, asRB.Chamber.transform.position, asRB.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                    else
                    {
                        FireArmRoundType prevRoundType = asRB.Chamber.RoundType;
                        asRB.Chamber.RoundType = roundType;
                        ++ChamberPatch.chamberSkip;
                        asRB.Chamber.SetRound(roundClass, asRB.Chamber.transform.position, asRB.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                        asRB.Chamber.RoundType = prevRoundType;
                    }
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool FireRollingBlock(int chamberIndex)
        {
            RollingBlock asRB = (RollingBlock)dataObject;
            Mod.RollingBlock_Fire.Invoke(asRB, null);
            return true;
        }

        private void SetRollingBlockUpdateOverride(FireArmRoundType roundType, FireArmRoundClass roundClass, int chamberIndex)
        {
            RollingBlock asRB = (RollingBlock)dataObject;

            FireArmRoundType prevRoundType = asRB.Chamber.RoundType;
            asRB.Chamber.RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asRB.Chamber.SetRound(roundClass, asRB.Chamber.transform.position, asRB.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
            asRB.Chamber.RoundType = prevRoundType;
        }

        private bool UpdateRGM40()
        {
            RGM40 asRGM40 = dataObject as RGM40;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[4];
                modified = true;
            }

            byte preval = data.data[0];
            byte preval0 = data.data[1];
            byte preval1 = data.data[2];
            byte preval2 = data.data[3];

            // Write chambered round class
            if (asRGM40.Chamber.GetRound() == null || asRGM40.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 2);
            }
            else
            {
                BitConverter.GetBytes((short)asRGM40.Chamber.GetRound().RoundType).CopyTo(data.data, 0);
                BitConverter.GetBytes((short)asRGM40.Chamber.GetRound().RoundClass).CopyTo(data.data, 2);
            }

            modified |= (preval != data.data[0] || preval0 != data.data[1] || preval1 != data.data[2] || preval2 != data.data[3]);

            return modified;
        }

        private bool UpdateGivenRGM40(byte[] newData)
        {
            bool modified = false;
            RGM40 asRGM40 = dataObject as RGM40;

            // Set chamber
            short chamberTypeIndex = BitConverter.ToInt16(newData, 0);
            short chamberClassIndex = BitConverter.ToInt16(newData, 2);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asRGM40.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asRGM40.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asRGM40.Chamber.GetRound() == null || asRGM40.Chamber.GetRound().RoundClass != roundClass)
                {
                    if (asRGM40.Chamber.RoundType == roundType)
                    {
                        ++ChamberPatch.chamberSkip;
                        asRGM40.Chamber.SetRound(roundClass, asRGM40.Chamber.transform.position, asRGM40.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                    else
                    {
                        FireArmRoundType prevRoundType = asRGM40.Chamber.RoundType;
                        asRGM40.Chamber.RoundType = roundType;
                        ++ChamberPatch.chamberSkip;
                        asRGM40.Chamber.SetRound(roundClass, asRGM40.Chamber.transform.position, asRGM40.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                        asRGM40.Chamber.RoundType = prevRoundType;
                    }
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool FireRGM40(int chamberIndex)
        {
            RGM40 asRGM40 = dataObject as RGM40;

            asRGM40.Fire();
            return true;
        }

        private void SetRGM40UpdateOverride(FireArmRoundType roundType, FireArmRoundClass roundClass, int chamberIndex)
        {
            RGM40 asRGM40 = dataObject as RGM40;

            FireArmRoundType prevRoundType = asRGM40.Chamber.RoundType;
            asRGM40.Chamber.RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asRGM40.Chamber.SetRound(roundClass, asRGM40.Chamber.transform.position, asRGM40.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
            asRGM40.Chamber.RoundType = prevRoundType;
        }

        private bool UpdateRemoteMissileLauncher()
        {
            RemoteMissileLauncher asRML = dataObject as RemoteMissileLauncher;
            bool modified = false;

            RemoteMissile missile = Mod.RemoteMissileLauncher_m_missile.GetValue(asRML) as RemoteMissile;

            if (data.data == null)
            {
                data.data = new byte[missile == null ? 3 : 35];
                modified = true;
            }
            else
            {
                if (missile == null)
                {
                    if (data.data.Length == 35)
                    {
                        data.data = new byte[3];
                        modified = true;
                    }
                }
                else
                {
                    if (data.data.Length == 3)
                    {
                        data.data = new byte[35];
                        modified = true;
                    }
                }
            }

            byte preval0 = data.data[0];

            // Write poweredUp
            data.data[0] = asRML.IsPoweredUp ? (byte)1 : (byte)0;

            modified |= preval0 != data.data[0];

            preval0 = data.data[1];

            // Write chamber full
            data.data[1] = asRML.Chamber.IsFull ? (byte)1 : (byte)0;

            modified |= preval0 != data.data[1];

            preval0 = data.data[2];

            // Write has missile
            data.data[2] = missile == null ? (byte)0 : (byte)1;

            modified |= preval0 != data.data[2];

            if (missile != null)
            {
                modified = true;

                // Write missile pos
                BitConverter.GetBytes(missile.transform.position.x).CopyTo(data.data, 3);
                BitConverter.GetBytes(missile.transform.position.y).CopyTo(data.data, 7);
                BitConverter.GetBytes(missile.transform.position.z).CopyTo(data.data, 11);

                // Write missile rot
                BitConverter.GetBytes(missile.transform.rotation.eulerAngles.x).CopyTo(data.data, 15);
                BitConverter.GetBytes(missile.transform.rotation.eulerAngles.y).CopyTo(data.data, 19);
                BitConverter.GetBytes(missile.transform.rotation.eulerAngles.z).CopyTo(data.data, 23);

                // Write target speed
                BitConverter.GetBytes((float)Mod.RemoteMissile_speed.GetValue(missile)).CopyTo(data.data, 27);

                // Write speed
                BitConverter.GetBytes((float)Mod.RemoteMissile_tarSpeed.GetValue(missile)).CopyTo(data.data, 31);
            }

            return modified;
        }

        private bool UpdateGivenRemoteMissileLauncher(byte[] newData)
        {
            bool modified = false;
            RemoteMissileLauncher asRML = dataObject as RemoteMissileLauncher;

            // Set powered up
            if((asRML.IsPoweredUp && newData[0] == 0) || (!asRML.IsPoweredUp && newData[0] == 1))
            {
                // Toggle
                asRML.TogglePower();
                modified = true;
            }

            if (newData[1] == 0) // We don't want round in chamber
            {
                if (asRML.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asRML.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                if (asRML.Chamber.GetRound() == null)
                {
                    ++ChamberPatch.chamberSkip;
                    asRML.Chamber.SetRound(FireArmRoundClass.FragExplosive, asRML.Chamber.transform.position, asRML.Chamber.transform.rotation);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }

            RemoteMissile missile = Mod.RemoteMissileLauncher_m_missile.GetValue(asRML) as RemoteMissile;
            if (missile != null)
            {
                if (newData[2] == 1)
                {
                    missile.transform.position = new Vector3(BitConverter.ToSingle(newData, 3), BitConverter.ToSingle(newData, 7), BitConverter.ToSingle(newData, 11));
                    missile.transform.rotation = Quaternion.Euler(BitConverter.ToSingle(newData, 15), BitConverter.ToSingle(newData, 19), BitConverter.ToSingle(newData, 23));

                    Mod.RemoteMissile_speed.SetValue(missile, BitConverter.ToSingle(newData, 27));
                    Mod.RemoteMissile_tarSpeed.SetValue(missile, BitConverter.ToSingle(newData, 31));
                } 
                //else if (newData[2] == 0)
                //{
                //    NOTE: This would destroy the current missile we have if the update says we do not want a missile
                //          The problem is that, once the controller detonates the missile, detonation gets sent, but the latest update which says there is no missile
                //          gets received first, so the missile would get destroyed here before we receive the detonation, detonation would always fail
                //          Kept here as an example of update desync
                //    GameObject.Destroy(actualMissile.gameObject);
                //}
            }

            data.data = newData;

            return modified;
        }

        private bool FireRemoteMissileLauncher(int chamberIndex)
        {
            RemoteMissileLauncher asRML = dataObject as RemoteMissileLauncher;
            Mod.RemoteMissileLauncher_FireShot.Invoke(asRML, null);
            return true;
        }

        private void SetRemoteMissileLauncherUpdateOverride(FireArmRoundType roundType, FireArmRoundClass roundClass, int chamberIndex)
        {
            RemoteMissileLauncher asRML = dataObject as RemoteMissileLauncher;

            FireArmRoundType prevRoundType = asRML.Chamber.RoundType;
            asRML.Chamber.RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asRML.Chamber.SetRound(roundClass, asRML.Chamber.transform.position, asRML.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
            asRML.Chamber.RoundType = prevRoundType;
        }

        private bool UpdatePotatoGun()
        {
            PotatoGun asPG = dataObject as PotatoGun;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[5];
                modified = true;
            }

            byte preval0 = data.data[0];
            byte preval1 = data.data[1];
            byte preval2 = data.data[2];
            byte preval3 = data.data[3];

            // Write m_chamberGas
            BitConverter.GetBytes((float)Mod.PotatoGun_m_chamberGas.GetValue(asPG)).CopyTo(data.data, 0);

            modified |= (preval0 != data.data[0] || preval1 != data.data[1] || preval2 != data.data[2] || preval3 != data.data[3]);

            preval0 = data.data[4];

            // Write chamber full
            data.data[4] = asPG.Chamber.IsFull ? (byte)1 : (byte)0;

            modified |= preval0 != data.data[4];

            return modified;
        }

        private bool UpdateGivenPotatoGun(byte[] newData)
        {
            bool modified = false;
            PotatoGun asPG = dataObject as PotatoGun;

            if (data.data == null)
            {
                modified = true;

                // Set m_chamberGas
                Mod.PotatoGun_m_chamberGas.SetValue(asPG, BitConverter.ToSingle(newData, 0));
            }
            else
            {
                if (data.data[0] != newData[0] ||data.data[1] != newData[1] ||data.data[2] != newData[2] ||data.data[3] != newData[3])
                {
                    // Set m_chamberGas
                    Mod.PotatoGun_m_chamberGas.SetValue(asPG, BitConverter.ToSingle(newData, 0));
                    modified = true;
                }
            }

            if (newData[4] == 0) // We don't want round in chamber
            {
                if (asPG.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asPG.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                if (asPG.Chamber.GetRound() == null)
                {
                    ++ChamberPatch.chamberSkip;
                    asPG.Chamber.SetRound(FireArmRoundClass.FMJ, asPG.Chamber.transform.position, asPG.Chamber.transform.rotation);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool FirePotatoGun(int chamberIndex)
        {
            PotatoGun asPG = dataObject as PotatoGun;
            asPG.Fire();
            return true;
        }

        private void SetPotatoGunUpdateOverride(FireArmRoundType roundType, FireArmRoundClass roundClass, int chamberIndex)
        {
            PotatoGun asPG = dataObject as PotatoGun;

            FireArmRoundType prevRoundType = asPG.Chamber.RoundType;
            asPG.Chamber.RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asPG.Chamber.SetRound(roundClass, asPG.Chamber.transform.position, asPG.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
            asPG.Chamber.RoundType = prevRoundType;
        }

        private bool UpdateMinigun()
        {
            Minigun asMinigun = dataObject as Minigun;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[8];
                modified = true;
            }

            byte preval0 = data.data[0];
            byte preval1 = data.data[1];
            byte preval2 = data.data[2];
            byte preval3 = data.data[3];

            // Write heat
            BitConverter.GetBytes((float)Mod.Minigun_m_heat.GetValue(asMinigun)).CopyTo(data.data, 0);

            modified |= (preval0 != data.data[0] || preval1 != data.data[1] || preval2 != data.data[2] || preval3 != data.data[3]);

            preval0 = data.data[4];
            preval1 = data.data[5];
            preval2 = data.data[6];
            preval3 = data.data[7];

            // Write heat
            BitConverter.GetBytes((float)Mod.Minigun_m_motorRate.GetValue(asMinigun)).CopyTo(data.data, 4);

            modified |= (preval0 != data.data[4] || preval1 != data.data[5] || preval2 != data.data[6] || preval3 != data.data[7]);

            return modified;
        }

        private bool UpdateGivenMinigun(byte[] newData)
        {
            bool modified = false;
            Minigun asMinigun = dataObject as Minigun;

            if (data.data == null)
            {
                modified = true;

                // Set heat
                Mod.Minigun_m_heat.SetValue(asMinigun, BitConverter.ToSingle(newData, 0));

                // Set motorrate
                Mod.Minigun_m_motorRate.SetValue(asMinigun, BitConverter.ToSingle(newData, 4));
            }
            else
            {
                if (data.data[0] != newData[0] ||data.data[1] != newData[1] ||data.data[2] != newData[2] ||data.data[3] != newData[3])
                {
                    // Set heat
                    Mod.Minigun_m_heat.SetValue(asMinigun, BitConverter.ToSingle(newData, 0));
                    modified = true;
                }
                if (data.data[4] != newData[4] || data.data[5] != newData[5] || data.data[6] != newData[6] || data.data[7] != newData[7])
                {
                    // Set motorrate
                    Mod.Minigun_m_motorRate.SetValue(asMinigun, BitConverter.ToSingle(newData, 4));
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool UpdateM72()
        {
            M72 asM72 = dataObject as M72;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[4];
                modified = true;
            }

            byte preval = data.data[0];

            // Write safety
            data.data[0] = (bool)Mod.M72_m_isSafetyEngaged.GetValue(asM72) ? (byte)1 : (byte)0;

            modified |= preval != data.data[0];

            preval = data.data[1];

            // Write m_isCapOpen
            data.data[1] = asM72.CanTubeBeGrabbed()? (byte)1 : (byte)0;

            modified |= preval != data.data[1];

            preval = data.data[2];

            // Write tube state
            data.data[2] = (byte)asM72.TState;

            modified |= preval != data.data[2];

            preval = data.data[3];

            // Write chamber full
            data.data[3] = asM72.Chamber.IsFull ? (byte)1 : (byte)0;

            modified |= preval != data.data[3];

            return modified;
        }

        private bool UpdateGivenM72(byte[] newData)
        {
            bool modified = false;
            M72 asM72 = dataObject as M72;

            if (data.data == null)
            {
                modified = true;

                // Set safety
                bool currentSafety = (bool)Mod.M72_m_isSafetyEngaged.GetValue(asM72);
                if ((currentSafety && newData[0] == 0) || (!currentSafety && newData[0] == 1)) 
                {
                    asM72.ToggleSafety();
                }

                // Set cap
                if((asM72.CanTubeBeGrabbed() && newData[1] == 0)||(!asM72.CanTubeBeGrabbed() && newData[1] == 1))
                {
                    asM72.ToggleCap();
                }

                // Set Tube state
                if((asM72.TState == M72.TubeState.Forward || asM72.TState == M72.TubeState.Mid) && newData[2] == 2)
                {
                    asM72.TState = M72.TubeState.Rear;
                    asM72.Tube.transform.localPosition = asM72.Tube_Rear.localPosition;
                }
                else if((asM72.TState == M72.TubeState.Mid || asM72.TState == M72.TubeState.Rear) && newData[2] == 0)
                {
                    asM72.TState = M72.TubeState.Forward;
                    asM72.Tube.transform.localPosition = asM72.Tube_Front.localPosition;
                }
            }
            else
            {
                // Set safety
                bool currentSafety = (bool)Mod.M72_m_isSafetyEngaged.GetValue(asM72);
                if ((currentSafety && newData[0] == 0) || (!currentSafety && newData[0] == 1))
                {
                    asM72.ToggleSafety();
                    modified = true;
                }
                // Set cap
                if ((asM72.CanTubeBeGrabbed() && newData[1] == 0) || (!asM72.CanTubeBeGrabbed() && newData[1] == 1))
                {
                    asM72.ToggleCap();
                }
                modified = true;
                // Set Tube state
                if ((asM72.TState == M72.TubeState.Forward || asM72.TState == M72.TubeState.Mid) && newData[2] == 2)
                {
                    asM72.TState = M72.TubeState.Rear;
                    asM72.Tube.transform.localPosition = asM72.Tube_Rear.localPosition;
                    modified = true;
                }
                else if ((asM72.TState == M72.TubeState.Mid || asM72.TState == M72.TubeState.Rear) && newData[2] == 0)
                {
                    asM72.TState = M72.TubeState.Forward;
                    asM72.Tube.transform.localPosition = asM72.Tube_Front.localPosition;
                    modified = true;
                }
            }

            if (newData[3] == 0) // We don't want round in chamber
            {
                if (asM72.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asM72.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                if (asM72.Chamber.GetRound() == null)
                {
                    ++ChamberPatch.chamberSkip;
                    asM72.Chamber.SetRound(FireArmRoundClass.FragExplosive, asM72.Chamber.transform.position, asM72.Chamber.transform.rotation);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool FireM72(int chamberIndex)
        {
            M72 asM72 = dataObject as M72;
            asM72.Fire();
            return true;
        }

        private void SetM72UpdateOverride(FireArmRoundType roundType, FireArmRoundClass roundClass, int chamberIndex)
        {
            M72 asM72 = dataObject as M72;

            FireArmRoundType prevRoundType = asM72.Chamber.RoundType;
            asM72.Chamber.RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asM72.Chamber.SetRound(roundClass, asM72.Chamber.transform.position, asM72.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
            asM72.Chamber.RoundType = prevRoundType;
        }

        private bool UpdateOpenBoltReceiver()
        {
            OpenBoltReceiver asOBR = dataObject as OpenBoltReceiver;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[6];
                modified = true;
            }

            byte preval = data.data[0];

            // Write fire mode index
            data.data[0] = (byte)asOBR.FireSelectorModeIndex;

            modified |= preval != data.data[0];

            preval = data.data[1];

            // Write camBurst
            data.data[1] = (byte)(int)Mod.OpenBoltReceiver_m_CamBurst.GetValue(asOBR);

            modified |= preval != data.data[1];

            preval = data.data[2];
            byte preval0 = data.data[3];
            byte preval1 = data.data[4];
            byte preval2 = data.data[5];

            // Write chambered round class
            if (asOBR.Chamber.GetRound() == null || asOBR.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 4);
            }
            else
            {
                BitConverter.GetBytes((short)asOBR.Chamber.GetRound().RoundType).CopyTo(data.data, 2);
                BitConverter.GetBytes((short)asOBR.Chamber.GetRound().RoundClass).CopyTo(data.data, 4);
            }

            modified |= (preval != data.data[2] || preval0 != data.data[3] || preval1 != data.data[4] || preval2 != data.data[5]);

            return modified;
        }

        private bool UpdateGivenOpenBoltReceiver(byte[] newData)
        {
            bool modified = false;
            OpenBoltReceiver asOBR = dataObject as OpenBoltReceiver;

            if (data.data == null)
            {
                modified = true;

                // Set fire select mode
                Mod.OpenBoltReceiver_m_fireSelectorMode.SetValue(asOBR, (int)newData[0]);

                // Set camBurst
                Mod.OpenBoltReceiver_m_CamBurst.SetValue(asOBR, (int)newData[1]);
            }
            else
            {
                if (data.data[0] != newData[0])
                {
                    // Set fire select mode
                    Mod.OpenBoltReceiver_m_fireSelectorMode.SetValue(asOBR, (int)newData[0]);
                    modified = true;
                }
                if (data.data[1] != newData[1])
                {
                    // Set camBurst
                    Mod.OpenBoltReceiver_m_CamBurst.SetValue(asOBR, (int)newData[1]);
                    modified = true;
                }
            }

            // Set chamber
            short chamberTypeIndex = BitConverter.ToInt16(newData, 2);
            short chamberClassIndex = BitConverter.ToInt16(newData, 4);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asOBR.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asOBR.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asOBR.Chamber.GetRound() == null || asOBR.Chamber.GetRound().RoundClass != roundClass)
                {
                    if (asOBR.Chamber.RoundType == roundType)
                    {
                        ++ChamberPatch.chamberSkip;
                        asOBR.Chamber.SetRound(roundClass, asOBR.Chamber.transform.position, asOBR.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                    else
                    {
                        FireArmRoundType prevRoundType = asOBR.Chamber.RoundType;
                        asOBR.Chamber.RoundType = roundType;
                        ++ChamberPatch.chamberSkip;
                        asOBR.Chamber.SetRound(roundClass, asOBR.Chamber.transform.position, asOBR.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                        asOBR.Chamber.RoundType = prevRoundType;
                    }
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private void SetOBRUpdateOverride(FireArmRoundType roundType, FireArmRoundClass roundClass, int chamberIndex)
        {
            OpenBoltReceiver asOBR = dataObject as OpenBoltReceiver;

            FireArmRoundType prevRoundType = asOBR.Chamber.RoundType;
            asOBR.Chamber.RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asOBR.Chamber.SetRound(roundClass, asOBR.Chamber.transform.position, asOBR.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
            asOBR.Chamber.RoundType = prevRoundType;
        }

        private void ChamberOpenBoltReceiverRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            OpenBoltReceiver asOBR = dataObject as OpenBoltReceiver;

            ++ChamberPatch.chamberSkip;
            asOBR.Chamber.SetRound(roundClass, asOBR.Chamber.transform.position, asOBR.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
        }

        private bool FireOBR(int chamberIndex)
        {
            return (dataObject as OpenBoltReceiver).Fire();
        }

        private bool UpdateHCB()
        {
            HCB asHCB = dataObject as HCB;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[6];
                modified = true;
            }

            byte preval0 = data.data[0];

            // Write m_sledState
            data.data[0] = (byte)(int)Mod.HCB_m_sledState.GetValue(asHCB);

            modified |= preval0 != data.data[0];

            preval0 = data.data[1];

            // Write chamber accessible
            data.data[1] = asHCB.Chamber.IsAccessible ? (byte)1 : (byte)0;

            modified |= preval0 != data.data[1];

            // Write chambered rounds
            preval0 = data.data[2];
            byte preval1 = data.data[3];
            byte preval2 = data.data[4];
            byte preval3 = data.data[5];

            if (asHCB.Chamber.GetRound() == null || asHCB.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 4);
            }
            else
            {
                BitConverter.GetBytes((short)asHCB.Chamber.GetRound().RoundType).CopyTo(data.data, 2);
                BitConverter.GetBytes((short)asHCB.Chamber.GetRound().RoundClass).CopyTo(data.data, 4);
            }

            modified |= (preval0 != data.data[2] || preval1 != data.data[3] || preval2 != data.data[4] || preval3 != data.data[5]);

            return modified;
        }

        private bool UpdateGivenHCB(byte[] newData)
        {
            bool modified = false;
            HCB asHCB = dataObject as HCB;

            if (data.data == null)
            {
                modified = true;

                // Set m_sledState
                Mod.HCB_m_sledState.SetValue(asHCB, newData[0]);

                // Set chamber accessible
                asHCB.Chamber.IsAccessible = newData[1] == 1;
            }
            else
            {
                if (data.data[0] != newData[0])
                {
                    // Set m_sledState
                    Mod.HCB_m_sledState.SetValue(asHCB, newData[0]);
                    modified = true;
                }
                if (data.data[1] != newData[1])
                {
                    // Set chamber accessible
                    asHCB.Chamber.IsAccessible = newData[1] == 1;
                    modified = true;
                }
            }

            // Set chamber
            short chamberTypeIndex = BitConverter.ToInt16(newData, 2);
            short chamberClassIndex = BitConverter.ToInt16(newData, 4);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asHCB.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asHCB.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asHCB.Chamber.GetRound() == null || asHCB.Chamber.GetRound().RoundClass != roundClass)
                {
                    if (asHCB.Chamber.RoundType == roundType)
                    {
                        ++ChamberPatch.chamberSkip;
                        asHCB.Chamber.SetRound(roundClass, asHCB.Chamber.transform.position, asHCB.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                    else
                    {
                        FireArmRoundType prevRoundType = asHCB.Chamber.RoundType;
                        asHCB.Chamber.RoundType = roundType;
                        ++ChamberPatch.chamberSkip;
                        asHCB.Chamber.SetRound(roundClass, asHCB.Chamber.transform.position, asHCB.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                        asHCB.Chamber.RoundType = prevRoundType;
                    }
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool UpdateGrappleGun()
        {
            GrappleGun asGG = dataObject as GrappleGun;
            bool modified = false;

            int necessarySize = asGG.Chambers.Length * 4 + 2;

            if (data.data == null)
            {
                data.data = new byte[necessarySize];
                modified = true;
            }

            byte preval0 = data.data[0];

            // Write cur chamber
            data.data[0] = (byte)(int)Mod.GrappleGun_m_curChamber.GetValue(asGG);

            modified |= preval0 != data.data[0];

            preval0 = data.data[1];

            // Write mag loaded
            data.data[1] = asGG.IsMagLoaded ? (byte)1 : (byte)0;

            modified |= preval0 != data.data[1];

            // Write chambered rounds
            byte preval1;
            byte preval2;
            byte preval3;
            for (int i = 0; i < asGG.Chambers.Length; ++i)
            {
                int firstIndex = i * 4 + 2;
                preval0 = data.data[firstIndex];
                preval1 = data.data[firstIndex + 1];
                preval2 = data.data[firstIndex + 2];
                preval3 = data.data[firstIndex + 3];

                if (asGG.Chambers[i].GetRound() == null || asGG.Chambers[i].GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(data.data, firstIndex + 2);
                }
                else
                {
                    BitConverter.GetBytes((short)asGG.Chambers[i].GetRound().RoundType).CopyTo(data.data, firstIndex);
                    BitConverter.GetBytes((short)asGG.Chambers[i].GetRound().RoundClass).CopyTo(data.data, firstIndex + 2);
                }

                modified |= (preval0 != data.data[firstIndex] || preval1 != data.data[firstIndex + 1] || preval2 != data.data[firstIndex + 2] || preval3 != data.data[firstIndex + 3]);
            }

            return modified;
        }

        private bool UpdateGivenGrappleGun(byte[] newData)
        {
            bool modified = false;
            GrappleGun asGG = dataObject as GrappleGun;

            if (data.data == null)
            {
                modified = true;

                // Set cur chamber
                Mod.GrappleGun_m_curChamber.SetValue(asGG, newData[0]);

                // Set mag loaded
                bool newCylLoaded = newData[1] == 1;
                if (newCylLoaded && !asGG.IsMagLoaded)
                {
                    // Load cylinder, chambers will be updated separately
                    asGG.ProxyMag.gameObject.SetActive(true);
                    asGG.PlayAudioEvent(FirearmAudioEventType.MagazineIn, 1f);
                }
                else if (!newCylLoaded && asGG.IsMagLoaded)
                {
                    // Eject cylinder, chambers will be updated separately, handling the spawn of a physical cylinder will also be handled separately
                    asGG.PlayAudioEvent(FirearmAudioEventType.MagazineOut, 1f);
                    asGG.EjectDelay = 0.4f;
                    asGG.ProxyMag.gameObject.SetActive(false);
                }
                asGG.IsMagLoaded = newCylLoaded;
            }
            else
            {
                if (data.data[0] != newData[0])
                {
                    // Set cur chamber
                    Mod.GrappleGun_m_curChamber.SetValue(asGG, newData[0]);
                    modified = true;
                }
                // Set cyl loaded
                bool newCylLoaded = newData[1] == 1;
                if (newCylLoaded && !asGG.IsMagLoaded)
                {
                    // Load cylinder, chambers will be updated separately
                    asGG.ProxyMag.gameObject.SetActive(true);
                    asGG.PlayAudioEvent(FirearmAudioEventType.MagazineIn, 1f);
                    modified = true;
                }
                else if (!newCylLoaded && asGG.IsMagLoaded)
                {
                    // Eject cylinder, chambers will be updated separately, handling the spawn of a physical cylinder will also be handled separately
                    asGG.PlayAudioEvent(FirearmAudioEventType.MagazineOut, 1f);
                    asGG.EjectDelay = 0.4f;
                    asGG.ProxyMag.gameObject.SetActive(false);
                    modified = true;
                }
                asGG.IsMagLoaded = newCylLoaded;
            }

            // Set chambers
            for (int i = 0; i < asGG.Chambers.Length; ++i)
            {
                int firstIndex = i * 4 + 2;
                short chamberTypeIndex = BitConverter.ToInt16(newData, firstIndex);
                short chamberClassIndex = BitConverter.ToInt16(newData, firstIndex + 2);
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (asGG.Chambers[i].GetRound() != null)
                    {
                        ++ChamberPatch.chamberSkip;
                        asGG.Chambers[i].SetRound(null, false);
                        --ChamberPatch.chamberSkip;
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (asGG.Chambers[i].GetRound() == null || asGG.Chambers[i].GetRound().RoundClass != roundClass)
                    {
                        if (asGG.Chambers[i].RoundType == roundType)
                        {
                            ++ChamberPatch.chamberSkip;
                            asGG.Chambers[i].SetRound(roundClass, asGG.Chambers[i].transform.position, asGG.Chambers[i].transform.rotation);
                            --ChamberPatch.chamberSkip;
                        }
                        else
                        {
                            FireArmRoundType prevRoundType = asGG.Chambers[i].RoundType;
                            asGG.Chambers[i].RoundType = roundType;
                            ++ChamberPatch.chamberSkip;
                            asGG.Chambers[i].SetRound(roundClass, asGG.Chambers[i].transform.position, asGG.Chambers[i].transform.rotation);
                            --ChamberPatch.chamberSkip;
                            asGG.Chambers[i].RoundType = prevRoundType;
                        }
                        modified = true;
                    }
                }
            }

            data.data = newData;

            return modified;
        }

        public int GetGrappleGunChamberIndex(FVRFireArmChamber chamber)
        {
            GrappleGun asGG = dataObject as GrappleGun;
            FVRFireArmChamber[] chambers = asGG.Chambers;

            if (chamber == null)
            {
                return -1;
            }

            for(int i=0; i < chambers.Length; ++i)
            {
                if (chambers[i] == chamber)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool UpdateGBeamer()
        {
            GBeamer asGBeamer = dataObject as GBeamer;
            
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[7];
                modified = true;
            }

            byte preval = data.data[0];

            // Write battery switch state
            data.data[0] = ((bool)Mod.GBeamer_m_isBatterySwitchedOn.GetValue(asGBeamer)) ? (byte)1 : (byte)0;

            modified |= preval != data.data[0];

            preval = data.data[1];

            // Write capacitor switch state
            data.data[1] = ((bool)Mod.GBeamer_m_isCapacitorSwitchedOn.GetValue(asGBeamer)) ? (byte)1 : (byte)0;

            modified |= preval != data.data[1];

            preval = data.data[2];

            // Write motor switch state
            data.data[2] = ((bool)Mod.GBeamer_m_isMotorSwitchedOn.GetValue(asGBeamer)) ? (byte)1 : (byte)0;

            modified |= preval != data.data[2];

            byte preval0 = data.data[3];
            byte preval1 = data.data[4];
            byte preval2 = data.data[5];
            byte preval3 = data.data[6];

            // Write cap charge
            BitConverter.GetBytes((float)Mod.GBeamer_m_capacitorCharge.GetValue(asGBeamer)).CopyTo(data.data, 3);

            modified |= (preval0 != data.data[3] || preval1 != data.data[4] || preval2 != data.data[5] || preval3 != data.data[6]);

            return modified;
        }

        private bool UpdateGivenGBeamer(byte[] newData)
        {
            bool modified = false;
            GBeamer asGBeamer = dataObject as GBeamer;

            // Set battery switch state
            bool preVal = (bool)Mod.GBeamer_m_isBatterySwitchedOn.GetValue(asGBeamer);
            if((preVal && newData[0] == 0) || (!preVal && newData[0] == 1))
            {
                // Toggle
                asGBeamer.BatterySwitch.ToggleSwitch(false);
                modified = true;
            }

            // Set capacitor switch state
            preVal = (bool)Mod.GBeamer_m_isCapacitorSwitchedOn.GetValue(asGBeamer);
            if((preVal && newData[1] == 0) || (!preVal && newData[1] == 1))
            {
                // Toggle
                asGBeamer.CapacitorSwitch.ToggleSwitch(false);
                modified = true;
            }

            // Set motor switch state
            preVal = (bool)Mod.GBeamer_m_isMotorSwitchedOn.GetValue(asGBeamer);
            if((preVal && newData[2] == 0) || (!preVal && newData[2] == 1))
            {
                // Toggle
                asGBeamer.MotorSwitch.ToggleSwitch(false);
                modified = true;
            }

            float capPreval = (float)Mod.GBeamer_m_capacitorCharge.GetValue(asGBeamer);
            float newCapCharge = BitConverter.ToSingle(newData, 3);
            if(capPreval != newCapCharge)
            {
                Mod.GBeamer_m_capacitorCharge.SetValue(asGBeamer, newCapCharge);
                modified = true;
            }

            data.data = newData;

            return modified;
        }

        private bool UpdateFlintlockWeapon()
        {
            FlintlockWeapon asFLW = physicalObject as FlintlockWeapon;
            
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[6];
                modified = true;
            }

            byte preval = data.data[0];

            // Write hammer state
            data.data[0] = (byte)asFLW.HammerState;

            modified |= preval != data.data[0];

            preval = data.data[1];

            // Write has flint
            data.data[1] = asFLW.HasFlint() ? (byte)1 : (byte)0;

            modified |= preval != data.data[1];

            preval = data.data[2];

            // Write flint state
            data.data[2] = (byte)asFLW.FState;

            modified |= preval != data.data[2];

            byte preval0 = data.data[3];
            byte preval1 = data.data[4];
            byte preval2 = data.data[5];

            // Write flint uses
            Vector3 uses = (Vector3)Mod.FlintlockWeapon_m_flintUses.GetValue(asFLW);
            data.data[3] = (byte)(int)uses.x;
            data.data[4] = (byte)(int)uses.y;
            data.data[5] = (byte)(int)uses.z;

            modified |= (preval0 != data.data[3] || preval1 != data.data[4] || preval2 != data.data[5]);

            return modified;
        }

        private bool UpdateGivenFlintlockWeapon(byte[] newData)
        {
            bool modified = false;
            FlintlockWeapon asFLW = physicalObject as FlintlockWeapon;

            // Set hammer state
            FlintlockWeapon.HState preVal = asFLW.HammerState;

            asFLW.HammerState = (FlintlockWeapon.HState)newData[0];

            modified |= preVal != asFLW.HammerState;

            // Set hasFlint
            bool preVal0 = asFLW.HasFlint();

            Mod.FlintlockWeapon_m_hasFlint.SetValue(asFLW, newData[1] == 1);

            modified |= preVal0 ^ asFLW.HasFlint();

            // Set flint state
            FlintlockWeapon.FlintState preVal1 = asFLW.FState;

            asFLW.FState = (FlintlockWeapon.FlintState)newData[2];

            modified |= preVal1 != asFLW.FState;

            Vector3 preUses = (Vector3)Mod.FlintlockWeapon_m_flintUses.GetValue(asFLW);

            // Write flint uses
            Vector3 uses = Vector3.zero;
            uses.x = newData[3];
            uses.y = newData[4];
            uses.z = newData[5];
            Mod.FlintlockWeapon_m_flintUses.SetValue(asFLW, uses);

            modified |= !preUses.Equals(uses);

            data.data = newData;

            return modified;
        }

        private bool UpdateFlaregun()
        {
            Flaregun asFG = dataObject as Flaregun;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[5];
                modified = true;
            }

            byte preval = data.data[0];

            // Write hammer state
            data.data[0] = (bool)Mod.Flaregun_m_isHammerCocked.GetValue(asFG) ? (byte)1: (byte)0;

            modified |= preval != data.data[0];

            preval = data.data[1];
            byte preval0 = data.data[2];

            // Write chambered round class
            if (asFG.Chamber.GetRound() == null || asFG.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 3);
            }
            else
            {
                BitConverter.GetBytes((short)asFG.Chamber.GetRound().RoundType).CopyTo(data.data, 1);
                BitConverter.GetBytes((short)asFG.Chamber.GetRound().RoundClass).CopyTo(data.data, 3);
            }

            modified |= (preval != data.data[1] || preval0 != data.data[2]);

            return modified;
        }

        private bool UpdateGivenFlaregun(byte[] newData)
        {
            bool modified = false;
            Flaregun asFG = dataObject as Flaregun;

            // Set hammer state
            bool preVal = (bool)Mod.Flaregun_m_isHammerCocked.GetValue(asFG);

            asFG.SetHammerCocked(newData[0] == 1);

            modified |= preVal ^ (newData[0] == 1);

            // Set chamber
            short chamberTypeIndex = BitConverter.ToInt16(newData, 1);
            short chamberClassIndex = BitConverter.ToInt16(newData, 3);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asFG.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asFG.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asFG.Chamber.GetRound() == null || asFG.Chamber.GetRound().RoundClass != roundClass)
                {
                    if (asFG.Chamber.RoundType == roundType)
                    {
                        ++ChamberPatch.chamberSkip;
                        asFG.Chamber.SetRound(roundClass, asFG.Chamber.transform.position, asFG.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                    else
                    {
                        FireArmRoundType prevRoundType = asFG.Chamber.RoundType;
                        asFG.Chamber.RoundType = roundType;
                        ++ChamberPatch.chamberSkip;
                        asFG.Chamber.SetRound(roundClass, asFG.Chamber.transform.position, asFG.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                        asFG.Chamber.RoundType = prevRoundType;
                    }
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool FireFlaregun(int chamberIndex)
        {
            Mod.Flaregun_Fire.Invoke((dataObject as Flaregun), null);
            return true;
        }

        private void SetFlaregunUpdateOverride(FireArmRoundType roundType, FireArmRoundClass roundClass, int chamberIndex)
        {
            Flaregun asFG = dataObject as Flaregun;

            FireArmRoundType prevRoundType = asFG.Chamber.RoundType;
            asFG.Chamber.RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asFG.Chamber.SetRound(roundClass, asFG.Chamber.transform.position, asFG.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
            asFG.Chamber.RoundType = prevRoundType;
        }

        private bool UpdateFlameThrower()
        {
            FlameThrower asFT = dataObject as FlameThrower;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[1];
                modified = true;
            }

            // Write firing
            byte preval = data.data[0];

            data.data[0] = (bool)Mod.FlameThrower_m_isFiring.GetValue(asFT) ? (byte)1 : (byte)0;

            modified |= preval != data.data[0];

            return modified;
        }

        private bool UpdateGivenFlameThrower(byte[] newData)
        {
            bool modified = false;
            FlameThrower asFT = dataObject as FlameThrower;

            // Set firing
            bool currentFiring = (bool)Mod.FlameThrower_m_isFiring.GetValue(asFT);
            if (currentFiring && newData[0] == 0)
            {
                // Stop firing
                Mod.FlameThrower_StopFiring.Invoke(asFT, null);
                modified = true;
            }
            else if(!currentFiring && newData[0] == 1)
            {
                // Start firing
                Mod.FlameThrower_m_hasFiredStartSound.SetValue(asFT, true);
                SM.PlayCoreSound(FVRPooledAudioType.GenericClose, asFT.AudEvent_Ignite, asFT.GetMuzzle().position);
                asFT.AudSource_FireLoop.volume = 0.4f;
                float vlerp;
                if (asFT.UsesValve)
                {
                    vlerp = asFT.Valve.ValvePos;
                }
                else if (asFT.UsesMF2Valve)
                {
                    vlerp = asFT.MF2Valve.Lerp;
                }
                else
                {
                    vlerp = 0.5f;
                }
                asFT.AudSource_FireLoop.pitch = Mathf.Lerp(asFT.AudioPitchRange.x, asFT.AudioPitchRange.y, vlerp);
                if (!asFT.AudSource_FireLoop.isPlaying)
                {
                    asFT.AudSource_FireLoop.Play();
                }
                modified = true;
            }

            data.data = newData;

            return modified;
        }

        private bool UpdateCarlGustaf()
        {
            CarlGustaf asCG = dataObject as CarlGustaf;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[8];
                modified = true;
            }

            // Write chamber round
            byte preval0 = data.data[0];
            byte preval1 = data.data[1];
            byte preval2 = data.data[2];
            byte preval3 = data.data[3];

            if (asCG.Chamber.GetRound() == null || asCG.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 2);
            }
            else
            {
                BitConverter.GetBytes((short)asCG.Chamber.GetRound().RoundType).CopyTo(data.data, 0);
                BitConverter.GetBytes((short)asCG.Chamber.GetRound().RoundClass).CopyTo(data.data, 2);
            }

            modified |= (preval0 != data.data[0] || preval1 != data.data[1] || preval2 != data.data[2] || preval3 != data.data[3]);

            // Write charge handle state
            preval0 = data.data[4];

            data.data[4] = (byte)asCG.CHState;

            modified |= preval0 != data.data[4];

            // Write lid latch state
            preval0 = data.data[5];

            data.data[5] = (byte)asCG.TailLatch.LState;

            modified |= preval0 != data.data[5];

            // Write shell slide state
            preval0 = data.data[6];

            data.data[6] = (byte)asCG.ShellInsertEject.CSState;

            modified |= preval0 != data.data[6];

            // Write lock latch state
            preval0 = data.data[7];

            data.data[7] = (byte)asCG.TailLatch.RestrictingLatch.LState;

            modified |= preval0 != data.data[7];

            return modified;
        }

        private bool UpdateGivenCarlGustaf(byte[] newData)
        {
            bool modified = false;
            CarlGustaf asCG = dataObject as CarlGustaf;

            // Set chamber round
            short chamberTypeIndex = BitConverter.ToInt16(newData, 0);
            short chamberClassIndex = BitConverter.ToInt16(newData, 2);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asCG.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asCG.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asCG.Chamber.GetRound() == null || asCG.Chamber.GetRound().RoundClass != roundClass)
                {
                    if (asCG.Chamber.RoundType == roundType)
                    {
                        ++ChamberPatch.chamberSkip;
                        asCG.Chamber.SetRound(roundClass, asCG.Chamber.transform.position, asCG.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                    else
                    {
                        FireArmRoundType prevRoundType = asCG.Chamber.RoundType;
                        asCG.Chamber.RoundType = roundType;
                        ++ChamberPatch.chamberSkip;
                        asCG.Chamber.SetRound(roundClass, asCG.Chamber.transform.position, asCG.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                        asCG.Chamber.RoundType = prevRoundType;
                    }
                    modified = true;
                }
            }

            // Set charging handle state
            if (newData[4] == 0) // Forward
            {
                if (asCG.CHState != CarlGustaf.ChargingHandleState.Forward)
                {
                    Mod.CarlGustaf_m_curZ.SetValue(asCG, asCG.ChargingHandForward.localPosition.z);
                    Mod.CarlGustaf_m_tarZ.SetValue(asCG, asCG.ChargingHandForward.localPosition.z);
                    asCG.ChargingHandle.localPosition = new Vector3(asCG.ChargingHandle.localPosition.x, asCG.ChargingHandle.localPosition.y, asCG.ChargingHandForward.localPosition.z);
                    asCG.CHState = CarlGustaf.ChargingHandleState.Forward;
                }
            }else if (newData[4] == 1) // Middle
            {
                if (asCG.CHState != CarlGustaf.ChargingHandleState.Middle)
                {
                    float val = Mathf.Lerp(asCG.ChargingHandForward.localPosition.z, asCG.ChargingHandBack.localPosition.z, 0.5f);
                    Mod.CarlGustaf_m_curZ.SetValue(asCG, val);
                    Mod.CarlGustaf_m_tarZ.SetValue(asCG, val);
                    asCG.ChargingHandle.localPosition = new Vector3(asCG.ChargingHandle.localPosition.x, asCG.ChargingHandle.localPosition.y, val);
                    asCG.CHState = CarlGustaf.ChargingHandleState.Middle;
                }
            }
            else if (asCG.CHState != CarlGustaf.ChargingHandleState.Back)
            {
                Mod.CarlGustaf_m_curZ.SetValue(asCG, asCG.ChargingHandBack.localPosition.z);
                Mod.CarlGustaf_m_tarZ.SetValue(asCG, asCG.ChargingHandBack.localPosition.z);
                asCG.ChargingHandle.localPosition = new Vector3(asCG.ChargingHandle.localPosition.x, asCG.ChargingHandle.localPosition.y, asCG.ChargingHandBack.localPosition.z);
                asCG.CHState = CarlGustaf.ChargingHandleState.Back;
            }

            // Set lid latch state
            if (newData[5] == 0) // Closed
            {
                if (asCG.TailLatch.LState != CarlGustafLatch.CGLatchState.Closed)
                {
                    float val = asCG.TailLatch.IsMinOpen ? asCG.TailLatch.RotMax : asCG.TailLatch.RotMin;
                    Mod.CarlGustafLatch_m_curRot.SetValue(asCG.TailLatch, val);
                    Mod.CarlGustafLatch_m_tarRot.SetValue(asCG.TailLatch, val);
                    asCG.TailLatch.transform.localEulerAngles = new Vector3(0f, val, 0f);
                    asCG.TailLatch.LState = CarlGustafLatch.CGLatchState.Closed;
                }
            }
            else if (newData[5] == 1) // Middle
            {
                if (asCG.TailLatch.LState != CarlGustafLatch.CGLatchState.Middle)
                {
                    float val = Mathf.Lerp(asCG.TailLatch.RotMin, asCG.TailLatch.RotMax, 0.5f);
                    Mod.CarlGustafLatch_m_curRot.SetValue(asCG.TailLatch, val);
                    Mod.CarlGustafLatch_m_tarRot.SetValue(asCG.TailLatch, val);
                    asCG.TailLatch.transform.localEulerAngles = new Vector3(0f, val, 0f);
                    asCG.TailLatch.LState = CarlGustafLatch.CGLatchState.Middle;
                }
            }
            else if (asCG.TailLatch.LState != CarlGustafLatch.CGLatchState.Open)
            {
                    float val = asCG.TailLatch.IsMinOpen ? asCG.TailLatch.RotMin : asCG.TailLatch.RotMax;
                    Mod.CarlGustafLatch_m_curRot.SetValue(asCG.TailLatch, val);
                    Mod.CarlGustafLatch_m_tarRot.SetValue(asCG.TailLatch, val);
                    asCG.TailLatch.transform.localEulerAngles = new Vector3(0f, val, 0f);
                    asCG.TailLatch.LState = CarlGustafLatch.CGLatchState.Open;
            }

            // Set shell slide state
            if (newData[6] == 0) // In
            {
                if (asCG.ShellInsertEject.CSState != CarlGustafShellInsertEject.ChamberSlideState.In)
                {
                    Mod.CarlGustafShellInsertEject_m_curZ.SetValue(asCG.ShellInsertEject, asCG.ShellInsertEject.ChamberPoint_Forward.localPosition.z);
                    Mod.CarlGustafShellInsertEject_m_tarZ.SetValue(asCG.ShellInsertEject, asCG.ShellInsertEject.ChamberPoint_Forward.localPosition.z);
                    asCG.Chamber.transform.localPosition = new Vector3(asCG.Chamber.transform.localPosition.x, asCG.Chamber.transform.localPosition.y, asCG.ShellInsertEject.ChamberPoint_Forward.localPosition.z);
                    asCG.ShellInsertEject.CSState = CarlGustafShellInsertEject.ChamberSlideState.In;
                }
            }
            else if (newData[6] == 1) // Middle
            {
                if (asCG.ShellInsertEject.CSState != CarlGustafShellInsertEject.ChamberSlideState.Middle)
                {
                    float val = Mathf.Lerp(asCG.TailLatch.RotMin, asCG.TailLatch.RotMax, 0.5f);
                    Mod.CarlGustafShellInsertEject_m_curZ.SetValue(asCG.ShellInsertEject, val);
                    Mod.CarlGustafShellInsertEject_m_tarZ.SetValue(asCG.ShellInsertEject, val);
                    asCG.Chamber.transform.localPosition = new Vector3(asCG.Chamber.transform.localPosition.x, asCG.Chamber.transform.localPosition.y, val);
                    asCG.ShellInsertEject.CSState = CarlGustafShellInsertEject.ChamberSlideState.Middle;
                }
            }
            else if (asCG.ShellInsertEject.CSState != CarlGustafShellInsertEject.ChamberSlideState.Out)
            {
                Mod.CarlGustafShellInsertEject_m_curZ.SetValue(asCG.ShellInsertEject, asCG.ShellInsertEject.ChamberPoint_Back.localPosition.z);
                Mod.CarlGustafShellInsertEject_m_tarZ.SetValue(asCG.ShellInsertEject, asCG.ShellInsertEject.ChamberPoint_Back.localPosition.z);
                asCG.Chamber.transform.localPosition = new Vector3(asCG.Chamber.transform.localPosition.x, asCG.Chamber.transform.localPosition.y, asCG.ShellInsertEject.ChamberPoint_Back.localPosition.z);
                asCG.ShellInsertEject.CSState = CarlGustafShellInsertEject.ChamberSlideState.Out;
            }

            // Set lock latch state
            if (newData[7] == 0) // Closed
            {
                if (asCG.TailLatch.RestrictingLatch.LState != CarlGustafLatch.CGLatchState.Closed)
                {
                    float val = asCG.TailLatch.RestrictingLatch.IsMinOpen ? asCG.TailLatch.RestrictingLatch.RotMax : asCG.TailLatch.RestrictingLatch.RotMin;
                    Mod.CarlGustafLatch_m_curRot.SetValue(asCG.TailLatch.RestrictingLatch, val);
                    Mod.CarlGustafLatch_m_tarRot.SetValue(asCG.TailLatch.RestrictingLatch, val);
                    asCG.TailLatch.RestrictingLatch.transform.localEulerAngles = new Vector3(0f, val, 0f);
                    asCG.TailLatch.RestrictingLatch.LState = CarlGustafLatch.CGLatchState.Closed;
                }
            }
            else if (newData[7] == 1) // Middle
            {
                if (asCG.TailLatch.RestrictingLatch.LState != CarlGustafLatch.CGLatchState.Middle)
                {
                    float val = Mathf.Lerp(asCG.TailLatch.RestrictingLatch.RotMin, asCG.TailLatch.RestrictingLatch.RotMax, 0.5f);
                    Mod.CarlGustafLatch_m_curRot.SetValue(asCG.TailLatch.RestrictingLatch, val);
                    Mod.CarlGustafLatch_m_tarRot.SetValue(asCG.TailLatch.RestrictingLatch, val);
                    asCG.TailLatch.RestrictingLatch.transform.localEulerAngles = new Vector3(0f, val, 0f);
                    asCG.TailLatch.RestrictingLatch.LState = CarlGustafLatch.CGLatchState.Middle;
                }
            }
            else if (asCG.TailLatch.RestrictingLatch.LState != CarlGustafLatch.CGLatchState.Open)
            {
                    float val = asCG.TailLatch.RestrictingLatch.IsMinOpen ? asCG.TailLatch.RestrictingLatch.RotMin : asCG.TailLatch.RestrictingLatch.RotMax;
                    Mod.CarlGustafLatch_m_curRot.SetValue(asCG.TailLatch.RestrictingLatch, val);
                    Mod.CarlGustafLatch_m_tarRot.SetValue(asCG.TailLatch.RestrictingLatch, val);
                    asCG.TailLatch.RestrictingLatch.transform.localEulerAngles = new Vector3(0f, val, 0f);
                    asCG.TailLatch.RestrictingLatch.LState = CarlGustafLatch.CGLatchState.Open;
            }

            data.data = newData;

            return modified;
        }

        private void ChamberCarlGustafRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            CarlGustaf asCG = dataObject as CarlGustaf;

            ++ChamberPatch.chamberSkip;
            asCG.Chamber.SetRound(roundClass, asCG.Chamber.transform.position, asCG.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
        }

        private bool FireCarlGustaf(int chamberIndex)
        {
            Mod.CarlGustaf_TryToFire.Invoke(dataObject as CarlGustaf, null);
            return true;
        }

        private void SetCarlGustafUpdateOverride(FireArmRoundType roundType, FireArmRoundClass roundClass, int chamberIndex)
        {
            CarlGustaf asCG = dataObject as CarlGustaf;

            FireArmRoundType prevRoundType = asCG.Chamber.RoundType;
            asCG.Chamber.RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asCG.Chamber.SetRound(roundClass, asCG.Chamber.transform.position, asCG.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
            asCG.Chamber.RoundType = prevRoundType;
        }

        private bool UpdateLeverActionFirearm()
        {
            LeverActionFirearm asLAF = dataObject as LeverActionFirearm;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[10];
                modified = true;
            }

            // Write chamber round
            byte preval0 = data.data[0];
            byte preval1 = data.data[1];
            byte preval2 = data.data[2];
            byte preval3 = data.data[3];

            if (asLAF.Chamber.GetRound() == null || asLAF.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 2);
            }
            else
            {
                BitConverter.GetBytes((short)asLAF.Chamber.GetRound().RoundType).CopyTo(data.data, 0);
                BitConverter.GetBytes((short)asLAF.Chamber.GetRound().RoundClass).CopyTo(data.data, 2);
            }

            modified |= (preval0 != data.data[0] || preval1 != data.data[1] || preval2 != data.data[2] || preval3 != data.data[3]);

            // Write hammer state
            preval0 = data.data[4];

            data.data[4] = asLAF.IsHammerCocked ? (byte)1 : (byte)0;

            modified |= preval0 != data.data[4];

            if (asLAF.UsesSecondChamber)
            {
                // Write chamber2 round
                preval0 = data.data[5];
                preval1 = data.data[6];
                preval2 = data.data[7];
                preval3 = data.data[8];

                if (asLAF.Chamber2.GetRound() == null || asLAF.Chamber2.GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(data.data, 7);
                }
                else
                {
                    BitConverter.GetBytes((short)asLAF.Chamber2.GetRound().RoundClass).CopyTo(data.data, 5);
                    BitConverter.GetBytes((short)asLAF.Chamber2.GetRound().RoundClass).CopyTo(data.data, 7);
                }

                modified |= (preval0 != data.data[5] || preval1 != data.data[6] || preval2 != data.data[7] || preval3 != data.data[8]);

                // Write hammer2 state
                preval0 = data.data[9];

                data.data[9] = ((bool)Mod.LeverActionFirearm_m_isHammerCocked2.GetValue(asLAF)) ? (byte)1 : (byte)0;

                modified |= preval0 != data.data[9];
            }

            return modified;
        }

        private bool UpdateGivenLeverActionFirearm(byte[] newData)
        {
            bool modified = false;
            LeverActionFirearm asLAF = dataObject as LeverActionFirearm;

            // Set chamber round
            short chamberTypeIndex = BitConverter.ToInt16(newData, 0);
            short chamberClassIndex = BitConverter.ToInt16(newData, 2);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asLAF.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asLAF.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asLAF.Chamber.GetRound() == null || asLAF.Chamber.GetRound().RoundClass != roundClass)
                {
                    if (asLAF.Chamber.RoundType == roundType)
                    {
                        ++ChamberPatch.chamberSkip;
                        asLAF.Chamber.SetRound(roundClass, asLAF.Chamber.transform.position, asLAF.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                    else
                    {
                        FireArmRoundType prevRoundType = asLAF.Chamber.RoundType;
                        asLAF.Chamber.RoundType = roundType;
                        ++ChamberPatch.chamberSkip;
                        asLAF.Chamber.SetRound(roundClass, asLAF.Chamber.transform.position, asLAF.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                        asLAF.Chamber.RoundType = prevRoundType;
                    }
                    modified = true;
                }
            }

            // Set hammer state
            Mod.LeverActionFirearm_m_isHammerCocked.SetValue(asLAF, newData[4] == 1);

            if (asLAF.UsesSecondChamber)
            {
                // Set chamber2 round
                chamberTypeIndex = BitConverter.ToInt16(newData, 5);
                chamberClassIndex = BitConverter.ToInt16(newData, 7);
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (asLAF.Chamber2.GetRound() != null)
                    {
                        ++ChamberPatch.chamberSkip;
                        asLAF.Chamber2.SetRound(null, false);
                        --ChamberPatch.chamberSkip;
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (asLAF.Chamber2.GetRound() == null || asLAF.Chamber2.GetRound().RoundClass != roundClass)
                    {
                        if (asLAF.Chamber2.RoundType == roundType)
                        {
                            ++ChamberPatch.chamberSkip;
                            asLAF.Chamber2.SetRound(roundClass, asLAF.Chamber2.transform.position, asLAF.Chamber2.transform.rotation);
                            --ChamberPatch.chamberSkip;
                        }
                        else
                        {
                            FireArmRoundType prevRoundType = asLAF.Chamber2.RoundType;
                            asLAF.Chamber2.RoundType = roundType;
                            ++ChamberPatch.chamberSkip;
                            asLAF.Chamber2.SetRound(roundClass, asLAF.Chamber2.transform.position, asLAF.Chamber2.transform.rotation);
                            --ChamberPatch.chamberSkip;
                            asLAF.Chamber2.RoundType = prevRoundType;
                        }
                        modified = true;
                    }
                }

                // Set hammer2 state
                Mod.LeverActionFirearm_m_isHammerCocked2.SetValue(asLAF, newData[9] == 1);
            }

            data.data = newData;

            return modified;
        }

        private int GetLeverActionFirearmChamberIndex(FVRFireArmChamber chamber)
        {
            LeverActionFirearm asLAF = dataObject as LeverActionFirearm;

            if (chamber == null)
            {
                return -1;
            }

            if(asLAF.Chamber == chamber)
            {
                return 0;
            }
            else if(asLAF.Chamber2 == chamber)
            {
                return 1;
            }

            return -1;
        }

        private void ChamberLeverActionFirearmRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            LeverActionFirearm asLAF = dataObject as LeverActionFirearm;

            if(chamberIndex == 0)
            {
                ++ChamberPatch.chamberSkip;
                asLAF.Chamber.SetRound(roundClass, asLAF.Chamber.transform.position, asLAF.Chamber.transform.rotation);
                --ChamberPatch.chamberSkip;
            }
            else
            {
                ++ChamberPatch.chamberSkip;
                asLAF.Chamber2.SetRound(roundClass, asLAF.Chamber2.transform.position, asLAF.Chamber2.transform.rotation);
                --ChamberPatch.chamberSkip;
            }
        }

        private bool UpdateDerringer()
        {
            Derringer asDerringer = dataObject as Derringer;
            bool modified = false;

            int necessarySize = asDerringer.Barrels.Count * 4 + 1;

            if (data.data == null)
            {
                data.data = new byte[necessarySize];
                modified = true;
            }

            // Write hammer state
            byte preval0 = data.data[0];

            data.data[0] = asDerringer.IsExternalHammerCocked() ? (byte)1 : (byte)0;

            modified |= preval0 != data.data[0];

            // Write chambered rounds
            byte preval1;
            byte preval2;
            byte preval3;
            for (int i = 0; i < asDerringer.Barrels.Count; ++i)
            {
                // Write chambered round
                int firstIndex = i * 4 + 1;
                preval0 = data.data[firstIndex];
                preval1 = data.data[firstIndex + 1];
                preval2 = data.data[firstIndex + 2];
                preval3 = data.data[firstIndex + 3];

                if (asDerringer.Barrels[i].Chamber.GetRound() == null || asDerringer.Barrels[i].Chamber.GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(data.data, firstIndex + 2);
                }
                else
                {
                    BitConverter.GetBytes((short)asDerringer.Barrels[i].Chamber.GetRound().RoundType).CopyTo(data.data, firstIndex);
                    BitConverter.GetBytes((short)asDerringer.Barrels[i].Chamber.GetRound().RoundClass).CopyTo(data.data, firstIndex + 2);
                }

                modified |= (preval0 != data.data[firstIndex] || preval1 != data.data[firstIndex + 1] || preval2 != data.data[firstIndex + 2] || preval3 != data.data[firstIndex + 3]);
            }

            return modified;
        }

        private bool UpdateGivenDerringer(byte[] newData)
        {
            bool modified = false;
            Derringer asDerringer = dataObject as Derringer;

            if (data.data == null)
            {
                modified = true;

                // Set hammer state
                if (newData[0] == 1 && !asDerringer.IsExternalHammerCocked())
                {
                    Mod.Derringer_CockHammer.Invoke(asDerringer, null);
                }
                else if(newData[0] == 0 && asDerringer.IsExternalHammerCocked())
                {
                    Mod.Derringer_m_isExternalHammerCocked.SetValue(asDerringer, false);
                }
            }
            else
            {
                // Set hammer state
                if (newData[0] == 1 && !asDerringer.IsExternalHammerCocked())
                {
                    Mod.Derringer_CockHammer.Invoke(asDerringer, null);
                    modified = true;
                }
                else if (newData[0] == 0 && asDerringer.IsExternalHammerCocked())
                {
                    Mod.Derringer_m_isExternalHammerCocked.SetValue(asDerringer, false);
                    modified = true;
                }
            }

            // Set barrels
            for (int i = 0; i < asDerringer.Barrels.Count; ++i)
            {
                int firstIndex = i * 4 + 1;
                short chamberTypeIndex = BitConverter.ToInt16(newData, firstIndex);
                short chamberClassIndex = BitConverter.ToInt16(newData, firstIndex + 2);
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (asDerringer.Barrels[i].Chamber.GetRound() != null)
                    {
                        ++ChamberPatch.chamberSkip;
                        asDerringer.Barrels[i].Chamber.SetRound(null, false);
                        --ChamberPatch.chamberSkip;
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (asDerringer.Barrels[i].Chamber.GetRound() == null || asDerringer.Barrels[i].Chamber.GetRound().RoundClass != roundClass)
                    {
                        if (asDerringer.Barrels[i].Chamber.RoundType == roundType)
                        {
                            ++ChamberPatch.chamberSkip;
                            asDerringer.Barrels[i].Chamber.SetRound(roundClass, asDerringer.Barrels[i].Chamber.transform.position, asDerringer.Barrels[i].Chamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                        }
                        else
                        {
                            FireArmRoundType prevRoundType = asDerringer.Barrels[i].Chamber.RoundType;
                            asDerringer.Barrels[i].Chamber.RoundType = roundType;
                            ++ChamberPatch.chamberSkip;
                            asDerringer.Barrels[i].Chamber.SetRound(roundClass, asDerringer.Barrels[i].Chamber.transform.position, asDerringer.Barrels[i].Chamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                            asDerringer.Barrels[i].Chamber.RoundType = prevRoundType;
                        }
                        modified = true;
                    }
                }
            }

            data.data = newData;

            return modified;
        }

        private int GetDerringerChamberIndex(FVRFireArmChamber chamber)
        {
            Derringer asDerringer = dataObject as Derringer;
            List<Derringer.DBarrel> barrels = asDerringer.Barrels;

            if(chamber == null)
            {
                return -1;
            }

            for(int i=0; i < barrels.Count; ++i)
            {
                if (barrels[i].Chamber == chamber)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool UpdateBreakActionWeapon()
        {
            BreakActionWeapon asBreakActionWeapon = dataObject as BreakActionWeapon;
            bool modified = false;

            int necessarySize = asBreakActionWeapon.Barrels.Length * 5;

            if (data.data == null)
            {
                data.data = new byte[necessarySize];
                modified = true;
            }

            // Write chambered rounds
            byte preval0;
            byte preval1;
            byte preval2;
            byte preval3;
            for (int i = 0; i < asBreakActionWeapon.Barrels.Length; ++i)
            {
                // Write chambered round
                int firstIndex = i * 5;
                preval0 = data.data[firstIndex];
                preval1 = data.data[firstIndex + 1];
                preval2 = data.data[firstIndex + 2];
                preval3 = data.data[firstIndex + 3];

                if (asBreakActionWeapon.Barrels[i].Chamber.GetRound() == null || asBreakActionWeapon.Barrels[i].Chamber.GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(data.data, firstIndex + 2);
                }
                else
                {
                    BitConverter.GetBytes((short)asBreakActionWeapon.Barrels[i].Chamber.GetRound().RoundType).CopyTo(data.data, firstIndex);
                    BitConverter.GetBytes((short)asBreakActionWeapon.Barrels[i].Chamber.GetRound().RoundClass).CopyTo(data.data, firstIndex + 2);
                }

                modified |= (preval0 != data.data[firstIndex] || preval1 != data.data[firstIndex + 1] || preval2 != data.data[firstIndex + 2] || preval3 != data.data[firstIndex + 3]);

                // Write hammer state
                preval0 = data.data[firstIndex + 4];

                data.data[firstIndex + 4] = asBreakActionWeapon.Barrels[i].m_isHammerCocked ? (byte)1 : (byte)0;

                modified |= preval0 != data.data[firstIndex + 4];
            }

            return modified;
        }

        private bool UpdateGivenBreakActionWeapon(byte[] newData)
        {
            bool modified = false;
            BreakActionWeapon asBreakActionWeapon = dataObject as BreakActionWeapon;

            // Set barrels
            for (int i = 0; i < asBreakActionWeapon.Barrels.Length; ++i)
            {
                int firstIndex = i * 5;
                short chamberTypeIndex = BitConverter.ToInt16(newData, firstIndex);
                short chamberClassIndex = BitConverter.ToInt16(newData, firstIndex + 2);
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (asBreakActionWeapon.Barrels[i].Chamber.GetRound() != null)
                    {
                        ++ChamberPatch.chamberSkip;
                        asBreakActionWeapon.Barrels[i].Chamber.SetRound(null, false);
                        --ChamberPatch.chamberSkip;
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (asBreakActionWeapon.Barrels[i].Chamber.GetRound() == null || asBreakActionWeapon.Barrels[i].Chamber.GetRound().RoundClass != roundClass)
                    {
                        if (asBreakActionWeapon.Barrels[i].Chamber.RoundType == roundType)
                        {
                            ++ChamberPatch.chamberSkip;
                            asBreakActionWeapon.Barrels[i].Chamber.SetRound(roundClass, asBreakActionWeapon.Barrels[i].Chamber.transform.position, asBreakActionWeapon.Barrels[i].Chamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                        }
                        else
                        {
                            FireArmRoundType prevRoundType = asBreakActionWeapon.Barrels[i].Chamber.RoundType;
                            asBreakActionWeapon.Barrels[i].Chamber.RoundType = roundType;
                            ++ChamberPatch.chamberSkip;
                            asBreakActionWeapon.Barrels[i].Chamber.SetRound(roundClass, asBreakActionWeapon.Barrels[i].Chamber.transform.position, asBreakActionWeapon.Barrels[i].Chamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                            asBreakActionWeapon.Barrels[i].Chamber.RoundType = prevRoundType;
                        }
                        modified = true;
                    }
                }

                asBreakActionWeapon.Barrels[i].m_isHammerCocked = newData[firstIndex + 4] == 1;
            }

            data.data = newData;

            return modified;
        }

        private int GetBreakActionWeaponChamberIndex(FVRFireArmChamber chamber)
        {
            BreakActionWeapon asBreakActionWeapon = dataObject as BreakActionWeapon;
            BreakActionWeapon.BreakActionBarrel[] barrels = asBreakActionWeapon.Barrels;

            if (chamber == null)
            {
                return -1;
            }

            for (int i = 0; i < barrels.Length; ++i)
            {
                if (barrels[i].Chamber == chamber)
                {
                    return i;
                }
            }

            return -1;
        }

        private void ChamberBreakActionWeaponRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            BreakActionWeapon asBreakActionWeapon = dataObject as BreakActionWeapon;

            ++ChamberPatch.chamberSkip;
            asBreakActionWeapon.GetChambers()[chamberIndex].SetRound(roundClass, asBreakActionWeapon.GetChambers()[chamberIndex].transform.position, asBreakActionWeapon.GetChambers()[chamberIndex].transform.rotation);
            --ChamberPatch.chamberSkip;
        }

        private bool UpdateBAP()
        {
            BAP asBAP = dataObject as BAP;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[6];
                modified = true;
            }

            byte preval = data.data[0];

            // Write fire mode index
            data.data[0] = (byte)(int)Mod.BAP_m_fireSelectorMode.GetValue(asBAP);

            modified |= preval != data.data[0];

            preval = data.data[1];

            // Write hammer state
            data.data[1] = BitConverter.GetBytes((bool)Mod.BAP_m_isHammerCocked.GetValue(asBAP))[0];

            modified |= preval != data.data[1];

            preval = data.data[2];
            byte preval0 = data.data[3];
            byte preval1 = data.data[4];
            byte preval2 = data.data[5];

            // Write chambered round class
            if (asBAP.Chamber.GetRound() == null || asBAP.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 4);
            }
            else
            {
                BitConverter.GetBytes((short)asBAP.Chamber.GetRound().RoundType).CopyTo(data.data, 2);
                BitConverter.GetBytes((short)asBAP.Chamber.GetRound().RoundClass).CopyTo(data.data, 4);
            }

            modified |= (preval != data.data[2] || preval0 != data.data[3] || preval1 != data.data[4] || preval2 != data.data[5]);

            return modified;
        }

        private bool UpdateGivenBAP(byte[] newData)
        {
            bool modified = false;
            BAP asBAP = dataObject as BAP;

            if (data.data == null)
            {
                modified = true;

                // Set fire select mode
                Mod.BAP_m_fireSelectorMode.SetValue(asBAP, (int)newData[0]);
            }
            else
            {
                if (data.data[0] != newData[0])
                {
                    // Set fire select mode
                    Mod.BAP_m_fireSelectorMode.SetValue(asBAP, (int)newData[0]);
                    modified = true;
                }
            }

            // Set hammer state
            if (newData[1] == 0)
            {
                if ((bool)Mod.BAP_m_isHammerCocked.GetValue(asBAP))
                {
                    Mod.BAP_m_isHammerCocked.SetValue(asBAP, false);
                    modified = true;
                }
            }
            else // Hammer should be cocked
            {
                if (!(bool)Mod.BAP_m_isHammerCocked.GetValue(asBAP))
                {
                    asBAP.CockHammer();
                    modified = true;
                }
            }

            // Set chamber
            short chamberTypeIndex = BitConverter.ToInt16(newData, 2);
            short chamberClassIndex = BitConverter.ToInt16(newData, 4);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asBAP.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asBAP.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asBAP.Chamber.GetRound() == null || asBAP.Chamber.GetRound().RoundClass != roundClass)
                {
                    if (asBAP.Chamber.RoundType == roundType)
                    {
                        ++ChamberPatch.chamberSkip;
                        asBAP.Chamber.SetRound(roundClass, asBAP.Chamber.transform.position, asBAP.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                    else
                    {
                        FireArmRoundType prevRoundType = asBAP.Chamber.RoundType;
                        asBAP.Chamber.RoundType = roundType;
                        ++ChamberPatch.chamberSkip;
                        asBAP.Chamber.SetRound(roundClass, asBAP.Chamber.transform.position, asBAP.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                        asBAP.Chamber.RoundType = prevRoundType;
                    }
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private void SetBAPUpdateOverride(FireArmRoundType roundType, FireArmRoundClass roundClass, int chamberIndex)
        {
            BAP asBAP = dataObject as BAP;

            FireArmRoundType prevRoundType = asBAP.Chamber.RoundType;
            asBAP.Chamber.RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asBAP.Chamber.SetRound(roundClass, asBAP.Chamber.transform.position, asBAP.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
            asBAP.Chamber.RoundType = prevRoundType;
        }

        private void ChamberBAPRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            BAP asBAP = dataObject as BAP;

            ++ChamberPatch.chamberSkip;
            asBAP.Chamber.SetRound(roundClass, asBAP.Chamber.transform.position, asBAP.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
        }

        private bool FireBAP(int chamberIndex)
        {
            return (dataObject as BAP).Fire();
        }

        private bool UpdateRevolvingShotgun()
        {
            RevolvingShotgun asRS = dataObject as RevolvingShotgun;
            bool modified = false;

            int necessarySize = asRS.Chambers.Length * 4 + 2;

            if (data.data == null)
            {
                data.data = new byte[necessarySize];
                modified = true;
            }

            byte preval0 = data.data[0];

            // Write cur chamber
            data.data[0] = (byte)asRS.CurChamber;

            modified |= preval0 != data.data[0];

            preval0 = data.data[1];

            // Write cylLoaded
            data.data[1] = asRS.CylinderLoaded ? (byte)1 : (byte)0;

            modified |= preval0 != data.data[1];

            // Write chambered rounds
            byte preval1;
            byte preval2;
            byte preval3;
            for (int i = 0; i < asRS.Chambers.Length; ++i)
            {
                int firstIndex = i * 4 + 2;
                preval0 = data.data[firstIndex];
                preval1 = data.data[firstIndex + 1];
                preval2 = data.data[firstIndex + 2];
                preval3 = data.data[firstIndex + 3];

                if (asRS.Chambers[i].GetRound() == null || asRS.Chambers[i].GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(data.data, firstIndex + 2);
                }
                else
                {
                    BitConverter.GetBytes((short)asRS.Chambers[i].GetRound().RoundType).CopyTo(data.data, firstIndex);
                    BitConverter.GetBytes((short)asRS.Chambers[i].GetRound().RoundClass).CopyTo(data.data, firstIndex + 2);
                }

                modified |= (preval0 != data.data[firstIndex] || preval1 != data.data[firstIndex + 1] || preval2 != data.data[firstIndex + 2] || preval3 != data.data[firstIndex + 3]);
            }

            return modified;
        }

        private bool UpdateGivenRevolvingShotgun(byte[] newData)
        {
            bool modified = false;
            RevolvingShotgun asRS = dataObject as RevolvingShotgun;

            if (data.data == null)
            {
                modified = true;

                // Set cur chamber
                asRS.CurChamber = newData[0];

                // Set cyl loaded
                bool newCylLoaded = newData[1] == 1;
                if(newCylLoaded && !asRS.CylinderLoaded)
                {
                    // Load cylinder, chambers will be updated separately
                    asRS.ProxyCylinder.gameObject.SetActive(true);
                    asRS.PlayAudioEvent(FirearmAudioEventType.MagazineIn);
                    asRS.CurChamber = 0;
                    asRS.ProxyCylinder.localRotation = asRS.GetLocalRotationFromCylinder(0);
                }
                else if(!newCylLoaded && asRS.CylinderLoaded)
                {
                    // Eject cylinder, chambers will be updated separately, handling the spawn of a physical cylinder will also be handled separately
                    asRS.PlayAudioEvent(FirearmAudioEventType.MagazineOut, 1f);
                    asRS.EjectDelay = 0.4f;
                    asRS.CylinderLoaded = false;
                    asRS.ProxyCylinder.gameObject.SetActive(false);
                }
                asRS.CylinderLoaded = newCylLoaded;
            }
            else
            {
                if (data.data[0] != newData[0])
                {
                    // Set cur chamber
                    asRS.CurChamber = newData[0];
                    modified = true;
                }
                // Set cyl loaded
                bool newCylLoaded = newData[1] == 1;
                if (newCylLoaded && !asRS.CylinderLoaded)
                {
                    // Load cylinder, chambers will be updated separately
                    asRS.ProxyCylinder.gameObject.SetActive(true);
                    asRS.PlayAudioEvent(FirearmAudioEventType.MagazineIn);
                    asRS.CurChamber = 0;
                    asRS.ProxyCylinder.localRotation = asRS.GetLocalRotationFromCylinder(0);
                    modified = true;
                }
                else if (!newCylLoaded && asRS.CylinderLoaded)
                {
                    // Eject cylinder, chambers will be updated separately, handling the spawn of a physical cylinder will also be handled separately
                    asRS.PlayAudioEvent(FirearmAudioEventType.MagazineOut, 1f);
                    asRS.EjectDelay = 0.4f;
                    asRS.CylinderLoaded = false;
                    asRS.ProxyCylinder.gameObject.SetActive(false);
                    modified = true;
                }
                asRS.CylinderLoaded = newCylLoaded;
            }

            // Set chambers
            for (int i = 0; i < asRS.Chambers.Length; ++i)
            {
                int firstIndex = i * 4 + 2;
                short chamberTypeIndex = BitConverter.ToInt16(newData, firstIndex);
                short chamberClassIndex = BitConverter.ToInt16(newData, firstIndex + 2);
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (asRS.Chambers[i].GetRound() != null)
                    {
                        ++ChamberPatch.chamberSkip;
                        asRS.Chambers[i].SetRound(null, false);
                        --ChamberPatch.chamberSkip;
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (asRS.Chambers[i].GetRound() == null || asRS.Chambers[i].GetRound().RoundClass != roundClass)
                    {
                        if (asRS.Chambers[i].RoundType == roundType)
                        {
                            ++ChamberPatch.chamberSkip;
                            asRS.Chambers[i].SetRound(roundClass, asRS.Chambers[i].transform.position, asRS.Chambers[i].transform.rotation);
                            --ChamberPatch.chamberSkip;
                        }
                        else
                        {
                            FireArmRoundType prevRoundType = asRS.Chambers[i].RoundType;
                            asRS.Chambers[i].RoundType = roundType;
                            ++ChamberPatch.chamberSkip;
                            asRS.Chambers[i].SetRound(roundClass, asRS.Chambers[i].transform.position, asRS.Chambers[i].transform.rotation);
                            --ChamberPatch.chamberSkip;
                            asRS.Chambers[i].RoundType = prevRoundType;
                        }
                        modified = true;
                    }
                }
            }

            data.data = newData;

            return modified;
        }

        private int GetRevolvingShotgunChamberIndex(FVRFireArmChamber chamber)
        {
            RevolvingShotgun asRS = dataObject as RevolvingShotgun;
            FVRFireArmChamber[] chambers = asRS.Chambers;

            if(chamber == null)
            {
                return -1;
            }

            for(int i=0; i < chambers.Length; ++i)
            {
                if (chambers[i] == chamber)
                {
                    return i;
                }
            }

            return -1;
        }

        private void ChamberRevolvingShotgunRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            RevolvingShotgun asRS = dataObject as RevolvingShotgun;

            ++ChamberPatch.chamberSkip;
            asRS.GetChambers()[chamberIndex].SetRound(roundClass, asRS.GetChambers()[chamberIndex].transform.position, asRS.GetChambers()[chamberIndex].transform.rotation);
            --ChamberPatch.chamberSkip;
        }

        private bool UpdateRevolver()
        {
            Revolver asRevolver = dataObject as Revolver;
            bool modified = false;

            int necessarySize = asRevolver.Cylinder.numChambers * 4 + 1;

            if (data.data == null)
            {
                data.data = new byte[necessarySize];
                modified = true;
            }

            byte preval0 = data.data[0];

            // Write cur chamber
            data.data[0] = (byte)asRevolver.CurChamber;

            modified |= preval0 != data.data[0];

            // Write chambered rounds
            byte preval1;
            byte preval2;
            byte preval3;
            for (int i = 0; i < asRevolver.Chambers.Length; ++i)
            {
                int firstIndex = i * 4 + 1;
                preval0 = data.data[firstIndex];
                preval1 = data.data[firstIndex + 1];
                preval2 = data.data[firstIndex + 2];
                preval3 = data.data[firstIndex + 3];

                if (asRevolver.Chambers[i].GetRound() == null || asRevolver.Chambers[i].GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(data.data, firstIndex + 2);
                }
                else
                {
                    BitConverter.GetBytes((short)asRevolver.Chambers[i].GetRound().RoundType).CopyTo(data.data, firstIndex);
                    BitConverter.GetBytes((short)asRevolver.Chambers[i].GetRound().RoundClass).CopyTo(data.data, firstIndex + 2);
                }

                modified |= (preval0 != data.data[firstIndex] || preval1 != data.data[firstIndex + 1] || preval2 != data.data[firstIndex + 2] || preval3 != data.data[firstIndex + 3]);
            }

            return modified;
        }

        private bool UpdateGivenRevolver(byte[] newData)
        {
            bool modified = false;
            Revolver asRevolver = dataObject as Revolver;

            if (data.data == null)
            {
                modified = true;

                // Set cur chamber
                asRevolver.CurChamber = newData[0];
            }
            else
            {
                if (data.data[0] != newData[0])
                {
                    // Set cur chamber
                    asRevolver.CurChamber = newData[0];
                    modified = true;
                }
            }

            // Set chambers
            for (int i = 0; i < asRevolver.Chambers.Length; ++i)
            {
                int firstIndex = i * 4 + 1;
                short chamberTypeIndex = BitConverter.ToInt16(newData, firstIndex);
                short chamberClassIndex = BitConverter.ToInt16(newData, firstIndex + 2);
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (asRevolver.Chambers[i].GetRound() != null)
                    {
                        ++ChamberPatch.chamberSkip;
                        asRevolver.Chambers[i].SetRound(null, false);
                        --ChamberPatch.chamberSkip;
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (asRevolver.Chambers[i].GetRound() == null || asRevolver.Chambers[i].GetRound().RoundClass != roundClass)
                    {
                        if (asRevolver.Chambers[i].RoundType == roundType)
                        {
                            ++ChamberPatch.chamberSkip;
                            asRevolver.Chambers[i].SetRound(roundClass, asRevolver.Chambers[i].transform.position, asRevolver.Chambers[i].transform.rotation);
                            --ChamberPatch.chamberSkip;
                        }
                        else
                        {
                            FireArmRoundType prevRoundType = asRevolver.Chambers[i].RoundType;
                            asRevolver.Chambers[i].RoundType = roundType;
                            ++ChamberPatch.chamberSkip;
                            asRevolver.Chambers[i].SetRound(roundClass, asRevolver.Chambers[i].transform.position, asRevolver.Chambers[i].transform.rotation);
                            --ChamberPatch.chamberSkip;
                            asRevolver.Chambers[i].RoundType = prevRoundType;
                        }
                        modified = true;
                    }
                }
            }

            data.data = newData;

            return modified;
        }

        private int GetRevolverChamberIndex(FVRFireArmChamber chamber)
        {
            Revolver asRevolver = dataObject as Revolver;
            FVRFireArmChamber[] chambers = asRevolver.Chambers;

            if (chamber == null)
            {
                return -1;
            }

            for (int i = 0; i < chambers.Length; ++i)
            {
                if (chambers[i] == chamber)
                {
                    return i;
                }
            }

            return -1;
        }

        private void ChamberRevolverRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            Revolver asRevolver = dataObject as Revolver;

            ++ChamberPatch.chamberSkip;
            asRevolver.Chambers[chamberIndex].SetRound(roundClass, asRevolver.Chambers[chamberIndex].transform.position, asRevolver.Chambers[chamberIndex].transform.rotation);
            --ChamberPatch.chamberSkip;
        }

        private bool UpdateM203()
        {
            M203 asM203 = dataObject as M203;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[5];
                modified = true;
            }

            byte preIndex = data.data[0];

            // Write attached mount index
            if (asM203.Attachment.curMount == null)
            {
                data.data[0] = 255;
            }
            else
            {
                // Find the mount and set it
                bool found = false;
                for (int i = 0; i < asM203.Attachment.curMount.Parent.AttachmentMounts.Count; ++i)
                {
                    if (asM203.Attachment.curMount.Parent.AttachmentMounts[i] == asM203.Attachment.curMount)
                    {
                        data.data[0] = (byte)i;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    data.data[0] = 255;
                }
            }
            modified |= preIndex != data.data[0];

            byte preval = data.data[1];
            byte preval0 = data.data[2];
            byte preval1 = data.data[3];
            byte preval2 = data.data[4];

            // Write chambered round class
            if (asM203.Chamber.GetRound() == null || asM203.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 3);
            }
            else
            {
                BitConverter.GetBytes((short)asM203.Chamber.GetRound().RoundType).CopyTo(data.data, 1);
                BitConverter.GetBytes((short)asM203.Chamber.GetRound().RoundClass).CopyTo(data.data, 3);
            }

            modified |= (preval != data.data[1] || preval0 != data.data[2] || preval1 != data.data[3] || preval2 != data.data[4]);

            return modified;
        }

        private bool UpdateGivenM203(byte[] newData)
        {
            bool modified = data.data == null;
            M203 asM203 = dataObject as M203;

            byte preMountIndex = currentMountIndex;
            if (newData[0] == 255)
            {
                // Should not be mounted, check if currently is
                if (asM203.Attachment.curMount != null)
                {
                    ++data.ignoreParentChanged;
                    asM203.Attachment.DetachFromMount();
                    --data.ignoreParentChanged;
                    currentMountIndex = 255;

                    // Detach from mount will recover rigidbody, set as kinematic if not controller
                    // TODO: Review: May not need to do this anymore due to RecoverRigidBodyPatch
                    if (data.controller != H3MP_GameManager.ID)
                    {
                        Mod.SetKinematicRecursive(asM203.Attachment.transform, true);
                    }
                }
            }
            else
            {
                // Find mount instance we want to be mounted to
                FVRFireArmAttachmentMount mount = null;
                H3MP_TrackedItemData parentTrackedItemData = null;
                if (H3MP_ThreadManager.host)
                {
                    parentTrackedItemData = H3MP_Server.items[data.parent];
                }
                else
                {
                    parentTrackedItemData = H3MP_Client.items[data.parent];
                }

                if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null)
                {
                    // We want to be mounted, we have a parent
                    if (parentTrackedItemData.physicalItem.physicalObject.AttachmentMounts.Count > newData[0])
                    {
                        mount = parentTrackedItemData.physicalItem.physicalObject.AttachmentMounts[newData[0]];
                    }
                }

                // Mount could be null if the mount index corresponds to a parent we have yet to receive a change to
                if (mount != null && asM203.Attachment.curMount != mount)
                {
                    ++data.ignoreParentChanged;
                    if (asM203.Attachment.curMount != null)
                    {
                        asM203.Attachment.DetachFromMount();
                    }

                    if (mount.isMountableOn(asM203.Attachment))
                    {
                        asM203.Attachment.AttachToMount(mount, true);
                    }
                    currentMountIndex = newData[0];
                    --data.ignoreParentChanged;
                }
            }
            modified |= preMountIndex != currentMountIndex;

            // Set chamber
            short chamberTypeIndex = BitConverter.ToInt16(newData, 1);
            short chamberClassIndex = BitConverter.ToInt16(newData, 3);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asM203.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asM203.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asM203.Chamber.GetRound() == null || asM203.Chamber.GetRound().RoundClass != roundClass)
                {
                    if (asM203.Chamber.RoundType == roundType)
                    {
                        ++ChamberPatch.chamberSkip;
                        asM203.Chamber.SetRound(roundClass, asM203.Chamber.transform.position, asM203.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                    else
                    {
                        FireArmRoundType prevRoundType = asM203.Chamber.RoundType;
                        asM203.Chamber.RoundType = roundType;
                        ++ChamberPatch.chamberSkip;
                        asM203.Chamber.SetRound(roundClass, asM203.Chamber.transform.position, asM203.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                        asM203.Chamber.RoundType = prevRoundType;
                    }
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private void M203ChamberRound(FireArmRoundType roundType, FireArmRoundClass roundClass)
        {
            M203 asM203 = dataObject as M203;
            FireArmRoundType prevRoundType = asM203.Chamber.RoundType;
            asM203.Chamber.RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asM203.Chamber.SetRound(roundClass, asM203.Chamber.transform.position, asM203.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
            asM203.Chamber.RoundType = prevRoundType;
        }

        private FVRFireArmChamber M203GetChamber()
        {
            M203 asM203 = dataObject as M203;
            return asM203.Chamber;
        }

        private void ChamberM203Round(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            M203 asM203 = dataObject as M203;

            ++ChamberPatch.chamberSkip;
            asM203.Chamber.SetRound(roundClass, asM203.Chamber.transform.position, asM203.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
        }

        private bool UpdateGP25()
        {
            GP25 asGP25 = dataObject as GP25;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[6];
                modified = true;
            }

            byte preIndex = data.data[0];

            // Write attached mount index
            if (asGP25.Attachment.curMount == null)
            {
                data.data[0] = 255;
            }
            else
            {
                // Find the mount and set it
                bool found = false;
                for (int i = 0; i < asGP25.Attachment.curMount.Parent.AttachmentMounts.Count; ++i)
                {
                    if (asGP25.Attachment.curMount.Parent.AttachmentMounts[i] == asGP25.Attachment.curMount)
                    {
                        data.data[0] = (byte)i;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    data.data[0] = 255;
                }
            }
            modified |= preIndex != data.data[0];

            byte preval = data.data[1];

            // Write safety
            data.data[1] = (byte)(asGP25.m_safetyEngaged ? 1:0);

            modified |= preval != data.data[1];

            preval = data.data[2];
            byte preval0 = data.data[3];
            byte preval1 = data.data[4];
            byte preval2 = data.data[5];

            // Write chambered round class
            if (asGP25.Chamber.GetRound() == null || asGP25.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 4);
            }
            else
            {
                BitConverter.GetBytes((short)asGP25.Chamber.GetRound().RoundType).CopyTo(data.data, 2);
                BitConverter.GetBytes((short)asGP25.Chamber.GetRound().RoundClass).CopyTo(data.data, 4);
            }

            modified |= (preval != data.data[2] || preval0 != data.data[3] || preval1 != data.data[4] || preval2 != data.data[5]);

            return modified;
        }

        private bool UpdateGivenGP25(byte[] newData)
        {
            bool modified = false;
            GP25 asGP25 = dataObject as GP25;

            byte preMountIndex = currentMountIndex;
            if (newData[0] == 255)
            {
                // Should not be mounted, check if currently is
                if (asGP25.Attachment.curMount != null)
                {
                    ++data.ignoreParentChanged;
                    asGP25.Attachment.DetachFromMount();
                    --data.ignoreParentChanged;
                    currentMountIndex = 255;

                    // Detach from mount will recover rigidbody, set as kinematic if not controller
                    // TODO: Review: May not need to do this anymore due to RecoverRigidBodyPatch
                    if (data.controller != H3MP_GameManager.ID)
                    {
                        Mod.SetKinematicRecursive(asGP25.Attachment.transform, true);
                    }
                }
            }
            else
            {
                // Find mount instance we want to be mounted to
                FVRFireArmAttachmentMount mount = null;
                H3MP_TrackedItemData parentTrackedItemData = null;
                if (H3MP_ThreadManager.host)
                {
                    parentTrackedItemData = H3MP_Server.items[data.parent];
                }
                else
                {
                    parentTrackedItemData = H3MP_Client.items[data.parent];
                }

                if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null)
                {
                    // We want to be mounted, we have a parent
                    if (parentTrackedItemData.physicalItem.physicalObject.AttachmentMounts.Count > newData[0])
                    {
                        mount = parentTrackedItemData.physicalItem.physicalObject.AttachmentMounts[newData[0]];
                    }
                }

                // Mount could be null if the mount index corresponds to a parent we have yet a receive a change to
                if (mount != null && asGP25.Attachment.curMount != mount)
                {
                    ++data.ignoreParentChanged;
                    if (asGP25.Attachment.curMount != null)
                    {
                        asGP25.Attachment.DetachFromMount();
                    }

                    if (mount.isMountableOn(asGP25.Attachment)) 
                    { 
                        asGP25.Attachment.AttachToMount(mount, true);
                    }
                    currentMountIndex = newData[0];
                    --data.ignoreParentChanged;
                }
            }
            modified |= preMountIndex != currentMountIndex;

            if (data.data == null)
            {
                modified = true;

                // Set safety
                asGP25.m_safetyEngaged = newData[1] == 1;
            }
            else
            {
                if (data.data[1] != newData[1])
                {
                    // Set safety
                    asGP25.m_safetyEngaged = newData[1] == 1;
                    modified = true;
                }
            }

            // Set chamber
            short chamberTypeIndex = BitConverter.ToInt16(newData, 2);
            short chamberClassIndex = BitConverter.ToInt16(newData, 4);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asGP25.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asGP25.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asGP25.Chamber.GetRound() == null || asGP25.Chamber.GetRound().RoundClass != roundClass)
                {
                    if (asGP25.Chamber.RoundType == roundType)
                    {
                        ++ChamberPatch.chamberSkip;
                        asGP25.Chamber.SetRound(roundClass, asGP25.Chamber.transform.position, asGP25.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                    else
                    {
                        FireArmRoundType prevRoundType = asGP25.Chamber.RoundType;
                        asGP25.Chamber.RoundType = roundType;
                        ++ChamberPatch.chamberSkip;
                        asGP25.Chamber.SetRound(roundClass, asGP25.Chamber.transform.position, asGP25.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                        asGP25.Chamber.RoundType = prevRoundType;
                    }
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private void GP25ChamberRound(FireArmRoundType roundType, FireArmRoundClass roundClass)
        {
            GP25 asGP25 = dataObject as GP25;
            FireArmRoundType prevRoundType = asGP25.Chamber.RoundType;
            asGP25.Chamber.RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asGP25.Chamber.SetRound(roundClass, asGP25.Chamber.transform.position, asGP25.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
            asGP25.Chamber.RoundType = prevRoundType;
        }

        private FVRFireArmChamber GP25GetChamber()
        {
            GP25 asGP25 = dataObject as GP25;
            return asGP25.Chamber;
        }

        private void ChamberGP25Round(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            GP25 asGP25 = dataObject as GP25;

            ++ChamberPatch.chamberSkip;
            asGP25.Chamber.SetRound(roundClass, asGP25.Chamber.transform.position, asGP25.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
        }

        private bool UpdateAttachableTubeFed()
        {
            AttachableTubeFed asATF = dataObject as AttachableTubeFed;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[9];
                modified = true;
            }

            byte preIndex = data.data[0];

            // Write attached mount index
            if (asATF.Attachment.curMount == null)
            {
                data.data[0] = 255;
            }
            else
            {
                // Find the mount and set it
                bool found = false;
                for (int i = 0; i < asATF.Attachment.curMount.Parent.AttachmentMounts.Count; ++i)
                {
                    if (asATF.Attachment.curMount.Parent.AttachmentMounts[i] == asATF.Attachment.curMount)
                    {
                        data.data[0] = (byte)i;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    data.data[0] = 255;
                }
            }
            modified |= preIndex != data.data[0];

            byte preval = data.data[1];

            // Write fire mode index
            data.data[1] = asATF.IsSafetyEngaged ? (byte)1 : (byte)0;

            modified |= preval != data.data[1];

            preval = data.data[2];

            // Write hammer state
            data.data[2] = BitConverter.GetBytes(asATF.IsHammerCocked)[0];

            modified |= preval != data.data[2];

            preval = data.data[3];
            byte preval0 = data.data[4];
            byte preval1 = data.data[5];
            byte preval2 = data.data[6];

            // Write chambered round class
            if (asATF.Chamber.GetRound() == null || asATF.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 5);
            }
            else
            {
                BitConverter.GetBytes((short)asATF.Chamber.GetRound().RoundType).CopyTo(data.data, 3);
                BitConverter.GetBytes((short)asATF.Chamber.GetRound().RoundClass).CopyTo(data.data, 5);
            }

            modified |= (preval != data.data[3] || preval0 != data.data[4] || preval1 != data.data[5] || preval2 != data.data[6]);

            preval = data.data[7];

            // Write bolt handle pos
            data.data[7] = (byte)asATF.Bolt.CurPos;

            modified |= preval != data.data[7];

            if (asATF.HasHandle)
            {
                preval = data.data[8];

                // Write bolt handle pos
                data.data[8] = (byte)asATF.Handle.CurPos;

                modified |= preval != data.data[8];
            }

            return modified;
        }

        private bool UpdateGivenAttachableTubeFed(byte[] newData)
        {
            bool modified = false;
            AttachableTubeFed asATF = dataObject as AttachableTubeFed;

            byte preMountIndex = currentMountIndex;
            if (newData[0] == 255)
            {
                // Should not be mounted, check if currently is
                if (asATF.Attachment.curMount != null)
                {
                    ++data.ignoreParentChanged;
                    asATF.Attachment.DetachFromMount();
                    --data.ignoreParentChanged;
                    currentMountIndex = 255;

                    // Detach from mount will recover rigidbody, set as kinematic if not controller
                    // TODO: Review: May not need to do this anymore due to RecoverRigidBodyPatch
                    if (data.controller != H3MP_GameManager.ID)
                    {
                        Mod.SetKinematicRecursive(asATF.Attachment.transform, true);
                    }
                }
            }
            else
            {
                // Find mount instance we want to be mounted to
                FVRFireArmAttachmentMount mount = null;
                H3MP_TrackedItemData parentTrackedItemData = null;
                if (H3MP_ThreadManager.host)
                {
                    parentTrackedItemData = H3MP_Server.items[data.parent];
                }
                else
                {
                    parentTrackedItemData = H3MP_Client.items[data.parent];
                }

                if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null)
                {
                    // We want to be mounted, we have a parent
                    if (parentTrackedItemData.physicalItem.physicalObject.AttachmentMounts.Count > newData[0])
                    {
                        mount = parentTrackedItemData.physicalItem.physicalObject.AttachmentMounts[newData[0]];
                    }
                }

                // Mount could be null if the mount index corresponds to a parent we have yet a receive a change to
                if (mount != null && asATF.Attachment.curMount != mount)
                {
                    ++data.ignoreParentChanged;
                    if (asATF.Attachment.curMount != null)
                    {
                        asATF.Attachment.DetachFromMount();
                    }

                    if (mount.isMountableOn(asATF.Attachment)) 
                    { 
                        asATF.Attachment.AttachToMount(mount, true);
                    }
                    currentMountIndex = newData[0];
                    --data.ignoreParentChanged;
                }
            }
            modified |= preMountIndex != currentMountIndex;

            if (data.data == null)
            {
                modified = true;

                // Set safety
                if ((newData[1] == 1 && !asATF.IsSafetyEngaged) || (newData[1] == 0 && asATF.IsSafetyEngaged))
                {
                    asATF.ToggleSafety();
                }

                // Set bolt pos
                asATF.Bolt.LastPos = asATF.Bolt.CurPos;
                asATF.Bolt.CurPos = (AttachableTubeFedBolt.BoltPos)newData[7];

                if (asATF.HasHandle)
                {
                    // Set handle pos
                    asATF.Handle.LastPos = asATF.Handle.CurPos;
                    asATF.Handle.CurPos = (AttachableTubeFedFore.BoltPos)newData[8];
                }
            }
            else
            {
                // Set safety
                if ((newData[1] == 1 && !asATF.IsSafetyEngaged) || (newData[1] == 0 && asATF.IsSafetyEngaged))
                {
                    asATF.ToggleSafety();
                    modified = true;
                }
                if (data.data[7] != newData[7])
                {
                    // Set bolt pos
                    asATF.Bolt.LastPos = asATF.Bolt.CurPos;
                    asATF.Bolt.CurPos = (AttachableTubeFedBolt.BoltPos)newData[7];
                }
                if (asATF.HasHandle && data.data[8] != newData[8])
                {
                    // Set handle pos
                    asATF.Handle.LastPos = asATF.Handle.CurPos;
                    asATF.Handle.CurPos = (AttachableTubeFedFore.BoltPos)newData[8];
                }
            }

            // Set hammer state
            if (newData[2] == 0)
            {
                if (asATF.IsHammerCocked)
                {
                    Mod.AttachableTubeFed_m_isHammerCocked.SetValue(asATF, newData[2] == 1);
                    modified = true;
                }
            }
            else // Hammer should be cocked
            {
                if (!asATF.IsHammerCocked)
                {
                    asATF.CockHammer();
                    modified = true;
                }
            }

            // Set chamber
            short chamberTypeIndex = BitConverter.ToInt16(newData, 3);
            short chamberClassIndex = BitConverter.ToInt16(newData, 5);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asATF.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asATF.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asATF.Chamber.GetRound() == null || asATF.Chamber.GetRound().RoundClass != roundClass)
                {
                    if (asATF.Chamber.RoundType == roundType)
                    {
                        ++ChamberPatch.chamberSkip;
                        asATF.Chamber.SetRound(roundClass, asATF.Chamber.transform.position, asATF.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                    else
                    {
                        FireArmRoundType prevRoundType = asATF.Chamber.RoundType;
                        asATF.Chamber.RoundType = roundType;
                        ++ChamberPatch.chamberSkip;
                        asATF.Chamber.SetRound(roundClass, asATF.Chamber.transform.position, asATF.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                        asATF.Chamber.RoundType = prevRoundType;
                    }
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private void AttachableTubeFedChamberRound(FireArmRoundType roundType, FireArmRoundClass roundClass)
        {
            AttachableTubeFed asATF = dataObject as AttachableTubeFed;
            FireArmRoundType prevRoundType = asATF.Chamber.RoundType;
            asATF.Chamber.RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asATF.Chamber.SetRound(roundClass, asATF.Chamber.transform.position, asATF.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
            asATF.Chamber.RoundType = prevRoundType;
        }

        private FVRFireArmChamber AttachableTubeFedGetChamber()
        {
            AttachableTubeFed asATF = dataObject as AttachableTubeFed;
            return asATF.Chamber;
        }

        private void ChamberAttachableTubeFedRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            AttachableTubeFed asATF = dataObject as AttachableTubeFed;

            ++ChamberPatch.chamberSkip;
            asATF.Chamber.SetRound(roundClass, asATF.Chamber.transform.position, asATF.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
        }

        private bool UpdateAttachableClosedBoltWeapon()
        {
            AttachableClosedBoltWeapon asACBW = dataObject as AttachableClosedBoltWeapon;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[8];
                modified = true;
            }

            byte preIndex = data.data[0];

            // Write attached mount index
            if (asACBW.Attachment.curMount == null)
            {
                data.data[0] = 255;
            }
            else
            {
                // Find the mount and set it
                bool found = false;
                for (int i = 0; i < asACBW.Attachment.curMount.Parent.AttachmentMounts.Count; ++i)
                {
                    if (asACBW.Attachment.curMount.Parent.AttachmentMounts[i] == asACBW.Attachment.curMount)
                    {
                        data.data[0] = (byte)i;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    data.data[0] = 255;
                }
            }
            modified |= preIndex != data.data[0];

            byte preval = data.data[1];

            // Write fire mode index
            data.data[1] = (byte)asACBW.FireSelectorModeIndex;

            modified |= preval != data.data[1];

            preval = data.data[2];

            // Write camBurst
            data.data[2] = (byte)(int)Mod.AttachableClosedBoltWeapon_m_CamBurst.GetValue(asACBW);

            modified |= preval != data.data[2];

            preval = data.data[3];

            // Write hammer state
            data.data[3] = BitConverter.GetBytes(asACBW.IsHammerCocked)[0];

            modified |= preval != data.data[3];

            preval = data.data[4];
            byte preval0 = data.data[5];
            byte preval1 = data.data[6];
            byte preval2 = data.data[7];

            // Write chambered round class
            if (asACBW.Chamber.GetRound() == null || asACBW.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 6);
            }
            else
            {
                BitConverter.GetBytes((short)asACBW.Chamber.GetRound().RoundType).CopyTo(data.data, 4);
                BitConverter.GetBytes((short)asACBW.Chamber.GetRound().RoundClass).CopyTo(data.data, 6);
            }

            modified |= (preval != data.data[4] || preval0 != data.data[5] || preval1 != data.data[5] || preval2 != data.data[6]);

            return modified;
        }

        private bool UpdateGivenAttachableClosedBoltWeapon(byte[] newData)
        {
            bool modified = false;
            AttachableClosedBoltWeapon asACBW = dataObject as AttachableClosedBoltWeapon;

            byte preMountIndex = currentMountIndex;
            if (newData[0] == 255)
            {
                // Should not be mounted, check if currently is
                if (asACBW.Attachment.curMount != null)
                {
                    ++data.ignoreParentChanged;
                    asACBW.Attachment.DetachFromMount();
                    --data.ignoreParentChanged;
                    currentMountIndex = 255;

                    // Detach from mount will recover rigidbody, set as kinematic if not controller
                    // TODO: Review: May not need to do this anymore due to RecoverRigidBodyPatch
                    if (data.controller != H3MP_GameManager.ID)
                    {
                        Mod.SetKinematicRecursive(asACBW.Attachment.transform, true);
                    }
                }
            }
            else
            {
                // Find mount instance we want to be mounted to
                FVRFireArmAttachmentMount mount = null;
                H3MP_TrackedItemData parentTrackedItemData = null;
                if (H3MP_ThreadManager.host)
                {
                    parentTrackedItemData = H3MP_Server.items[data.parent];
                }
                else
                {
                    parentTrackedItemData = H3MP_Client.items[data.parent];
                }

                if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null)
                {
                    // We want to be mounted, we have a parent
                    if (parentTrackedItemData.physicalItem.physicalObject.AttachmentMounts.Count > newData[0])
                    {
                        mount = parentTrackedItemData.physicalItem.physicalObject.AttachmentMounts[newData[0]];
                    }
                }

                // Mount could be null if the mount index corresponds to a parent we have yet a receive a change to
                if (mount != null && asACBW.Attachment.curMount != mount)
                {
                    ++data.ignoreParentChanged;
                    if (asACBW.Attachment.curMount != null)
                    {
                        asACBW.Attachment.DetachFromMount();
                    }

                    if (mount.isMountableOn(asACBW.Attachment))
                    { 
                        asACBW.Attachment.AttachToMount(mount, true);
                    }
                    currentMountIndex = newData[0];
                    --data.ignoreParentChanged;
                }
            }
            modified |= preMountIndex != currentMountIndex;

            if (data.data == null)
            {
                modified = true;

                // Set fire select mode
                Mod.AttachableClosedBoltWeapon_m_fireSelectorMode.SetValue(asACBW, (int)newData[1]);

                // Set camBurst
                Mod.AttachableClosedBoltWeapon_m_CamBurst.SetValue(asACBW, (int)newData[2]);
            }
            else
            {
                if (data.data[0] != newData[0])
                {
                    // Set fire select mode
                    Mod.AttachableClosedBoltWeapon_m_fireSelectorMode.SetValue(asACBW, (int)newData[1]);
                    modified = true;
                }
                if (data.data[1] != newData[1])
                {
                    // Set camBurst
                    Mod.AttachableClosedBoltWeapon_m_CamBurst.SetValue(asACBW, (int)newData[2]);
                    modified = true;
                }
            }

            // Set hammer state
            if (newData[3] == 0)
            {
                if (asACBW.IsHammerCocked)
                {
                    Mod.AttachableClosedBoltWeapon_m_isHammerCocked.SetValue(asACBW, BitConverter.ToBoolean(newData, 3));
                    modified = true;
                }
            }
            else // Hammer should be cocked
            {
                if (!asACBW.IsHammerCocked)
                {
                    asACBW.CockHammer();
                    modified = true;
                }
            }

            // Set chamber
            short chamberTypeIndex = BitConverter.ToInt16(newData, 4);
            short chamberClassIndex = BitConverter.ToInt16(newData, 6);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asACBW.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asACBW.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asACBW.Chamber.GetRound() == null || asACBW.Chamber.GetRound().RoundClass != roundClass)
                {
                    if (asACBW.Chamber.RoundType == roundType)
                    {
                        ++ChamberPatch.chamberSkip;
                        asACBW.Chamber.SetRound(roundClass, asACBW.Chamber.transform.position, asACBW.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                    else
                    {
                        FireArmRoundType prevRoundType = asACBW.Chamber.RoundType;
                        asACBW.Chamber.RoundType = roundType;
                        ++ChamberPatch.chamberSkip;
                        asACBW.Chamber.SetRound(roundClass, asACBW.Chamber.transform.position, asACBW.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                        asACBW.Chamber.RoundType = prevRoundType;
                    }
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private void AttachableClosedBoltWeaponChamberRound(FireArmRoundType roundType, FireArmRoundClass roundClass)
        {
            AttachableClosedBoltWeapon asACBW = dataObject as AttachableClosedBoltWeapon;
            FireArmRoundType prevRoundType = asACBW.Chamber.RoundType;
            asACBW.Chamber.RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asACBW.Chamber.SetRound(roundClass, asACBW.Chamber.transform.position, asACBW.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
            asACBW.Chamber.RoundType = prevRoundType;
        }

        private FVRFireArmChamber AttachableClosedBoltWeaponGetChamber()
        {
            AttachableClosedBoltWeapon asACBW = dataObject as AttachableClosedBoltWeapon;
            return asACBW.Chamber;
        }

        private void ChamberAttachableClosedBoltWeaponRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            AttachableClosedBoltWeapon asACBW = dataObject as AttachableClosedBoltWeapon;

            ++ChamberPatch.chamberSkip;
            asACBW.Chamber.SetRound(roundClass, asACBW.Chamber.transform.position, asACBW.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
        }

        private bool UpdateAttachableBreakActions()
        {
            AttachableBreakActions asABA = dataObject as AttachableBreakActions;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[6];
                modified = true;
            }

            byte preIndex = data.data[0];

            // Write attached mount index
            if (asABA.Attachment.curMount == null)
            {
                data.data[0] = 255;
            }
            else
            {
                // Find the mount and set it
                bool found = false;
                for (int i = 0; i < asABA.Attachment.curMount.Parent.AttachmentMounts.Count; ++i)
                {
                    if (asABA.Attachment.curMount.Parent.AttachmentMounts[i] == asABA.Attachment.curMount)
                    {
                        data.data[0] = (byte)i;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    data.data[0] = 255;
                }
            }
            modified |= preIndex != data.data[0];

            byte preval = data.data[1];

            // Write breachOpen
            data.data[1] = ((bool)Mod.AttachableBreakActions_m_isBreachOpen.GetValue(asABA)) ? (byte)1 : (byte)0;

            modified |= preval != data.data[1];

            preval = data.data[2];
            byte preval0 = data.data[3];
            byte preval1 = data.data[4];
            byte preval2 = data.data[5];

            // Write chambered round class
            if (asABA.Chamber.GetRound() == null || asABA.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 4);
            }
            else
            {
                BitConverter.GetBytes((short)asABA.Chamber.GetRound().RoundType).CopyTo(data.data, 2);
                BitConverter.GetBytes((short)asABA.Chamber.GetRound().RoundClass).CopyTo(data.data, 4);
            }

            modified |= (preval != data.data[2] || preval0 != data.data[3] || preval1 != data.data[4] || preval2 != data.data[5]);

            return modified;
        }

        private bool UpdateGivenAttachableBreakActions(byte[] newData)
        {
            bool modified = false;
            AttachableBreakActions asABA = dataObject as AttachableBreakActions;

            byte preMountIndex = currentMountIndex;
            if (newData[0] == 255)
            {
                // Should not be mounted, check if currently is
                if (asABA.Attachment.curMount != null)
                {
                    ++data.ignoreParentChanged;
                    asABA.Attachment.DetachFromMount();
                    --data.ignoreParentChanged;
                    currentMountIndex = 255;

                    // Detach from mount will recover rigidbody, set as kinematic if not controller
                    // TODO: Review: May not need to do this anymore due to RecoverRigidBodyPatch
                    if (data.controller != H3MP_GameManager.ID)
                    {
                        Mod.SetKinematicRecursive(asABA.Attachment.transform, true);
                    }
                }
            }
            else
            {
                // Find mount instance we want to be mounted to
                FVRFireArmAttachmentMount mount = null;
                H3MP_TrackedItemData parentTrackedItemData = null;
                if (H3MP_ThreadManager.host)
                {
                    parentTrackedItemData = H3MP_Server.items[data.parent];
                }
                else
                {
                    parentTrackedItemData = H3MP_Client.items[data.parent];
                }

                if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null)
                {
                    // We want to be mounted, we have a parent
                    if (parentTrackedItemData.physicalItem.physicalObject.AttachmentMounts.Count > newData[0])
                    {
                        mount = parentTrackedItemData.physicalItem.physicalObject.AttachmentMounts[newData[0]];
                    }
                }

                // Mount could be null if the mount index corresponds to a parent we have yet a receive a change to
                if (mount != null && asABA.Attachment.curMount != mount)
                {
                    ++data.ignoreParentChanged;
                    if (asABA.Attachment.curMount != null)
                    {
                        asABA.Attachment.DetachFromMount();
                    }

                    if (mount.isMountableOn(asABA.Attachment)) 
                    { 
                        asABA.Attachment.AttachToMount(mount, true);
                    }
                    currentMountIndex = newData[0];
                    --data.ignoreParentChanged;
                }
            }
            modified |= preMountIndex != currentMountIndex;

            // Set breachOpen
            bool current = ((bool)Mod.AttachableBreakActions_m_isBreachOpen.GetValue(asABA));
            bool newVal = newData[1] == 1;
            if ((current && !newVal)||(!current && newVal))
            {
                asABA.ToggleBreach();
                modified = true;
            }
            Mod.AttachableBreakActions_m_isBreachOpen.SetValue(asABA, newVal);

            // Set chamber
            short chamberTypeIndex = BitConverter.ToInt16(newData, 2);
            short chamberClassIndex = BitConverter.ToInt16(newData, 4);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asABA.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asABA.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asABA.Chamber.GetRound() == null || asABA.Chamber.GetRound().RoundClass != roundClass)
                {
                    if (asABA.Chamber.RoundType == roundType)
                    {
                        ++ChamberPatch.chamberSkip;
                        asABA.Chamber.SetRound(roundClass, asABA.Chamber.transform.position, asABA.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                    else
                    {
                        FireArmRoundType prevRoundType = asABA.Chamber.RoundType;
                        asABA.Chamber.RoundType = roundType;
                        ++ChamberPatch.chamberSkip;
                        asABA.Chamber.SetRound(roundClass, asABA.Chamber.transform.position, asABA.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                        asABA.Chamber.RoundType = prevRoundType;
                    }
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private void AttachableBreakActionsChamberRound(FireArmRoundType roundType, FireArmRoundClass roundClass)
        {
            AttachableBreakActions asABA = dataObject as AttachableBreakActions;
            FireArmRoundType prevRoundType = asABA.Chamber.RoundType;
            asABA.Chamber.RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asABA.Chamber.SetRound(roundClass, asABA.Chamber.transform.position, asABA.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
            asABA.Chamber.RoundType = prevRoundType;
        }

        private FVRFireArmChamber AttachableBreakActionsGetChamber()
        {
            AttachableBreakActions asABA = dataObject as AttachableBreakActions;
            return asABA.Chamber;
        }

        private void ChamberAttachableBreakActionsRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            AttachableBreakActions asABA = dataObject as AttachableBreakActions;

            ++ChamberPatch.chamberSkip;
            asABA.Chamber.SetRound(roundClass, asABA.Chamber.transform.position, asABA.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
        }

        private void UpdateAttachableFirearmParent()
        {
            FVRFireArmAttachment asAttachment = (dataObject as AttachableFirearm).Attachment;

            if (currentMountIndex != 255) // We want to be attached to a mount
            {
                if (data.parent != -1) // We have parent
                {
                    // We could be on wrong mount (or none physically) if we got a new mount through update but the parent hadn't been updated yet

                    // Get the mount we are supposed to be mounted to
                    FVRFireArmAttachmentMount mount = null;
                    H3MP_TrackedItemData parentTrackedItemData = null;
                    if (H3MP_ThreadManager.host)
                    {
                        parentTrackedItemData = H3MP_Server.items[data.parent];
                    }
                    else
                    {
                        parentTrackedItemData = H3MP_Client.items[data.parent];
                    }

                    if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null)
                    {
                        mount = parentTrackedItemData.physicalItem.physicalObject.AttachmentMounts[currentMountIndex];
                    }

                    // If not yet physically mounted to anything, can right away mount to the proper mount
                    if (asAttachment.curMount == null)
                    {
                        if (mount.isMountableOn(asAttachment))
                        {
                            ++data.ignoreParentChanged;
                            asAttachment.AttachToMount(mount, true);
                            --data.ignoreParentChanged;
                        }
                    }
                    else if (asAttachment.curMount != mount) // Already mounted, but not on the right one, need to unmount, then mount of right one
                    {
                        ++data.ignoreParentChanged;
                        if (asAttachment.curMount != null)
                        {
                            asAttachment.DetachFromMount();
                        }

                        if (mount.isMountableOn(asAttachment))
                        {
                            asAttachment.AttachToMount(mount, true);
                        }
                        --data.ignoreParentChanged;
                    }
                }
                // else, if this happens it is because we received a parent update to null and just haven't gotten the up to date mount index of -1 yet
                //       This will be handled on update
            }
            // else, on update we will detach from any current mount if this is the case, no need to handle this here
        }

        private bool UpdateLAPD2019Battery()
        {
            LAPD2019Battery asLAPD2019Battery = dataObject as LAPD2019Battery;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[4];
                modified = true;
            }

            byte preval = data.data[0];
            byte preval0 = data.data[1];
            byte preval1 = data.data[2];
            byte preval2 = data.data[3];

            // Write energy
            BitConverter.GetBytes(asLAPD2019Battery.GetEnergy()).CopyTo(data.data, 0);

            modified |= (preval != data.data[0] || preval0 != data.data[1] || preval1 != data.data[2] || preval2 != data.data[3]);

            return modified;
        }

        private bool UpdateGivenLAPD2019Battery(byte[] newData)
        {
            bool modified = false;
            LAPD2019Battery asLAPD2019Battery = dataObject as LAPD2019Battery;

            if (data.data == null)
            {
                modified = true;

                // Set energy
                asLAPD2019Battery.SetEnergy(BitConverter.ToSingle(newData, 0));
            }
            else
            {
                if (data.data[11] != newData[11] || data.data[12] != newData[12] || data.data[13] != newData[13] || data.data[14] != newData[14])
                {
                    // Set energy
                    asLAPD2019Battery.SetEnergy(BitConverter.ToSingle(newData, 0));
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool UpdateLAPD2019()
        {
            LAPD2019 asLAPD2019 = dataObject as LAPD2019;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[26];
                modified = true;
            }

            byte preval = data.data[0];

            // Write curChamber
            data.data[0] = (byte)asLAPD2019.CurChamber;

            modified |= preval != data.data[0];

            byte preval0;
            byte preval1;
            byte preval2;

            // Write chambered round classes
            for(int i=0; i < 5; ++i)
            {
                int firstIndex = i * 4 + 1;
                preval = data.data[firstIndex];
                preval0 = data.data[firstIndex + 1];
                preval1 = data.data[firstIndex + 2];
                preval2 = data.data[firstIndex + 3];
                if (asLAPD2019.Chambers[i].GetRound() == null || asLAPD2019.Chambers[i].GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(data.data, firstIndex + 2);
                }
                else
                {
                    BitConverter.GetBytes((short)asLAPD2019.Chambers[i].GetRound().RoundType).CopyTo(data.data, firstIndex);
                    BitConverter.GetBytes((short)asLAPD2019.Chambers[i].GetRound().RoundClass).CopyTo(data.data, firstIndex + 2);
                }

                modified |= (preval != data.data[firstIndex] || preval0 != data.data[firstIndex + 1] || preval1 != data.data[firstIndex + 2] || preval2 != data.data[firstIndex + 3]);
            }

            preval = data.data[21];
            preval0 = data.data[22];
            preval1 = data.data[23];
            preval2 = data.data[24];

            // Write capacitor charge
            BitConverter.GetBytes((float)Mod.LAPD2019_m_capacitorCharge.GetValue(asLAPD2019)).CopyTo(data.data, 21);

            modified |= (preval != data.data[21] || preval0 != data.data[22] || preval1 != data.data[23] || preval2 != data.data[24]);

            preval = data.data[25];

            // Write capacitor charged
            data.data[25] = (bool)Mod.LAPD2019_m_isCapacitorCharged.GetValue(asLAPD2019) ? (byte)1 : (byte)0;

            modified |= preval != data.data[25];

            return modified;
        }

        private bool UpdateGivenLAPD2019(byte[] newData)
        {
            bool modified = false;
            LAPD2019 asLAPD2019 = dataObject as LAPD2019;

            if (data.data == null)
            {
                modified = true;

                // Set curChamber
                asLAPD2019.CurChamber = newData[0];

                // Set capacitor charge
                Mod.LAPD2019_m_capacitorCharge.SetValue(asLAPD2019, BitConverter.ToSingle(newData, 21));

                // Set capacitor charged
                Mod.LAPD2019_m_capacitorCharge.SetValue(asLAPD2019, newData[25] == 1);
            }
            else
            {
                if (data.data[0] != newData[0])
                {
                    // Set curChamber
                    asLAPD2019.CurChamber = newData[0];
                    modified = true;
                }
                if (data.data[21] != newData[21] || data.data[22] != newData[22] || data.data[23] != newData[23] || data.data[24] != newData[24])
                {
                    // Set capacitor charge
                    Mod.LAPD2019_m_capacitorCharge.SetValue(asLAPD2019, BitConverter.ToSingle(newData, 21));
                    modified = true;
                }
                if (data.data[25] != newData[25])
                {
                    // Set capacitor charged
                    Mod.LAPD2019_m_capacitorCharge.SetValue(asLAPD2019, newData[25] == 1);
                    modified = true;
                }
            }

            // Set chambers
            for (int i = 0; i < 5; ++i)
            {
                short chamberTypeIndex = BitConverter.ToInt16(newData, i * 4 + 1);
                short chamberClassIndex = BitConverter.ToInt16(newData, i * 4 + 3);
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (asLAPD2019.Chambers[i].GetRound() != null)
                    {
                        ++ChamberPatch.chamberSkip;
                        asLAPD2019.Chambers[i].SetRound(null, false);
                        --ChamberPatch.chamberSkip;
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (asLAPD2019.Chambers[i].GetRound() == null || asLAPD2019.Chambers[i].GetRound().RoundClass != roundClass)
                    {
                        if (asLAPD2019.Chambers[i].RoundType == roundType)
                        {
                            ++ChamberPatch.chamberSkip;
                            asLAPD2019.Chambers[i].SetRound(roundClass, asLAPD2019.Chambers[i].transform.position, asLAPD2019.Chambers[i].transform.rotation);
                            --ChamberPatch.chamberSkip;
                        }
                        else
                        {
                            FireArmRoundType prevRoundType = asLAPD2019.Chambers[i].RoundType;
                            asLAPD2019.Chambers[i].RoundType = roundType;
                            ++ChamberPatch.chamberSkip;
                            asLAPD2019.Chambers[i].SetRound(roundClass, asLAPD2019.Chambers[i].transform.position, asLAPD2019.Chambers[i].transform.rotation);
                            --ChamberPatch.chamberSkip;
                            asLAPD2019.Chambers[i].RoundType = prevRoundType;
                        }
                        modified = true;
                    }
                }
            }

            data.data = newData;

            return modified;
        }

        private int GetLAPD2019ChamberIndex(FVRFireArmChamber chamber)
        {
            LAPD2019 asLAPD2019 = dataObject as LAPD2019;
            FVRFireArmChamber[] chambers = asLAPD2019.Chambers;

            if(chamber == null)
            {
                return -1;
            }

            for(int i = 0; i < chambers.Length; ++i)
            {
                if (chambers[i] == chamber)
                {
                    return i;
                }
            }

            return - 1;
        }

        private bool UpdateSosigWeaponInterface()
        {
            SosigWeaponPlayerInterface asInterface = dataObject as SosigWeaponPlayerInterface;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[3];
                modified = true;
            }

            byte preval = data.data[0];
            byte preval0 = data.data[1];

            // Write shots left
            BitConverter.GetBytes((short)(int)Mod.SosigWeapon_m_shotsLeft.GetValue(asInterface.W)).CopyTo(data.data, 0);

            modified |= (preval != data.data[0] || preval0 != data.data[1]);

            preval = data.data[2];

            // Write MechaState
            data.data[2] = (byte)asInterface.W.MechaState;

            modified |= preval != data.data[2];

            return modified;
        }

        private bool UpdateGivenSosigWeaponInterface(byte[] newData)
        {
            bool modified = false;
            SosigWeaponPlayerInterface asInterface = dataObject as SosigWeaponPlayerInterface;

            if (data.data == null)
            {
                modified = true;

                // Set shots left
                Mod.SosigWeapon_m_shotsLeft.SetValue(asInterface.W, BitConverter.ToInt16(newData, 0));

                // Set MechaState
                asInterface.W.MechaState = (SosigWeapon.SosigWeaponMechaState)newData[2];
            }
            else 
            {
                if (data.data[0] != newData[0] || data.data[1] != newData[1])
                {
                    // Set shots left
                    Mod.SosigWeapon_m_shotsLeft.SetValue(asInterface.W, BitConverter.ToInt16(newData, 0));
                    modified = true;
                }
                if (data.data[2] != newData[2])
                {
                    // Set MechaState
                    asInterface.W.MechaState = (SosigWeapon.SosigWeaponMechaState)newData[2];
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool UpdateClosedBoltWeapon()
        {
            ClosedBoltWeapon asCBW = dataObject as ClosedBoltWeapon;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[7];
                modified = true;
            }

            byte preval = data.data[0];

            // Write fire mode index
            data.data[0] = (byte)asCBW.FireSelectorModeIndex;

            modified |= preval != data.data[0];

            preval = data.data[1];

            // Write camBurst
            data.data[1] = (byte)(int)Mod.ClosedBoltWeapon_m_CamBurst.GetValue(asCBW);

            modified |= preval != data.data[1];

            preval = data.data[2];

            // Write hammer state
            data.data[2] = asCBW.IsHammerCocked ? (byte)1 : (byte)0;

            modified |= preval != data.data[2];

            preval = data.data[3];
            byte preval0 = data.data[4];
            byte preval1 = data.data[5];
            byte preval2 = data.data[6];

            // Write chambered round class
            if(asCBW.Chamber.GetRound() == null || asCBW.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 5);
            }
            else
            {
                BitConverter.GetBytes((short)asCBW.Chamber.GetRound().RoundType).CopyTo(data.data, 3);
                BitConverter.GetBytes((short)asCBW.Chamber.GetRound().RoundClass).CopyTo(data.data, 5);
            }

            modified |= (preval != data.data[3] || preval0 != data.data[4] || preval1 != data.data[5] || preval2 != data.data[6]);

            return modified;
        }

        private bool UpdateGivenClosedBoltWeapon(byte[] newData)
        {
            bool modified = false;
            ClosedBoltWeapon asCBW = dataObject as ClosedBoltWeapon;

            if(data.data == null)
            {
                modified = true;

                // Set fire select mode
                Mod.ClosedBoltWeapon_m_fireSelectorMode.SetValue(asCBW, (int)newData[0]);

                // Set camBurst
                Mod.ClosedBoltWeapon_m_CamBurst.SetValue(asCBW, (int)newData[1]);
            }
            else 
            {
                if (data.data[0] != newData[0])
                {
                    // Set fire select mode
                    Mod.ClosedBoltWeapon_m_fireSelectorMode.SetValue(asCBW, (int)newData[0]);
                    modified = true;
                }
                if (data.data[1] != newData[1])
                {
                    // Set camBurst
                    Mod.ClosedBoltWeapon_m_CamBurst.SetValue(asCBW, (int)newData[1]);
                    modified = true;
                }
            }

            // Set hammer state
            if (newData[2] == 0)
            {
                if (asCBW.IsHammerCocked)
                {
                    Mod.ClosedBoltWeapon_m_isHammerCocked.SetValue(asCBW, BitConverter.ToBoolean(newData, 2));
                    modified = true;
                }
            }
            else // Hammer should be cocked
            {
                if (!asCBW.IsHammerCocked)
                {
                    asCBW.CockHammer();
                    modified = true;
                }
            }

            // Set chamber
            short chamberTypeIndex = BitConverter.ToInt16(newData, 3);
            short chamberClassIndex = BitConverter.ToInt16(newData, 5);
            if(chamberClassIndex == -1) // We don't want round in chamber
            {
                if(asCBW.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asCBW.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                FireArmRoundClass roundClass = (FireArmRoundClass) chamberClassIndex;
                if (asCBW.Chamber.GetRound() == null || asCBW.Chamber.GetRound().RoundClass != roundClass)
                {
                    if (asCBW.Chamber.RoundType == roundType)
                    {
                        ++ChamberPatch.chamberSkip;
                        asCBW.Chamber.SetRound(roundClass, asCBW.Chamber.transform.position, asCBW.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                    else
                    {
                        FireArmRoundType prevRoundType = asCBW.Chamber.RoundType;
                        asCBW.Chamber.RoundType = roundType;
                        ++ChamberPatch.chamberSkip;
                        asCBW.Chamber.SetRound(roundClass, asCBW.Chamber.transform.position, asCBW.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                        asCBW.Chamber.RoundType = prevRoundType;
                    }
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private void SetCBWUpdateOverride(FireArmRoundType roundType, FireArmRoundClass roundClass, int chamberIndex)
        {
            ClosedBoltWeapon asCBW = (ClosedBoltWeapon)dataObject;

            FireArmRoundType prevRoundType = asCBW.Chamber.RoundType;
            asCBW.Chamber.RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asCBW.Chamber.SetRound(roundClass, asCBW.Chamber.transform.position, asCBW.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
            asCBW.Chamber.RoundType = prevRoundType;
        }

        private void ChamberClosedBoltWeaponRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            ClosedBoltWeapon asCBW = (ClosedBoltWeapon)dataObject;

            ++ChamberPatch.chamberSkip;
            asCBW.Chamber.SetRound(roundClass, asCBW.Chamber.transform.position, asCBW.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
        }

        private bool FireCBW(int chamberIndex)
        {
            return (dataObject as ClosedBoltWeapon).Fire();
        }

        private bool UpdateHandgun()
        {
            Handgun asHandgun = dataObject as Handgun;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[7];
                modified = true;
            }

            byte preval = data.data[0];

            // Write fire mode index
            data.data[0] = (byte)asHandgun.FireSelectorModeIndex;

            modified |= preval != data.data[0];

            preval = data.data[1];

            // Write camBurst
            data.data[1] = (byte)(int)Mod.Handgun_m_CamBurst.GetValue(asHandgun);

            modified |= preval != data.data[1];

            preval = data.data[2];

            // Write hammer state
            data.data[2] = BitConverter.GetBytes((bool)Mod.Handgun_m_isHammerCocked.GetValue(asHandgun))[0];

            modified |= preval != data.data[2];

            preval = data.data[3];
            byte preval0 = data.data[4];
            byte preval1 = data.data[5];
            byte preval2 = data.data[6];

            // Write chambered round class
            if (asHandgun.Chamber.GetRound() == null || asHandgun.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 5);
            }
            else
            {
                BitConverter.GetBytes((short)asHandgun.Chamber.GetRound().RoundType).CopyTo(data.data, 3);
                BitConverter.GetBytes((short)asHandgun.Chamber.GetRound().RoundClass).CopyTo(data.data, 5);
            }

            modified |= (preval != data.data[3] || preval0 != data.data[4] || preval1 != data.data[5] || preval2 != data.data[6]);

            return modified;
        }

        private bool UpdateGivenHandgun(byte[] newData)
        {
            bool modified = false;
            Handgun asHandgun = dataObject as Handgun;

            if(data.data == null)
            {
                modified = true;

                // Set fire select mode
                Mod.Handgun_m_fireSelectorMode.SetValue(asHandgun, (int)newData[0]);

                // Set camBurst
                Mod.Handgun_m_CamBurst.SetValue(asHandgun, (int)newData[1]);
            }
            else 
            {
                if (data.data[0] != newData[0])
                {
                    // Set fire select mode
                    Mod.Handgun_m_fireSelectorMode.SetValue(asHandgun, (int)newData[0]);
                    modified = true;
                }
                if (data.data[1] != newData[1])
                {
                    // Set camBurst
                    Mod.Handgun_m_CamBurst.SetValue(asHandgun, (int)newData[1]);
                    modified = true;
                }
            }

            bool isHammerCocked = (bool)Mod.Handgun_m_isHammerCocked.GetValue(asHandgun);

            // Set hammer state
            if (newData[2] == 0)
            {
                if (isHammerCocked)
                {
                    Mod.Handgun_m_isHammerCocked.SetValue(asHandgun, BitConverter.ToBoolean(newData, 2));
                    modified = true;
                }
            }
            else // Hammer should be cocked
            {
                if (!isHammerCocked)
                {
                    asHandgun.CockHammer(false);
                    modified = true;
                }
            }

            // Set chamber
            short chamberTypeIndex = BitConverter.ToInt16(newData, 3);
            short chamberClassIndex = BitConverter.ToInt16(newData, 5);
            if(chamberClassIndex == -1) // We don't want round in chamber
            {
                if(asHandgun.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asHandgun.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                FireArmRoundClass roundClass = (FireArmRoundClass) chamberClassIndex;
                if (asHandgun.Chamber.GetRound() == null || asHandgun.Chamber.GetRound().RoundClass != roundClass)
                {
                    if (asHandgun.Chamber.RoundType == roundType)
                    {
                        ++ChamberPatch.chamberSkip;
                        asHandgun.Chamber.SetRound(roundClass, asHandgun.Chamber.transform.position, asHandgun.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                    else
                    {
                        FireArmRoundType prevRoundType = asHandgun.Chamber.RoundType;
                        asHandgun.Chamber.RoundType = roundType;
                        ++ChamberPatch.chamberSkip;
                        asHandgun.Chamber.SetRound(roundClass, asHandgun.Chamber.transform.position, asHandgun.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                        asHandgun.Chamber.RoundType = prevRoundType;
                    }
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private void SetHandgunUpdateOverride(FireArmRoundType roundType, FireArmRoundClass roundClass, int chamberIndex)
        {
            Handgun asHandgun = dataObject as Handgun;

            FireArmRoundType prevRoundType = asHandgun.Chamber.RoundType;
            asHandgun.Chamber.RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asHandgun.Chamber.SetRound(roundClass, asHandgun.Chamber.transform.position, asHandgun.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
            asHandgun.Chamber.RoundType = prevRoundType;
        }

        private void ChamberHandgunRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            Handgun asHandgun = dataObject as Handgun;

            ++ChamberPatch.chamberSkip;
            asHandgun.Chamber.SetRound(roundClass, asHandgun.Chamber.transform.position, asHandgun.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
        }

        private bool FireHandgun(int chamberIndex)
        {
            return (dataObject as Handgun).Fire();
        }

        private bool UpdateTubeFedShotgun()
        {
            TubeFedShotgun asTFS = dataObject as TubeFedShotgun;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[8];
                modified = true;
            }

            byte preval = data.data[0];

            // Write fire mode index
            data.data[0] = asTFS.IsSafetyEngaged ? (byte)1 : (byte)0;

            modified |= preval != data.data[0];

            preval = data.data[1];

            // Write hammer state
            data.data[1] = BitConverter.GetBytes(asTFS.IsHammerCocked)[0];

            modified |= preval != data.data[1];

            preval = data.data[2];
            byte preval0 = data.data[3];
            byte preval1 = data.data[4];
            byte preval2 = data.data[5];

            // Write chambered round class
            if(asTFS.Chamber.GetRound() == null || asTFS.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 4);
            }
            else
            {
                BitConverter.GetBytes((short)asTFS.Chamber.GetRound().RoundType).CopyTo(data.data, 2);
                BitConverter.GetBytes((short)asTFS.Chamber.GetRound().RoundClass).CopyTo(data.data, 4);
            }

            modified |= (preval != data.data[2] || preval0 != data.data[3] || preval != data.data[4] || preval0 != data.data[5]);

            preval = data.data[6];

            // Write bolt handle pos
            data.data[6] = (byte)asTFS.Bolt.CurPos;

            modified |= preval != data.data[6];

            if (asTFS.HasHandle)
            {
                preval = data.data[7];

                // Write bolt handle pos
                data.data[7] = (byte)asTFS.Handle.CurPos;

                modified |= preval != data.data[7];
            }

            return modified;
        }

        private bool UpdateGivenTubeFedShotgun(byte[] newData)
        {
            bool modified = false;
            TubeFedShotgun asTFS = dataObject as TubeFedShotgun;

            if (data.data == null)
            {
                modified = true;

                // Set safety
                if (asTFS.HasSafety && ((newData[0] == 1 && !asTFS.IsSafetyEngaged) || (newData[0] == 0 && asTFS.IsSafetyEngaged)))
                {
                    asTFS.ToggleSafety();
                }

                // Set bolt pos
                asTFS.Bolt.LastPos = asTFS.Bolt.CurPos;
                asTFS.Bolt.CurPos = (TubeFedShotgunBolt.BoltPos)newData[6];

                if (asTFS.HasHandle)
                {
                    // Set handle pos
                    asTFS.Handle.LastPos = asTFS.Handle.CurPos;
                    asTFS.Handle.CurPos = (TubeFedShotgunHandle.BoltPos)newData[7];
                }
            }
            else
            {
                // Set safety
                if (asTFS.HasSafety && ((newData[0] == 1 && !asTFS.IsSafetyEngaged) || (newData[0] == 0 && asTFS.IsSafetyEngaged)))
                {
                    asTFS.ToggleSafety();
                    modified = true;
                }
                if (data.data[6] != newData[6])
                {
                    // Set bolt pos
                    asTFS.Bolt.LastPos = asTFS.Bolt.CurPos;
                    asTFS.Bolt.CurPos = (TubeFedShotgunBolt.BoltPos)newData[6];
                }
                if (asTFS.HasHandle && data.data[7] != newData[7])
                {
                    // Set handle pos
                    asTFS.Handle.LastPos = asTFS.Handle.CurPos;
                    asTFS.Handle.CurPos = (TubeFedShotgunHandle.BoltPos)newData[7];
                }
            }

            // Set hammer state
            if (newData[1] == 0)
            {
                if (asTFS.IsHammerCocked)
                {
                    Mod.TubeFedShotgun_m_isHammerCocked.SetValue(asTFS, false);
                    modified = true;
                }
            }
            else // Hammer should be cocked
            {
                if (!asTFS.IsHammerCocked)
                {
                    asTFS.CockHammer();
                    modified = true;
                }
            }

            // Set chamber
            short chamberTypeIndex = BitConverter.ToInt16(newData, 2);
            short chamberClassIndex = BitConverter.ToInt16(newData, 4);
            if(chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asTFS.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asTFS.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                FireArmRoundClass roundClass = (FireArmRoundClass) chamberClassIndex;
                if (asTFS.Chamber.GetRound() == null || asTFS.Chamber.GetRound().RoundClass != roundClass)
                {
                    if (asTFS.Chamber.RoundType == roundType)
                    {
                        ++ChamberPatch.chamberSkip;
                        asTFS.Chamber.SetRound(roundClass, asTFS.Chamber.transform.position, asTFS.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                    else
                    {
                        FireArmRoundType prevRoundType = asTFS.Chamber.RoundType;
                        asTFS.Chamber.RoundType = roundType;
                        ++ChamberPatch.chamberSkip;
                        asTFS.Chamber.SetRound(roundClass, asTFS.Chamber.transform.position, asTFS.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                        asTFS.Chamber.RoundType = prevRoundType;
                    }
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private void SetTFSUpdateOverride(FireArmRoundType roundType, FireArmRoundClass roundClass, int chamberIndex)
        {
            TubeFedShotgun asTFS = dataObject as TubeFedShotgun;

            FireArmRoundType prevRoundType = asTFS.Chamber.RoundType;
            asTFS.Chamber.RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asTFS.Chamber.SetRound(roundClass, asTFS.Chamber.transform.position, asTFS.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
            asTFS.Chamber.RoundType = prevRoundType;
        }

        private void ChamberTubeFedShotgunRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            TubeFedShotgun asTFS = dataObject as TubeFedShotgun;

            ++ChamberPatch.chamberSkip;
            asTFS.Chamber.SetRound(roundClass, asTFS.Chamber.transform.position, asTFS.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
        }

        private bool FireTFS(int chamberIndex)
        {
            return (dataObject as TubeFedShotgun).Fire();
        }

        private bool UpdateBoltActionRifle()
        {
            BoltActionRifle asBAR = dataObject as BoltActionRifle;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[8];
                modified = true;
            }

            byte preval = data.data[0];

            // Write fire mode index
            data.data[0] = (byte)(int)Mod.BoltActionRifle_m_fireSelectorMode.GetValue(asBAR);

            modified |= preval != data.data[0];

            preval = data.data[1];

            // Write hammer state
            data.data[1] = BitConverter.GetBytes(asBAR.IsHammerCocked)[0];

            modified |= preval != data.data[1];

            preval = data.data[2];
            byte preval0 = data.data[3];
            byte preval1 = data.data[4];
            byte preval2 = data.data[5];

            // Write chambered round class
            if(asBAR.Chamber.GetRound() == null || asBAR.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 4);
            }
            else
            {
                BitConverter.GetBytes((short)asBAR.Chamber.GetRound().RoundType).CopyTo(data.data, 2);
                BitConverter.GetBytes((short)asBAR.Chamber.GetRound().RoundClass).CopyTo(data.data, 4);
            }

            modified |= (preval != data.data[2] || preval0 != data.data[3] || preval1 != data.data[4] || preval2 != data.data[5]);

            preval = data.data[6];

            // Write bolt handle state
            data.data[6] = (byte)asBAR.CurBoltHandleState;

            modified |= preval != data.data[6];

            preval = data.data[7];

            // Write bolt handle rot
            data.data[7] = (byte)asBAR.BoltHandle.HandleRot;

            modified |= preval != data.data[7];

            return modified;
        }

        private bool UpdateGivenBoltActionRifle(byte[] newData)
        {
            bool modified = false;
            BoltActionRifle asBAR = dataObject as BoltActionRifle;

            if (data.data == null)
            {
                modified = true;

                // Set fire select mode
                Mod.BoltActionRifle_m_fireSelectorMode.SetValue(asBAR, (int)newData[0]);

                // Set bolt handle state
                asBAR.LastBoltHandleState = asBAR.CurBoltHandleState;
                asBAR.CurBoltHandleState = (BoltActionRifle_Handle.BoltActionHandleState)newData[6];

                // Set bolt handle rot
                asBAR.BoltHandle.LastHandleRot = asBAR.BoltHandle.HandleRot;
                asBAR.BoltHandle.HandleRot = (BoltActionRifle_Handle.BoltActionHandleRot)newData[7];
            }
            else 
            {
                if (data.data[0] != newData[0])
                {
                    // Set fire select mode
                    Mod.BoltActionRifle_m_fireSelectorMode.SetValue(asBAR, (int)newData[0]);
                    modified = true;
                }
                if (data.data[6] != newData[6])
                {
                    // Set bolt handle state
                    asBAR.LastBoltHandleState = asBAR.CurBoltHandleState;
                    asBAR.CurBoltHandleState = (BoltActionRifle_Handle.BoltActionHandleState)newData[6];
                }
                if (data.data[7] != newData[7])
                {
                    // Set bolt handle rot
                    asBAR.BoltHandle.LastHandleRot = asBAR.BoltHandle.HandleRot;
                    asBAR.BoltHandle.HandleRot = (BoltActionRifle_Handle.BoltActionHandleRot)newData[7];
                }
            }

            // Set hammer state
            if (newData[1] == 0)
            {
                if (asBAR.IsHammerCocked)
                {
                    Mod.BoltActionRifle_m_isHammerCocked.SetValue(asBAR, BitConverter.ToBoolean(newData, 1));
                    modified = true;
                }
            }
            else // Hammer should be cocked
            {
                if (!asBAR.IsHammerCocked)
                {
                    asBAR.CockHammer();
                    modified = true;
                }
            }
            
            // Set chamber
            short chamberTypeIndex = BitConverter.ToInt16(newData, 2);
            short chamberClassIndex = BitConverter.ToInt16(newData, 4);
            if(chamberClassIndex == -1) // We don't want round in chamber
            {
                if(asBAR.Chamber.GetRound() != null)
                {
                    ++ChamberPatch.chamberSkip;
                    asBAR.Chamber.SetRound(null, false);
                    --ChamberPatch.chamberSkip;
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                FireArmRoundClass roundClass = (FireArmRoundClass) chamberClassIndex;
                if (asBAR.Chamber.GetRound() == null || asBAR.Chamber.GetRound().RoundClass != roundClass)
                {
                    if (asBAR.Chamber.RoundType == roundType)
                    {
                        ++ChamberPatch.chamberSkip;
                        asBAR.Chamber.SetRound(roundClass, asBAR.Chamber.transform.position, asBAR.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                    }
                    else
                    {
                        FireArmRoundType prevRoundType = asBAR.Chamber.RoundType;
                        asBAR.Chamber.RoundType = roundType;
                        ++ChamberPatch.chamberSkip;
                        asBAR.Chamber.SetRound(roundClass, asBAR.Chamber.transform.position, asBAR.Chamber.transform.rotation);
                        --ChamberPatch.chamberSkip;
                        asBAR.Chamber.RoundType = prevRoundType;
                    }
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private void SetBARUpdateOverride(FireArmRoundType roundType, FireArmRoundClass roundClass, int chamberIndex)
        {
            BoltActionRifle asBar = dataObject as BoltActionRifle;

            FireArmRoundType prevRoundType = asBar.Chamber.RoundType;
            asBar.Chamber.RoundType = roundType;
            ++ChamberPatch.chamberSkip;
            asBar.Chamber.SetRound(roundClass, asBar.Chamber.transform.position, asBar.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
            asBar.Chamber.RoundType = prevRoundType;
        }

        private void ChamberBoltActionRifleRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            BoltActionRifle asBar = dataObject as BoltActionRifle;

            ++ChamberPatch.chamberSkip;
            asBar.Chamber.SetRound(roundClass, asBar.Chamber.transform.position, asBar.Chamber.transform.rotation);
            --ChamberPatch.chamberSkip;
        }

        private bool FireBAR(int chamberIndex)
        {
            return (dataObject as BoltActionRifle).Fire();
        }

        private bool UpdateSuppressor()
        {
            bool modified = false;
            Suppressor asAttachment = dataObject as Suppressor;

            if (data.data == null)
            {
                data.data = new byte[5];
                modified = true;
            }

            byte preIndex = data.data[0];

            // Write attached mount index
            if (asAttachment.curMount == null)
            {
                data.data[0] = 255;
            }
            else
            {
                // Find the mount and set it
                bool found = false;
                for (int i = 0; i < asAttachment.curMount.Parent.AttachmentMounts.Count; ++i)
                {
                    if (asAttachment.curMount.Parent.AttachmentMounts[i] == asAttachment.curMount)
                    {
                        data.data[0] = (byte)i;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    data.data[0] = 255;
                }
            }

            byte[] preVals = new byte[4];
            preVals[0] = data.data[1];
            preVals[1] = data.data[2];
            preVals[2] = data.data[3];
            preVals[3] = data.data[4];
            BitConverter.GetBytes(asAttachment.CatchRot).CopyTo(data.data, 1);
            modified |= (preVals[0] != data.data[1] || preVals[1] != data.data[2] || preVals[2] != data.data[3] || preVals[3] != data.data[4]);

            return modified || (preIndex != data.data[0]);
        }

        private bool UpdateGivenSuppressor(byte[] newData)
        {
            bool modified = false;
            Suppressor asAttachment = dataObject as Suppressor;

            if (data.data == null || data.data.Length != newData.Length)
            {
                data.data = new byte[5];
                data.data[0] = 255;
                currentMountIndex = 255;
                modified = true;
            }

            byte preMountIndex = currentMountIndex;
            if (newData[0] == 255)
            {
                // Should not be mounted, check if currently is
                if (asAttachment.curMount != null)
                {
                    ++data.ignoreParentChanged;
                    asAttachment.DetachFromMount();
                    --data.ignoreParentChanged;
                    currentMountIndex = 255;

                    // Detach from mount will recover rigidbody, set as kinematic if not controller
                    if (data.controller != H3MP_GameManager.ID)
                    {
                        Mod.SetKinematicRecursive(asAttachment.transform, true);
                    }
                }
            }
            else if(data.parent != -1)
            {
                // Find mount instance we want to be mounted to
                FVRFireArmAttachmentMount mount = null;
                H3MP_TrackedItemData parentTrackedItemData = null;
                if (H3MP_ThreadManager.host)
                {
                    parentTrackedItemData = H3MP_Server.items[data.parent];
                }
                else
                {
                    parentTrackedItemData = H3MP_Client.items[data.parent];
                }

                if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null)
                {
                    // We want to be mounted, we have a parent
                    if (parentTrackedItemData.physicalItem.physicalObject.AttachmentMounts.Count > newData[0])
                    {
                        mount = parentTrackedItemData.physicalItem.physicalObject.AttachmentMounts[newData[0]];
                    }
                }

                // Mount could be null if the mount index corresponds to a parent we have yet a receive a change to
                if (mount != null && mount != asAttachment.curMount)
                {
                    ++data.ignoreParentChanged;
                    if (asAttachment.curMount != null)
                    {
                        asAttachment.DetachFromMount();
                    }

                    if (mount.isMountableOn(asAttachment))
                    {
                        asAttachment.AttachToMount(mount, true);
                    }
                    currentMountIndex = newData[0];
                    --data.ignoreParentChanged;
                }
            }
            else // We have mount index but no parent index
            {
                // Detach from any mount we are still on
                if(asAttachment.curMount != null)
                {
                    ++data.ignoreParentChanged;
                    asAttachment.DetachFromMount();
                    --data.ignoreParentChanged;

                    // Detach from mount will recover rigidbody, set as kinematic if not controller
                    if (data.controller != H3MP_GameManager.ID)
                    {
                        Mod.SetKinematicRecursive(asAttachment.transform, true);
                    }
                }
            }

            float newRot = BitConverter.ToSingle(newData, 1);
            if(asAttachment.CatchRot != newRot)
            {
                asAttachment.CatchRot = newRot;
                asAttachment.transform.localEulerAngles = new Vector3(0f, 0f, newRot);
                modified = true;
            }

            data.data = newData;

            return modified || (preMountIndex != currentMountIndex);
        }

        private bool UpdateAttachment()
        {
            bool modified = false;
            FVRFireArmAttachment asAttachment = dataObject as FVRFireArmAttachment;

            if (data.data == null)
            {
                data.data = new byte[1 + attachmentInterfaceDataSize];
                modified = true;
            }

            byte preIndex = currentMountIndex;

            // Write attached mount index
            if (asAttachment.curMount == null)
            {
                data.data[0] = 255;
                currentMountIndex = 255;
            }
            else
            {
                // Find the mount and set it
                bool found = false;
                for(int i=0; i < asAttachment.curMount.MyObject.AttachmentMounts.Count; ++i)
                {
                    if (asAttachment.curMount.MyObject.AttachmentMounts[i] == asAttachment.curMount)
                    {
                        data.data[0] = (byte)i;
                        currentMountIndex = data.data[0];
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    data.data[0] = 255;
                    currentMountIndex = 255;
                }
            }

            // Do interface update
            if(attachmentInterfaceUpdateFunc != null)
            {
                attachmentInterfaceUpdateFunc(asAttachment, ref modified);
            }

            return modified || (preIndex != currentMountIndex);
        }

        private bool UpdateGivenAttachment(byte[] newData)
        {
            bool modified = false;
            FVRFireArmAttachment asAttachment = dataObject as FVRFireArmAttachment;

            if (data.data == null || data.data.Length != newData.Length)
            {
                data.data = new byte[1 + attachmentInterfaceDataSize];
                data.data[0] = 255;
                currentMountIndex = 255;
                modified = true;
            }

            byte preMountIndex = currentMountIndex;
            if (newData[0] == 255)
            {
                // Should not be mounted, check if currently is
                if (asAttachment.curMount != null)
                {
                    ++data.ignoreParentChanged;
                    asAttachment.DetachFromMount();
                    --data.ignoreParentChanged;
                    currentMountIndex = 255;

                    // Detach from mount will recover rigidbody, set as kinematic if not controller
                    if (data.controller != H3MP_GameManager.ID)
                    {
                        Mod.SetKinematicRecursive(asAttachment.transform, true);
                    }
                }
            }
            else if (data.parent != -1)
            {
                // Find mount instance we want to be mounted to
                FVRFireArmAttachmentMount mount = null;
                H3MP_TrackedItemData parentTrackedItemData = null;
                if (H3MP_ThreadManager.host)
                {
                    parentTrackedItemData = H3MP_Server.items[data.parent];
                }
                else
                {
                    parentTrackedItemData = H3MP_Client.items[data.parent];
                }

                if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null)
                {
                    // We want to be mounted, we have a parent
                    if (parentTrackedItemData.physicalItem.physicalObject.AttachmentMounts.Count > newData[0])
                    {
                        mount = parentTrackedItemData.physicalItem.physicalObject.AttachmentMounts[newData[0]];
                    }
                }

                // Mount could be null if the mount index corresponds to a parent we have yet to receive a change to
                if (mount != null && mount != asAttachment.curMount)
                {
                    if (asAttachment.curMount != null)
                    {
                        asAttachment.DetachFromMount();
                    }

                    Mod.LogInfo("Item " + name + " got update to attach to mount " + mount.name + ", mount parent null?: " + (mount.Parent == null));
                    if (mount.isMountableOn(asAttachment)) 
                    { 
                        asAttachment.AttachToMount(mount, true);
                    }
                    currentMountIndex = newData[0];
                }
            }
            else // We have mount index but no parent index
            {
                // Detach from any mount we are still on
                if (asAttachment.curMount != null)
                {
                    asAttachment.DetachFromMount();

                    // Detach from mount will recover rigidbody, set as kinematic if not controller
                    if (data.controller != H3MP_GameManager.ID)
                    {
                        Mod.SetKinematicRecursive(asAttachment.transform, true);
                    }
                }
            }

            // Do interface update
            if (attachmentInterfaceUpdateGivenFunc != null)
            {
                attachmentInterfaceUpdateGivenFunc(asAttachment, newData, ref modified);
            }

            data.data = newData;

            return modified || (preMountIndex != currentMountIndex);
        }

        private void UpdateAttachmentParent()
        {
            FVRFireArmAttachment asAttachment = dataObject as FVRFireArmAttachment;

            if(currentMountIndex != 255) // We want to be attached to a mount
            {
                if (data.parent != -1) // We have parent
                {
                    // We could be on wrong mount (or none physically) if we got a new mount through update but the parent hadn't been updated yet

                    // Get the mount we are supposed to be mounted to
                    FVRFireArmAttachmentMount mount = null;
                    H3MP_TrackedItemData parentTrackedItemData = null;
                    if (H3MP_ThreadManager.host)
                    {
                        parentTrackedItemData = H3MP_Server.items[data.parent];
                    }
                    else
                    {
                        parentTrackedItemData = H3MP_Client.items[data.parent];
                    }

                    if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null)
                    {
                        mount = parentTrackedItemData.physicalItem.physicalObject.AttachmentMounts[currentMountIndex];
                    }

                    // If not yet physically mounted to anything, can right away mount to the proper mount
                    if (asAttachment.curMount == null)
                    {
                        if (mount.isMountableOn(asAttachment))
                        { 
                            ++data.ignoreParentChanged;
                            asAttachment.AttachToMount(mount, true);
                            --data.ignoreParentChanged;
                        }
                    }
                    else if(asAttachment.curMount != mount) // Already mounted, but not on the right one, need to unmount, then mount of right one
                    {
                        ++data.ignoreParentChanged;
                        if (asAttachment.curMount != null)
                        {
                            asAttachment.DetachFromMount();
                        }

                        if (mount.isMountableOn(asAttachment))
                        {
                            asAttachment.AttachToMount(mount, true);
                        }
                        --data.ignoreParentChanged;
                    }
                }
                // else, if this happens it is because we received a parent update to null and just haven't gotten the up to date mount index of -1 yet
                //       This will be handled on update
            }
            // else, on update we will detach from any current mount if this is the case, no need to handle this here
        }

        private void UpdateAttachableBipod(FVRFireArmAttachment att, ref bool modified)
        {
            AttachableBipodInterface asInterface = att.AttachmentInterface as AttachableBipodInterface;

            // Write expanded
            byte preval = data.data[1];
            data.data[1] = (bool)Mod.FVRFireArmBipod_m_isBipodExpanded.GetValue(asInterface.Bipod) ? (byte)1 : (byte)0;
            modified |= preval != data.data[1];
        }

        private void UpdateGivenAttachableBipod(FVRFireArmAttachment att, byte[] newData, ref bool modified)
        {
            AttachableBipodInterface asInterface = att.AttachmentInterface as AttachableBipodInterface;

            // Set expanded
            bool expanded = (bool)Mod.FVRFireArmBipod_m_isBipodExpanded.GetValue(asInterface.Bipod);
            if ((newData[1] == 1 && !expanded) || (newData[1] == 0 && expanded))
            {
                asInterface.Bipod.Toggle();
                modified = true;
            }
        }

        private void UpdateFlagPoseSwitcher(FVRFireArmAttachment att, ref bool modified)
        {
            FlagPoseSwitcher asInterface = att.AttachmentInterface as FlagPoseSwitcher;

            // Write index
            byte preval0 = data.data[1];
            byte preval1 = data.data[2];
            BitConverter.GetBytes((short)(int)Mod.FlagPoseSwitcher_m_index.GetValue(asInterface)).CopyTo(data.data, 1);
            modified |= (preval0 != data.data[1] || preval1 != data.data[2]);
        }

        private void UpdateGivenFlagPoseSwitcher(FVRFireArmAttachment att, byte[] newData, ref bool modified)
        {
            FlagPoseSwitcher asInterface = att.AttachmentInterface as FlagPoseSwitcher;

            // Set index
            int index = (int)Mod.FlagPoseSwitcher_m_index.GetValue(asInterface);
            int newIndex = BitConverter.ToInt16(newData, 1);
            if (newIndex != index)
            {
                Mod.FlagPoseSwitcher_m_index.SetValue(asInterface, newIndex);
                asInterface.Flag.localPosition = asInterface.Poses[newIndex].localPosition;
                asInterface.Flag.localRotation = asInterface.Poses[newIndex].localRotation;
                modified = true;
            }
        }

        private void UpdateFlipSight(FVRFireArmAttachment att, ref bool modified)
        {
            FlipSight asInterface = att.AttachmentInterface as FlipSight;

            // Write up
            byte preval0 = data.data[1];
            data.data[1] = asInterface.IsUp ? (byte)1 : (byte)0;
            modified |= preval0 != data.data[1];
        }

        private void UpdateGivenFlipSight(FVRFireArmAttachment att, byte[] newData, ref bool modified)
        {
            FlipSight asInterface = att.AttachmentInterface as FlipSight;

            // Set up
            if ((newData[1] == 1 && !asInterface.IsUp)||(newData[1] == 0 && asInterface.IsUp))
            {
                Mod.FlipSight_Flip.Invoke(asInterface, null);
                modified = true;
            }
        }

        private void UpdateFlipSightY(FVRFireArmAttachment att, ref bool modified)
        {
            FlipSightY asInterface = att.AttachmentInterface as FlipSightY;

            // Write up
            byte preval0 = data.data[1];
            data.data[1] = asInterface.IsUp ? (byte)1 : (byte)0;
            modified |= preval0 != data.data[1];
        }

        private void UpdateGivenFlipSightY(FVRFireArmAttachment att, byte[] newData, ref bool modified)
        {
            FlipSightY asInterface = att.AttachmentInterface as FlipSightY;

            // Set up
            if ((newData[1] == 1 && !asInterface.IsUp)||(newData[1] == 0 && asInterface.IsUp))
            {
                Mod.FlipSightY_Flip.Invoke(asInterface, null);
                modified = true;
            }
        }

        private void UpdateLAM(FVRFireArmAttachment att, ref bool modified)
        {
            LAM asInterface = att.AttachmentInterface as LAM;

            // Write state
            byte preval0 = data.data[1];
            data.data[1] = (byte)asInterface.LState;
            modified |= preval0 != data.data[1];
        }

        private void UpdateGivenLAM(FVRFireArmAttachment att, byte[] newData, ref bool modified)
        {
            LAM asInterface = att.AttachmentInterface as LAM;

            // Set state
            if ((LAM.LAMState)newData[1] != asInterface.LState)
            {
                asInterface.LState = (LAM.LAMState)newData[1];

                if (asInterface.LState == LAM.LAMState.Off)
                {
                    SM.PlayCoreSound(FVRPooledAudioType.GenericClose, asInterface.AudEvent_LAMOFF, base.transform.position);
                }
                else
                {
                    SM.PlayCoreSound(FVRPooledAudioType.GenericClose, asInterface.AudEvent_LAMON, base.transform.position);
                }
                if (asInterface.LState == LAM.LAMState.Laser || asInterface.LState == LAM.LAMState.LaserLight)
                {
                    asInterface.BeamHitPoint.SetActive(true);
                    asInterface.BeamEffect.SetActive(true);
                }
                else
                {
                    asInterface.BeamHitPoint.SetActive(false);
                    asInterface.BeamEffect.SetActive(false);
                }
                if (asInterface.LState == LAM.LAMState.Light || asInterface.LState == LAM.LAMState.LaserLight)
                {
                    asInterface.LightParts.SetActive(true);
                    if (GM.CurrentSceneSettings.IsSceneLowLight)
                    {
                        ((Light)Mod.AlloyAreaLight_get_Light.Invoke(asInterface.FlashlightLight, null)).intensity = 2f;
                    }
                    else
                    {
                        ((Light)Mod.AlloyAreaLight_get_Light.Invoke(asInterface.FlashlightLight, null)).intensity = 0.5f;
                    }
                }
                else
                {
                    asInterface.LightParts.SetActive(false);
                }

                modified = true;
            }
        }

        private void UpdateLaserPointer(FVRFireArmAttachment att, ref bool modified)
        {
            LaserPointer asInterface = att.AttachmentInterface as LaserPointer;

            // Write on
            byte preval0 = data.data[1];
            data.data[1] = asInterface.BeamHitPoint.activeSelf ? (byte)1 : (byte)0;
            modified |= preval0 != data.data[1];
        }

        private void UpdateGivenLaserPointer(FVRFireArmAttachment att, byte[] newData, ref bool modified)
        {
            LaserPointer asInterface = att.AttachmentInterface as LaserPointer;

            // Set up
            if ((newData[1] == 1 && !asInterface.BeamHitPoint.activeSelf) || (newData[1] == 0 && asInterface.BeamHitPoint.activeSelf))
            {
                Mod.LaserPointer_ToggleOn.Invoke(asInterface, null);
                modified = true;
            }
        }

        private void UpdateTacticalFlashlight(FVRFireArmAttachment att, ref bool modified)
        {
            TacticalFlashlight asInterface = att.AttachmentInterface as TacticalFlashlight;

            // Write on
            byte preval0 = data.data[1];
            data.data[1] = asInterface.LightParts.activeSelf ? (byte)1 : (byte)0;
            modified |= preval0 != data.data[1];
        }

        private void UpdateGivenTacticalFlashlight(FVRFireArmAttachment att, byte[] newData, ref bool modified)
        {
            TacticalFlashlight asInterface = att.AttachmentInterface as TacticalFlashlight;

            // Set up
            if ((newData[1] == 1 && !asInterface.LightParts.activeSelf) || (newData[1] == 0 && asInterface.LightParts.activeSelf))
            {
                Mod.TacticalFlashlight_ToggleOn.Invoke(asInterface, null);
                modified = true;
            }
        }

        private bool UpdateMagazine()
        {
            bool modified = false;
            FVRFireArmMagazine asMag = dataObject as FVRFireArmMagazine;

            int necessarySize = asMag.m_capacity * 2 + 10;

            if(data.data == null || data.data.Length < necessarySize)
            {
                data.data = new byte[necessarySize];
                modified = true;
            }

            byte preval0 = data.data[0];
            byte preval1 = data.data[1];

            // Write count of loaded rounds
            BitConverter.GetBytes((short)asMag.m_numRounds).CopyTo(data.data, 0);

            modified |= (preval0 != data.data[0] || preval1 != data.data[1]);

            // Write loaded round classes
            for (int i=0; i < asMag.m_numRounds; ++i)
            {
                preval0 = data.data[i * 2 + 2];
                preval1 = data.data[i * 2 + 3];

                BitConverter.GetBytes((short)asMag.LoadedRounds[i].LR_Class).CopyTo(data.data, i * 2 + 2);

                modified |= (preval0 != data.data[i * 2 + 2] || preval1 != data.data[i * 2 + 3]);
            }

            // Write loaded into firearm
            preval0 = data.data[necessarySize - 8];
            data.data[necessarySize - 8] = asMag.FireArm != null ? (byte)1 : (byte)0;
            modified |= preval0 != data.data[necessarySize - 8];

            // Write secondary slot index, TODO: Having to look through each secondary slot for equality every update is obviously not optimal
            // We might want to look into patching (Attachable)Firearm's LoadMagIntoSecondary and eject from secondary to keep track of this instead
            preval0 = data.data[necessarySize - 7];
            if (asMag.FireArm == null)
            {
                data.data[necessarySize - 7] = (byte)255;
            }
            else
            {
                bool found = false;
                for (int i = 0; i < asMag.FireArm.SecondaryMagazineSlots.Length; ++i)
                {
                    if (asMag.FireArm.SecondaryMagazineSlots[i].Magazine == asMag)
                    {
                        found = true;
                        data.data[necessarySize - 7] = (byte)i;
                        break;
                    }
                }
                if (!found)
                {
                    data.data[necessarySize - 7] = (byte)255;
                }
            }
            modified |= preval0 != data.data[necessarySize - 7];

            // Write loaded into AttachableFirearm
            preval0 = data.data[necessarySize - 6];
            data.data[necessarySize - 6] = asMag.AttachableFireArm != null ? (byte)1 : (byte)0;
            modified |= preval0 != data.data[necessarySize - 6];

            // Write secondary slot index, TODO: Having to look through each secondary slot for equality every update is obviously not optimal
            // We might want to look into patching (Attachable)Firearm's LoadMagIntoSecondary and eject from secondary to keep track of this instead
            preval0 = data.data[necessarySize - 5];
            if (asMag.AttachableFireArm == null)
            {
                data.data[necessarySize - 5] = (byte)255;
            }
            else
            {
                bool found = false;
                for (int i = 0; i < asMag.AttachableFireArm.SecondaryMagazineSlots.Length; ++i)
                {
                    if (asMag.AttachableFireArm.SecondaryMagazineSlots[i].Magazine == asMag)
                    {
                        data.data[necessarySize - 5] = (byte)i;
                        break;
                    }
                }
                if (!found)
                {
                    data.data[necessarySize - 5] = (byte)255;
                }
            }
            modified |= preval0 != data.data[necessarySize - 5];

            // Write fuel amount left
            preval0 = data.data[necessarySize - 4];
            preval1 = data.data[necessarySize - 3];
            byte preval2 = data.data[necessarySize - 2];
            byte preval3 = data.data[necessarySize - 1];
            BitConverter.GetBytes(asMag.FuelAmountLeft).CopyTo(data.data, necessarySize - 4);
            modified |= (preval0 != data.data[necessarySize - 4] || preval1 != data.data[necessarySize - 3] || preval2 != data.data[necessarySize - 2] || preval3 != data.data[necessarySize - 1]);

            return modified;
        }

        private bool UpdateGivenMagazine(byte[] newData)
        {
            bool modified = false;
            FVRFireArmMagazine asMag = dataObject as FVRFireArmMagazine;

            if (data.data == null || data.data.Length != newData.Length)
            {
                modified = true;
            }

            int preRoundCount = asMag.m_numRounds;
            asMag.m_numRounds = 0;
            short numRounds = BitConverter.ToInt16(newData, 0);

            // Load rounds
            for (int i = 0; i < numRounds; ++i)
            {
                int first = i * 2 + 2;
                FireArmRoundClass newClass = (FireArmRoundClass)BitConverter.ToInt16(newData, first);
                if(asMag.LoadedRounds.Length > i && asMag.LoadedRounds[i] != null && newClass == asMag.LoadedRounds[i].LR_Class)
                {
                    ++asMag.m_numRounds;
                }
                else
                {
                    ++MagazinePatch.addRoundSkip;
                    asMag.AddRound(newClass, false, false);
                    --MagazinePatch.addRoundSkip;
                    modified = true;
                }
            }

            modified |= preRoundCount != asMag.m_numRounds;

            if (modified)
            {
                asMag.UpdateBulletDisplay();
            }

            // Load into firearm if necessary
            if (newData[newData.Length - 8] == 1)
            {
                if (data.parent != -1)
                {
                    H3MP_TrackedItemData parentTrackedItemData = null;
                    if (H3MP_ThreadManager.host)
                    {
                        parentTrackedItemData = H3MP_Server.items[data.parent];
                    }
                    else
                    {
                        parentTrackedItemData = H3MP_Client.items[data.parent];
                    }

                    if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null && parentTrackedItemData.physicalItem.dataObject is FVRFireArm)
                    {
                        // We want to be loaded in a firearm, we have a parent, it is a firearm
                        if (asMag.FireArm != null)
                        {
                            if (asMag.FireArm != parentTrackedItemData.physicalItem.dataObject)
                            {
                                // Unload from current, load into new firearm
                                if (asMag.FireArm.Magazine == asMag)
                                {
                                    asMag.FireArm.EjectMag(true);
                                }
                                else
                                {
                                    for(int i=0; i < asMag.FireArm.SecondaryMagazineSlots.Length; ++i)
                                    {
                                        if (asMag.FireArm.SecondaryMagazineSlots[i].Magazine == asMag)
                                        {
                                            asMag.FireArm.EjectSecondaryMagFromSlot(i, true);
                                            break;
                                        }
                                    }
                                }
                                if (newData[newData.Length - 7] == 255)
                                {
                                    ++MagazinePatch.loadSkip;
                                    asMag.Load(parentTrackedItemData.physicalItem.dataObject as FVRFireArm);
                                    --MagazinePatch.loadSkip;
                                }
                                else
                                {
                                    ++MagazinePatch.loadSkip;
                                    asMag.LoadIntoSecondary(parentTrackedItemData.physicalItem.dataObject as FVRFireArm, newData[newData.Length - 7]);
                                    --MagazinePatch.loadSkip;
                                }
                                modified = true;
                            }
                        }
                        else if(asMag.AttachableFireArm != null)
                        {
                            // Unload from current, load into new firearm
                            if (asMag.AttachableFireArm.Magazine == asMag)
                            {
                                asMag.AttachableFireArm.EjectMag(true);
                            }
                            else
                            {
                                for (int i = 0; i < asMag.AttachableFireArm.SecondaryMagazineSlots.Length; ++i)
                                {
                                    if (asMag.AttachableFireArm.SecondaryMagazineSlots[i].Magazine == asMag)
                                    {
                                        // TODO: Future: When H3 adds support for secondary slots on attachable firearm uncomment the following:
                                        //asMag.AttachableFireArm.EjectSecondaryMagFromSlot(i, true);
                                        break;
                                    }
                                }
                            }
                            if (newData[newData.Length - 7] == 255)
                            {
                                ++MagazinePatch.loadSkip;
                                asMag.Load(parentTrackedItemData.physicalItem.dataObject as FVRFireArm);
                                --MagazinePatch.loadSkip;
                            }
                            else
                            {
                                ++MagazinePatch.loadSkip;
                                asMag.LoadIntoSecondary(parentTrackedItemData.physicalItem.dataObject as FVRFireArm, newData[newData.Length - 7]);
                                --MagazinePatch.loadSkip;
                            }
                            modified = true;
                        }
                        else
                        {
                            // Load into firearm
                            if (newData[newData.Length - 7] == 255)
                            {
                                ++MagazinePatch.loadSkip;
                                asMag.Load(parentTrackedItemData.physicalItem.dataObject as FVRFireArm);
                                --MagazinePatch.loadSkip;
                            }
                            else
                            {
                                ++MagazinePatch.loadSkip;
                                asMag.LoadIntoSecondary(parentTrackedItemData.physicalItem.dataObject as FVRFireArm, newData[newData.Length - 7]);
                                --MagazinePatch.loadSkip;
                            }
                            modified = true;
                        }
                    }
                }
            }
            else if (newData[newData.Length - 6] == 1)
            {
                if (data.parent != -1)
                {
                    H3MP_TrackedItemData parentTrackedItemData = null;
                    if (H3MP_ThreadManager.host)
                    {
                        parentTrackedItemData = H3MP_Server.items[data.parent];
                    }
                    else
                    {
                        parentTrackedItemData = H3MP_Client.items[data.parent];
                    }

                    if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null && parentTrackedItemData.physicalItem.dataObject is AttachableFirearmPhysicalObject)
                    {
                        // We want to be loaded in a AttachableFireArm, we have a parent, it is a AttachableFireArm
                        if (asMag.AttachableFireArm != null)
                        {
                            if (asMag.AttachableFireArm != (parentTrackedItemData.physicalItem.dataObject as AttachableFirearmPhysicalObject).FA)
                            {
                                // Unload from current, load into new AttachableFireArm
                                if (asMag.AttachableFireArm.Magazine == asMag)
                                {
                                    asMag.AttachableFireArm.EjectMag(true);
                                }
                                else
                                {
                                    for (int i = 0; i < asMag.AttachableFireArm.SecondaryMagazineSlots.Length; ++i)
                                    {
                                        if (asMag.AttachableFireArm.SecondaryMagazineSlots[i].Magazine == asMag)
                                        {
                                            // TODO: Future: When H3 adds support for secondary slots on attachable firearm uncomment the following:
                                            //asMag.AttachableFireArm.EjectSecondaryMagFromSlot(i, true);
                                            break;
                                        }
                                    }
                                }
                                if (newData[newData.Length - 5] == 255)
                                {
                                    ++MagazinePatch.loadSkip;
                                    asMag.Load((parentTrackedItemData.physicalItem.dataObject as AttachableFirearmPhysicalObject).FA);
                                    --MagazinePatch.loadSkip;
                                }
                                else
                                {
                                    // TODO: Future: When H3 adds support for secondary slots on attachable firearm uncomment the following:
                                    //asMag.LoadIntoSecondary((parentTrackedItemData.physicalItem.dataObject as AttachableFirearmPhysicalObject).FA, newData[newData.Length - 1]);
                                }
                                modified = true;
                            }
                        }
                        else if (asMag.FireArm != null)
                        {
                            // Unload from current firearm, load into new AttachableFireArm
                            if (asMag.FireArm.Magazine == asMag)
                            {
                                asMag.FireArm.EjectMag(true);
                            }
                            else
                            {
                                for (int i = 0; i < asMag.FireArm.SecondaryMagazineSlots.Length; ++i)
                                {
                                    if (asMag.FireArm.SecondaryMagazineSlots[i].Magazine == asMag)
                                    {
                                        asMag.FireArm.EjectSecondaryMagFromSlot(i, true);
                                        break;
                                    }
                                }
                            }
                            if (newData[newData.Length - 5] == 255)
                            {
                                ++MagazinePatch.loadSkip;
                                asMag.Load((parentTrackedItemData.physicalItem.dataObject as AttachableFirearmPhysicalObject).FA);
                                --MagazinePatch.loadSkip;
                            }
                            else
                            {
                                // TODO: Future: When H3 adds support for secondary slots on attachable firearm uncomment the following:
                                //asMag.LoadIntoSecondary((parentTrackedItemData.physicalItem.dataObject as AttachableFirearmPhysicalObject).FA, newData[newData.Length - 1]);
                            }
                            modified = true;
                        }
                        else
                        {
                            // Load into AttachableFireArm
                            if (newData[newData.Length - 5] == 255)
                            {
                                ++MagazinePatch.loadSkip;
                                asMag.Load((parentTrackedItemData.physicalItem.dataObject as AttachableFirearmPhysicalObject).FA);
                                --MagazinePatch.loadSkip;
                            }
                            else
                            {
                                // TODO: Future: When H3 adds support for secondary slots on attachable firearm uncomment the following:
                                //asMag.LoadIntoSecondary((parentTrackedItemData.physicalItem.dataObject as AttachableFirearmPhysicalObject).FA, newData[newData.Length - 1]);
                            }
                            modified = true;
                        }
                    }
                }
            }
            else
            {
                if (asMag.FireArm != null)
                {
                    // Don't want to be loaded, but we are loaded, unload
                    if (asMag.FireArm.Magazine == asMag)
                    {
                        asMag.FireArm.EjectMag(true);
                    }
                    else
                    {
                        for (int i = 0; i < asMag.FireArm.SecondaryMagazineSlots.Length; ++i)
                        {
                            if (asMag.FireArm.SecondaryMagazineSlots[i].Magazine == asMag)
                            {
                                asMag.FireArm.EjectSecondaryMagFromSlot(i, true);
                                break;
                            }
                        }
                    }
                    modified = true;
                }
                else if(asMag.AttachableFireArm != null)
                {
                    if (asMag.AttachableFireArm.Magazine == asMag)
                    {
                        asMag.AttachableFireArm.EjectMag(true);
                    }
                    else
                    {
                        for (int i = 0; i < asMag.AttachableFireArm.SecondaryMagazineSlots.Length; ++i)
                        {
                            if (asMag.AttachableFireArm.SecondaryMagazineSlots[i].Magazine == asMag)
                            {
                                // TODO: Future: When H3 adds support for secondary slots on attachable firearm uncomment the following:
                                //asMag.AttachableFireArm.EjectSecondaryMagFromSlot(i, true);
                                break;
                            }
                        }
                    }
                    modified = true;
                }
            }

            float preAmount = asMag.FuelAmountLeft;

            asMag.FuelAmountLeft = BitConverter.ToSingle(newData, newData.Length - 4);

            modified |= preAmount != asMag.FuelAmountLeft;

            data.data = newData;

            return modified;
        }

        private bool UpdateClip()
        {
            bool modified = false;
            FVRFireArmClip asClip = dataObject as FVRFireArmClip;

            int necessarySize = asClip.m_capacity * 2 + 3;

            if (data.data == null || data.data.Length < necessarySize)
            {
                data.data = new byte[necessarySize];
                modified = true;
            }

            byte preval0 = data.data[0];
            byte preval1 = data.data[1];

            // Write count of loaded rounds
            BitConverter.GetBytes((short)asClip.m_numRounds).CopyTo(data.data, 0);

            modified |= (preval0 != data.data[0] || preval1 != data.data[1]);

            // Write loaded round classes
            for (int i = 0; i < asClip.m_numRounds; ++i)
            {
                preval0 = data.data[i * 2 + 2];
                preval1 = data.data[i * 2 + 3];

                BitConverter.GetBytes((short)asClip.LoadedRounds[i].LR_Class).CopyTo(data.data, i * 2 + 2);

                modified |= (preval0 != data.data[i * 2 + 2] || preval1 != data.data[i * 2 + 3]);
            }

            // Write loaded into firearm
            preval0 = data.data[necessarySize - 1];
            data.data[necessarySize - 1] = asClip.FireArm != null ? (byte)1 : (byte)0;
            modified |= preval0 != data.data[necessarySize - 1];

            return modified;
        }

        private bool UpdateGivenClip(byte[] newData)
        {
            bool modified = false;
            FVRFireArmClip asClip = dataObject as FVRFireArmClip;

            if (data.data == null || data.data.Length != newData.Length)
            {
                modified = true;
            }

            int preRoundCount = asClip.m_numRounds;
            asClip.m_numRounds = 0;
            short numRounds = BitConverter.ToInt16(newData, 0);

            // Load rounds
            for (int i = 0; i < numRounds; ++i)
            {
                int first = i * 2 + 2;
                FireArmRoundClass newClass = (FireArmRoundClass)BitConverter.ToInt16(newData, first);
                if (asClip.LoadedRounds.Length > i && asClip.LoadedRounds[i] != null && newClass == asClip.LoadedRounds[i].LR_Class)
                {
                    ++asClip.m_numRounds;
                }
                else
                {
                    ++ClipPatch.addRoundSkip;
                    asClip.AddRound(newClass, false, false);
                    --ClipPatch.addRoundSkip;
                    modified = true;
                }
            }

            modified |= preRoundCount != asClip.m_numRounds;

            if (modified)
            {
                asClip.UpdateBulletDisplay();
            }

            // Load into firearm if necessary
            if (BitConverter.ToBoolean(newData, newData.Length - 1))
            {
                if (data.parent != -1)
                {
                    H3MP_TrackedItemData parentTrackedItemData = null;
                    if (H3MP_ThreadManager.host)
                    {
                        parentTrackedItemData = H3MP_Server.items[data.parent];
                    }
                    else
                    {
                        parentTrackedItemData = H3MP_Client.items[data.parent];
                    }

                    if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null && parentTrackedItemData.physicalItem.dataObject is FVRFireArm)
                    {
                        // We want to be loaded in a firearm, we have a parent, it is a firearm
                        if (asClip.FireArm != null)
                        {
                            if (asClip.FireArm != parentTrackedItemData.physicalItem.dataObject)
                            {
                                // Unload from current, load into new firearm
                                asClip.FireArm.EjectClip();
                                ++ClipPatch.loadSkip;
                                asClip.Load(parentTrackedItemData.physicalItem.dataObject as FVRFireArm);
                                --ClipPatch.loadSkip;
                                modified = true;
                            }
                        }
                        else
                        {
                            // Load into firearm
                            ++ClipPatch.loadSkip;
                            asClip.Load(parentTrackedItemData.physicalItem.dataObject as FVRFireArm);
                            --ClipPatch.loadSkip;
                            modified = true;
                        }
                    }
                }
            }
            else if (asClip.FireArm != null)
            {
                // Don't want to be loaded, but we are loaded, unload
                asClip.FireArm.EjectClip();
                modified = true;
            }

            data.data = newData;

            return modified;
        }

        private bool UpdateSpeedloader()
        {
            bool modified = false;
            Speedloader asSpeedloader = dataObject as Speedloader;

            int necessarySize = asSpeedloader.Chambers.Count * 2;

            if (data.data == null || data.data.Length < necessarySize)
            {
                data.data = new byte[necessarySize];
                modified = true;
            }

            byte preval0;
            byte preval1;

            // Write loaded round classes (-1 for none)
            for (int i = 0; i < asSpeedloader.Chambers.Count; ++i)
            {
                preval0 = data.data[i * 2];
                preval1 = data.data[i * 2 + 1];

                if (asSpeedloader.Chambers[i].IsLoaded && !asSpeedloader.Chambers[i].IsSpent)
                {
                    BitConverter.GetBytes((short)asSpeedloader.Chambers[i].LoadedClass).CopyTo(data.data, i * 2);
                }
                else
                {
                    BitConverter.GetBytes((short)-1).CopyTo(data.data, i * 2);
                }

                modified |= (preval0 != data.data[i * 2] || preval1 != data.data[i * 2 + 1]);
            }

            return modified;
        }

        private bool UpdateGivenSpeedloader(byte[] newData)
        {
            bool modified = false;
            Speedloader asSpeedloader = dataObject as Speedloader;

            if (data.data == null || data.data.Length != newData.Length)
            {
                modified = true;
            }

            // Load rounds
            for (int i = 0; i < asSpeedloader.Chambers.Count; ++i)
            {
                int first = i * 2;
                short classIndex = BitConverter.ToInt16(newData, first);
                if (classIndex != -1 && (!asSpeedloader.Chambers[i].IsLoaded || (short)asSpeedloader.Chambers[i].LoadedClass != classIndex))
                {
                    FireArmRoundClass newClass = (FireArmRoundClass)classIndex;
                    ++SpeedloaderChamberPatch.loadSkip;
                    asSpeedloader.Chambers[i].Load(newClass, false);
                    --SpeedloaderChamberPatch.loadSkip;
                }
                else if(classIndex == -1 && asSpeedloader.Chambers[i].IsLoaded)
                {
                    asSpeedloader.Chambers[i].Unload();
                }
            }

            data.data = newData;

            return modified;
        }
        #endregion

        private void FixedUpdate()
        {
            if (interpolated && physicalObject != null && data.controller != H3MP_GameManager.ID && data.position != null && data.rotation != null)
            {
                if (data.previousPos != null && data.velocity.magnitude < 1f)
                {
                    if (data.parent == -1)
                    {
                        physicalObject.transform.position = Vector3.Lerp(physicalObject.transform.position, data.position + data.velocity, interpolationSpeed * Time.deltaTime);
                    }
                    else
                    {
                        physicalObject.transform.localPosition = Vector3.Lerp(physicalObject.transform.localPosition, data.position + data.velocity, interpolationSpeed * Time.deltaTime);
                    }
                }
                else
                {
                    if (data.parent == -1)
                    {
                        physicalObject.transform.position = data.position;
                    }
                    else
                    {
                        physicalObject.transform.localPosition = data.position;
                    }
                }
                if (data.parent == -1)
                {
                    physicalObject.transform.rotation = Quaternion.Lerp(physicalObject.transform.rotation, data.rotation, interpolationSpeed * Time.deltaTime);
                }
                else
                {
                    physicalObject.transform.localRotation = Quaternion.Lerp(physicalObject.transform.localRotation, data.rotation, interpolationSpeed * Time.deltaTime);
                }
            }
        }

        private void OnDestroy()
        {
            // A skip of the entire destruction process may be used if H3MP has become irrelevant, like in the case of disconnection
            if (skipFullDestroy)
            {
                return;
            }

            // Remove from tracked lists, which has to be done no matter what OnDestroy because we will not have the phyiscalObject anymore
            H3MP_GameManager.trackedItemByItem.Remove(physicalObject);
            if (physicalObject is SosigWeaponPlayerInterface)
            {
                H3MP_GameManager.trackedItemBySosigWeapon.Remove((physicalObject as SosigWeaponPlayerInterface).W);
            }

            // Ensure uncontrolled, which has to be done no matter what OnDestroy because we will not have the phyiscalObject anymore
            H3MP_GameManager.EnsureUncontrolled(physicalObject);

            // Have a flag in case we don't actually want to remove it from local after processing
            // In case we can't detroy gobally because we are still waiting for a tracked ID for example
            bool removeFromLocal = true;

            // Check if we want to process sending, giving control, etc.
            // We might want to skip just this part if the object was refused by the server
            if (!skipDestroyProcessing)
            {
                // Check if we want to give control of any destroyed objects
                // This would be the case while we change scene, objects will be destroyed but if there are other clients
                // in our previous scene/instance, we don't want to destroy the object globally, we want to give control of it to one of them
                // We might receive an order to destroy an object while we have giveControlOfDestroyed > 0, if so dontGiveControl flag 
                // explicitly says to destroy
                if (H3MP_GameManager.giveControlOfDestroyed == 0 || dontGiveControl)
                {
                    DestroyGlobally(ref removeFromLocal);
                }
                else // We want to give control of this object instead of destroying it globally
                {
                    if (data.controller == H3MP_GameManager.ID)
                    {
                        // Find best potential host
                        int otherPlayer = Mod.GetBestPotentialObjectHost(data.controller, true, true, H3MP_GameManager.playersAtLoadStart);
                        if (otherPlayer == -1)
                        {
                            // No other best potential host, destroy globally
                            DestroyGlobally(ref removeFromLocal);
                        }
                        else // We have a potential new host to give control to
                        {
                            // Check if can give control
                            if (data.trackedID > -1)
                            {
                                // Give control with us as debounce because we know we are no longer eligible to control this object
                                if (H3MP_ThreadManager.host)
                                {
                                    H3MP_ServerSend.GiveControl(data.trackedID, otherPlayer, new List<int>() { H3MP_GameManager.ID });
                                }
                                else
                                {
                                    H3MP_ClientSend.GiveControl(data.trackedID, otherPlayer, new List<int>() { H3MP_GameManager.ID });
                                }

                                // Also change controller locally
                                data.SetController(otherPlayer);
                            }
                            else // trackedID == -1, note that it cannot == -2 because DestroyGlobally will never get called in that case due to skipDestroyProcessing flag
                            {
                                // Tell destruction we want to keep this in local for later
                                removeFromLocal = false;

                                // Keep the control change in unknown so we can send it to others if we get a tracked ID
                                if (unknownControlTrackedIDs.TryGetValue(data.localWaitingIndex, out int val))
                                {
                                    if(val != otherPlayer)
                                    {
                                        unknownControlTrackedIDs[data.localWaitingIndex] = otherPlayer;
                                    }
                                }
                                else
                                {
                                    unknownControlTrackedIDs.Add(data.localWaitingIndex, otherPlayer);
                                }
                            }
                        }
                    }
                    // else, we don't control this object, it will simply be destroyed physically on our side
                }
            }

            // If we control this item, remove it from local lists
            // Which has to be done no matter what OnDestroy because we will not have a physicalObject to control after
            // We have either destroyed it or given control of it above
            if (data.localTrackedID != -1 && removeFromLocal)
            {
                data.RemoveFromLocal();
            }

            // Reset relevant flags
            data.removeFromListOnDestroy = true;
            sendDestroy = true;
            skipDestroyProcessing = false;
        }

        private void DestroyGlobally(ref bool removeFromLocal)
        {
            // Check if can destroy globally
            if (data.trackedID > -1)
            {
                // Check if want to send destruction
                // Used to prevent feedback loops
                if (sendDestroy)
                {
                    // Send destruction
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.DestroyItem(data.trackedID, data.removeFromListOnDestroy);
                    }
                    else
                    {
                        H3MP_ClientSend.DestroyItem(data.trackedID, data.removeFromListOnDestroy);
                    }
                }

                // Remove from globals lists if we want
                if (data.removeFromListOnDestroy)
                {
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_Server.items[data.trackedID] = null;
                        H3MP_Server.availableItemIndices.Add(data.trackedID);
                    }
                    else
                    {
                        H3MP_Client.items[data.trackedID] = null;
                    }

                    H3MP_GameManager.itemsByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
                }
            }
            else // trackedID == -1, note that it cannot == -2 because DestroyGlobally will never get called in that case due to skipDestroyProcessing flag
            {
                // Tell destruction we want to keep this in local for later
                removeFromLocal = false;

                // Keep the destruction in unknown so we can send it to others if we get a tracked ID
                if (!unknownDestroyTrackedIDs.Contains(data.localWaitingIndex))
                {
                    unknownDestroyTrackedIDs.Add(data.localWaitingIndex);
                }
            }
        }

        private void OnTransformParentChanged()
        {
            if (data.ignoreParentChanged > 0)
            {
                return;
            }

            if (data.controller == H3MP_GameManager.ID)
            {
                Transform currentParent = transform.parent;
                H3MP_TrackedItem parentTrackedItem = null;
                while (currentParent != null)
                {
                    parentTrackedItem = currentParent.GetComponent<H3MP_TrackedItem>();
                    if (parentTrackedItem != null)
                    {
                        break;
                    }
                    currentParent = currentParent.parent;
                }
                if (parentTrackedItem != null)
                {
                    // Handle case of unknown tracked IDs
                    //      If ours is not yet known, put our waiting index in a wait dict with value as parent's LOCAL tracked ID if it is under our control
                    //      and the actual tracked ID if not, when we receive the tracked ID we set the parent
                    //          Note that if the parent is under our control, we need to store the local tracked ID because we might not have its tracked ID yet either
                    //          If it is not under our control then we have guarantee that is has a tracked ID
                    //      If the parent's tracked ID is not yet known, put it in a wait dict where key is the local tracked ID of the parent,
                    //      and the value is a list of all children that must be attached to this parent once we know the parent's tracked ID
                    //          Note that if we do not know the parent's tracked ID, it is because it is under our control
                    bool haveParentID = parentTrackedItem.data.trackedID > -1;
                    if (data.trackedID == -1)
                    {
                        KeyValuePair<uint, bool> parentIDPair = new KeyValuePair<uint, bool>(haveParentID ? (uint)parentTrackedItem.data.trackedID : parentTrackedItem.data.localWaitingIndex, haveParentID);
                        if (unknownTrackedIDs.ContainsKey(data.localWaitingIndex))
                        {
                            unknownTrackedIDs[data.localWaitingIndex] = parentIDPair;
                        }
                        else
                        {
                            unknownTrackedIDs.Add(data.localWaitingIndex, parentIDPair);
                        }
                        if (!haveParentID)
                        {
                            if (unknownParentWaitList.TryGetValue(parentTrackedItem.data.localWaitingIndex, out List<uint> waitList))
                            {
                                waitList.Add(data.localWaitingIndex);
                            }
                            else
                            {
                                unknownParentWaitList.Add(parentTrackedItem.data.localWaitingIndex, new List<uint>() { data.localWaitingIndex });
                            }
                        }
                    }
                    else
                    {
                        if (haveParentID)
                        {
                            if (parentTrackedItem.data.trackedID != data.parent)
                            {
                                Mod.LogInfo(name + " has new parent: " + parentTrackedItem.data.itemID + ", sending");
                                // We have a parent trackedItem and it is new
                                // Update other clients
                                if (H3MP_ThreadManager.host)
                                {
                                    H3MP_ServerSend.ItemParent(data.trackedID, parentTrackedItem.data.trackedID);
                                }
                                else
                                {
                                    H3MP_ClientSend.ItemParent(data.trackedID, parentTrackedItem.data.trackedID);
                                }

                                // Update local
                                data.SetParent(parentTrackedItem.data, false);
                            }
                        }
                        else
                        {
                            if (unknownParentTrackedIDs.ContainsKey(parentTrackedItem.data.localWaitingIndex))
                            {
                                unknownParentTrackedIDs[parentTrackedItem.data.localWaitingIndex].Add(data.trackedID);
                            }
                            else
                            {
                                unknownParentTrackedIDs.Add(parentTrackedItem.data.localWaitingIndex, new List<int>() { data.trackedID });
                            }
                        }
                    }
                }
                else if (data.parent != -1)
                {
                    if (data.trackedID == -1)
                    {
                        if(unknownTrackedIDs.TryGetValue(data.localWaitingIndex, out KeyValuePair<uint, bool> entry))
                        {
                            if (!entry.Value && unknownParentWaitList.TryGetValue(entry.Key, out List<uint> waitlist))
                            {
                                waitlist.Remove(data.localWaitingIndex);
                            }
                        }
                        unknownTrackedIDs.Remove(data.localWaitingIndex);
                    }
                    else
                    {
                        Mod.LogInfo(name + " was unparented, sending to others");
                        // We were detached from current parent
                        // Update other clients
                        if (H3MP_ThreadManager.host)
                        {
                            H3MP_ServerSend.ItemParent(data.trackedID, -1);
                        }
                        else
                        {
                            H3MP_ClientSend.ItemParent(data.trackedID, -1);
                        }

                        // Update locally
                        data.SetParent(null, false);
                    }
                }
            }
        }
    }
}
