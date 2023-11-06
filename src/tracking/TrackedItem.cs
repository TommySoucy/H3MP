using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using H3MP.Scripts;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static RootMotion.FinalIK.IKSolver;

namespace H3MP.Tracking
{
    public class TrackedItem : TrackedObject
    {
        public TrackedItemData itemData;
        public FVRPhysicalObject physicalItem;

        public static float interpolationSpeed = 12f;
        public static bool interpolated = true;

        // Scene/instance change
        int securedCode = -1; // -1 means it was not secured

        // Unknown tracked ID queues
        public static Dictionary<uint, byte> unknownCrateHolding = new Dictionary<uint, byte>();
        public static Dictionary<SosigWeapon, KeyValuePair<TrackedSosigData, int>> unknownSosigInventoryObjects = new Dictionary<SosigWeapon, KeyValuePair<TrackedSosigData, int>>();
        public static Dictionary<uint, KeyValuePair<TrackedSosigData, int>> unknownSosigInventoryItems = new Dictionary<uint, KeyValuePair<TrackedSosigData, int>>();
        public static Dictionary<uint, List<KeyValuePair<Vector3, Vector3>>> unknownGasCuboidGout = new Dictionary<uint, List<KeyValuePair<Vector3, Vector3>>>();
        public static List<uint> unknownGasCuboidDamageHandle = new List<uint>();

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
        public delegate void RemoveTrackedDamageables();
        public delegate int FindSecondary(object secondary);
        public delegate object GetSecondary(int index);
        public delegate void ParentChanged();
        public delegate void OnDestruction();
        public delegate bool HandleShatterSpecific(UberShatterable shatterable, Vector3 point, Vector3 dir, float intensity, bool received, int clientID, byte[] data);
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
        public ChamberRound chamberRound; // Set round of chamber at given index of this item
        public RemoveTrackedDamageables removeTrackedDamageables;
        public FindSecondary findSecondary;
        public GetSecondary getSecondary;
        public ParentChanged parentChanged; // Update item's mount object depending on current state (See updateParentFunc above)
        public OnDestruction onDestruction;
        public HandleShatterSpecific handleShatterSpecific;
        public object[] secondaries;
        public byte currentMountIndex = 255; // Used by attachment, TODO: This limits number of mounts to 255, if necessary could make index into a short
        public int mountObjectID;
        public Vector3 mountObjectScale;
        public UnityEngine.Object dataObject;
        public int attachmentInterfaceDataSize;
        private bool positionSet;
        private bool rotationSet;

        // StingerLauncher specific
        public StingerMissile stingerMissile;

        // Integrated laser specific
        // Note: Implemented for specific types only. If a type of weapon can have an integrated LAPD2019Laser
        //       the state of the laser must be added to the update packet for that type. Currently only implemented for Handgun and Derringer
        public LAPD2019Laser integratedLaser;
        public bool usesAutoToggle;

        // Attachment specific
        public static Dictionary<int, List<TrackedItem>> toAttachByMountObjectID = new Dictionary<int, List<TrackedItem>>();

        /// <summary>
        /// CUSTOMIZATION
        /// Delegate for the OnInitItemType event
        /// </summary>
        /// <param name="trackedItem">The TrackedItem we must find the subtype of</param>
        /// <param name="physObj">The FVRPhysicalObject this tracked item is tracking</param>
        /// <param name="found">Custom override for whether the item type was found</param>
        public delegate void OnInitItemTypeDelegate(TrackedItem trackedItem, FVRPhysicalObject physObj, ref bool found);

        /// <summary>
        /// CUSTOMIZATION
        /// Event called when we initialize the type of a TrackedItem
        /// </summary>
        public static event OnInitItemTypeDelegate OnInitItemType;

        public override void Awake()
        {
            GameManager.OnPlayerBodyInit += OnPlayerBodyInit;

            InitItemType();

            base.Awake();

            GameManager.OnSceneLeft += OnSceneLeft;
            GameManager.OnSceneJoined += OnSceneJoined;
            GameManager.OnInstanceJoined += OnInstanceJoined;
        }

        private void InitItemType()
        {
            FVRPhysicalObject physObj = GetComponent<FVRPhysicalObject>();

            bool found = false;
            if (OnInitItemType != null)
            {
                OnInitItemType(this, physObj, ref found);
            }
            if (found)
            {
                return;
            }

            // For each relevant type for which we may want to store additional data, we set a specific update function and the object ref
            // NOTE: We want to handle a subtype before its parent type (ex.: sblpCell before FVRFireArmMagazine) 
            // TODO: Optimization: Maybe instead of having a big if statement like this, put all of them in a dictionnary for faster lookup
            if (physObj is sblpCell)
            {
                updateFunc = UpdateSBLPCell;
                updateGivenFunc = UpdateGivenSBLPCell;
                dataObject = physObj as sblpCell;
            }
            else if (physObj is MinigunBox)
            {
                updateFunc = UpdateMinigunBox;
                updateGivenFunc = UpdateGivenMinigunBox;
                dataObject = physObj as MinigunBox;
            }
            else if (physObj is FVRFireArmMagazine)
            {
                updateFunc = UpdateMagazine;
                updateGivenFunc = UpdateGivenMagazine;
                dataObject = physObj as FVRFireArmMagazine;
            }
            else if (physObj is FVRFireArmClip)
            {
                updateFunc = UpdateClip;
                updateGivenFunc = UpdateGivenClip;
                dataObject = physObj as FVRFireArmClip;
            }
            else if (physObj is Speedloader)
            {
                updateFunc = UpdateSpeedloader;
                updateGivenFunc = UpdateGivenSpeedloader;
                dataObject = physObj as Speedloader;
            }
            else if (physObj is FVRFireArm)
            {
                FVRFireArm asFA = physObj as FVRFireArm;

                // Process integrated attachable firearm if we have one
                if (asFA.GetIntegratedAttachableFirearm() != null)
                {
                    if (asFA.GetIntegratedAttachableFirearm() is AttachableBreakActions)
                    {
                        attachableFirearmFireFunc = (asFA.GetIntegratedAttachableFirearm() as AttachableBreakActions).Fire;
                        attachableFirearmChamberRoundFunc = AttachableBreakActionsChamberRound;
                        attachableFirearmGetChamberFunc = AttachableBreakActionsGetChamber;
                    }
                    else if (asFA.GetIntegratedAttachableFirearm() is AttachableClosedBoltWeapon)
                    {
                        attachableFirearmFireFunc = (asFA.GetIntegratedAttachableFirearm() as AttachableClosedBoltWeapon).Fire;
                        attachableFirearmChamberRoundFunc = AttachableClosedBoltWeaponChamberRound;
                        attachableFirearmGetChamberFunc = AttachableClosedBoltWeaponGetChamber;
                    }
                    else if (asFA.GetIntegratedAttachableFirearm() is AttachableTubeFed)
                    {
                        attachableFirearmFireFunc = (asFA.GetIntegratedAttachableFirearm() as AttachableTubeFed).Fire;
                        attachableFirearmChamberRoundFunc = AttachableTubeFedChamberRound;
                        attachableFirearmGetChamberFunc = AttachableTubeFedGetChamber;
                    }
                    else if (asFA.GetIntegratedAttachableFirearm() is GP25)
                    {
                        attachableFirearmFireFunc = (asFA.GetIntegratedAttachableFirearm() as GP25).Fire;
                        attachableFirearmChamberRoundFunc = GP25ChamberRound;
                        attachableFirearmGetChamberFunc = GP25GetChamber;
                    }
                    else if (asFA.GetIntegratedAttachableFirearm() is M203)
                    {
                        attachableFirearmFireFunc = (asFA.GetIntegratedAttachableFirearm() as M203).Fire;
                        attachableFirearmChamberRoundFunc = M203ChamberRound;
                        attachableFirearmGetChamberFunc = M203GetChamber;
                    }
                }

                // Process integrated LAPD2019Laser if we have one
                LAPD2019Laser integratedLaser = asFA.GetComponentInChildren<LAPD2019Laser>();
                if (integratedLaser != null)
                {
                    this.integratedLaser = integratedLaser;
                    usesAutoToggle = integratedLaser.UsesAutoOnOff;
                }

                // Process the type
                if (asFA is ClosedBoltWeapon)
                {
                    ClosedBoltWeapon asCBW = (ClosedBoltWeapon)physObj;
                    updateFunc = UpdateClosedBoltWeapon;
                    updateGivenFunc = UpdateGivenClosedBoltWeapon;
                    dataObject = asCBW;
                    fireFunc = FireCBW;
                    setFirearmUpdateOverride = SetCBWUpdateOverride;
                    getChamberIndex = GetClosedBoltWeaponChamberIndex;
                    chamberRound = ChamberClosedBoltWeaponRound;
                }
                else if (asFA is OpenBoltReceiver)
                {
                    OpenBoltReceiver asOBR = (OpenBoltReceiver)physObj;
                    updateFunc = UpdateOpenBoltReceiver;
                    updateGivenFunc = UpdateGivenOpenBoltReceiver;
                    dataObject = asOBR;
                    fireFunc = FireOBR;
                    setFirearmUpdateOverride = SetOBRUpdateOverride;
                    getChamberIndex = GetOpenBoltReceiverChamberIndex;
                    chamberRound = ChamberOpenBoltReceiverRound;
                }
                else if (asFA is BoltActionRifle)
                {
                    BoltActionRifle asBAR = (BoltActionRifle)physObj;
                    updateFunc = UpdateBoltActionRifle;
                    updateGivenFunc = UpdateGivenBoltActionRifle;
                    dataObject = asBAR;
                    fireFunc = FireBAR;
                    setFirearmUpdateOverride = SetBARUpdateOverride;
                    getChamberIndex = GetBoltActionRifleChamberIndex;
                    chamberRound = ChamberBoltActionRifleRound;
                }
                else if (asFA is Handgun)
                {
                    Handgun asHandgun = (Handgun)physObj;
                    updateFunc = UpdateHandgun;
                    updateGivenFunc = UpdateGivenHandgun;
                    dataObject = asHandgun;
                    fireFunc = FireHandgun;
                    setFirearmUpdateOverride = SetHandgunUpdateOverride;
                    getChamberIndex = GetHandgunChamberIndex;
                    chamberRound = ChamberHandgunRound;
                }
                else if (asFA is TubeFedShotgun)
                {
                    TubeFedShotgun asTFS = (TubeFedShotgun)physObj;
                    updateFunc = UpdateTubeFedShotgun;
                    updateGivenFunc = UpdateGivenTubeFedShotgun;
                    dataObject = asTFS;
                    fireFunc = FireTFS;
                    setFirearmUpdateOverride = SetTFSUpdateOverride;
                    getChamberIndex = GetTubeFedShotgunChamberIndex;
                    chamberRound = ChamberTubeFedShotgunRound;
                }
                else if (asFA is Revolver)
                {
                    Revolver asRevolver = (Revolver)physObj;
                    updateFunc = UpdateRevolver;
                    updateGivenFunc = UpdateGivenRevolver;
                    dataObject = asRevolver;
                    getChamberIndex = GetRevolverChamberIndex;
                    chamberRound = ChamberRevolverRound;
                }
                else if (asFA is SingleActionRevolver)
                {
                    SingleActionRevolver asSAR = (SingleActionRevolver)physObj;
                    updateFunc = UpdateSingleActionRevolver;
                    updateGivenFunc = UpdateGivenSingleActionRevolver;
                    dataObject = asSAR;
                    getChamberIndex = GetSingleActionRevolverChamberIndex;
                    chamberRound = ChamberSingleActionRevolverRound;
                }
                else if (asFA is RevolvingShotgun)
                {
                    RevolvingShotgun asRS = (RevolvingShotgun)physObj;
                    updateFunc = UpdateRevolvingShotgun;
                    updateGivenFunc = UpdateGivenRevolvingShotgun;
                    dataObject = asRS;
                    getChamberIndex = GetRevolvingShotgunChamberIndex;
                    chamberRound = ChamberRevolvingShotgunRound;
                }
                else if (asFA is BAP)
                {
                    BAP asBAP = (BAP)physObj;
                    updateFunc = UpdateBAP;
                    updateGivenFunc = UpdateGivenBAP;
                    dataObject = asBAP;
                    fireFunc = FireBAP;
                    setFirearmUpdateOverride = SetBAPUpdateOverride;
                    getChamberIndex = GetBAPChamberIndex;
                    chamberRound = ChamberBAPRound;
                }
                else if (asFA is BreakActionWeapon)
                {
                    updateFunc = UpdateBreakActionWeapon;
                    updateGivenFunc = UpdateGivenBreakActionWeapon;
                    dataObject = physObj as BreakActionWeapon;
                    getChamberIndex = GetBreakActionWeaponChamberIndex;
                    chamberRound = ChamberBreakActionWeaponRound;
                }
                else if (asFA is LeverActionFirearm)
                {
                    LeverActionFirearm LAF = (LeverActionFirearm)physObj;
                    updateFunc = UpdateLeverActionFirearm;
                    updateGivenFunc = UpdateGivenLeverActionFirearm;
                    dataObject = LAF;
                    getChamberIndex = GetLeverActionFirearmChamberIndex;
                    chamberRound = ChamberLeverActionFirearmRound;
                }
                else if (asFA is CarlGustaf)
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
                else if (asFA is Derringer)
                {
                    updateFunc = UpdateDerringer;
                    updateGivenFunc = UpdateGivenDerringer;
                    dataObject = physObj as Derringer;
                    getChamberIndex = GetDerringerChamberIndex;
                }
                else if (asFA is FlameThrower)
                {
                    updateFunc = UpdateFlameThrower;
                    updateGivenFunc = UpdateGivenFlameThrower;
                    dataObject = physObj as FlameThrower;

                    GameObject trackedItemRef = new GameObject();
                    trackedItemRef.transform.parent = transform;
                    trackedItemRef.SetActive(false);
                    if (availableTrackedRefIndices.Count == 0)
                    {
                        GameObject[] tempRefs = trackedReferenceObjects;
                        trackedReferenceObjects = new GameObject[tempRefs.Length + 100];
                        for (int i = 0; i < tempRefs.Length; ++i)
                        {
                            trackedReferenceObjects[i] = tempRefs[i];
                        }
                        TrackedObject[] tempItems = trackedReferences;
                        trackedReferences = new TrackedObject[tempItems.Length + 100];
                        for (int i = 0; i < tempItems.Length; ++i)
                        {
                            trackedReferences[i] = tempItems[i];
                        }
                        for (int i = tempItems.Length; i < trackedReferences.Length; ++i)
                        {
                            availableTrackedRefIndices.Add(i);
                        }
                    }
                    int refIndex = availableTrackedRefIndices[availableTrackedRefIndices.Count - 1];
                    availableTrackedRefIndices.RemoveAt(availableTrackedRefIndices.Count - 1);
                    trackedReferenceObjects[refIndex] = trackedItemRef;
                    trackedReferences[refIndex] = this;
                    trackedItemRef.name = refIndex.ToString();
                    asFA.BeltBoxMountPos = trackedItemRef.transform;
                }
                else if (asFA is sblp)
                {
                    updateFunc = UpdateLaserGun;
                    updateGivenFunc = UpdateGivenLaserGun;
                    dataObject = physObj as sblp;
                }
                else if (asFA is Flaregun)
                {
                    Flaregun asFG = physObj as Flaregun;
                    updateFunc = UpdateFlaregun;
                    updateGivenFunc = UpdateGivenFlaregun;
                    dataObject = asFG;
                    fireFunc = FireFlaregun;
                    setFirearmUpdateOverride = SetFlaregunUpdateOverride;
                    getChamberIndex = GetFirstChamberIndex;
                }
                else if (asFA is Airgun)
                {
                    Airgun asAG = (Airgun)physObj;
                    updateFunc = UpdateAirgun;
                    updateGivenFunc = UpdateGivenAirgun;
                    dataObject = asAG;
                    fireFunc = FireAirgun;
                    setFirearmUpdateOverride = SetAirgunUpdateOverride;
                    getChamberIndex = GetFirstChamberIndex;
                }
                else if (asFA is FlintlockWeapon)
                {
                    updateFunc = UpdateFlintlockWeapon;
                    updateGivenFunc = UpdateGivenFlintlockWeapon;
                    dataObject = physObj.GetComponentInChildren<FlintlockBarrel>();
                }
                else if (asFA is GBeamer)
                {
                    updateFunc = UpdateGBeamer;
                    updateGivenFunc = UpdateGivenGBeamer;
                    dataObject = physObj as GBeamer;
                }
                else if (asFA is GrappleGun)
                {
                    updateFunc = UpdateGrappleGun;
                    updateGivenFunc = UpdateGivenGrappleGun;
                    dataObject = physObj as GrappleGun;
                    getChamberIndex = GetGrappleGunChamberIndex;
                }
                else if (asFA is HCB)
                {
                    updateFunc = UpdateHCB;
                    updateGivenFunc = UpdateGivenHCB;
                    dataObject = physObj as HCB;
                    getChamberIndex = GetFirstChamberIndex;
                }
                else if (asFA is M72)
                {
                    M72 asM72 = physObj as M72;
                    updateFunc = UpdateM72;
                    updateGivenFunc = UpdateGivenM72;
                    dataObject = asM72;
                    fireFunc = FireM72;
                    setFirearmUpdateOverride = SetM72UpdateOverride;
                    getChamberIndex = GetFirstChamberIndex;
                }
                else if (asFA is Minigun)
                {
                    updateFunc = UpdateMinigun;
                    updateGivenFunc = UpdateGivenMinigun;
                    dataObject = physObj as Minigun;
                }
                else if (asFA is PotatoGun)
                {
                    PotatoGun asPG = physObj as PotatoGun;
                    updateFunc = UpdatePotatoGun;
                    updateGivenFunc = UpdateGivenPotatoGun;
                    dataObject = asPG;
                    fireFunc = FirePotatoGun;
                    setFirearmUpdateOverride = SetPotatoGunUpdateOverride;
                    getChamberIndex = GetFirstChamberIndex;
                }
                else if (asFA is RemoteMissileLauncher)
                {
                    RemoteMissileLauncher asRML = physObj as RemoteMissileLauncher;
                    updateFunc = UpdateRemoteMissileLauncher;
                    updateGivenFunc = UpdateGivenRemoteMissileLauncher;
                    dataObject = asRML;
                    fireFunc = FireRemoteMissileLauncher;
                    setFirearmUpdateOverride = SetRemoteMissileLauncherUpdateOverride;
                    getChamberIndex = GetFirstChamberIndex;
                }
                else if (asFA is StingerLauncher)
                {
                    updateFunc = UpdateStingerLauncher;
                    updateGivenFunc = UpdateGivenStingerLauncher;
                    dataObject = physObj as StingerLauncher;
                }
                else if (asFA is RGM40)
                {
                    RGM40 asRGM40 = physObj as RGM40;
                    updateFunc = UpdateRGM40;
                    updateGivenFunc = UpdateGivenRGM40;
                    dataObject = asRGM40;
                    fireFunc = FireRGM40;
                    setFirearmUpdateOverride = SetRGM40UpdateOverride;
                    getChamberIndex = GetFirstChamberIndex;
                }
                else if (asFA is RollingBlock)
                {
                    RollingBlock asRB = physObj as RollingBlock;
                    updateFunc = UpdateRollingBlock;
                    updateGivenFunc = UpdateGivenRollingBlock;
                    dataObject = asRB;
                    fireFunc = FireRollingBlock;
                    setFirearmUpdateOverride = SetRollingBlockUpdateOverride;
                    getChamberIndex = GetFirstChamberIndex;
                }
                else if (asFA is RPG7)
                {
                    RPG7 asRPG7 = physObj as RPG7;
                    updateFunc = UpdateRPG7;
                    updateGivenFunc = UpdateGivenRPG7;
                    dataObject = asRPG7;
                    fireFunc = FireRPG7;
                    setFirearmUpdateOverride = SetRPG7UpdateOverride;
                    getChamberIndex = GetFirstChamberIndex;
                }
                else if (asFA is SimpleLauncher)
                {
                    SimpleLauncher asSimpleLauncher = physObj as SimpleLauncher;
                    updateFunc = UpdateSimpleLauncher;
                    updateGivenFunc = UpdateGivenSimpleLauncher;
                    dataObject = asSimpleLauncher;
                    fireFunc = FireSimpleLauncher;
                    setFirearmUpdateOverride = SetSimpleLauncherUpdateOverride;
                    getChamberIndex = GetFirstChamberIndex;
                }
                else if (asFA is SimpleLauncher2)
                {
                    SimpleLauncher2 asSimpleLauncher = physObj as SimpleLauncher2;
                    updateFunc = UpdateSimpleLauncher2;
                    updateGivenFunc = UpdateGivenSimpleLauncher2;
                    dataObject = asSimpleLauncher;
                    fireFunc = FireSimpleLauncher2;
                    setFirearmUpdateOverride = SetSimpleLauncher2UpdateOverride;
                    getChamberIndex = GetFirstChamberIndex;
                }
                else if (asFA is MF2_RL)
                {
                    MF2_RL asMF2_RL = physObj as MF2_RL;
                    updateFunc = UpdateMF2_RL;
                    updateGivenFunc = UpdateGivenMF2_RL;
                    dataObject = asMF2_RL;
                    fireFunc = FireMF2_RL;
                    setFirearmUpdateOverride = SetMF2_RLUpdateOverride;
                    getChamberIndex = GetFirstChamberIndex;
                }
                else if (asFA is LAPD2019)
                {
                    updateFunc = UpdateLAPD2019;
                    updateGivenFunc = UpdateGivenLAPD2019;
                    dataObject = physObj as LAPD2019;
                    getChamberIndex = GetLAPD2019ChamberIndex;
                }
                else
                {
                    updateFunc = UpdateFireArm;
                    updateGivenFunc = UpdateGivenFireArm;
                    dataObject = asFA;
                    fireFunc = FireFireArm;
                    setFirearmUpdateOverride = SetFireArmUpdateOverride;
                    getChamberIndex = GetFireArmChamberIndex;
                    chamberRound = ChamberFireArmRound;
                }
            }
            else if (physObj is SteelPopTarget)
            {
                SteelPopTarget asSPT = (SteelPopTarget)physObj;
                updateFunc = UpdateSteelPopTarget;
                updateGivenFunc = UpdateGivenSteelPopTarget;
                dataObject = asSPT;
                findSecondary = FindSteelPopTargetSecondary;
                getSecondary = GetSteelPopTargetSecondary;

                ReactiveSteelTarget[] secondaries = physObj.GetComponentsInChildren<ReactiveSteelTarget>();
                this.secondaries = secondaries;
                for (int i = 0; i < secondaries.Length; ++i)
                {
                    GameManager.trackedObjectByDamageable.Add(secondaries[i], this);
                    removeTrackedDamageables = RemoveTrackedSteelPopTargetDamageables;
                }
            }
            else if (physObj is Molotov)
            {
                Molotov asMolotov = (Molotov)physObj;
                updateFunc = UpdateMolotov;
                updateGivenFunc = UpdateGivenMolotov;
                dataObject = asMolotov;

                GameManager.trackedObjectByDamageable.Add(asMolotov, this);
                removeTrackedDamageables = RemoveTrackedCommonDamageables;
            }
            else if (physObj is PinnedGrenade)
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
                TrackedObjectReference refScript = trackedItemRef.AddComponent<TrackedObjectReference>();
                trackedItemRef.SetActive(false);
                if (availableTrackedRefIndices.Count == 0)
                {
                    GameObject[] tempRefs = trackedReferenceObjects;
                    trackedReferenceObjects = new GameObject[tempRefs.Length + 100];
                    for (int i = 0; i < tempRefs.Length; ++i)
                    {
                        trackedReferenceObjects[i] = tempRefs[i];
                    }
                    TrackedObject[] tempItems = trackedReferences;
                    trackedReferences = new TrackedObject[tempItems.Length + 100];
                    for (int i = 0; i < tempItems.Length; ++i)
                    {
                        trackedReferences[i] = tempItems[i];
                    }
                    for (int i = tempItems.Length; i < trackedReferences.Length; ++i)
                    {
                        availableTrackedRefIndices.Add(i);
                    }
                }
                int refIndex = availableTrackedRefIndices[availableTrackedRefIndices.Count - 1];
                availableTrackedRefIndices.RemoveAt(availableTrackedRefIndices.Count - 1);
                trackedReferenceObjects[refIndex] = trackedItemRef;
                trackedReferences[refIndex] = this;
                trackedItemRef.name = refIndex.ToString();
                refScript.refIndex = refIndex;
                asPG.SpawnOnSplode.Add(trackedItemRef);
            }
            else if (physObj is FVRGrenade)
            {
                FVRGrenade asGrenade = (FVRGrenade)physObj;
                updateFunc = UpdateGrenade;
                updateGivenFunc = UpdateGivenGrenade;
                dataObject = asGrenade;
                if (asGrenade.FuseTimings == null)
                {
                    asGrenade.FuseTimings = new Dictionary<int, float>();
                }
                if (availableTrackedRefIndices.Count == 0)
                {
                    GameObject[] tempRefs = trackedReferenceObjects;
                    trackedReferenceObjects = new GameObject[tempRefs.Length + 100];
                    for (int i = 0; i < tempRefs.Length; ++i)
                    {
                        trackedReferenceObjects[i] = tempRefs[i];
                    }
                    TrackedObject[] tempItems = trackedReferences;
                    trackedReferences = new TrackedObject[tempItems.Length + 100];
                    for (int i = 0; i < tempItems.Length; ++i)
                    {
                        trackedReferences[i] = tempItems[i];
                    }
                    for (int i = tempItems.Length; i < trackedReferences.Length; ++i)
                    {
                        availableTrackedRefIndices.Add(i);
                    }
                }
                int refIndex = availableTrackedRefIndices[availableTrackedRefIndices.Count - 1];
                availableTrackedRefIndices.RemoveAt(availableTrackedRefIndices.Count - 1);
                asGrenade.FuseTimings.Add(-1, refIndex);
                trackedReferences[refIndex] = this;

                // Just make an object that will handle re-adding ref index to available list when it gets destroyed by scene change
                GameObject trackedItemRef = new GameObject("GrenadeRefIndex");
                Scripts.TrackedObjectReference refScript = trackedItemRef.AddComponent<Scripts.TrackedObjectReference>();
                refScript.refIndex = refIndex;
                trackedItemRef.SetActive(false);
            }
            else if (physObj is C4)
            {
                C4 asC4 = (C4)physObj;
                updateFunc = UpdateC4;
                updateGivenFunc = UpdateGivenC4;
                dataObject = asC4;
            }
            else if (physObj is ClaymoreMine)
            {
                ClaymoreMine asCM = (ClaymoreMine)physObj;
                updateFunc = UpdateClaymoreMine;
                updateGivenFunc = UpdateGivenClaymoreMine;
                dataObject = asCM;
            }
            else if (physObj is SLAM)
            {
                SLAM asSLAM = (SLAM)physObj;
                updateFunc = UpdateSLAM;
                updateGivenFunc = UpdateGivenSLAM;
                dataObject = asSLAM;

                GameManager.trackedObjectByDamageable.Add(asSLAM, this);
                removeTrackedDamageables = RemoveTrackedCommonDamageables;
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
                if (asAttachableFirearmPhysicalObject.FA is AttachableBreakActions)
                {
                    updateFunc = UpdateAttachableBreakActions;
                    updateGivenFunc = UpdateGivenAttachableBreakActions;
                    attachableFirearmFireFunc = (asAttachableFirearmPhysicalObject.FA as AttachableBreakActions).Fire;
                    attachableFirearmChamberRoundFunc = AttachableBreakActionsChamberRound;
                    attachableFirearmGetChamberFunc = AttachableBreakActionsGetChamber;
                    getChamberIndex = GetFirstChamberIndex;
                    chamberRound = ChamberAttachableBreakActionsRound;
                }
                else if (asAttachableFirearmPhysicalObject.FA is AttachableClosedBoltWeapon)
                {
                    updateFunc = UpdateAttachableClosedBoltWeapon;
                    updateGivenFunc = UpdateGivenAttachableClosedBoltWeapon;
                    attachableFirearmFireFunc = (asAttachableFirearmPhysicalObject.FA as AttachableClosedBoltWeapon).Fire;
                    attachableFirearmChamberRoundFunc = AttachableClosedBoltWeaponChamberRound;
                    attachableFirearmGetChamberFunc = AttachableClosedBoltWeaponGetChamber;
                    getChamberIndex = GetFirstChamberIndex;
                    chamberRound = ChamberAttachableClosedBoltWeaponRound;
                }
                else if (asAttachableFirearmPhysicalObject.FA is AttachableTubeFed)
                {
                    updateFunc = UpdateAttachableTubeFed;
                    updateGivenFunc = UpdateGivenAttachableTubeFed;
                    attachableFirearmFireFunc = (asAttachableFirearmPhysicalObject.FA as AttachableTubeFed).Fire;
                    attachableFirearmChamberRoundFunc = AttachableTubeFedChamberRound;
                    attachableFirearmGetChamberFunc = AttachableTubeFedGetChamber;
                    getChamberIndex = GetFirstChamberIndex;
                    chamberRound = ChamberAttachableTubeFedRound;
                }
                else if (asAttachableFirearmPhysicalObject.FA is GP25)
                {
                    updateFunc = UpdateGP25;
                    updateGivenFunc = UpdateGivenGP25;
                    attachableFirearmFireFunc = (asAttachableFirearmPhysicalObject.FA as GP25).Fire;
                    attachableFirearmChamberRoundFunc = GP25ChamberRound;
                    attachableFirearmGetChamberFunc = GP25GetChamber;
                    getChamberIndex = GetFirstChamberIndex;
                    chamberRound = ChamberGP25Round;
                }
                else if (asAttachableFirearmPhysicalObject.FA is M203)
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
                parentChanged = AttachmentParentChanged;
                dataObject = asAttachableFirearmPhysicalObject.FA;
            }
            else if (physObj is Suppressor)
            {
                Suppressor asAttachment = (Suppressor)physObj;
                updateFunc = UpdateSuppressor;
                updateGivenFunc = UpdateGivenSuppressor;
                updateParentFunc = UpdateAttachmentParent;
                parentChanged = AttachmentParentChanged;
                dataObject = asAttachment;
            }
            else if (physObj is FVRFireArmAttachment)
            {
                FVRFireArmAttachment asAttachment = (FVRFireArmAttachment)physObj;
                updateFunc = UpdateAttachment;
                updateGivenFunc = UpdateGivenAttachment;
                updateParentFunc = UpdateAttachmentParent;
                parentChanged = AttachmentParentChanged;
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
                    if (asAttachment.AttachmentInterface is AttachableBipodInterface)
                    {
                        attachmentInterfaceUpdateFunc = UpdateAttachableBipod;
                        attachmentInterfaceUpdateGivenFunc = UpdateGivenAttachableBipod;
                        attachmentInterfaceDataSize = 1;
                    }
                    else if (asAttachment.AttachmentInterface is FlagPoseSwitcher)
                    {
                        attachmentInterfaceUpdateFunc = UpdateFlagPoseSwitcher;
                        attachmentInterfaceUpdateGivenFunc = UpdateGivenFlagPoseSwitcher;
                        attachmentInterfaceDataSize = 2;
                    }
                    else if (asAttachment.AttachmentInterface is FlipSight)
                    {
                        attachmentInterfaceUpdateFunc = UpdateFlipSight;
                        attachmentInterfaceUpdateGivenFunc = UpdateGivenFlipSight;
                        attachmentInterfaceDataSize = 1;
                    }
                    else if (asAttachment.AttachmentInterface is FlipSightY)
                    {
                        attachmentInterfaceUpdateFunc = UpdateFlipSightY;
                        attachmentInterfaceUpdateGivenFunc = UpdateGivenFlipSightY;
                        attachmentInterfaceDataSize = 1;
                    }
                    else if (asAttachment.AttachmentInterface is LAM)
                    {
                        attachmentInterfaceUpdateFunc = UpdateLAM;
                        attachmentInterfaceUpdateGivenFunc = UpdateGivenLAM;
                        attachmentInterfaceDataSize = 1;
                    }
                    else if (asAttachment.AttachmentInterface is LaserPointer)
                    {
                        attachmentInterfaceUpdateFunc = UpdateLaserPointer;
                        attachmentInterfaceUpdateGivenFunc = UpdateGivenLaserPointer;
                        attachmentInterfaceDataSize = 1;
                    }
                    else if (asAttachment.AttachmentInterface is TacticalFlashlight)
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
                onDestruction = SosigWeaponDestruction;

                GameManager.trackedObjectByDamageable.Add(asInterface.W, this);
                removeTrackedDamageables = RemoveTrackedSosigWeaponDamageables;
            }
            else if (physObj is GrappleThrowable)
            {
                GrappleThrowable asGrappleThrowable = (GrappleThrowable)physObj;
                updateFunc = UpdateGrappleThrowable;
                updateGivenFunc = UpdateGivenGrappleThrowable;
                dataObject = asGrappleThrowable;
            }
            else // This is just a pure FVRPhysicalObject, we might want to track some other specific script on this item
            {
                if(TryGetComponent<UberShatterable>(physObj.gameObject, out UberShatterable uberShatterable))
                {
                    updateFunc = UpdateUberShatterable;
                    updateGivenFunc = UpdateGivenUberShatterable;
                    handleShatterSpecific = HandleShatterableShatter;
                    dataObject = uberShatterable;

                    GameManager.trackedObjectByDamageable.Add(uberShatterable, this);
                    removeTrackedDamageables = RemoveTrackedCommonDamageables;
                }
                else if(TryGetComponent<Brut_GasCuboid>(physObj.gameObject, out Brut_GasCuboid gasCuboid))
                {
                    updateFunc = UpdateGasCuboid;
                    updateGivenFunc = UpdateGivenGasCuboid;
                    dataObject = gasCuboid;

                    if (availableTrackedRefIndices.Count == 0)
                    {
                        GameObject[] tempRefs = trackedReferenceObjects;
                        trackedReferenceObjects = new GameObject[tempRefs.Length + 100];
                        for (int i = 0; i < tempRefs.Length; ++i)
                        {
                            trackedReferenceObjects[i] = tempRefs[i];
                        }
                        TrackedObject[] tempItems = trackedReferences;
                        trackedReferences = new TrackedObject[tempItems.Length + 100];
                        for (int i = 0; i < tempItems.Length; ++i)
                        {
                            trackedReferences[i] = tempItems[i];
                        }
                        for (int i = tempItems.Length; i < trackedReferences.Length; ++i)
                        {
                            availableTrackedRefIndices.Add(i);
                        }
                    }
                    int refIndex = availableTrackedRefIndices[availableTrackedRefIndices.Count - 1];
                    availableTrackedRefIndices.RemoveAt(availableTrackedRefIndices.Count - 1);
                    GameObject refObject = new GameObject(refIndex.ToString());
                    gasCuboid.SpawnOnSplodePoints.Add(refObject.transform);
                    trackedReferences[refIndex] = this;
                    trackedReferenceObjects[refIndex] = refObject;

                    GameManager.trackedObjectByDamageable.Add(gasCuboid, this);
                    GameManager.trackedObjectByDamageable.Add(gasCuboid.Handle.GetComponent<Brut_GasCuboidHandle>(), this);
                    removeTrackedDamageables = RemoveTrackedGasCuboidDamageables;
                }
            }
        }

        public static bool TryGetComponent<T>(GameObject obj, out T component)
        {
            component = obj.GetComponent<T>();
            return component != null;
        }

        public void RemoveTrackedCommonDamageables()
        {
            GameManager.trackedObjectByDamageable.Remove(dataObject as IFVRDamageable);
        }

        public void RemoveTrackedGasCuboidDamageables()
        {
            GameManager.trackedObjectByDamageable.Remove(dataObject as IFVRDamageable);
            GameManager.trackedObjectByDamageable.Remove((dataObject as Brut_GasCuboid).Handle.GetComponent<Brut_GasCuboidHandle>());
        }

        public void RemoveTrackedSosigWeaponDamageables()
        {
            GameManager.trackedObjectByDamageable.Remove(((SosigWeaponPlayerInterface)dataObject).W);
        }

        public void RemoveTrackedSteelPopTargetDamageables()
        {
            for (int i = 0; i < secondaries.Length; ++i)
            {
                if (secondaries[i] != null)
                {
                    GameManager.trackedObjectByDamageable.Remove(secondaries[i] as IFVRDamageable);
                }
            }
        }

        public bool UpdateItemData(byte[] newData = null)
        {
            if (dataObject != null)
            {
                if (newData != null)
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

            if (itemData.data == null || itemData.data.Length < necessarySize)
            {
                itemData.data = new byte[necessarySize];
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
                preval0 = itemData.data[firstIndex];
                preval1 = itemData.data[firstIndex + 1];
                preval2 = itemData.data[firstIndex + 2];
                preval3 = itemData.data[firstIndex + 3];

                if (asFA.GetChambers()[i].GetRound() == null || asFA.GetChambers()[i].IsSpent || asFA.GetChambers()[i].GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(itemData.data, firstIndex + 2);
                }
                else
                {
                    BitConverter.GetBytes((short)asFA.GetChambers()[i].GetRound().RoundType).CopyTo(itemData.data, firstIndex);
                    BitConverter.GetBytes((short)asFA.GetChambers()[i].GetRound().RoundClass).CopyTo(itemData.data, firstIndex + 2);
                }

                modified |= (preval0 != itemData.data[firstIndex] || preval1 != itemData.data[firstIndex + 1] || preval2 != itemData.data[firstIndex + 2] || preval3 != itemData.data[firstIndex + 3]);
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

                if (firstIndex >= asFA.GetChambers().Count)
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

            itemData.data = newData;

            return modified;
        }

        private bool FireFireArm(int chamberIndex)
        {
            if (chamberIndex == -1)
            {
                return false;
            }

            FVRFireArm asFA = dataObject as FVRFireArm;
            asFA.Fire(asFA.GetChambers()[chamberIndex], asFA.GetMuzzle(), false);
            return true;
        }

        private void SetFireArmUpdateOverride(FireArmRoundType roundType, FireArmRoundClass roundClass, int chamberIndex)
        {
            if (chamberIndex == -1)
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
            if (chamber == null)
            {
                return -1;
            }

            FVRFireArm asFA = dataObject as FVRFireArm;
            List<FVRFireArmChamber> chambers = asFA.GetChambers();
            for (int i = 0; i < chambers.Count; ++i)
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
            if (((int)roundClass) == -1)
            {
                asFA.GetChambers()[chamberIndex].SetRound(null);
            }
            else
            {
                asFA.GetChambers()[chamberIndex].SetRound(roundClass, asFA.GetChambers()[chamberIndex].transform.position, asFA.GetChambers()[chamberIndex].transform.rotation);
            }
            --ChamberPatch.chamberSkip;
        }

        private bool UpdateSBLPCell()
        {
            bool modified = false;
            sblpCell asCell = dataObject as sblpCell;

            if (itemData.data == null)
            {
                itemData.data = new byte[9];
                modified = true;
            }

            // Write loaded into firearm
            byte preval0 = itemData.data[0];
            itemData.data[0] = asCell.FireArm != null ? (byte)1 : (byte)0;
            modified |= preval0 != itemData.data[0];

            // Write secondary slot index, TODO: Having to look through each secondary slot for equality every update is obviously not optimal
            // We might want to look into patching (Attachable)Firearm's LoadMagIntoSecondary and eject from secondary to keep track of this instead
            preval0 = itemData.data[1];
            if (asCell.FireArm == null)
            {
                itemData.data[1] = (byte)255;
            }
            else
            {
                bool found = false;
                for (int i = 0; i < asCell.FireArm.SecondaryMagazineSlots.Length; ++i)
                {
                    if (asCell.FireArm.SecondaryMagazineSlots[i].Magazine == asCell)
                    {
                        found = true;
                        itemData.data[1] = (byte)i;
                        break;
                    }
                }
                if (!found)
                {
                    itemData.data[1] = (byte)255;
                }
            }
            modified |= preval0 != itemData.data[1];

            // Write loaded into AttachableFirearm
            preval0 = itemData.data[2];
            itemData.data[2] = asCell.AttachableFireArm != null ? (byte)1 : (byte)0;
            modified |= preval0 != itemData.data[2];

            // Write secondary slot index, TODO: Having to look through each secondary slot for equality every update is obviously not optimal
            // We might want to look into patching (Attachable)Firearm's LoadMagIntoSecondary and eject from secondary to keep track of this instead
            preval0 = itemData.data[3];
            if (asCell.AttachableFireArm == null)
            {
                itemData.data[3] = (byte)255;
            }
            else
            {
                bool found = false;
                for (int i = 0; i < asCell.AttachableFireArm.SecondaryMagazineSlots.Length; ++i)
                {
                    if (asCell.AttachableFireArm.SecondaryMagazineSlots[i].Magazine == asCell)
                    {
                        itemData.data[3] = (byte)i;
                        break;
                    }
                }
                if (!found)
                {
                    itemData.data[3] = (byte)255;
                }
            }
            modified |= preval0 != itemData.data[3];

            // Write fuel amount left
            preval0 = itemData.data[4];
            byte preval1 = itemData.data[5];
            byte preval2 = itemData.data[6];
            byte preval3 = itemData.data[7];
            BitConverter.GetBytes(asCell.FuelAmountLeft).CopyTo(itemData.data, 4);
            modified |= (preval0 != itemData.data[4] || preval1 != itemData.data[5] || preval2 != itemData.data[6] || preval3 != itemData.data[7]);

            // Write PL
            preval0 = itemData.data[8];
            itemData.data[8] = (byte)asCell.PL;
            modified |= preval0 != itemData.data[8];

            return modified;
        }

        private bool UpdateGivenSBLPCell(byte[] newData)
        {
            bool modified = false;
            sblpCell asCell = dataObject as sblpCell;

            if (itemData.data == null)
            {
                modified = true;
            }

            // Load into firearm if necessary
            if (newData[0] == 1)
            {
                if (data.parent != -1)
                {
                    TrackedItemData parentTrackedItemData = null;
                    if (ThreadManager.host)
                    {
                        parentTrackedItemData = Server.objects[data.parent] as TrackedItemData;
                    }
                    else
                    {
                        parentTrackedItemData = Client.objects[data.parent] as TrackedItemData;
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
                    TrackedItemData parentTrackedItemData = null;
                    if (ThreadManager.host)
                    {
                        parentTrackedItemData = Server.objects[data.parent] as TrackedItemData;
                    }
                    else
                    {
                        parentTrackedItemData = Client.objects[data.parent] as TrackedItemData;
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

            itemData.data = newData;

            return modified;
        }

        private bool UpdateLaserGun()
        {
            sblp asLG = dataObject as sblp;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[3];
                modified = true;
            }

            byte preval = itemData.data[0];

            // Write m_isShotEngaged
            itemData.data[0] = asLG.m_isShotEngaged ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[0];

            return modified;
        }

        private bool UpdateGivenLaserGun(byte[] newData)
        {
            bool modified = false;
            sblp asLG = dataObject as sblp;

            if (itemData.data == null)
            {
                modified = true;

                // Set m_isShotEngaged
                if (newData[0] == 1 && !asLG.m_isShotEngaged)
                {
                    asLG.TryToEngageShot();
                }
                else if (newData[0] == 0 && asLG.m_isShotEngaged)
                {
                    asLG.TryToDisengageShot();
                }
            }
            else
            {
                // Set m_isShotEngaged
                if (newData[0] == 1 && !asLG.m_isShotEngaged)
                {
                    asLG.TryToEngageShot();
                    modified = true;
                }
                else if (newData[0] == 0 && asLG.m_isShotEngaged)
                {
                    asLG.TryToDisengageShot();
                    modified = true;
                }
            }

            itemData.data = newData;

            return modified;
        }

        private bool UpdateAirgun()
        {
            Airgun asAG = dataObject as Airgun;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[3];
                modified = true;
            }

            byte preval = itemData.data[0];

            // Write hammer state
            itemData.data[0] = asAG.IsHammerCocked ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[0];

            preval = itemData.data[1];
            byte preval0 = itemData.data[2];

            // Write chambered round class
            if (asAG.Chamber.GetRound() == null || asAG.Chamber.IsSpent || asAG.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 1);
            }
            else
            {
                BitConverter.GetBytes((short)asAG.Chamber.GetRound().RoundClass).CopyTo(itemData.data, 1);
            }

            modified |= (preval != itemData.data[1] || preval0 != itemData.data[2]);

            return modified;
        }

        private bool UpdateGivenAirgun(byte[] newData)
        {
            bool modified = false;
            Airgun asAG = dataObject as Airgun;

            if (itemData.data == null)
            {
                modified = true;

                // Set hammer
                if (newData[0] == 1 && !asAG.IsHammerCocked)
                {
                    asAG.CockHammer();
                }
                else if (newData[0] == 0 && asAG.IsHammerCocked)
                {
                    asAG.m_isHammerCocked = false;
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
                    asAG.m_isHammerCocked = false;
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

            itemData.data = newData;

            return modified;
        }

        private bool FireAirgun(int chamberIndex)
        {
            Airgun asAG = dataObject as Airgun;
            asAG.DropHammer();
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

            if (itemData.data == null)
            {
                itemData.data = new byte[1];
                modified = true;
            }

            // Write armed
            byte preval = itemData.data[0];

            itemData.data[0] = (byte)asSLAM.Mode;

            modified |= preval != itemData.data[0];

            return modified;
        }

        private bool UpdateGivenSLAM(byte[] newData)
        {
            bool modified = false;
            SLAM asSLAM = (SLAM)dataObject;

            if (itemData.data == null)
            {
                modified = true;

                // Set mode
                asSLAM.SetMode((SLAM.SLAMMode)newData[0]);
            }
            else
            {
                if (itemData.data[0] != newData[0])
                {
                    // Set mode
                    asSLAM.SetMode((SLAM.SLAMMode)newData[0]);
                    modified = true;
                }
            }

            itemData.data = newData;

            return modified;
        }

        private bool UpdateClaymoreMine()
        {
            ClaymoreMine asCM = (ClaymoreMine)dataObject;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[2];
                modified = true;
            }

            // Write armed
            byte preval = itemData.data[0];

            itemData.data[0] = asCM.m_isArmed ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[0];

            // Write planted
            preval = itemData.data[1];

            itemData.data[1] = asCM.m_isPlanted ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[1];

            return modified;
        }

        private bool UpdateGivenClaymoreMine(byte[] newData)
        {
            bool modified = false;
            ClaymoreMine asCM = (ClaymoreMine)dataObject;

            if (itemData.data == null)
            {
                modified = true;

                // Set armed
                asCM.m_isArmed = newData[0] == 1;

                // Set planted
                asCM.m_isPlanted = newData[1] == 1;
            }
            else
            {
                if (itemData.data[0] != newData[0])
                {
                    // Set armed
                    asCM.m_isArmed = newData[0] == 1;
                    modified = true;
                }
                if (itemData.data[1] != newData[1])
                {
                    // Set planted
                    asCM.m_isPlanted = newData[1] == 1;
                    modified = true;
                }
            }

            itemData.data = newData;

            return modified;
        }

        private bool UpdateC4()
        {
            C4 asC4 = (C4)dataObject;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[1];
                modified = true;
            }

            // Write armed
            byte preval = itemData.data[0];

            itemData.data[0] = asC4.m_isArmed ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[0];

            return modified;
        }

        private bool UpdateGivenC4(byte[] newData)
        {
            bool modified = false;
            C4 asC4 = (C4)dataObject;

            if (itemData.data == null)
            {
                modified = true;

                // Set armed
                asC4.SetArmed(newData[0] == 1);
            }
            else
            {
                if (itemData.data[0] != newData[0])
                {
                    // Set armed
                    asC4.SetArmed(newData[0] == 1);
                    modified = true;
                }
            }

            itemData.data = newData;

            return modified;
        }

        private bool UpdateGrenade()
        {
            FVRGrenade asGrenade = (FVRGrenade)dataObject;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[asGrenade.Uses2ndPin ? 3 : 2];
                modified = true;
            }

            byte preval = itemData.data[0];

            itemData.data[0] = asGrenade.m_isLeverReleased ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[0];

            preval = itemData.data[1];

            itemData.data[1] = asGrenade.Pin.m_hasBeenPulled ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[1];

            if (asGrenade.Uses2ndPin)
            {
                preval = itemData.data[2];

                itemData.data[2] = asGrenade.Pin2.m_hasBeenPulled ? (byte)1 : (byte)0;

                modified |= preval != itemData.data[2];
            }

            return modified;
        }

        private bool UpdateGivenGrenade(byte[] newData)
        {
            bool modified = false;
            FVRGrenade asGrenade = (FVRGrenade)dataObject;

            if (itemData.data == null)
            {
                modified = true;

                // Set lever released
                if (newData[0] == 1 && !asGrenade.m_isLeverReleased)
                {
                    asGrenade.ReleaseLever();
                }

                // Set pin
                if (newData[1] == 1 && !asGrenade.Pin.m_hasBeenPulled)
                {
                    asGrenade.Pin.m_hasBeenPulled = true;
                    asGrenade.Pin.transform.SetParent(null);
                    asGrenade.Pin.PinPiece.transform.SetParent(asGrenade.Pin.transform);
                    Rigidbody rigidbody = asGrenade.Pin.PinPiece.AddComponent<Rigidbody>();
                    rigidbody.mass = 0.01f;
                    HingeJoint component = asGrenade.Pin.GetComponent<HingeJoint>();
                    component.connectedBody = rigidbody;
                    asGrenade.Pin.Grenade.PullPin();
                    asGrenade.Pin.m_isDying = true;
                    if (asGrenade.Pin.UXGeo_Held != null)
                    {
                        UnityEngine.Object.Destroy(asGrenade.Pin.UXGeo_Held);
                    }
                }

                if (asGrenade.Uses2ndPin)
                {
                    // Set pin2
                    if (newData[2] == 1 && !asGrenade.Pin2.m_hasBeenPulled)
                    {
                        asGrenade.Pin2.m_hasBeenPulled = true;
                        asGrenade.Pin2.transform.SetParent(null);
                        asGrenade.Pin2.PinPiece.transform.SetParent(asGrenade.Pin2.transform);
                        Rigidbody rigidbody = asGrenade.Pin2.PinPiece.AddComponent<Rigidbody>();
                        rigidbody.mass = 0.01f;
                        HingeJoint component = asGrenade.Pin2.GetComponent<HingeJoint>();
                        component.connectedBody = rigidbody;
                        asGrenade.Pin2.Grenade.PullPin2();
                        asGrenade.Pin2.m_isDying = true;
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
                if (newData[0] == 1 && !asGrenade.m_isLeverReleased)
                {
                    asGrenade.ReleaseLever();
                    modified = true;
                }
                // Set pin
                if (newData[1] == 1 && !asGrenade.Pin.m_hasBeenPulled)
                {
                    asGrenade.Pin.m_hasBeenPulled = true;
                    asGrenade.Pin.transform.SetParent(null);
                    asGrenade.Pin.PinPiece.transform.SetParent(asGrenade.Pin.transform);
                    Rigidbody rigidbody = asGrenade.Pin.PinPiece.AddComponent<Rigidbody>();
                    rigidbody.mass = 0.01f;
                    HingeJoint component = asGrenade.Pin.GetComponent<HingeJoint>();
                    component.connectedBody = rigidbody;
                    asGrenade.Pin.Grenade.PullPin();
                    asGrenade.Pin.m_isDying = true;
                    if (asGrenade.Pin.UXGeo_Held != null)
                    {
                        UnityEngine.Object.Destroy(asGrenade.Pin.UXGeo_Held);
                    }
                    modified = true;
                }

                if (asGrenade.Uses2ndPin)
                {
                    // Set pin2
                    if (newData[2] == 1 && !asGrenade.Pin2.m_hasBeenPulled)
                    {
                        asGrenade.Pin2.m_hasBeenPulled = true;
                        asGrenade.Pin2.transform.SetParent(null);
                        asGrenade.Pin2.PinPiece.transform.SetParent(asGrenade.Pin2.transform);
                        Rigidbody rigidbody = asGrenade.Pin2.PinPiece.AddComponent<Rigidbody>();
                        rigidbody.mass = 0.01f;
                        HingeJoint component = asGrenade.Pin2.GetComponent<HingeJoint>();
                        component.connectedBody = rigidbody;
                        asGrenade.Pin2.Grenade.PullPin2();
                        asGrenade.Pin2.m_isDying = true;
                        if (asGrenade.Pin2.UXGeo_Held != null)
                        {
                            UnityEngine.Object.Destroy(asGrenade.Pin2.UXGeo_Held);
                        }
                        modified = true;
                    }
                }
            }

            itemData.data = newData;

            return modified;
        }

        private bool UpdatePinnedGrenade()
        {
            PinnedGrenade asPG = (PinnedGrenade)dataObject;
            bool modified = false;

            int neededSize = asPG.m_rings.Count + 1;
            if (itemData.data == null || itemData.data.Length != neededSize)
            {
                itemData.data = new byte[neededSize];
                modified = true;
            }

            byte preval = itemData.data[0];

            itemData.data[0] = asPG.IsLeverReleased() ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[0];

            if (asPG.m_rings != null)
            {
                for (int i = 0; i < asPG.m_rings.Count; ++i)
                {
                    preval = itemData.data[i + 1];

                    itemData.data[i + 1] = asPG.m_rings[i].HasPinDetached() ? (byte)1 : (byte)0;

                    modified |= preval != itemData.data[i + 1];
                }
            }

            return modified;
        }

        private bool UpdateGivenPinnedGrenade(byte[] newData)
        {
            bool modified = false;
            PinnedGrenade asPG = (PinnedGrenade)dataObject;

            if (itemData.data == null)
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

            if (asPG.m_rings != null)
            {
                bool pinPulled = asPG.m_isPinPulled;
                bool newPinPulled = true;
                for (int i = 0; i < asPG.m_rings.Count; ++i)
                {
                    if (!asPG.m_rings[i].HasPinDetached())
                    {
                        if (newData[i + 1] == 1)
                        {
                            asPG.m_rings[i].m_hasPinDetached = true;
                            asPG.m_rings[i].Pin.RootRigidbody = asPG.m_rings[i].Pin.gameObject.AddComponent<Rigidbody>();
                            asPG.m_rings[i].Pin.RootRigidbody.mass = 0.02f;
                            asPG.m_rings[i].ForceBreakInteraction();
                            asPG.m_rings[i].transform.SetParent(asPG.m_rings[i].Pin.transform);
                            asPG.m_rings[i].Pin.enabled = true;
                            SM.PlayCoreSound(FVRPooledAudioType.GenericClose, asPG.m_rings[i].G.AudEvent_Pinpull, asPG.m_rings[i].transform.position);
                            asPG.m_rings[i].GetComponent<Collider>().enabled = false;
                            asPG.m_rings[i].enabled = false;
                            modified = true;
                        }
                        else
                        {
                            newPinPulled = false;
                        }
                    }
                }

                if (pinPulled != newPinPulled)
                {
                    asPG.m_isPinPulled = newPinPulled;
                }
            }

            itemData.data = newData;

            return modified;
        }

        private bool UpdateSteelPopTarget()
        {
            SteelPopTarget asSPT = (SteelPopTarget)dataObject;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[asSPT.Joints.Count * 12 + 1];
                modified = true;
            }

            byte preval = itemData.data[0];

            itemData.data[0] = asSPT.m_isSpringEnabled ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[0];

            for (int i = 0, index; i < asSPT.Joints.Count; ++i)
            {
                index = i * 12 + 1;
                preval = itemData.data[index];
                byte preval1 = itemData.data[index + 1];
                byte preval2 = itemData.data[index + 2];
                byte preval3 = itemData.data[index + 3];
                BitConverter.GetBytes(asSPT.Joints[i].transform.localEulerAngles.x).CopyTo(itemData.data, index);
                modified |= preval != itemData.data[index] || preval1 != itemData.data[index + 1] || preval2 != itemData.data[index + 2] || preval3 != itemData.data[index + 3];

                preval = itemData.data[index + 4];
                preval1 = itemData.data[index + 5];
                preval2 = itemData.data[index + 6];
                preval3 = itemData.data[index + 7];
                BitConverter.GetBytes(asSPT.Joints[i].transform.localEulerAngles.y).CopyTo(itemData.data, index + 4);
                modified |= preval != itemData.data[index + 4] || preval1 != itemData.data[index + 5] || preval2 != itemData.data[index + 6] || preval3 != itemData.data[index + 7];

                preval = itemData.data[index + 8];
                preval1 = itemData.data[index + 9];
                preval2 = itemData.data[index + 10];
                preval3 = itemData.data[index + 11];
                BitConverter.GetBytes(asSPT.Joints[i].transform.localEulerAngles.z).CopyTo(itemData.data, index + 8);
                modified |= preval != itemData.data[index + 8] || preval1 != itemData.data[index + 9] || preval2 != itemData.data[index + 10] || preval3 != itemData.data[index + 11];
            }

            return modified;
        }

        private bool UpdateGivenSteelPopTarget(byte[] newData)
        {
            bool modified = false;
            SteelPopTarget asSPT = (SteelPopTarget)dataObject;

            if (itemData.data == null)
            {
                modified = true;

                // Set spring enabled
                if ((newData[0] == 1 && !asSPT.m_isSpringEnabled) || (newData[0] == 0 && asSPT.m_isSpringEnabled))
                {
                    asSPT.ToggleSpringSwitch();
                }
            }
            else
            {

                // Set spring enabled
                if ((newData[0] == 1 && !asSPT.m_isSpringEnabled) || (newData[0] == 0 && asSPT.m_isSpringEnabled))
                {
                    asSPT.ToggleSpringSwitch();
                    modified = true;
                }
            }

            for (int i = 0; i < asSPT.Joints.Count; ++i)
            {
                Vector3 preval = asSPT.Joints[i].transform.localEulerAngles;

                asSPT.Joints[i].transform.localEulerAngles = new Vector3(BitConverter.ToSingle(newData, i * 12 + 1), BitConverter.ToSingle(newData, i * 12 + 5), BitConverter.ToSingle(newData, i * 12 + 9));

                modified |= !preval.Equals(asSPT.Joints[i].transform.localEulerAngles);
            }

            itemData.data = newData;

            return modified;
        }

        private int FindSteelPopTargetSecondary(object o)
        {
            for (int i = 0; i < secondaries.Length; ++i)
            {
                if (secondaries[i] == o)
                {
                    return i;
                }
            }

            return -1;
        }

        private object GetSteelPopTargetSecondary(int i)
        {
            if (i > -1 && i < secondaries.Length)
            {
                return secondaries[i];
            }
            return null;
        }

        private bool UpdateMolotov()
        {
            Molotov asMolotov = (Molotov)dataObject;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[1];
                modified = true;
            }

            byte preval = itemData.data[0];

            itemData.data[0] = asMolotov.Igniteable.IsOnFire() ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[0];

            return modified;
        }

        private bool UpdateGivenMolotov(byte[] newData)
        {
            bool modified = false;
            Molotov asMolotov = (Molotov)dataObject;

            if (itemData.data == null)
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

            itemData.data = newData;

            return modified;
        }

        private bool UpdateMF2_RL()
        {
            MF2_RL asMF2_RL = (MF2_RL)dataObject;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[4];
                modified = true;
            }

            byte preval = itemData.data[0];
            byte preval0 = itemData.data[1];
            byte preval1 = itemData.data[2];
            byte preval2 = itemData.data[3];

            // Write chambered round class
            if (asMF2_RL.Chamber.GetRound() == null || asMF2_RL.Chamber.IsSpent || asMF2_RL.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 2);
            }
            else
            {
                BitConverter.GetBytes((short)asMF2_RL.Chamber.GetRound().RoundType).CopyTo(itemData.data, 0);
                BitConverter.GetBytes((short)asMF2_RL.Chamber.GetRound().RoundClass).CopyTo(itemData.data, 2);
            }

            modified |= (preval != itemData.data[0] || preval0 != itemData.data[1] || preval1 != itemData.data[2] || preval2 != itemData.data[3]);

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

            itemData.data = newData;

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

            if (itemData.data == null)
            {
                itemData.data = new byte[1];
                modified = true;
            }

            byte preval0 = itemData.data[0];

            // Write has missile
            itemData.data[0] = asSL.m_hasMissile ? (byte)1 : (byte)0;

            modified |= preval0 != itemData.data[0];

            return modified;
        }

        private bool UpdateGivenStingerLauncher(byte[] newData)
        {
            bool modified = false;
            StingerLauncher asSL = dataObject as StingerLauncher;

            if (itemData.data == null)
            {
                modified = true;

                // Set has missile
                asSL.m_hasMissile = newData[0] == 1;
            }
            else
            {
                if (itemData.data[0] != newData[0])
                {
                    // Set has missile
                    asSL.m_hasMissile = newData[0] == 1;
                    modified = true;
                }
            }

            itemData.data = newData;

            return modified;
        }

        private bool UpdateSingleActionRevolver()
        {
            SingleActionRevolver asRevolver = dataObject as SingleActionRevolver;
            bool modified = false;

            int necessarySize = asRevolver.Cylinder.NumChambers * 4 + 2;

            if (itemData.data == null)
            {
                itemData.data = new byte[asRevolver.GetIntegratedAttachableFirearm() == null ? necessarySize : necessarySize + 4];
                modified = true;
            }

            byte preval0 = itemData.data[0];

            // Write cur chamber
            itemData.data[0] = (byte)asRevolver.CurChamber;

            modified |= preval0 != itemData.data[0];

            preval0 = itemData.data[1];

            // Write hammer cocked
            itemData.data[1] = asRevolver.m_isHammerCocked ? (byte)1 : (byte)0;

            modified |= preval0 != itemData.data[1];

            // Write chambered rounds
            byte preval1;
            byte preval2;
            byte preval3;
            for (int i = 0; i < asRevolver.Cylinder.Chambers.Length; ++i)
            {
                int firstIndex = i * 4 + 2;
                preval0 = itemData.data[firstIndex];
                preval1 = itemData.data[firstIndex + 1];
                preval2 = itemData.data[firstIndex + 2];
                preval3 = itemData.data[firstIndex + 3];

                if (asRevolver.Cylinder.Chambers[i].GetRound() == null || asRevolver.Cylinder.Chambers[i].IsSpent || asRevolver.Cylinder.Chambers[i].GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(itemData.data, firstIndex + 2);
                }
                else
                {
                    BitConverter.GetBytes((short)asRevolver.Cylinder.Chambers[i].GetRound().RoundType).CopyTo(itemData.data, firstIndex);
                    BitConverter.GetBytes((short)asRevolver.Cylinder.Chambers[i].GetRound().RoundClass).CopyTo(itemData.data, firstIndex + 2);
                }

                modified |= (preval0 != itemData.data[firstIndex] || preval1 != itemData.data[firstIndex + 1] || preval2 != itemData.data[firstIndex + 2] || preval3 != itemData.data[firstIndex + 3]);
            }

            if (asRevolver.GetIntegratedAttachableFirearm() != null)
            {
                preval0 = itemData.data[necessarySize];
                preval1 = itemData.data[necessarySize + 1];
                preval2 = itemData.data[necessarySize + 2];
                preval3 = itemData.data[necessarySize + 3];

                // Write chambered round class
                FVRFireArmChamber integratedChamber = attachableFirearmGetChamberFunc();
                if (integratedChamber.GetRound() == null || integratedChamber.IsSpent || integratedChamber.GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(itemData.data, necessarySize + 2);
                }
                else
                {
                    BitConverter.GetBytes((short)integratedChamber.GetRound().RoundType).CopyTo(itemData.data, necessarySize);
                    BitConverter.GetBytes((short)integratedChamber.GetRound().RoundClass).CopyTo(itemData.data, necessarySize + 2);
                }

                modified |= (preval0 != itemData.data[necessarySize] || preval1 != itemData.data[necessarySize + 1] || preval2 != itemData.data[necessarySize + 2] || preval3 != itemData.data[necessarySize + 3]);
            }

            return modified;
        }

        private bool UpdateGivenSingleActionRevolver(byte[] newData)
        {
            bool modified = false;
            SingleActionRevolver asRevolver = dataObject as SingleActionRevolver;

            if (itemData.data == null)
            {
                modified = true;

                // Set cur chamber
                asRevolver.CurChamber = newData[0];

                // Set hammer cocked
                asRevolver.m_isHammerCocked = newData[1] == 1;
            }
            else
            {
                if (itemData.data[0] != newData[0])
                {
                    // Set cur chamber
                    asRevolver.CurChamber = newData[0];
                    modified = true;
                }
                if (itemData.data[1] != newData[1])
                {
                    // Set hammer cocked
                    asRevolver.m_isHammerCocked = newData[1] == 1;
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

            // Set integrated firearm chamber
            if (asRevolver.GetIntegratedAttachableFirearm() != null)
            {
                short chamberTypeIndex = BitConverter.ToInt16(newData, newData.Length - 4);
                short chamberClassIndex = BitConverter.ToInt16(newData, newData.Length - 2);
                FVRFireArmChamber integratedChamber = attachableFirearmGetChamberFunc();
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (integratedChamber.GetRound() != null)
                    {
                        ++ChamberPatch.chamberSkip;
                        integratedChamber.SetRound(null, false);
                        --ChamberPatch.chamberSkip;
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (integratedChamber.GetRound() == null || integratedChamber.GetRound().RoundClass != roundClass)
                    {
                        if (integratedChamber.RoundType == roundType)
                        {
                            ++ChamberPatch.chamberSkip;
                            integratedChamber.SetRound(roundClass, integratedChamber.transform.position, integratedChamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                        }
                        else
                        {
                            FireArmRoundType prevRoundType = integratedChamber.RoundType;
                            integratedChamber.RoundType = roundType;
                            ++ChamberPatch.chamberSkip;
                            integratedChamber.SetRound(roundClass, integratedChamber.transform.position, integratedChamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                            integratedChamber.RoundType = prevRoundType;
                        }
                        modified = true;
                    }
                }
            }

            itemData.data = newData;

            return modified;
        }

        public int GetSingleActionRevolverChamberIndex(FVRFireArmChamber chamber)
        {
            SingleActionRevolver asRevolver = dataObject as SingleActionRevolver;
            List<FVRFireArmChamber> chambers = asRevolver.GetChambers();

            if (chamber == null)
            {
                return -1;
            }

            if (asRevolver.GetIntegratedAttachableFirearm() != null && attachableFirearmGetChamberFunc() == chamber)
            {
                return chambers.Count;
            }

            for (int i = 0; i < chambers.Count; ++i)
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
            if (chamberIndex == asRevolver.GetChambers().Count)
            {
                if (((int)roundClass) == -1)
                {
                    attachableFirearmGetChamberFunc().SetRound(null);
                }
                else
                {
                    attachableFirearmGetChamberFunc().SetRound(roundClass, asRevolver.GetChambers()[chamberIndex].transform.position, asRevolver.GetChambers()[chamberIndex].transform.rotation);
                }
            }
            else
            {
                if (((int)roundClass) == -1)
                {
                    asRevolver.GetChambers()[chamberIndex].SetRound(null);
                }
                else
                {
                    asRevolver.GetChambers()[chamberIndex].SetRound(roundClass, asRevolver.GetChambers()[chamberIndex].transform.position, asRevolver.GetChambers()[chamberIndex].transform.rotation);
                }
            }
            --ChamberPatch.chamberSkip;
        }

        private bool UpdateSimpleLauncher2()
        {
            SimpleLauncher2 asSimpleLauncher = (SimpleLauncher2)dataObject;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[5];
                modified = true;
            }

            byte preval = itemData.data[0];

            // Write mode
            itemData.data[0] = (byte)(int)asSimpleLauncher.Mode;

            modified |= preval != itemData.data[0];

            preval = itemData.data[1];
            byte preval0 = itemData.data[2];
            byte preval1 = itemData.data[3];
            byte preval2 = itemData.data[4];

            // Write chambered round class
            if (asSimpleLauncher.Chamber.GetRound() == null || asSimpleLauncher.Chamber.IsSpent || asSimpleLauncher.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 3);
            }
            else
            {
                BitConverter.GetBytes((short)asSimpleLauncher.Chamber.GetRound().RoundType).CopyTo(itemData.data, 1);
                BitConverter.GetBytes((short)asSimpleLauncher.Chamber.GetRound().RoundClass).CopyTo(itemData.data, 3);
            }

            modified |= (preval != itemData.data[1] || preval0 != itemData.data[2] || preval1 != itemData.data[3] || preval2 != itemData.data[4]);

            return modified;
        }

        private bool UpdateGivenSimpleLauncher2(byte[] newData)
        {
            bool modified = false;
            SimpleLauncher2 asSimpleLauncher = (SimpleLauncher2)dataObject;

            if (itemData.data == null)
            {
                modified = true;

                // Set mode
                asSimpleLauncher.Mode = (SimpleLauncher2.fMode)newData[0];
                if (asSimpleLauncher.Mode == SimpleLauncher2.fMode.dr)
                {
                    // Dont want it to go into DR mode if not in control
                    asSimpleLauncher.Mode = SimpleLauncher2.fMode.tr;
                }
            }
            else
            {
                if (itemData.data[0] != newData[0])
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

            itemData.data = newData;

            return modified;
        }

        private bool FireSimpleLauncher2(int chamberIndex)
        {
            SimpleLauncher2 asSimpleLauncher = (SimpleLauncher2)dataObject;
            bool wasOnSA = false;
            if (asSimpleLauncher.Mode == SimpleLauncher2.fMode.sa)
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

            if (itemData.data == null)
            {
                itemData.data = new byte[4];
                modified = true;
            }

            byte preval = itemData.data[0];
            byte preval0 = itemData.data[1];
            byte preval1 = itemData.data[2];
            byte preval2 = itemData.data[3];

            // Write chambered round class
            if (asSimpleLauncher.Chamber.GetRound() == null || asSimpleLauncher.Chamber.IsSpent || asSimpleLauncher.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 2);
            }
            else
            {
                BitConverter.GetBytes((short)asSimpleLauncher.Chamber.GetRound().RoundType).CopyTo(itemData.data, 0);
                BitConverter.GetBytes((short)asSimpleLauncher.Chamber.GetRound().RoundClass).CopyTo(itemData.data, 2);
            }

            modified |= (preval != itemData.data[0] || preval0 != itemData.data[1] || preval1 != itemData.data[2] || preval2 != itemData.data[3]);

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

            itemData.data = newData;

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

            if (itemData.data == null)
            {
                itemData.data = new byte[5];
                modified = true;
            }

            byte preval = itemData.data[0];

            // Write hammer state
            itemData.data[0] = asRPG7.m_isHammerCocked ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[0];

            preval = itemData.data[1];
            byte preval0 = itemData.data[2];
            byte preval1 = itemData.data[3];
            byte preval2 = itemData.data[4];

            // Write chambered round class
            if (asRPG7.Chamber.GetRound() == null || asRPG7.Chamber.IsSpent || asRPG7.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 3);
            }
            else
            {
                BitConverter.GetBytes((short)asRPG7.Chamber.GetRound().RoundType).CopyTo(itemData.data, 1);
                BitConverter.GetBytes((short)asRPG7.Chamber.GetRound().RoundClass).CopyTo(itemData.data, 3);
            }

            modified |= (preval != itemData.data[1] || preval0 != itemData.data[2] || preval1 != itemData.data[3] || preval2 != itemData.data[4]);

            return modified;
        }

        private bool UpdateGivenRPG7(byte[] newData)
        {
            bool modified = false;
            RPG7 asRPG7 = (RPG7)dataObject;

            if (itemData.data == null)
            {
                modified = true;

                // Set hammer state
                asRPG7.m_isHammerCocked = newData[0] == 1;
            }
            else
            {
                if (itemData.data[0] != newData[0])
                {
                    // Set hammer state
                    asRPG7.m_isHammerCocked = newData[0] == 1;
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

            itemData.data = newData;

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
            asRPG7.m_isHammerCocked = true;
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

            if (itemData.data == null)
            {
                itemData.data = new byte[5];
                modified = true;
            }

            byte preval = itemData.data[0];

            // Write block state
            itemData.data[0] = (byte)asRB.m_state;

            modified |= preval != itemData.data[0];

            preval = itemData.data[1];
            byte preval0 = itemData.data[2];
            byte preval1 = itemData.data[3];
            byte preval2 = itemData.data[4];

            // Write chambered round class
            if (asRB.Chamber.GetRound() == null || asRB.Chamber.IsSpent || asRB.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 3);
            }
            else
            {
                BitConverter.GetBytes((short)asRB.Chamber.GetRound().RoundType).CopyTo(itemData.data, 1);
                BitConverter.GetBytes((short)asRB.Chamber.GetRound().RoundClass).CopyTo(itemData.data, 3);
            }

            modified |= (preval != itemData.data[1] || preval0 != itemData.data[2] || preval != itemData.data[3] || preval0 != itemData.data[4]);

            return modified;
        }

        private bool UpdateGivenRollingBlock(byte[] newData)
        {
            bool modified = false;
            RollingBlock asRB = (RollingBlock)dataObject;

            if (itemData.data == null)
            {
                modified = true;

                // Set block state
                asRB.m_state = (RollingBlock.RollingBlockState)newData[0];
            }
            else
            {
                if (itemData.data[0] != newData[0])
                {
                    // Set block state
                    asRB.m_state = (RollingBlock.RollingBlockState)newData[0];
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

            itemData.data = newData;

            return modified;
        }

        private bool FireRollingBlock(int chamberIndex)
        {
            RollingBlock asRB = (RollingBlock)dataObject;
            asRB.Fire();
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

            if (itemData.data == null)
            {
                itemData.data = new byte[4];
                modified = true;
            }

            byte preval = itemData.data[0];
            byte preval0 = itemData.data[1];
            byte preval1 = itemData.data[2];
            byte preval2 = itemData.data[3];

            // Write chambered round class
            if (asRGM40.Chamber.GetRound() == null || asRGM40.Chamber.IsSpent || asRGM40.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 2);
            }
            else
            {
                BitConverter.GetBytes((short)asRGM40.Chamber.GetRound().RoundType).CopyTo(itemData.data, 0);
                BitConverter.GetBytes((short)asRGM40.Chamber.GetRound().RoundClass).CopyTo(itemData.data, 2);
            }

            modified |= (preval != itemData.data[0] || preval0 != itemData.data[1] || preval1 != itemData.data[2] || preval2 != itemData.data[3]);

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

            itemData.data = newData;

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

            if (itemData.data == null)
            {
                itemData.data = new byte[asRML.m_missile == null ? 3 : 35];
                modified = true;
            }
            else
            {
                if (asRML.m_missile == null)
                {
                    if (itemData.data.Length == 35)
                    {
                        itemData.data = new byte[3];
                        modified = true;
                    }
                }
                else
                {
                    if (itemData.data.Length == 3)
                    {
                        itemData.data = new byte[35];
                        modified = true;
                    }
                }
            }

            byte preval0 = itemData.data[0];

            // Write poweredUp
            itemData.data[0] = asRML.IsPoweredUp ? (byte)1 : (byte)0;

            modified |= preval0 != itemData.data[0];

            preval0 = itemData.data[1];

            // Write chamber full
            itemData.data[1] = asRML.Chamber.IsFull ? (byte)1 : (byte)0;

            modified |= preval0 != itemData.data[1];

            preval0 = itemData.data[2];

            // Write has missile
            itemData.data[2] = asRML.m_missile == null ? (byte)0 : (byte)1;

            modified |= preval0 != itemData.data[2];

            if (asRML.m_missile != null)
            {
                modified = true;

                // Write missile pos
                BitConverter.GetBytes(asRML.m_missile.transform.position.x).CopyTo(itemData.data, 3);
                BitConverter.GetBytes(asRML.m_missile.transform.position.y).CopyTo(itemData.data, 7);
                BitConverter.GetBytes(asRML.m_missile.transform.position.z).CopyTo(itemData.data, 11);

                // Write missile rot
                BitConverter.GetBytes(asRML.m_missile.transform.rotation.eulerAngles.x).CopyTo(itemData.data, 15);
                BitConverter.GetBytes(asRML.m_missile.transform.rotation.eulerAngles.y).CopyTo(itemData.data, 19);
                BitConverter.GetBytes(asRML.m_missile.transform.rotation.eulerAngles.z).CopyTo(itemData.data, 23);

                // Write target speed
                BitConverter.GetBytes(asRML.m_missile.speed).CopyTo(itemData.data, 27);

                // Write speed
                BitConverter.GetBytes(asRML.m_missile.tarSpeed).CopyTo(itemData.data, 31);
            }

            return modified;
        }

        private bool UpdateGivenRemoteMissileLauncher(byte[] newData)
        {
            bool modified = false;
            RemoteMissileLauncher asRML = dataObject as RemoteMissileLauncher;

            // Set powered up
            if ((asRML.IsPoweredUp && newData[0] == 0) || (!asRML.IsPoweredUp && newData[0] == 1))
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

            if (asRML.m_missile != null)
            {
                if (newData[2] == 1)
                {
                    asRML.m_missile.transform.position = new Vector3(BitConverter.ToSingle(newData, 3), BitConverter.ToSingle(newData, 7), BitConverter.ToSingle(newData, 11));
                    asRML.m_missile.transform.rotation = Quaternion.Euler(BitConverter.ToSingle(newData, 15), BitConverter.ToSingle(newData, 19), BitConverter.ToSingle(newData, 23));

                    asRML.m_missile.speed = BitConverter.ToSingle(newData, 27);
                    asRML.m_missile.tarSpeed = BitConverter.ToSingle(newData, 31);
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

            itemData.data = newData;

            return modified;
        }

        private bool FireRemoteMissileLauncher(int chamberIndex)
        {
            RemoteMissileLauncher asRML = dataObject as RemoteMissileLauncher;
            asRML.FireShot();
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

            if (itemData.data == null)
            {
                itemData.data = new byte[5];
                modified = true;
            }

            byte preval0 = itemData.data[0];
            byte preval1 = itemData.data[1];
            byte preval2 = itemData.data[2];
            byte preval3 = itemData.data[3];

            // Write m_chamberGas
            BitConverter.GetBytes(asPG.m_chamberGas).CopyTo(itemData.data, 0);

            modified |= (preval0 != itemData.data[0] || preval1 != itemData.data[1] || preval2 != itemData.data[2] || preval3 != itemData.data[3]);

            preval0 = itemData.data[4];

            // Write chamber full
            itemData.data[4] = asPG.Chamber.IsFull ? (byte)1 : (byte)0;

            modified |= preval0 != itemData.data[4];

            return modified;
        }

        private bool UpdateGivenPotatoGun(byte[] newData)
        {
            bool modified = false;
            PotatoGun asPG = dataObject as PotatoGun;

            if (itemData.data == null)
            {
                modified = true;

                // Set m_chamberGas
                asPG.m_chamberGas = BitConverter.ToSingle(newData, 0);
            }
            else
            {
                if (itemData.data[0] != newData[0] || itemData.data[1] != newData[1] || itemData.data[2] != newData[2] || itemData.data[3] != newData[3])
                {
                    // Set m_chamberGas
                    asPG.m_chamberGas = BitConverter.ToSingle(newData, 0);
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

            itemData.data = newData;

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

            if (itemData.data == null)
            {
                itemData.data = new byte[5];
                modified = true;
            }

            // Write motor rate
            byte preval0 = itemData.data[0];
            byte preval1 = itemData.data[1];
            byte preval2 = itemData.data[2];
            byte preval3 = itemData.data[3];
            BitConverter.GetBytes(asMinigun.m_motorRate).CopyTo(itemData.data, 0);
            modified |= (preval0 != itemData.data[0] || preval1 != itemData.data[1] || preval2 != itemData.data[2] || preval3 != itemData.data[3]);

            // Write triggerEngaged
            preval0 = itemData.data[4];
            itemData.data[4] = asMinigun.m_isTriggerEngaged ? (byte)1 : (byte)0;
            modified |= preval0 != itemData.data[4];

            return modified;
        }

        private bool UpdateGivenMinigun(byte[] newData)
        {
            bool modified = false;
            Minigun asMinigun = dataObject as Minigun;

            if (itemData.data == null)
            {
                modified = true;

                // Set motorrate
                asMinigun.m_motorRate = BitConverter.ToSingle(newData, 0);
                asMinigun.m_tarMotorRate = asMinigun.m_motorRate;
            }
            else
            {
                if (itemData.data[0] != newData[0] || itemData.data[1] != newData[1] || itemData.data[2] != newData[2] || itemData.data[3] != newData[3])
                {
                    // Set motorrate
                    modified = true;
                }

                // We are setting this outside of the check because non controller will still update and will stop the motor
                // so need to keep overriding, despite there being no difference
                asMinigun.m_motorRate = BitConverter.ToSingle(newData, 0);
                asMinigun.m_tarMotorRate = asMinigun.m_motorRate;
            }

            // Set trigger engaged
            asMinigun.m_isTriggerEngaged = newData[4] == 1;

            itemData.data = newData;

            return modified;
        }

        private bool UpdateM72()
        {
            M72 asM72 = dataObject as M72;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[4];
                modified = true;
            }

            byte preval = itemData.data[0];

            // Write safety
            itemData.data[0] = asM72.m_isSafetyEngaged ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[0];

            preval = itemData.data[1];

            // Write m_isCapOpen
            itemData.data[1] = asM72.CanTubeBeGrabbed() ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[1];

            preval = itemData.data[2];

            // Write tube state
            itemData.data[2] = (byte)asM72.TState;

            modified |= preval != itemData.data[2];

            preval = itemData.data[3];

            // Write chamber full
            itemData.data[3] = asM72.Chamber.IsFull ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[3];

            return modified;
        }

        private bool UpdateGivenM72(byte[] newData)
        {
            bool modified = false;
            M72 asM72 = dataObject as M72;

            if (itemData.data == null)
            {
                modified = true;

                // Set safety
                bool currentSafety = asM72.m_isSafetyEngaged;
                if ((currentSafety && newData[0] == 0) || (!currentSafety && newData[0] == 1))
                {
                    asM72.ToggleSafety();
                }

                // Set cap
                if ((asM72.CanTubeBeGrabbed() && newData[1] == 0) || (!asM72.CanTubeBeGrabbed() && newData[1] == 1))
                {
                    asM72.ToggleCap();
                }

                // Set Tube state
                if ((asM72.TState == M72.TubeState.Forward || asM72.TState == M72.TubeState.Mid) && newData[2] == 2)
                {
                    asM72.TState = M72.TubeState.Rear;
                    asM72.Tube.transform.localPosition = asM72.Tube_Rear.localPosition;
                }
                else if ((asM72.TState == M72.TubeState.Mid || asM72.TState == M72.TubeState.Rear) && newData[2] == 0)
                {
                    asM72.TState = M72.TubeState.Forward;
                    asM72.Tube.transform.localPosition = asM72.Tube_Front.localPosition;
                }
            }
            else
            {
                // Set safety
                bool currentSafety = asM72.m_isSafetyEngaged;
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

            itemData.data = newData;

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

            if (itemData.data == null)
            {
                itemData.data = new byte[asOBR.GetIntegratedAttachableFirearm() == null ? 6 : 10];
                modified = true;
            }

            byte preval = itemData.data[0];

            // Write fire mode index
            itemData.data[0] = (byte)asOBR.FireSelectorModeIndex;

            modified |= preval != itemData.data[0];

            preval = itemData.data[1];

            // Write camBurst
            itemData.data[1] = (byte)asOBR.m_CamBurst;

            modified |= preval != itemData.data[1];

            preval = itemData.data[2];
            byte preval0 = itemData.data[3];
            byte preval1 = itemData.data[4];
            byte preval2 = itemData.data[5];

            // Write chambered round class
            if (asOBR.Chamber.GetRound() == null || asOBR.Chamber.IsSpent || asOBR.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 4);
            }
            else
            {
                BitConverter.GetBytes((short)asOBR.Chamber.GetRound().RoundType).CopyTo(itemData.data, 2);
                BitConverter.GetBytes((short)asOBR.Chamber.GetRound().RoundClass).CopyTo(itemData.data, 4);
            }

            modified |= (preval != itemData.data[2] || preval0 != itemData.data[3] || preval1 != itemData.data[4] || preval2 != itemData.data[5]);

            if (asOBR.GetIntegratedAttachableFirearm() != null)
            {
                preval = itemData.data[6];
                preval0 = itemData.data[7];
                preval1 = itemData.data[8];
                preval2 = itemData.data[9];

                // Write chambered round class
                FVRFireArmChamber integratedChamber = attachableFirearmGetChamberFunc();
                if (integratedChamber.GetRound() == null || integratedChamber.IsSpent || integratedChamber.GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 8);
                }
                else
                {
                    BitConverter.GetBytes((short)integratedChamber.GetRound().RoundType).CopyTo(itemData.data, 6);
                    BitConverter.GetBytes((short)integratedChamber.GetRound().RoundClass).CopyTo(itemData.data, 8);
                }

                modified |= (preval != itemData.data[6] || preval0 != itemData.data[7] || preval1 != itemData.data[8] || preval2 != itemData.data[9]);
            }

            return modified;
        }

        private bool UpdateGivenOpenBoltReceiver(byte[] newData)
        {
            bool modified = false;
            OpenBoltReceiver asOBR = dataObject as OpenBoltReceiver;

            if (itemData.data == null)
            {
                modified = true;

                // Set fire select mode
                asOBR.m_fireSelectorMode = newData[0];

                // Set camBurst
                asOBR.m_CamBurst = newData[1];
            }
            else
            {
                if (itemData.data[0] != newData[0])
                {
                    // Set fire select mode
                    asOBR.m_fireSelectorMode = newData[0];
                    modified = true;
                }
                if (itemData.data[1] != newData[1])
                {
                    // Set camBurst
                    asOBR.m_CamBurst = newData[1];
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

            // Set integrated firearm chamber
            if (asOBR.GetIntegratedAttachableFirearm() != null)
            {
                chamberTypeIndex = BitConverter.ToInt16(newData, 6);
                chamberClassIndex = BitConverter.ToInt16(newData, 8);
                FVRFireArmChamber integratedChamber = attachableFirearmGetChamberFunc();
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (integratedChamber.GetRound() != null)
                    {
                        ++ChamberPatch.chamberSkip;
                        integratedChamber.SetRound(null, false);
                        --ChamberPatch.chamberSkip;
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (integratedChamber.GetRound() == null || integratedChamber.GetRound().RoundClass != roundClass)
                    {
                        if (integratedChamber.RoundType == roundType)
                        {
                            ++ChamberPatch.chamberSkip;
                            integratedChamber.SetRound(roundClass, integratedChamber.transform.position, integratedChamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                        }
                        else
                        {
                            FireArmRoundType prevRoundType = integratedChamber.RoundType;
                            integratedChamber.RoundType = roundType;
                            ++ChamberPatch.chamberSkip;
                            integratedChamber.SetRound(roundClass, integratedChamber.transform.position, integratedChamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                            integratedChamber.RoundType = prevRoundType;
                        }
                        modified = true;
                    }
                }
            }

            itemData.data = newData;

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

        private int GetOpenBoltReceiverChamberIndex(FVRFireArmChamber chamber)
        {
            OpenBoltReceiver asOBR = dataObject as OpenBoltReceiver;

            if (asOBR.GetIntegratedAttachableFirearm() == null)
            {
                return 0;
            }
            else
            {
                return chamber == asOBR.Chamber ? 0 : 1;
            }
        }

        private void ChamberOpenBoltReceiverRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            OpenBoltReceiver asOBR = dataObject as OpenBoltReceiver;

            ++ChamberPatch.chamberSkip;
            if (chamberIndex == 1)
            {
                if(((int)roundClass) == -1)
                {
                    attachableFirearmGetChamberFunc().SetRound(null);
                }
                else
                {
                    attachableFirearmGetChamberFunc().SetRound(roundClass, asOBR.Chamber.transform.position, asOBR.Chamber.transform.rotation);
                }
            }
            else
            {
                if (((int)roundClass) == -1)
                {
                    asOBR.Chamber.SetRound(null);
                }
                else
                {
                    asOBR.Chamber.SetRound(roundClass, asOBR.Chamber.transform.position, asOBR.Chamber.transform.rotation);
                }
            }
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

            if (itemData.data == null)
            {
                itemData.data = new byte[6];
                modified = true;
            }

            byte preval0 = itemData.data[0];

            // Write m_sledState
            itemData.data[0] = (byte)asHCB.m_sledState;

            modified |= preval0 != itemData.data[0];

            preval0 = itemData.data[1];

            // Write chamber accessible
            itemData.data[1] = asHCB.Chamber.IsAccessible ? (byte)1 : (byte)0;

            modified |= preval0 != itemData.data[1];

            // Write chambered rounds
            preval0 = itemData.data[2];
            byte preval1 = itemData.data[3];
            byte preval2 = itemData.data[4];
            byte preval3 = itemData.data[5];

            if (asHCB.Chamber.GetRound() == null || asHCB.Chamber.IsSpent || asHCB.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 4);
            }
            else
            {
                BitConverter.GetBytes((short)asHCB.Chamber.GetRound().RoundType).CopyTo(itemData.data, 2);
                BitConverter.GetBytes((short)asHCB.Chamber.GetRound().RoundClass).CopyTo(itemData.data, 4);
            }

            modified |= (preval0 != itemData.data[2] || preval1 != itemData.data[3] || preval2 != itemData.data[4] || preval3 != itemData.data[5]);

            return modified;
        }

        private bool UpdateGivenHCB(byte[] newData)
        {
            bool modified = false;
            HCB asHCB = dataObject as HCB;

            if (itemData.data == null)
            {
                modified = true;

                // Set m_sledState
                asHCB.m_sledState = (HCB.SledState)newData[0];

                // Set chamber accessible
                asHCB.Chamber.IsAccessible = newData[1] == 1;
            }
            else
            {
                if (itemData.data[0] != newData[0])
                {
                    // Set m_sledState
                    asHCB.m_sledState = (HCB.SledState)newData[0];
                    modified = true;
                }
                if (itemData.data[1] != newData[1])
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

            itemData.data = newData;

            return modified;
        }

        private bool UpdateGrappleGun()
        {
            GrappleGun asGG = dataObject as GrappleGun;
            bool modified = false;

            int necessarySize = asGG.Chambers.Length * 4 + 2;

            if (itemData.data == null)
            {
                itemData.data = new byte[necessarySize];
                modified = true;
            }

            byte preval0 = itemData.data[0];

            // Write cur chamber
            itemData.data[0] = (byte)asGG.m_curChamber;

            modified |= preval0 != itemData.data[0];

            preval0 = itemData.data[1];

            // Write mag loaded
            itemData.data[1] = asGG.IsMagLoaded ? (byte)1 : (byte)0;

            modified |= preval0 != itemData.data[1];

            // Write chambered rounds
            byte preval1;
            byte preval2;
            byte preval3;
            for (int i = 0; i < asGG.Chambers.Length; ++i)
            {
                int firstIndex = i * 4 + 2;
                preval0 = itemData.data[firstIndex];
                preval1 = itemData.data[firstIndex + 1];
                preval2 = itemData.data[firstIndex + 2];
                preval3 = itemData.data[firstIndex + 3];

                if (asGG.Chambers[i].GetRound() == null || asGG.Chambers[i].IsSpent || asGG.Chambers[i].GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(itemData.data, firstIndex + 2);
                }
                else
                {
                    BitConverter.GetBytes((short)asGG.Chambers[i].GetRound().RoundType).CopyTo(itemData.data, firstIndex);
                    BitConverter.GetBytes((short)asGG.Chambers[i].GetRound().RoundClass).CopyTo(itemData.data, firstIndex + 2);
                }

                modified |= (preval0 != itemData.data[firstIndex] || preval1 != itemData.data[firstIndex + 1] || preval2 != itemData.data[firstIndex + 2] || preval3 != itemData.data[firstIndex + 3]);
            }

            return modified;
        }

        private bool UpdateGivenGrappleGun(byte[] newData)
        {
            bool modified = false;
            GrappleGun asGG = dataObject as GrappleGun;

            if (itemData.data == null)
            {
                modified = true;

                // Set cur chamber
                asGG.m_curChamber = newData[0];

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
                if (itemData.data[0] != newData[0])
                {
                    // Set cur chamber
                    asGG.m_curChamber = newData[0];
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

            itemData.data = newData;

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

            for (int i = 0; i < chambers.Length; ++i)
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

            if (itemData.data == null)
            {
                itemData.data = new byte[7];
                modified = true;
            }

            byte preval = itemData.data[0];

            // Write battery switch state
            itemData.data[0] = asGBeamer.m_isBatterySwitchedOn ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[0];

            preval = itemData.data[1];

            // Write capacitor switch state
            itemData.data[1] = asGBeamer.m_isCapacitorSwitchedOn ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[1];

            preval = itemData.data[2];

            // Write motor switch state
            itemData.data[2] = asGBeamer.m_isMotorSwitchedOn ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[2];

            byte preval0 = itemData.data[3];
            byte preval1 = itemData.data[4];
            byte preval2 = itemData.data[5];
            byte preval3 = itemData.data[6];

            // Write cap charge
            BitConverter.GetBytes(asGBeamer.m_capacitorCharge).CopyTo(itemData.data, 3);

            modified |= (preval0 != itemData.data[3] || preval1 != itemData.data[4] || preval2 != itemData.data[5] || preval3 != itemData.data[6]);

            return modified;
        }

        private bool UpdateGivenGBeamer(byte[] newData)
        {
            bool modified = false;
            GBeamer asGBeamer = dataObject as GBeamer;

            // Set battery switch state
            if ((asGBeamer.m_isBatterySwitchedOn && newData[0] == 0) || (!asGBeamer.m_isBatterySwitchedOn && newData[0] == 1))
            {
                // Toggle
                asGBeamer.BatterySwitch.ToggleSwitch(false);
                modified = true;
            }

            // Set capacitor switch state
            if ((asGBeamer.m_isCapacitorSwitchedOn && newData[1] == 0) || (!asGBeamer.m_isCapacitorSwitchedOn && newData[1] == 1))
            {
                // Toggle
                asGBeamer.CapacitorSwitch.ToggleSwitch(false);
                modified = true;
            }

            // Set motor switch state
            if ((asGBeamer.m_isMotorSwitchedOn && newData[2] == 0) || (!asGBeamer.m_isMotorSwitchedOn && newData[2] == 1))
            {
                // Toggle
                asGBeamer.MotorSwitch.ToggleSwitch(false);
                modified = true;
            }

            float newCapCharge = BitConverter.ToSingle(newData, 3);
            if (asGBeamer.m_capacitorCharge != newCapCharge)
            {
                asGBeamer.m_capacitorCharge = newCapCharge;
                modified = true;
            }

            itemData.data = newData;

            return modified;
        }

        private bool UpdateFlintlockWeapon()
        {
            FlintlockWeapon asFLW = physicalItem as FlintlockWeapon;

            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[6];
                modified = true;
            }

            byte preval = itemData.data[0];

            // Write hammer state
            itemData.data[0] = (byte)asFLW.HammerState;

            modified |= preval != itemData.data[0];

            preval = itemData.data[1];

            // Write has flint
            itemData.data[1] = asFLW.HasFlint() ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[1];

            preval = itemData.data[2];

            // Write flint state
            itemData.data[2] = (byte)asFLW.FState;

            modified |= preval != itemData.data[2];

            byte preval0 = itemData.data[3];
            byte preval1 = itemData.data[4];
            byte preval2 = itemData.data[5];

            // Write flint uses
            itemData.data[3] = (byte)(int)asFLW.m_flintUses.x;
            itemData.data[4] = (byte)(int)asFLW.m_flintUses.y;
            itemData.data[5] = (byte)(int)asFLW.m_flintUses.z;

            modified |= (preval0 != itemData.data[3] || preval1 != itemData.data[4] || preval2 != itemData.data[5]);

            return modified;
        }

        private bool UpdateGivenFlintlockWeapon(byte[] newData)
        {
            bool modified = false;
            FlintlockWeapon asFLW = physicalItem as FlintlockWeapon;

            // Set hammer state
            FlintlockWeapon.HState preVal = asFLW.HammerState;

            asFLW.HammerState = (FlintlockWeapon.HState)newData[0];

            modified |= preVal != asFLW.HammerState;

            // Set hasFlint
            bool preVal0 = asFLW.HasFlint();

            asFLW.m_hasFlint = newData[1] == 1;

            modified |= preVal0 ^ asFLW.HasFlint();

            // Set flint state
            FlintlockWeapon.FlintState preVal1 = asFLW.FState;

            asFLW.FState = (FlintlockWeapon.FlintState)newData[2];

            modified |= preVal1 != asFLW.FState;

            Vector3 preUses = asFLW.m_flintUses;

            // Write flint uses
            Vector3 uses = Vector3.zero;
            uses.x = newData[3];
            uses.y = newData[4];
            uses.z = newData[5];
            asFLW.m_flintUses = uses;

            modified |= !preUses.Equals(uses);

            itemData.data = newData;

            return modified;
        }

        private bool UpdateFlaregun()
        {
            Flaregun asFG = dataObject as Flaregun;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[6];
                modified = true;
            }

            byte preval = itemData.data[0];

            // Write hammer state
            itemData.data[0] = asFG.m_isHammerCocked ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[0];

            preval = itemData.data[1];
            byte preval0 = itemData.data[2];

            // Write chambered round class
            if (asFG.Chamber.GetRound() == null || asFG.Chamber.IsSpent || asFG.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 3);
            }
            else
            {
                BitConverter.GetBytes((short)asFG.Chamber.GetRound().RoundType).CopyTo(itemData.data, 1);
                BitConverter.GetBytes((short)asFG.Chamber.GetRound().RoundClass).CopyTo(itemData.data, 3);
            }

            modified |= (preval != itemData.data[1] || preval0 != itemData.data[2]);

            // Write hingeState
            preval = itemData.data[5];
            itemData.data[5] = (byte)asFG.m_hingeState;
            modified |= preval != itemData.data[5];

            return modified;
        }

        private bool UpdateGivenFlaregun(byte[] newData)
        {
            bool modified = false;
            Flaregun asFG = dataObject as Flaregun;

            // Set hammer state
            bool preVal = asFG.m_isHammerCocked;
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

            // Set hingeState
            Flaregun.HingeState newHingeState = (Flaregun.HingeState)newData[5];
            if (asFG.m_hingeState == Flaregun.HingeState.Open
                && (newHingeState == Flaregun.HingeState.Closing || newHingeState == Flaregun.HingeState.Closed))
            {
                asFG.Latch();
                asFG.m_hingeState = Flaregun.HingeState.Closed;
                asFG.m_hingeLerp = 0;
                asFG.SetAnimatedComponent(asFG.Hinge, Mathf.Lerp(0f, asFG.RotOut, asFG.m_hingeLerp), FVRPhysicalObject.InterpStyle.Rotation, asFG.HingeAxis);
                modified = true;
            }
            else if (asFG.m_hingeState == Flaregun.HingeState.Closed
                     && (newHingeState == Flaregun.HingeState.Opening || newHingeState == Flaregun.HingeState.Open))
            {
                asFG.Unlatch();
                asFG.m_hingeState = Flaregun.HingeState.Open;
                asFG.m_hingeLerp = 1;
                asFG.SetAnimatedComponent(asFG.Hinge, Mathf.Lerp(0f, asFG.RotOut, asFG.m_hingeLerp), FVRPhysicalObject.InterpStyle.Rotation, asFG.HingeAxis);
                modified = true;
            }

            itemData.data = newData;

            return modified;
        }

        private bool FireFlaregun(int chamberIndex)
        {
            (dataObject as Flaregun).Fire();
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

            if (itemData.data == null)
            {
                itemData.data = new byte[1];
                modified = true;
            }

            // Write firing
            byte preval = itemData.data[0];

            itemData.data[0] = asFT.m_isFiring ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[0];

            return modified;
        }

        private bool UpdateGivenFlameThrower(byte[] newData)
        {
            bool modified = false;
            FlameThrower asFT = dataObject as FlameThrower;

            // Set firing
            if (asFT.m_isFiring && newData[0] == 0)
            {
                // Stop firing
                asFT.StopFiring();
                modified = true;
            }
            else if (!asFT.m_isFiring && newData[0] == 1)
            {
                // Start firing
                asFT.m_hasFiredStartSound = true;
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
                asFT.m_isFiring = true;
                modified = true;
            }

            itemData.data = newData;

            return modified;
        }

        private bool UpdateCarlGustaf()
        {
            CarlGustaf asCG = dataObject as CarlGustaf;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[9];
                modified = true;
            }

            // Write chamber round
            byte preval0 = itemData.data[0];
            byte preval1 = itemData.data[1];
            byte preval2 = itemData.data[2];
            byte preval3 = itemData.data[3];
            byte preval = itemData.data[8];

            if (asCG.Chamber.GetRound() == null)
            {
                BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 2);
            }
            else
            {
                BitConverter.GetBytes((short)asCG.Chamber.GetRound().RoundType).CopyTo(itemData.data, 0);
                BitConverter.GetBytes((short)asCG.Chamber.GetRound().RoundClass).CopyTo(itemData.data, 2);
                itemData.data[8] = (asCG.Chamber.IsSpent || asCG.Chamber.GetRound().IsSpent) ? (byte)1 : (byte)0;
            }

            modified |= (preval0 != itemData.data[0] || preval1 != itemData.data[1] || preval2 != itemData.data[2] || preval3 != itemData.data[3] || preval != itemData.data[8]);

            // Write charge handle state
            preval0 = itemData.data[4];

            itemData.data[4] = (byte)asCG.CHState;

            modified |= preval0 != itemData.data[4];

            // Write lid latch state
            preval0 = itemData.data[5];

            itemData.data[5] = (byte)asCG.TailLatch.LState;

            modified |= preval0 != itemData.data[5];

            // Write shell slide state
            preval0 = itemData.data[6];

            itemData.data[6] = (byte)asCG.ShellInsertEject.CSState;

            modified |= preval0 != itemData.data[6];

            // Write lock latch state
            preval0 = itemData.data[7];

            itemData.data[7] = (byte)asCG.TailLatch.RestrictingLatch.LState;

            modified |= preval0 != itemData.data[7];

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

                if (newData[8] == 1 && (!asCG.Chamber.IsSpent))
                {
                    asCG.Chamber.Fire();
                }
            }

            // Set charging handle state
            if (newData[4] == 0) // Forward
            {
                if (asCG.CHState != CarlGustaf.ChargingHandleState.Forward)
                {
                    asCG.m_curZ = asCG.ChargingHandForward.localPosition.z;
                    asCG.m_tarZ = asCG.ChargingHandForward.localPosition.z;
                    asCG.ChargingHandle.localPosition = new Vector3(asCG.ChargingHandle.localPosition.x, asCG.ChargingHandle.localPosition.y, asCG.ChargingHandForward.localPosition.z);
                    asCG.CHState = CarlGustaf.ChargingHandleState.Forward;
                }
            }
            else if (newData[4] == 1) // Middle
            {
                if (asCG.CHState != CarlGustaf.ChargingHandleState.Middle)
                {
                    float val = Mathf.Lerp(asCG.ChargingHandForward.localPosition.z, asCG.ChargingHandBack.localPosition.z, 0.5f);
                    asCG.m_curZ = val;
                    asCG.m_tarZ = val;
                    asCG.ChargingHandle.localPosition = new Vector3(asCG.ChargingHandle.localPosition.x, asCG.ChargingHandle.localPosition.y, val);
                    asCG.CHState = CarlGustaf.ChargingHandleState.Middle;
                }
            }
            else if (asCG.CHState != CarlGustaf.ChargingHandleState.Back)
            {
                asCG.m_curZ = asCG.ChargingHandBack.localPosition.z;
                asCG.m_tarZ = asCG.ChargingHandBack.localPosition.z;
                asCG.ChargingHandle.localPosition = new Vector3(asCG.ChargingHandle.localPosition.x, asCG.ChargingHandle.localPosition.y, asCG.ChargingHandBack.localPosition.z);
                asCG.CHState = CarlGustaf.ChargingHandleState.Back;
            }

            // Set lid latch state
            if (newData[5] == 0) // Closed
            {
                if (asCG.TailLatch.LState != CarlGustafLatch.CGLatchState.Closed && asCG.TailLatch.m_hand == null)
                {
                    float val = asCG.TailLatch.IsMinOpen ? asCG.TailLatch.RotMax : asCG.TailLatch.RotMin;
                    asCG.TailLatch.m_curRot = val;
                    asCG.TailLatch.m_tarRot = val;
                    asCG.TailLatch.transform.localEulerAngles = new Vector3(0f, val, 0f);
                    asCG.TailLatch.LState = CarlGustafLatch.CGLatchState.Closed;
                }
            }
            else if (newData[5] == 1) // Middle
            {
                if (asCG.TailLatch.LState != CarlGustafLatch.CGLatchState.Middle && asCG.TailLatch.m_hand == null)
                {
                    float val = Mathf.Lerp(asCG.TailLatch.RotMin, asCG.TailLatch.RotMax, 0.5f);
                    asCG.TailLatch.m_curRot = val;
                    asCG.TailLatch.m_tarRot = val;
                    asCG.TailLatch.transform.localEulerAngles = new Vector3(0f, val, 0f);
                    asCG.TailLatch.LState = CarlGustafLatch.CGLatchState.Middle;
                }
            }
            else if (asCG.TailLatch.LState != CarlGustafLatch.CGLatchState.Open && asCG.TailLatch.m_hand == null)
            {
                float val = asCG.TailLatch.IsMinOpen ? asCG.TailLatch.RotMin : asCG.TailLatch.RotMax;
                asCG.TailLatch.m_curRot = val;
                asCG.TailLatch.m_tarRot = val;
                asCG.TailLatch.transform.localEulerAngles = new Vector3(0f, val, 0f);
                asCG.TailLatch.LState = CarlGustafLatch.CGLatchState.Open;
            }

            // Set shell slide state
            if (newData[6] == 0) // In
            {
                if (asCG.ShellInsertEject.CSState != CarlGustafShellInsertEject.ChamberSlideState.In && asCG.ShellInsertEject.m_hand == null)
                {
                    asCG.ShellInsertEject.m_curZ = asCG.ShellInsertEject.ChamberPoint_Forward.localPosition.z;
                    asCG.ShellInsertEject.m_tarZ = asCG.ShellInsertEject.ChamberPoint_Forward.localPosition.z;
                    asCG.Chamber.transform.localPosition = new Vector3(asCG.Chamber.transform.localPosition.x, asCG.Chamber.transform.localPosition.y, asCG.ShellInsertEject.ChamberPoint_Forward.localPosition.z);
                    asCG.ShellInsertEject.CSState = CarlGustafShellInsertEject.ChamberSlideState.In;
                }
            }
            else if (newData[6] == 1) // Middle
            {
                if (asCG.ShellInsertEject.CSState != CarlGustafShellInsertEject.ChamberSlideState.Middle && asCG.ShellInsertEject.m_hand == null)
                {
                    float val = Mathf.Lerp(asCG.TailLatch.RotMin, asCG.TailLatch.RotMax, 0.5f);
                    asCG.ShellInsertEject.m_curZ = val;
                    asCG.ShellInsertEject.m_tarZ = val;
                    asCG.Chamber.transform.localPosition = new Vector3(asCG.Chamber.transform.localPosition.x, asCG.Chamber.transform.localPosition.y, val);
                    asCG.ShellInsertEject.CSState = CarlGustafShellInsertEject.ChamberSlideState.Middle;
                }
            }
            else if (asCG.ShellInsertEject.CSState != CarlGustafShellInsertEject.ChamberSlideState.Out && asCG.ShellInsertEject.m_hand == null)
            {
                asCG.ShellInsertEject.m_curZ = asCG.ShellInsertEject.ChamberPoint_Back.localPosition.z;
                asCG.ShellInsertEject.m_tarZ = asCG.ShellInsertEject.ChamberPoint_Back.localPosition.z;
                asCG.Chamber.transform.localPosition = new Vector3(asCG.Chamber.transform.localPosition.x, asCG.Chamber.transform.localPosition.y, asCG.ShellInsertEject.ChamberPoint_Back.localPosition.z);
                asCG.ShellInsertEject.CSState = CarlGustafShellInsertEject.ChamberSlideState.Out;
            }

            // Set lock latch state
            if (newData[7] == 0) // Closed
            {
                if (asCG.TailLatch.RestrictingLatch.LState != CarlGustafLatch.CGLatchState.Closed && asCG.TailLatch.RestrictingLatch.m_hand == null)
                {
                    float val = asCG.TailLatch.RestrictingLatch.IsMinOpen ? asCG.TailLatch.RestrictingLatch.RotMax : asCG.TailLatch.RestrictingLatch.RotMin;
                    asCG.TailLatch.RestrictingLatch.m_curRot = val;
                    asCG.TailLatch.RestrictingLatch.m_tarRot = val;
                    asCG.TailLatch.RestrictingLatch.transform.localEulerAngles = new Vector3(0f, val, 0f);
                    asCG.TailLatch.RestrictingLatch.LState = CarlGustafLatch.CGLatchState.Closed;
                }
            }
            else if (newData[7] == 1) // Middle
            {
                if (asCG.TailLatch.RestrictingLatch.LState != CarlGustafLatch.CGLatchState.Middle && asCG.TailLatch.RestrictingLatch.m_hand == null)
                {
                    float val = Mathf.Lerp(asCG.TailLatch.RestrictingLatch.RotMin, asCG.TailLatch.RestrictingLatch.RotMax, 0.5f);
                    asCG.TailLatch.RestrictingLatch.m_curRot = val;
                    asCG.TailLatch.RestrictingLatch.m_tarRot = val;
                    asCG.TailLatch.RestrictingLatch.transform.localEulerAngles = new Vector3(0f, val, 0f);
                    asCG.TailLatch.RestrictingLatch.LState = CarlGustafLatch.CGLatchState.Middle;
                }
            }
            else if (asCG.TailLatch.RestrictingLatch.LState != CarlGustafLatch.CGLatchState.Open && asCG.TailLatch.RestrictingLatch.m_hand == null)
            {
                float val = asCG.TailLatch.RestrictingLatch.IsMinOpen ? asCG.TailLatch.RestrictingLatch.RotMin : asCG.TailLatch.RestrictingLatch.RotMax;
                asCG.TailLatch.RestrictingLatch.m_curRot = val;
                asCG.TailLatch.RestrictingLatch.m_tarRot = val;
                asCG.TailLatch.RestrictingLatch.transform.localEulerAngles = new Vector3(0f, val, 0f);
                asCG.TailLatch.RestrictingLatch.LState = CarlGustafLatch.CGLatchState.Open;
            }

            itemData.data = newData;

            return modified;
        }

        private void ChamberCarlGustafRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            CarlGustaf asCG = dataObject as CarlGustaf;

            ++ChamberPatch.chamberSkip;
            if (((int)roundClass) == -1)
            {
                asCG.Chamber.SetRound(null);
            }
            else
            {
                asCG.Chamber.SetRound(roundClass, asCG.Chamber.transform.position, asCG.Chamber.transform.rotation);
            }
            --ChamberPatch.chamberSkip;
        }

        private bool FireCarlGustaf(int chamberIndex)
        {
            (dataObject as CarlGustaf).TryToFire();
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

            if (itemData.data == null)
            {
                itemData.data = new byte[asLAF.GetIntegratedAttachableFirearm() == null ? 10 : 14];
                modified = true;
            }

            // Write chamber round
            byte preval0 = itemData.data[0];
            byte preval1 = itemData.data[1];
            byte preval2 = itemData.data[2];
            byte preval3 = itemData.data[3];

            if (asLAF.Chamber.GetRound() == null || asLAF.Chamber.IsSpent || asLAF.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 2);
            }
            else
            {
                BitConverter.GetBytes((short)asLAF.Chamber.GetRound().RoundType).CopyTo(itemData.data, 0);
                BitConverter.GetBytes((short)asLAF.Chamber.GetRound().RoundClass).CopyTo(itemData.data, 2);
            }

            modified |= (preval0 != itemData.data[0] || preval1 != itemData.data[1] || preval2 != itemData.data[2] || preval3 != itemData.data[3]);

            // Write hammer state
            preval0 = itemData.data[4];

            itemData.data[4] = asLAF.IsHammerCocked ? (byte)1 : (byte)0;

            modified |= preval0 != itemData.data[4];

            if (asLAF.UsesSecondChamber)
            {
                // Write chamber2 round
                preval0 = itemData.data[5];
                preval1 = itemData.data[6];
                preval2 = itemData.data[7];
                preval3 = itemData.data[8];

                if (asLAF.Chamber2.GetRound() == null || asLAF.Chamber2.IsSpent || asLAF.Chamber2.GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 7);
                }
                else
                {
                    BitConverter.GetBytes((short)asLAF.Chamber2.GetRound().RoundType).CopyTo(itemData.data, 5);
                    BitConverter.GetBytes((short)asLAF.Chamber2.GetRound().RoundClass).CopyTo(itemData.data, 7);
                }

                modified |= (preval0 != itemData.data[5] || preval1 != itemData.data[6] || preval2 != itemData.data[7] || preval3 != itemData.data[8]);

                // Write hammer2 state
                preval0 = itemData.data[9];

                itemData.data[9] = asLAF.m_isHammerCocked2 ? (byte)1 : (byte)0;

                modified |= preval0 != itemData.data[9];
            }

            if (asLAF.GetIntegratedAttachableFirearm() != null)
            {
                preval0 = itemData.data[10];
                preval1 = itemData.data[11];
                preval2 = itemData.data[12];
                preval3 = itemData.data[13];

                // Write chambered round class
                FVRFireArmChamber integratedChamber = attachableFirearmGetChamberFunc();
                if (integratedChamber.GetRound() == null || integratedChamber.IsSpent || integratedChamber.GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 12);
                }
                else
                {
                    BitConverter.GetBytes((short)integratedChamber.GetRound().RoundType).CopyTo(itemData.data, 10);
                    BitConverter.GetBytes((short)integratedChamber.GetRound().RoundClass).CopyTo(itemData.data, 12);
                }

                modified |= (preval0 != itemData.data[10] || preval1 != itemData.data[11] || preval2 != itemData.data[12] || preval3 != itemData.data[13]);
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
            asLAF.m_isHammerCocked = newData[4] == 1;

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
                asLAF.m_isHammerCocked2 = newData[9] == 1;
            }

            // Set integrated firearm chamber
            if (asLAF.GetIntegratedAttachableFirearm() != null)
            {
                chamberTypeIndex = BitConverter.ToInt16(newData, newData.Length - 4);
                chamberClassIndex = BitConverter.ToInt16(newData, newData.Length - 2);
                FVRFireArmChamber integratedChamber = attachableFirearmGetChamberFunc();
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (integratedChamber.GetRound() != null)
                    {
                        ++ChamberPatch.chamberSkip;
                        integratedChamber.SetRound(null, false);
                        --ChamberPatch.chamberSkip;
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (integratedChamber.GetRound() == null || integratedChamber.GetRound().RoundClass != roundClass)
                    {
                        if (integratedChamber.RoundType == roundType)
                        {
                            ++ChamberPatch.chamberSkip;
                            integratedChamber.SetRound(roundClass, integratedChamber.transform.position, integratedChamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                        }
                        else
                        {
                            FireArmRoundType prevRoundType = integratedChamber.RoundType;
                            integratedChamber.RoundType = roundType;
                            ++ChamberPatch.chamberSkip;
                            integratedChamber.SetRound(roundClass, integratedChamber.transform.position, integratedChamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                            integratedChamber.RoundType = prevRoundType;
                        }
                        modified = true;
                    }
                }
            }

            itemData.data = newData;

            return modified;
        }

        private int GetLeverActionFirearmChamberIndex(FVRFireArmChamber chamber)
        {
            LeverActionFirearm asLAF = dataObject as LeverActionFirearm;

            if (chamber == null)
            {
                return -1;
            }

            if (asLAF.Chamber == chamber)
            {
                return 0;
            }
            else if (asLAF.Chamber2 == chamber)
            {
                return 1;
            }
            else if (asLAF.GetIntegratedAttachableFirearm() != null && attachableFirearmGetChamberFunc() == chamber)
            {
                return 2;
            }

            return -1;
        }

        private void ChamberLeverActionFirearmRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            LeverActionFirearm asLAF = dataObject as LeverActionFirearm;

            if (chamberIndex == 0)
            {
                ++ChamberPatch.chamberSkip;
                if (((int)roundClass) == -1)
                {
                    asLAF.Chamber.SetRound(null);
                }
                else
                {
                    asLAF.Chamber.SetRound(roundClass, asLAF.Chamber.transform.position, asLAF.Chamber.transform.rotation);
                }
                --ChamberPatch.chamberSkip;
            }
            else if (chamberIndex == 1)
            {
                ++ChamberPatch.chamberSkip;
                if (((int)roundClass) == -1)
                {
                    asLAF.Chamber2.SetRound(null);
                }
                else
                {
                    asLAF.Chamber2.SetRound(roundClass, asLAF.Chamber2.transform.position, asLAF.Chamber2.transform.rotation);
                }
                --ChamberPatch.chamberSkip;
            }
            else if (chamberIndex == 2)
            {
                ++ChamberPatch.chamberSkip;
                if (((int)roundClass) == -1)
                {
                    attachableFirearmGetChamberFunc().SetRound(null);
                }
                else
                {
                    attachableFirearmGetChamberFunc().SetRound(roundClass, asLAF.Chamber2.transform.position, asLAF.Chamber2.transform.rotation);
                }
                --ChamberPatch.chamberSkip;
            }
        }

        private bool UpdateDerringer()
        {
            Derringer asDerringer = dataObject as Derringer;
            bool modified = false;

            int necessarySize = asDerringer.Barrels.Count * 4 + 3;

            if (itemData.data == null)
            {
                itemData.data = new byte[necessarySize];
                modified = true;
            }

            // Write hammer state
            byte preval0 = itemData.data[0];

            itemData.data[0] = asDerringer.IsExternalHammerCocked() ? (byte)1 : (byte)0;

            modified |= preval0 != itemData.data[0];

            // Write hingeState
            preval0 = itemData.data[1];
            itemData.data[1] = (byte)asDerringer.m_hingeState;
            modified |= preval0 != itemData.data[1];

            if (integratedLaser != null)
            {
                preval0 = itemData.data[2];
                itemData.data[2] = integratedLaser.m_isOn ? (byte)1 : (byte)0;
                modified |= preval0 != itemData.data[2];
            }

            // Write chambered rounds
            byte preval1;
            byte preval2;
            byte preval3;
            for (int i = 0; i < asDerringer.Barrels.Count; ++i)
            {
                // Write chambered round
                int firstIndex = i * 4 + 3;
                preval0 = itemData.data[firstIndex];
                preval1 = itemData.data[firstIndex + 1];
                preval2 = itemData.data[firstIndex + 2];
                preval3 = itemData.data[firstIndex + 3];

                if (asDerringer.Barrels[i].Chamber.GetRound() == null || asDerringer.Barrels[i].Chamber.IsSpent || asDerringer.Barrels[i].Chamber.GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(itemData.data, firstIndex + 2);
                }
                else
                {
                    BitConverter.GetBytes((short)asDerringer.Barrels[i].Chamber.GetRound().RoundType).CopyTo(itemData.data, firstIndex);
                    BitConverter.GetBytes((short)asDerringer.Barrels[i].Chamber.GetRound().RoundClass).CopyTo(itemData.data, firstIndex + 2);
                }

                modified |= (preval0 != itemData.data[firstIndex] || preval1 != itemData.data[firstIndex + 1] || preval2 != itemData.data[firstIndex + 2] || preval3 != itemData.data[firstIndex + 3]);
            }

            return modified;
        }

        private bool UpdateGivenDerringer(byte[] newData)
        {
            bool modified = false;
            Derringer asDerringer = dataObject as Derringer;

            if (itemData.data == null)
            {
                modified = true;

                // Set hammer state
                if (newData[0] == 1 && !asDerringer.IsExternalHammerCocked())
                {
                    asDerringer.CockHammer();
                }
                else if (newData[0] == 0 && asDerringer.IsExternalHammerCocked())
                {
                    asDerringer.m_isExternalHammerCocked = false;
                }
            }
            else
            {
                // Set hammer state
                if (newData[0] == 1 && !asDerringer.IsExternalHammerCocked())
                {
                    asDerringer.CockHammer();
                    modified = true;
                }
                else if (newData[0] == 0 && asDerringer.IsExternalHammerCocked())
                {
                    asDerringer.m_isExternalHammerCocked = false;
                    modified = true;
                }
            }

            // Set hingeState
            Derringer.HingeState newHingeState = (Derringer.HingeState)newData[1];
            if (asDerringer.m_hingeState == Derringer.HingeState.Open
                && (newHingeState == Derringer.HingeState.Closing || newHingeState == Derringer.HingeState.Closed))
            {
                asDerringer.Latch();
                asDerringer.m_hingeState = Derringer.HingeState.Closed;
                asDerringer.m_hingeLerp = 0;
                asDerringer.PlayAudioEvent(FirearmAudioEventType.BreachClose);
                asDerringer.SetAnimatedComponent(asDerringer.Hinge, Mathf.Lerp(asDerringer.HingeValues.x, asDerringer.HingeValues.y, asDerringer.m_hingeLerp), asDerringer.Hinge_InterpStyle, asDerringer.Hinge_Axis);
                if (asDerringer.HasLatchPiece)
                {
                    asDerringer.SetAnimatedComponent(asDerringer.LatchPiece, Mathf.Lerp(asDerringer.LatchValues.x, asDerringer.LatchValues.y, asDerringer.m_hingeLerp), asDerringer.Latch_InterpStyle, asDerringer.Latch_Axis);
                }
                if (asDerringer.HasExtractor)
                {
                    asDerringer.SetAnimatedComponent(asDerringer.Extractor, Mathf.Lerp(asDerringer.Extractor_Values.x, asDerringer.Extractor_Values.y, asDerringer.m_hingeLerp), asDerringer.Extractor_InterpStyle, asDerringer.Extractor_Axis);
                }
                modified = true;
            }
            else if (asDerringer.m_hingeState == Derringer.HingeState.Closed
                     && (newHingeState == Derringer.HingeState.Opening || newHingeState == Derringer.HingeState.Open))
            {
                asDerringer.Unlatch();
                asDerringer.m_hingeState = Derringer.HingeState.Open;
                asDerringer.m_hingeLerp = 1;
                for (int j = 0; j < asDerringer.Barrels.Count; j++)
                {
                    asDerringer.Barrels[j].Chamber.IsAccessible = true;
                }
                asDerringer.SetAnimatedComponent(asDerringer.Hinge, Mathf.Lerp(asDerringer.HingeValues.x, asDerringer.HingeValues.y, asDerringer.m_hingeLerp), asDerringer.Hinge_InterpStyle, asDerringer.Hinge_Axis);
                if (asDerringer.HasLatchPiece)
                {
                    asDerringer.SetAnimatedComponent(asDerringer.LatchPiece, Mathf.Lerp(asDerringer.LatchValues.x, asDerringer.LatchValues.y, asDerringer.m_hingeLerp), asDerringer.Latch_InterpStyle, asDerringer.Latch_Axis);
                }
                if (asDerringer.HasExtractor)
                {
                    asDerringer.SetAnimatedComponent(asDerringer.Extractor, Mathf.Lerp(asDerringer.Extractor_Values.x, asDerringer.Extractor_Values.y, asDerringer.m_hingeLerp), asDerringer.Extractor_InterpStyle, asDerringer.Extractor_Axis);
                }
                modified = true;
            }

            if (integratedLaser != null)
            {
                if (newData[2] == 1 && !integratedLaser.m_isOn)
                {
                    integratedLaser.TurnOn();
                }
                else if (newData[2] == 0 && integratedLaser.m_isOn)
                {
                    integratedLaser.TurnOff();
                }
            }

            // Set barrels
            for (int i = 0; i < asDerringer.Barrels.Count; ++i)
            {
                int firstIndex = i * 4 + 3;
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

            itemData.data = newData;

            return modified;
        }

        private int GetDerringerChamberIndex(FVRFireArmChamber chamber)
        {
            Derringer asDerringer = dataObject as Derringer;
            List<Derringer.DBarrel> barrels = asDerringer.Barrels;

            if (chamber == null)
            {
                return -1;
            }

            for (int i = 0; i < barrels.Count; ++i)
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

            if (itemData.data == null)
            {
                itemData.data = new byte[asBreakActionWeapon.GetIntegratedAttachableFirearm() == null ? necessarySize : necessarySize + 4];
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
                preval0 = itemData.data[firstIndex];
                preval1 = itemData.data[firstIndex + 1];
                preval2 = itemData.data[firstIndex + 2];
                preval3 = itemData.data[firstIndex + 3];

                if (asBreakActionWeapon.Barrels[i].Chamber.GetRound() == null || asBreakActionWeapon.Barrels[i].Chamber.IsSpent || asBreakActionWeapon.Barrels[i].Chamber.GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(itemData.data, firstIndex + 2);
                }
                else
                {
                    BitConverter.GetBytes((short)asBreakActionWeapon.Barrels[i].Chamber.GetRound().RoundType).CopyTo(itemData.data, firstIndex);
                    BitConverter.GetBytes((short)asBreakActionWeapon.Barrels[i].Chamber.GetRound().RoundClass).CopyTo(itemData.data, firstIndex + 2);
                }

                modified |= (preval0 != itemData.data[firstIndex] || preval1 != itemData.data[firstIndex + 1] || preval2 != itemData.data[firstIndex + 2] || preval3 != itemData.data[firstIndex + 3]);

                // Write hammer state
                preval0 = itemData.data[firstIndex + 4];

                itemData.data[firstIndex + 4] = asBreakActionWeapon.Barrels[i].m_isHammerCocked ? (byte)1 : (byte)0;

                modified |= preval0 != itemData.data[firstIndex + 4];
            }

            if (asBreakActionWeapon.GetIntegratedAttachableFirearm() != null)
            {
                preval0 = itemData.data[necessarySize];
                preval1 = itemData.data[necessarySize + 1];
                preval2 = itemData.data[necessarySize + 2];
                preval3 = itemData.data[necessarySize + 3];

                // Write chambered round class
                FVRFireArmChamber integratedChamber = attachableFirearmGetChamberFunc();
                if (integratedChamber.GetRound() == null || integratedChamber.IsSpent || integratedChamber.GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(itemData.data, necessarySize + 2);
                }
                else
                {
                    BitConverter.GetBytes((short)integratedChamber.GetRound().RoundType).CopyTo(itemData.data, necessarySize);
                    BitConverter.GetBytes((short)integratedChamber.GetRound().RoundClass).CopyTo(itemData.data, necessarySize + 2);
                }

                modified |= (preval0 != itemData.data[necessarySize] || preval1 != itemData.data[necessarySize + 1] || preval2 != itemData.data[necessarySize + 2] || preval3 != itemData.data[necessarySize + 3]);
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

            // Set integrated firearm chamber
            if (asBreakActionWeapon.GetIntegratedAttachableFirearm() != null)
            {
                short chamberTypeIndex = BitConverter.ToInt16(newData, newData.Length - 4);
                short chamberClassIndex = BitConverter.ToInt16(newData, newData.Length - 2);
                FVRFireArmChamber integratedChamber = attachableFirearmGetChamberFunc();
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (integratedChamber.GetRound() != null)
                    {
                        ++ChamberPatch.chamberSkip;
                        integratedChamber.SetRound(null, false);
                        --ChamberPatch.chamberSkip;
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (integratedChamber.GetRound() == null || integratedChamber.GetRound().RoundClass != roundClass)
                    {
                        if (integratedChamber.RoundType == roundType)
                        {
                            ++ChamberPatch.chamberSkip;
                            integratedChamber.SetRound(roundClass, integratedChamber.transform.position, integratedChamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                        }
                        else
                        {
                            FireArmRoundType prevRoundType = integratedChamber.RoundType;
                            integratedChamber.RoundType = roundType;
                            ++ChamberPatch.chamberSkip;
                            integratedChamber.SetRound(roundClass, integratedChamber.transform.position, integratedChamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                            integratedChamber.RoundType = prevRoundType;
                        }
                        modified = true;
                    }
                }
            }

            itemData.data = newData;

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

            if (asBreakActionWeapon.GetIntegratedAttachableFirearm() != null && attachableFirearmGetChamberFunc() == chamber)
            {
                return barrels.Length;
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
            if (chamberIndex == asBreakActionWeapon.Barrels.Length)
            {
                if (((int)roundClass) == -1)
                {
                    attachableFirearmGetChamberFunc().SetRound(null);
                }
                else
                {
                    attachableFirearmGetChamberFunc().SetRound(roundClass, asBreakActionWeapon.GetChambers()[chamberIndex].transform.position, asBreakActionWeapon.GetChambers()[chamberIndex].transform.rotation);
                }
            }
            else
            {
                if (((int)roundClass) == -1)
                {
                    asBreakActionWeapon.GetChambers()[chamberIndex].SetRound(null);
                }
                else
                {
                    asBreakActionWeapon.GetChambers()[chamberIndex].SetRound(roundClass, asBreakActionWeapon.GetChambers()[chamberIndex].transform.position, asBreakActionWeapon.GetChambers()[chamberIndex].transform.rotation);
                }
            }
            --ChamberPatch.chamberSkip;
        }

        private bool UpdateBAP()
        {
            BAP asBAP = dataObject as BAP;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[asBAP.GetIntegratedAttachableFirearm() == null ? 6 : 10];
                modified = true;
            }

            byte preval = itemData.data[0];

            // Write fire mode index
            itemData.data[0] = (byte)asBAP.m_fireSelectorMode;

            modified |= preval != itemData.data[0];

            preval = itemData.data[1];

            // Write hammer state
            itemData.data[1] = asBAP.m_isHammerCocked ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[1];

            preval = itemData.data[2];
            byte preval0 = itemData.data[3];
            byte preval1 = itemData.data[4];
            byte preval2 = itemData.data[5];

            // Write chambered round class
            if (asBAP.Chamber.GetRound() == null || asBAP.Chamber.IsSpent || asBAP.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 4);
            }
            else
            {
                BitConverter.GetBytes((short)asBAP.Chamber.GetRound().RoundType).CopyTo(itemData.data, 2);
                BitConverter.GetBytes((short)asBAP.Chamber.GetRound().RoundClass).CopyTo(itemData.data, 4);
            }

            modified |= (preval != itemData.data[2] || preval0 != itemData.data[3] || preval1 != itemData.data[4] || preval2 != itemData.data[5]);

            if (asBAP.GetIntegratedAttachableFirearm() != null)
            {
                preval = itemData.data[6];
                preval0 = itemData.data[7];
                preval1 = itemData.data[8];
                preval2 = itemData.data[9];

                // Write chambered round class
                FVRFireArmChamber integratedChamber = attachableFirearmGetChamberFunc();
                if (integratedChamber.GetRound() == null || integratedChamber.IsSpent || integratedChamber.GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 8);
                }
                else
                {
                    BitConverter.GetBytes((short)integratedChamber.GetRound().RoundType).CopyTo(itemData.data, 6);
                    BitConverter.GetBytes((short)integratedChamber.GetRound().RoundClass).CopyTo(itemData.data, 8);
                }

                modified |= (preval != itemData.data[6] || preval0 != itemData.data[7] || preval1 != itemData.data[8] || preval2 != itemData.data[9]);
            }

            return modified;
        }

        private bool UpdateGivenBAP(byte[] newData)
        {
            bool modified = false;
            BAP asBAP = dataObject as BAP;

            if (itemData.data == null)
            {
                modified = true;

                // Set fire select mode
                asBAP.m_fireSelectorMode = newData[0];
            }
            else
            {
                if (itemData.data[0] != newData[0])
                {
                    // Set fire select mode
                    asBAP.m_fireSelectorMode = newData[0];
                    modified = true;
                }
            }

            // Set hammer state
            if (newData[1] == 0)
            {
                if (asBAP.m_isHammerCocked)
                {
                    asBAP.m_isHammerCocked = false;
                    modified = true;
                }
            }
            else // Hammer should be cocked
            {
                if (!asBAP.m_isHammerCocked)
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

            // Set integrated firearm chamber
            if (asBAP.GetIntegratedAttachableFirearm() != null)
            {
                chamberTypeIndex = BitConverter.ToInt16(newData, newData.Length - 4);
                chamberClassIndex = BitConverter.ToInt16(newData, newData.Length - 2);
                FVRFireArmChamber integratedChamber = attachableFirearmGetChamberFunc();
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (integratedChamber.GetRound() != null)
                    {
                        ++ChamberPatch.chamberSkip;
                        integratedChamber.SetRound(null, false);
                        --ChamberPatch.chamberSkip;
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (integratedChamber.GetRound() == null || integratedChamber.GetRound().RoundClass != roundClass)
                    {
                        if (integratedChamber.RoundType == roundType)
                        {
                            ++ChamberPatch.chamberSkip;
                            integratedChamber.SetRound(roundClass, integratedChamber.transform.position, integratedChamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                        }
                        else
                        {
                            FireArmRoundType prevRoundType = integratedChamber.RoundType;
                            integratedChamber.RoundType = roundType;
                            ++ChamberPatch.chamberSkip;
                            integratedChamber.SetRound(roundClass, integratedChamber.transform.position, integratedChamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                            integratedChamber.RoundType = prevRoundType;
                        }
                        modified = true;
                    }
                }
            }

            itemData.data = newData;

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
            if (chamberIndex == 1)
            {
                if (((int)roundClass) == -1)
                {
                    attachableFirearmGetChamberFunc().SetRound(null);
                }
                else
                {
                    attachableFirearmGetChamberFunc().SetRound(roundClass, asBAP.Chamber.transform.position, asBAP.Chamber.transform.rotation);
                }
            }
            else
            {
                if (((int)roundClass) == -1)
                {
                    asBAP.Chamber.SetRound(null);
                }
                else
                {
                    asBAP.Chamber.SetRound(roundClass, asBAP.Chamber.transform.position, asBAP.Chamber.transform.rotation);
                }
            }
            --ChamberPatch.chamberSkip;
        }

        private int GetBAPChamberIndex(FVRFireArmChamber chamber)
        {
            BAP asBAP = dataObject as BAP;

            if (asBAP.GetIntegratedAttachableFirearm() == null)
            {
                return 0;
            }
            else
            {
                return chamber == asBAP.Chamber ? 0 : 1;
            }
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

            if (itemData.data == null)
            {
                itemData.data = new byte[asRS.GetIntegratedAttachableFirearm() == null ? necessarySize : necessarySize + 4];
                modified = true;
            }

            byte preval0 = itemData.data[0];

            // Write cur chamber
            itemData.data[0] = (byte)asRS.CurChamber;

            modified |= preval0 != itemData.data[0];

            preval0 = itemData.data[1];

            // Write cylLoaded
            itemData.data[1] = asRS.CylinderLoaded ? (byte)1 : (byte)0;

            modified |= preval0 != itemData.data[1];

            // Write chambered rounds
            byte preval1;
            byte preval2;
            byte preval3;
            for (int i = 0; i < asRS.Chambers.Length; ++i)
            {
                int firstIndex = i * 4 + 2;
                preval0 = itemData.data[firstIndex];
                preval1 = itemData.data[firstIndex + 1];
                preval2 = itemData.data[firstIndex + 2];
                preval3 = itemData.data[firstIndex + 3];

                if (asRS.Chambers[i].GetRound() == null || asRS.Chambers[i].IsSpent || asRS.Chambers[i].GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(itemData.data, firstIndex + 2);
                }
                else
                {
                    BitConverter.GetBytes((short)asRS.Chambers[i].GetRound().RoundType).CopyTo(itemData.data, firstIndex);
                    BitConverter.GetBytes((short)asRS.Chambers[i].GetRound().RoundClass).CopyTo(itemData.data, firstIndex + 2);
                }

                modified |= (preval0 != itemData.data[firstIndex] || preval1 != itemData.data[firstIndex + 1] || preval2 != itemData.data[firstIndex + 2] || preval3 != itemData.data[firstIndex + 3]);
            }

            if (asRS.GetIntegratedAttachableFirearm() != null)
            {
                preval0 = itemData.data[necessarySize];
                preval1 = itemData.data[necessarySize + 1];
                preval2 = itemData.data[necessarySize + 2];
                preval3 = itemData.data[necessarySize + 3];

                // Write chambered round class
                FVRFireArmChamber integratedChamber = attachableFirearmGetChamberFunc();
                if (integratedChamber.GetRound() == null || integratedChamber.IsSpent || integratedChamber.GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(itemData.data, necessarySize + 2);
                }
                else
                {
                    BitConverter.GetBytes((short)integratedChamber.GetRound().RoundType).CopyTo(itemData.data, necessarySize);
                    BitConverter.GetBytes((short)integratedChamber.GetRound().RoundClass).CopyTo(itemData.data, necessarySize + 2);
                }

                modified |= (preval0 != itemData.data[necessarySize] || preval1 != itemData.data[necessarySize + 1] || preval2 != itemData.data[necessarySize + 2] || preval3 != itemData.data[necessarySize + 3]);
            }

            return modified;
        }

        private bool UpdateGivenRevolvingShotgun(byte[] newData)
        {
            bool modified = false;
            RevolvingShotgun asRS = dataObject as RevolvingShotgun;

            if (itemData.data == null)
            {
                modified = true;

                // Set cur chamber
                asRS.CurChamber = newData[0];

                // Set cyl loaded
                bool newCylLoaded = newData[1] == 1;
                if (newCylLoaded && !asRS.CylinderLoaded)
                {
                    // Load cylinder, chambers will be updated separately
                    asRS.ProxyCylinder.gameObject.SetActive(true);
                    asRS.PlayAudioEvent(FirearmAudioEventType.MagazineIn);
                    asRS.CurChamber = 0;
                    asRS.ProxyCylinder.localRotation = asRS.GetLocalRotationFromCylinder(0);
                }
                else if (!newCylLoaded && asRS.CylinderLoaded)
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
                if (itemData.data[0] != newData[0])
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

            // Set integrated firearm chamber
            if (asRS.GetIntegratedAttachableFirearm() != null)
            {
                short chamberTypeIndex = BitConverter.ToInt16(newData, newData.Length - 4);
                short chamberClassIndex = BitConverter.ToInt16(newData, newData.Length - 2);
                FVRFireArmChamber integratedChamber = attachableFirearmGetChamberFunc();
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (integratedChamber.GetRound() != null)
                    {
                        ++ChamberPatch.chamberSkip;
                        integratedChamber.SetRound(null, false);
                        --ChamberPatch.chamberSkip;
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (integratedChamber.GetRound() == null || integratedChamber.GetRound().RoundClass != roundClass)
                    {
                        if (integratedChamber.RoundType == roundType)
                        {
                            ++ChamberPatch.chamberSkip;
                            integratedChamber.SetRound(roundClass, integratedChamber.transform.position, integratedChamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                        }
                        else
                        {
                            FireArmRoundType prevRoundType = integratedChamber.RoundType;
                            integratedChamber.RoundType = roundType;
                            ++ChamberPatch.chamberSkip;
                            integratedChamber.SetRound(roundClass, integratedChamber.transform.position, integratedChamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                            integratedChamber.RoundType = prevRoundType;
                        }
                        modified = true;
                    }
                }
            }

            itemData.data = newData;

            return modified;
        }

        private int GetRevolvingShotgunChamberIndex(FVRFireArmChamber chamber)
        {
            RevolvingShotgun asRS = dataObject as RevolvingShotgun;
            FVRFireArmChamber[] chambers = asRS.Chambers;

            if (chamber == null)
            {
                return -1;
            }

            if (asRS.GetIntegratedAttachableFirearm() != null && attachableFirearmGetChamberFunc() == chamber)
            {
                return chambers.Length;
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

        private void ChamberRevolvingShotgunRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            RevolvingShotgun asRS = dataObject as RevolvingShotgun;

            ++ChamberPatch.chamberSkip;
            if (chamberIndex == asRS.GetChambers().Count)
            {
                if (((int)roundClass) == -1)
                {
                    attachableFirearmGetChamberFunc().SetRound(null);
                }
                else
                {
                    attachableFirearmGetChamberFunc().SetRound(roundClass, asRS.GetChambers()[chamberIndex].transform.position, asRS.GetChambers()[chamberIndex].transform.rotation);
                }
            }
            else
            {
                if (((int)roundClass) == -1)
                {
                    asRS.GetChambers()[chamberIndex].SetRound(null);
                }
                else
                {
                    asRS.GetChambers()[chamberIndex].SetRound(roundClass, asRS.GetChambers()[chamberIndex].transform.position, asRS.GetChambers()[chamberIndex].transform.rotation);
                }
            }
            --ChamberPatch.chamberSkip;
        }

        private bool UpdateRevolver()
        {
            Revolver asRevolver = dataObject as Revolver;
            bool modified = false;

            int necessarySize = asRevolver.Cylinder.numChambers * 4 + 1;

            if (itemData.data == null)
            {
                itemData.data = new byte[asRevolver.GetIntegratedAttachableFirearm() == null ? necessarySize : necessarySize + 4];
                modified = true;
            }

            byte preval0 = itemData.data[0];

            // Write cur chamber
            itemData.data[0] = (byte)asRevolver.CurChamber;

            modified |= preval0 != itemData.data[0];

            // Write chambered rounds
            byte preval1;
            byte preval2;
            byte preval3;
            for (int i = 0; i < asRevolver.Chambers.Length; ++i)
            {
                int firstIndex = i * 4 + 1;
                preval0 = itemData.data[firstIndex];
                preval1 = itemData.data[firstIndex + 1];
                preval2 = itemData.data[firstIndex + 2];
                preval3 = itemData.data[firstIndex + 3];

                if (asRevolver.Chambers[i].GetRound() == null || asRevolver.Chambers[i].IsSpent || asRevolver.Chambers[i].GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(itemData.data, firstIndex + 2);
                }
                else
                {
                    BitConverter.GetBytes((short)asRevolver.Chambers[i].GetRound().RoundType).CopyTo(itemData.data, firstIndex);
                    BitConverter.GetBytes((short)asRevolver.Chambers[i].GetRound().RoundClass).CopyTo(itemData.data, firstIndex + 2);
                }

                modified |= (preval0 != itemData.data[firstIndex] || preval1 != itemData.data[firstIndex + 1] || preval2 != itemData.data[firstIndex + 2] || preval3 != itemData.data[firstIndex + 3]);
            }

            if (asRevolver.GetIntegratedAttachableFirearm() != null)
            {
                preval0 = itemData.data[necessarySize];
                preval1 = itemData.data[necessarySize + 1];
                preval2 = itemData.data[necessarySize + 2];
                preval3 = itemData.data[necessarySize + 3];

                // Write chambered round class
                FVRFireArmChamber integratedChamber = attachableFirearmGetChamberFunc();
                if (integratedChamber.GetRound() == null || integratedChamber.IsSpent || integratedChamber.GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(itemData.data, necessarySize + 2);
                }
                else
                {
                    BitConverter.GetBytes((short)integratedChamber.GetRound().RoundType).CopyTo(itemData.data, necessarySize);
                    BitConverter.GetBytes((short)integratedChamber.GetRound().RoundClass).CopyTo(itemData.data, necessarySize + 2);
                }

                modified |= (preval0 != itemData.data[necessarySize] || preval1 != itemData.data[necessarySize + 1] || preval2 != itemData.data[necessarySize + 2] || preval3 != itemData.data[necessarySize + 3]);
            }

            return modified;
        }

        private bool UpdateGivenRevolver(byte[] newData)
        {
            bool modified = false;
            Revolver asRevolver = dataObject as Revolver;

            if (itemData.data == null)
            {
                modified = true;

                // Set cur chamber
                asRevolver.CurChamber = newData[0];
            }
            else
            {
                if (itemData.data[0] != newData[0])
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

            // Set integrated firearm chamber
            if (asRevolver.GetIntegratedAttachableFirearm() != null)
            {
                short chamberTypeIndex = BitConverter.ToInt16(newData, newData.Length - 4);
                short chamberClassIndex = BitConverter.ToInt16(newData, newData.Length - 2);
                FVRFireArmChamber integratedChamber = attachableFirearmGetChamberFunc();
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (integratedChamber.GetRound() != null)
                    {
                        ++ChamberPatch.chamberSkip;
                        integratedChamber.SetRound(null, false);
                        --ChamberPatch.chamberSkip;
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (integratedChamber.GetRound() == null || integratedChamber.GetRound().RoundClass != roundClass)
                    {
                        if (integratedChamber.RoundType == roundType)
                        {
                            ++ChamberPatch.chamberSkip;
                            integratedChamber.SetRound(roundClass, integratedChamber.transform.position, integratedChamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                        }
                        else
                        {
                            FireArmRoundType prevRoundType = integratedChamber.RoundType;
                            integratedChamber.RoundType = roundType;
                            ++ChamberPatch.chamberSkip;
                            integratedChamber.SetRound(roundClass, integratedChamber.transform.position, integratedChamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                            integratedChamber.RoundType = prevRoundType;
                        }
                        modified = true;
                    }
                }
            }

            itemData.data = newData;

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

            if (asRevolver.GetIntegratedAttachableFirearm() != null && attachableFirearmGetChamberFunc() == chamber)
            {
                return chambers.Length;
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
            if (chamberIndex == asRevolver.Chambers.Length)
            {
                if (((int)roundClass) == -1)
                {
                    attachableFirearmGetChamberFunc().SetRound(null);
                }
                else
                {
                    attachableFirearmGetChamberFunc().SetRound(roundClass, asRevolver.Chambers[chamberIndex].transform.position, asRevolver.Chambers[chamberIndex].transform.rotation);
                }
            }
            else
            {
                if (((int)roundClass) == -1)
                {
                    asRevolver.Chambers[chamberIndex].SetRound(null);
                }
                else
                {
                    asRevolver.Chambers[chamberIndex].SetRound(roundClass, asRevolver.Chambers[chamberIndex].transform.position, asRevolver.Chambers[chamberIndex].transform.rotation);
                }
            }
            --ChamberPatch.chamberSkip;
        }

        private bool UpdateM203()
        {
            M203 asM203 = dataObject as M203;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[21];
                modified = true;
            }

            byte preIndex = itemData.data[0];

            // Write attached mount index
            if (asM203.Attachment.curMount == null)
            {
                itemData.data[0] = 255;
            }
            else
            {
                // Find the mount and set it
                bool found = false;
                for (int i = 0; i < asM203.Attachment.curMount.MyObject.AttachmentMounts.Count; ++i)
                {
                    if (asM203.Attachment.curMount.MyObject.AttachmentMounts[i] == asM203.Attachment.curMount)
                    {
                        itemData.data[0] = (byte)i;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    itemData.data[0] = 255;
                }
            }
            modified |= preIndex != itemData.data[0];

            byte preval = itemData.data[1];
            byte preval0 = itemData.data[2];
            byte preval1 = itemData.data[3];
            byte preval2 = itemData.data[4];

            // Write chambered round class
            if (asM203.Chamber.GetRound() == null || asM203.Chamber.IsSpent || asM203.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 3);
            }
            else
            {
                BitConverter.GetBytes((short)asM203.Chamber.GetRound().RoundType).CopyTo(itemData.data, 1);
                BitConverter.GetBytes((short)asM203.Chamber.GetRound().RoundClass).CopyTo(itemData.data, 3);
            }

            modified |= (preval != itemData.data[1] || preval0 != itemData.data[2] || preval1 != itemData.data[3] || preval2 != itemData.data[4]);

            preval = itemData.data[5];
            preval0 = itemData.data[6];
            preval1 = itemData.data[7];
            preval2 = itemData.data[8];

            // Write mountObjectID
            BitConverter.GetBytes(mountObjectID).CopyTo(itemData.data, 5);

            modified |= (preval != itemData.data[5] || preval0 != itemData.data[6] || preval1 != itemData.data[7] || preval2 != itemData.data[8]);

            // Write mount object scale
            if(transform.parent == null)
            {
                mountObjectScale = Vector3.one;
                preval = itemData.data[9];
                preval0 = itemData.data[10];
                preval1 = itemData.data[11];
                preval2 = itemData.data[12];
                BitConverter.GetBytes(1).CopyTo(itemData.data, 9);
                modified |= (preval != itemData.data[9] || preval0 != itemData.data[10] || preval1 != itemData.data[11] || preval2 != itemData.data[12]);

                preval = itemData.data[13];
                preval0 = itemData.data[14];
                preval1 = itemData.data[15];
                preval2 = itemData.data[16];
                BitConverter.GetBytes(1).CopyTo(itemData.data, 13);
                modified |= (preval != itemData.data[13] || preval0 != itemData.data[14] || preval1 != itemData.data[15] || preval2 != itemData.data[16]);

                preval = itemData.data[17];
                preval0 = itemData.data[18];
                preval1 = itemData.data[19];
                preval2 = itemData.data[20];
                BitConverter.GetBytes(1).CopyTo(itemData.data, 17);
                modified |= (preval != itemData.data[17] || preval0 != itemData.data[18] || preval1 != itemData.data[19] || preval2 != itemData.data[20]);
            }
            else
            {
                mountObjectScale = transform.parent.localScale;
                preval = itemData.data[9];
                preval0 = itemData.data[10];
                preval1 = itemData.data[11];
                preval2 = itemData.data[12];
                BitConverter.GetBytes(transform.parent.localScale.x).CopyTo(itemData.data, 9);
                modified |= (preval != itemData.data[9] || preval0 != itemData.data[10] || preval1 != itemData.data[11] || preval2 != itemData.data[12]);

                preval = itemData.data[13];
                preval0 = itemData.data[14];
                preval1 = itemData.data[15];
                preval2 = itemData.data[16];
                BitConverter.GetBytes(transform.parent.localScale.y).CopyTo(itemData.data, 13);
                modified |= (preval != itemData.data[13] || preval0 != itemData.data[14] || preval1 != itemData.data[15] || preval2 != itemData.data[16]);

                preval = itemData.data[17];
                preval0 = itemData.data[18];
                preval1 = itemData.data[19];
                preval2 = itemData.data[20];
                BitConverter.GetBytes(transform.parent.localScale.z).CopyTo(itemData.data, 17);
                modified |= (preval != itemData.data[17] || preval0 != itemData.data[18] || preval1 != itemData.data[19] || preval2 != itemData.data[20]);
            }

            return modified;
        }

        private bool UpdateGivenM203(byte[] newData)
        {
            bool modified = itemData.data == null;
            M203 asM203 = dataObject as M203;

            byte preMountIndex = currentMountIndex;
            if (newData[0] == 255)
            {
                // Check if were waiting for mount object
                if (mountObjectID != -1 && toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                {
                    list.Remove(this);
                }

                // Should not be mounted, check if currently is
                if (asM203.Attachment.curMount != null)
                {
                    asM203.Attachment.Sensor.m_storedScaleMagnified = 1f;
                    asM203.Attachment.transform.localScale = new Vector3(1, 1, 1);
                    ++data.ignoreParentChanged;
                    asM203.Attachment.DetachFromMount();
                    --data.ignoreParentChanged;
                    currentMountIndex = 255;

                    // Detach from mount will recover rigidbody, set as kinematic if not controller
                    // TODO: Review: May not need to do this anymore due to RecoverRigidBodyPatch
                    if (data.controller != GameManager.ID)
                    {
                        Mod.SetKinematicRecursive(asM203.Attachment.transform, true);
                    }
                }
            }
            else
            {
                mountObjectID = BitConverter.ToInt32(newData, 5);
                mountObjectScale = new Vector3(BitConverter.ToSingle(newData, 9), BitConverter.ToSingle(newData, 13), BitConverter.ToSingle(newData, 17));
                if (mountObjectID != -1)
                {
                    // Detach from any mount we are still on
                    if (asM203.Attachment.curMount != null)
                    {
                        asM203.Attachment.Sensor.m_storedScaleMagnified = 1f;
                        asM203.Attachment.transform.localScale = new Vector3(1, 1, 1);
                        ++data.ignoreParentChanged;
                        asM203.Attachment.DetachFromMount();
                        --data.ignoreParentChanged;

                        // Detach from mount will recover rigidbody, set as kinematic if not controller
                        if (data.controller != GameManager.ID)
                        {
                            Mod.SetKinematicRecursive(asM203.Attachment.transform, true);
                        }
                    }
                }
                else
                {
                    // Find mount instance we want to be mounted to
                    FVRFireArmAttachmentMount mount = null;
                    TrackedItemData parentTrackedItemData = null;
                    if (ThreadManager.host)
                    {
                        parentTrackedItemData = Server.objects[mountObjectID] as TrackedItemData;
                    }
                    else
                    {
                        parentTrackedItemData = Client.objects[mountObjectID] as TrackedItemData;
                    }

                    bool addedForLater = false;
                    if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null)
                    {
                        // We want to be mounted, we have a parent
                        if (parentTrackedItemData.physicalItem.physicalItem.AttachmentMounts.Count > newData[0])
                        {
                            mount = parentTrackedItemData.physicalItem.physicalItem.AttachmentMounts[newData[0]];
                        }
                    }
                    else
                    {
                        addedForLater = true;
                        if (toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                        {
                            list.Add(this);
                        }
                        else
                        {
                            toAttachByMountObjectID.Add(mountObjectID, new List<TrackedItem>() { this });
                        }
                    }

                    // Mount could be null if the mount index corresponds to a parent we have yet to receive a change to
                    if (mount != null && asM203.Attachment.curMount != mount)
                    {
                        ++data.ignoreParentChanged;
                        if (asM203.Attachment.curMount != null)
                        {
                            asM203.Attachment.Sensor.m_storedScaleMagnified = 1f;
                            asM203.Attachment.transform.localScale = new Vector3(1, 1, 1);
                            asM203.Attachment.DetachFromMount();
                        }

                        if (CanAttachToMount(asM203.Attachment, mount))
                        {
                            // Apply mount object scale
                            if (mount.GetRootMount().ParentToThis)
                            {
                                mount.GetRootMount().transform.localScale = mountObjectScale;
                            }
                            else
                            {
                                mount.MyObject.transform.localScale = mountObjectScale;
                            }

                            if (asM203.Attachment.CanScaleToMount && mount.CanThisRescale())
                            {
                                asM203.Attachment.ScaleToMount(mount);
                            }
                            asM203.Attachment.AttachToMount(mount, true);

                            // Check if there were any children attachments waiting for us
                            if (toAttachByMountObjectID.TryGetValue(data.trackedID, out List<TrackedItem> list))
                            {
                                for (int i = 0; i < list.Count; ++i)
                                {
                                    if (list[i] != null)
                                    {
                                        list[i].data.UpdateFromData(list[i].data);
                                    }
                                }

                                toAttachByMountObjectID.Remove(data.trackedID);
                            }
                        }
                        else if(!addedForLater) // Can't attach to mount yet, probably because parent is unattached attachment, but will need to eventually
                        {
                            if (toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                            {
                                list.Add(this);
                            }
                            else
                            {
                                toAttachByMountObjectID.Add(mountObjectID, new List<TrackedItem>() { this });
                            }
                        }
                        currentMountIndex = newData[0];
                        --data.ignoreParentChanged;
                    }
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

            itemData.data = newData;

            return modified;
        }

        private void M203ChamberRound(FireArmRoundType roundType, FireArmRoundClass roundClass)
        {
            M203 asM203 = null;
            if (dataObject is AttachableFirearm)
            {
                asM203 = dataObject as M203;
            }
            else // FVRFireArm and this refers to an integrated attachable firearm
            {
                asM203 = (dataObject as FVRFireArm).GetIntegratedAttachableFirearm() as M203;
            }
            if (((int)roundClass) == -1)
            {
                ++ChamberPatch.chamberSkip;
                asM203.Chamber.SetRound(null);
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
        }

        private FVRFireArmChamber M203GetChamber()
        {
            M203 asM203 = null;
            if (dataObject is AttachableFirearm)
            {
                asM203 = dataObject as M203;
            }
            else // FVRFireArm and this refers to an integrated attachable firearm
            {
                asM203 = (dataObject as FVRFireArm).GetIntegratedAttachableFirearm() as M203;
            }
            return asM203.Chamber;
        }

        private void ChamberM203Round(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            M203 asM203 = dataObject as M203;

            ++ChamberPatch.chamberSkip;
            if (((int)roundClass) == -1)
            {
                asM203.Chamber.SetRound(null);
            }
            else
            {
                asM203.Chamber.SetRound(roundClass, asM203.Chamber.transform.position, asM203.Chamber.transform.rotation);
            }
            --ChamberPatch.chamberSkip;
        }

        private bool UpdateGP25()
        {
            GP25 asGP25 = dataObject as GP25;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[22];
                modified = true;
            }

            byte preIndex = itemData.data[0];

            // Write attached mount index
            if (asGP25.Attachment.curMount == null)
            {
                itemData.data[0] = 255;
            }
            else
            {
                // Find the mount and set it
                bool found = false;
                for (int i = 0; i < asGP25.Attachment.curMount.MyObject.AttachmentMounts.Count; ++i)
                {
                    if (asGP25.Attachment.curMount.MyObject.AttachmentMounts[i] == asGP25.Attachment.curMount)
                    {
                        itemData.data[0] = (byte)i;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    itemData.data[0] = 255;
                }
            }
            modified |= preIndex != itemData.data[0];

            byte preval = itemData.data[1];

            // Write safety
            itemData.data[1] = (byte)(asGP25.m_safetyEngaged ? 1 : 0);

            modified |= preval != itemData.data[1];

            preval = itemData.data[2];
            byte preval0 = itemData.data[3];
            byte preval1 = itemData.data[4];
            byte preval2 = itemData.data[5];

            // Write chambered round class
            if (asGP25.Chamber.GetRound() == null || asGP25.Chamber.IsSpent || asGP25.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 4);
            }
            else
            {
                BitConverter.GetBytes((short)asGP25.Chamber.GetRound().RoundType).CopyTo(itemData.data, 2);
                BitConverter.GetBytes((short)asGP25.Chamber.GetRound().RoundClass).CopyTo(itemData.data, 4);
            }

            modified |= (preval != itemData.data[2] || preval0 != itemData.data[3] || preval1 != itemData.data[4] || preval2 != itemData.data[5]);

            preval = itemData.data[6];
            preval0 = itemData.data[7];
            preval1 = itemData.data[8];
            preval2 = itemData.data[9];

            // Write mountObjectID
            BitConverter.GetBytes(mountObjectID).CopyTo(itemData.data, 6);

            modified |= (preval != itemData.data[6] || preval0 != itemData.data[7] || preval1 != itemData.data[8] || preval2 != itemData.data[9]);

            // Write mount object scale
            if (transform.parent == null)
            {
                mountObjectScale = Vector3.one;
                preval = itemData.data[10];
                preval0 = itemData.data[11];
                preval1 = itemData.data[12];
                preval2 = itemData.data[13];
                BitConverter.GetBytes(1).CopyTo(itemData.data, 10);
                modified |= (preval != itemData.data[10] || preval0 != itemData.data[11] || preval1 != itemData.data[12] || preval2 != itemData.data[13]);

                preval = itemData.data[14];
                preval0 = itemData.data[15];
                preval1 = itemData.data[16];
                preval2 = itemData.data[17];
                BitConverter.GetBytes(1).CopyTo(itemData.data, 14);
                modified |= (preval != itemData.data[14] || preval0 != itemData.data[15] || preval1 != itemData.data[16] || preval2 != itemData.data[17]);

                preval = itemData.data[18];
                preval0 = itemData.data[19];
                preval1 = itemData.data[20];
                preval2 = itemData.data[21];
                BitConverter.GetBytes(1).CopyTo(itemData.data, 18);
                modified |= (preval != itemData.data[18] || preval0 != itemData.data[19] || preval1 != itemData.data[20] || preval2 != itemData.data[21]);
            }
            else
            {
                mountObjectScale = transform.parent.localScale;
                preval = itemData.data[10];
                preval0 = itemData.data[11];
                preval1 = itemData.data[12];
                preval2 = itemData.data[13];
                BitConverter.GetBytes(transform.parent.localScale.x).CopyTo(itemData.data, 10);
                modified |= (preval != itemData.data[10] || preval0 != itemData.data[11] || preval1 != itemData.data[12] || preval2 != itemData.data[13]);

                preval = itemData.data[14];
                preval0 = itemData.data[15];
                preval1 = itemData.data[16];
                preval2 = itemData.data[17];
                BitConverter.GetBytes(transform.parent.localScale.y).CopyTo(itemData.data, 14);
                modified |= (preval != itemData.data[14] || preval0 != itemData.data[15] || preval1 != itemData.data[16] || preval2 != itemData.data[17]);

                preval = itemData.data[18];
                preval0 = itemData.data[19];
                preval1 = itemData.data[20];
                preval2 = itemData.data[21];
                BitConverter.GetBytes(transform.parent.localScale.z).CopyTo(itemData.data, 18);
                modified |= (preval != itemData.data[18] || preval0 != itemData.data[19] || preval1 != itemData.data[20] || preval2 != itemData.data[21]);
            }

            return modified;
        }

        private bool UpdateGivenGP25(byte[] newData)
        {
            bool modified = false;
            GP25 asGP25 = dataObject as GP25;

            byte preMountIndex = currentMountIndex;
            if (newData[0] == 255)
            {
                // Check if were waiting for mount object
                if (mountObjectID != -1 && toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                {
                    list.Remove(this);
                }

                // Should not be mounted, check if currently is
                if (asGP25.Attachment.curMount != null)
                {
                    asGP25.Attachment.Sensor.m_storedScaleMagnified = 1f;
                    asGP25.Attachment.transform.localScale = new Vector3(1, 1, 1);
                    ++data.ignoreParentChanged;
                    asGP25.Attachment.DetachFromMount();
                    --data.ignoreParentChanged;
                    currentMountIndex = 255;

                    // Detach from mount will recover rigidbody, set as kinematic if not controller
                    // TODO: Review: May not need to do this anymore due to RecoverRigidBodyPatch
                    if (data.controller != GameManager.ID)
                    {
                        Mod.SetKinematicRecursive(asGP25.Attachment.transform, true);
                    }
                }
            }
            else
            {
                mountObjectID = BitConverter.ToInt32(newData, 6);
                mountObjectScale = new Vector3(BitConverter.ToSingle(newData, 10), BitConverter.ToSingle(newData, 14), BitConverter.ToSingle(newData, 18));
                if (mountObjectID == -1)
                {
                    // Should not be mounted, check if currently is
                    if (asGP25.Attachment.curMount != null)
                    {
                        asGP25.Attachment.Sensor.m_storedScaleMagnified = 1f;
                        asGP25.Attachment.transform.localScale = new Vector3(1, 1, 1);
                        ++data.ignoreParentChanged;
                        asGP25.Attachment.DetachFromMount();
                        --data.ignoreParentChanged;
                        currentMountIndex = 255;

                        // Detach from mount will recover rigidbody, set as kinematic if not controller
                        // TODO: Review: May not need to do this anymore due to RecoverRigidBodyPatch
                        if (data.controller != GameManager.ID)
                        {
                            Mod.SetKinematicRecursive(asGP25.Attachment.transform, true);
                        }
                    }
                }
                else
                {
                    // Find mount instance we want to be mounted to
                    FVRFireArmAttachmentMount mount = null;
                    TrackedItemData parentTrackedItemData = null;
                    if (ThreadManager.host)
                    {
                        parentTrackedItemData = Server.objects[mountObjectID] as TrackedItemData;
                    }
                    else
                    {
                        parentTrackedItemData = Client.objects[mountObjectID] as TrackedItemData;
                    }

                    bool addedForLater = false;
                    if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null)
                    {
                        // We want to be mounted, we have a parent
                        if (parentTrackedItemData.physicalItem.physicalItem.AttachmentMounts.Count > newData[0])
                        {
                            mount = parentTrackedItemData.physicalItem.physicalItem.AttachmentMounts[newData[0]];
                        }
                    }
                    else
                    {
                        addedForLater = true;
                        if (toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                        {
                            list.Add(this);
                        }
                        else
                        {
                            toAttachByMountObjectID.Add(mountObjectID, new List<TrackedItem>() { this });
                        }
                    }

                    // Mount could be null if the mount index corresponds to a parent we have yet a receive a change to
                    if (mount != null && asGP25.Attachment.curMount != mount)
                    {
                        ++data.ignoreParentChanged;
                        if (asGP25.Attachment.curMount != null)
                        {
                            asGP25.Attachment.Sensor.m_storedScaleMagnified = 1f;
                            asGP25.Attachment.transform.localScale = new Vector3(1, 1, 1);
                            asGP25.Attachment.DetachFromMount();
                        }

                        if (CanAttachToMount(asGP25.Attachment, mount))
                        {
                            // Apply mount object scale
                            if (mount.GetRootMount().ParentToThis)
                            {
                                mount.GetRootMount().transform.localScale = mountObjectScale;
                            }
                            else
                            {
                                mount.MyObject.transform.localScale = mountObjectScale;
                            }

                            if (asGP25.Attachment.CanScaleToMount && mount.CanThisRescale())
                            {
                                asGP25.Attachment.ScaleToMount(mount);
                            }
                            asGP25.Attachment.AttachToMount(mount, true);

                            // Check if there were any children attachments waiting for us
                            if (toAttachByMountObjectID.TryGetValue(data.trackedID, out List<TrackedItem> list))
                            {
                                for (int i = 0; i < list.Count; ++i)
                                {
                                    if (list[i] != null)
                                    {
                                        list[i].data.UpdateFromData(list[i].data);
                                    }
                                }

                                toAttachByMountObjectID.Remove(data.trackedID);
                            }
                        }
                        else if(!addedForLater) // Can't attach to mount yet, probably because parent is unattached attachment, but will need to eventually
                        {
                            if (toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                            {
                                list.Add(this);
                            }
                            else
                            {
                                toAttachByMountObjectID.Add(mountObjectID, new List<TrackedItem>() { this });
                            }
                        }
                        currentMountIndex = newData[0];
                        --data.ignoreParentChanged;
                    }
                }
            }
            modified |= preMountIndex != currentMountIndex;

            if (itemData.data == null)
            {
                modified = true;

                // Set safety
                asGP25.m_safetyEngaged = newData[1] == 1;
            }
            else
            {
                if (itemData.data[1] != newData[1])
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

            itemData.data = newData;

            return modified;
        }

        private void GP25ChamberRound(FireArmRoundType roundType, FireArmRoundClass roundClass)
        {
            GP25 asGP25 = null;
            if (dataObject is AttachableFirearm)
            {
                asGP25 = dataObject as GP25;
            }
            else // FVRFireArm and this refers to an integrated attachable firearm
            {
                asGP25 = (dataObject as FVRFireArm).GetIntegratedAttachableFirearm() as GP25;
            }
            if (((int)roundClass) == -1)
            {
                ++ChamberPatch.chamberSkip;
                asGP25.Chamber.SetRound(null);
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
        }

        private FVRFireArmChamber GP25GetChamber()
        {
            GP25 asGP25 = null;
            if (dataObject is AttachableFirearm)
            {
                asGP25 = dataObject as GP25;
            }
            else // FVRFireArm and this refers to an integrated attachable firearm
            {
                asGP25 = (dataObject as FVRFireArm).GetIntegratedAttachableFirearm() as GP25;
            }
            return asGP25.Chamber;
        }

        private void ChamberGP25Round(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            GP25 asGP25 = dataObject as GP25;

            ++ChamberPatch.chamberSkip;
            if (((int)roundClass) == -1)
            {
                asGP25.Chamber.SetRound(null);
            }
            else
            {
                asGP25.Chamber.SetRound(roundClass, asGP25.Chamber.transform.position, asGP25.Chamber.transform.rotation);
            }
            --ChamberPatch.chamberSkip;
        }

        private bool UpdateAttachableTubeFed()
        {
            AttachableTubeFed asATF = dataObject as AttachableTubeFed;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[25];
                modified = true;
            }

            byte preIndex = itemData.data[0];

            // Write attached mount index
            if (asATF.Attachment.curMount == null)
            {
                itemData.data[0] = 255;
            }
            else
            {
                // Find the mount and set it
                bool found = false;
                for (int i = 0; i < asATF.Attachment.curMount.MyObject.AttachmentMounts.Count; ++i)
                {
                    if (asATF.Attachment.curMount.MyObject.AttachmentMounts[i] == asATF.Attachment.curMount)
                    {
                        itemData.data[0] = (byte)i;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    itemData.data[0] = 255;
                }
            }
            modified |= preIndex != itemData.data[0];

            byte preval = itemData.data[1];

            // Write fire mode index
            itemData.data[1] = asATF.IsSafetyEngaged ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[1];

            preval = itemData.data[2];

            // Write hammer state
            itemData.data[2] = BitConverter.GetBytes(asATF.IsHammerCocked)[0];

            modified |= preval != itemData.data[2];

            preval = itemData.data[3];
            byte preval0 = itemData.data[4];
            byte preval1 = itemData.data[5];
            byte preval2 = itemData.data[6];

            // Write chambered round class
            if (asATF.Chamber.GetRound() == null || asATF.Chamber.IsSpent || asATF.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 5);
            }
            else
            {
                BitConverter.GetBytes((short)asATF.Chamber.GetRound().RoundType).CopyTo(itemData.data, 3);
                BitConverter.GetBytes((short)asATF.Chamber.GetRound().RoundClass).CopyTo(itemData.data, 5);
            }

            modified |= (preval != itemData.data[3] || preval0 != itemData.data[4] || preval1 != itemData.data[5] || preval2 != itemData.data[6]);

            preval = itemData.data[7];

            // Write bolt handle pos
            itemData.data[7] = (byte)asATF.Bolt.CurPos;

            modified |= preval != itemData.data[7];

            if (asATF.HasHandle)
            {
                preval = itemData.data[8];

                // Write bolt handle pos
                itemData.data[8] = (byte)asATF.Handle.CurPos;

                modified |= preval != itemData.data[8];
            }

            preval = itemData.data[9];
            preval0 = itemData.data[10];
            preval1 = itemData.data[11];
            preval2 = itemData.data[12];

            // Write mountObjectID
            BitConverter.GetBytes(mountObjectID).CopyTo(itemData.data, 9);

            modified |= (preval != itemData.data[9] || preval0 != itemData.data[10] || preval1 != itemData.data[11] || preval2 != itemData.data[12]);

            // Write mount object scale
            if (transform.parent == null)
            {
                mountObjectScale = Vector3.one;
                preval = itemData.data[13];
                preval0 = itemData.data[14];
                preval1 = itemData.data[15];
                preval2 = itemData.data[16];
                BitConverter.GetBytes(1).CopyTo(itemData.data, 13);
                modified |= (preval != itemData.data[13] || preval0 != itemData.data[14] || preval1 != itemData.data[15] || preval2 != itemData.data[16]);

                preval = itemData.data[17];
                preval0 = itemData.data[18];
                preval1 = itemData.data[19];
                preval2 = itemData.data[20];
                BitConverter.GetBytes(1).CopyTo(itemData.data, 17);
                modified |= (preval != itemData.data[17] || preval0 != itemData.data[18] || preval1 != itemData.data[19] || preval2 != itemData.data[20]);

                preval = itemData.data[21];
                preval0 = itemData.data[22];
                preval1 = itemData.data[23];
                preval2 = itemData.data[24];
                BitConverter.GetBytes(1).CopyTo(itemData.data, 21);
                modified |= (preval != itemData.data[21] || preval0 != itemData.data[22] || preval1 != itemData.data[23] || preval2 != itemData.data[24]);
            }
            else
            {
                mountObjectScale = transform.parent.localScale;
                preval = itemData.data[13];
                preval0 = itemData.data[14];
                preval1 = itemData.data[15];
                preval2 = itemData.data[16];
                BitConverter.GetBytes(transform.parent.localScale.x).CopyTo(itemData.data, 13);
                modified |= (preval != itemData.data[13] || preval0 != itemData.data[14] || preval1 != itemData.data[15] || preval2 != itemData.data[16]);

                preval = itemData.data[17];
                preval0 = itemData.data[18];
                preval1 = itemData.data[19];
                preval2 = itemData.data[20];
                BitConverter.GetBytes(transform.parent.localScale.y).CopyTo(itemData.data, 17);
                modified |= (preval != itemData.data[17] || preval0 != itemData.data[18] || preval1 != itemData.data[19] || preval2 != itemData.data[20]);

                preval = itemData.data[21];
                preval0 = itemData.data[22];
                preval1 = itemData.data[23];
                preval2 = itemData.data[24];
                BitConverter.GetBytes(transform.parent.localScale.z).CopyTo(itemData.data, 21);
                modified |= (preval != itemData.data[21] || preval0 != itemData.data[22] || preval1 != itemData.data[23] || preval2 != itemData.data[24]);
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
                // Check if were waiting for mount object
                if (mountObjectID != -1 && toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                {
                    list.Remove(this);
                }

                // Should not be mounted, check if currently is
                if (asATF.Attachment.curMount != null)
                {
                    asATF.Attachment.Sensor.m_storedScaleMagnified = 1f;
                    asATF.Attachment.transform.localScale = new Vector3(1, 1, 1);
                    ++data.ignoreParentChanged;
                    asATF.Attachment.DetachFromMount();
                    --data.ignoreParentChanged;
                    currentMountIndex = 255;

                    // Detach from mount will recover rigidbody, set as kinematic if not controller
                    // TODO: Review: May not need to do this anymore due to RecoverRigidBodyPatch
                    if (data.controller != GameManager.ID)
                    {
                        Mod.SetKinematicRecursive(asATF.Attachment.transform, true);
                    }
                }
            }
            else
            {
                mountObjectID = BitConverter.ToInt32(newData, 9);
                mountObjectScale = new Vector3(BitConverter.ToSingle(newData, 13), BitConverter.ToSingle(newData, 17), BitConverter.ToSingle(newData, 21));
                if (mountObjectID != -1)
                {
                    // Should not be mounted, check if currently is
                    if (asATF.Attachment.curMount != null)
                    {
                        asATF.Attachment.Sensor.m_storedScaleMagnified = 1f;
                        asATF.Attachment.transform.localScale = new Vector3(1, 1, 1);
                        ++data.ignoreParentChanged;
                        asATF.Attachment.DetachFromMount();
                        --data.ignoreParentChanged;
                        currentMountIndex = 255;

                        // Detach from mount will recover rigidbody, set as kinematic if not controller
                        // TODO: Review: May not need to do this anymore due to RecoverRigidBodyPatch
                        if (data.controller != GameManager.ID)
                        {
                            Mod.SetKinematicRecursive(asATF.Attachment.transform, true);
                        }
                    }
                }
                else
                {
                    // Find mount instance we want to be mounted to
                    FVRFireArmAttachmentMount mount = null;
                    TrackedItemData parentTrackedItemData = null;
                    if (ThreadManager.host)
                    {
                        parentTrackedItemData = Server.objects[mountObjectID] as TrackedItemData;
                    }
                    else
                    {
                        parentTrackedItemData = Client.objects[mountObjectID] as TrackedItemData;
                    }

                    bool addedForLater = false;
                    if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null)
                    {
                        // We want to be mounted, we have a parent
                        if (parentTrackedItemData.physicalItem.physicalItem.AttachmentMounts.Count > newData[0])
                        {
                            mount = parentTrackedItemData.physicalItem.physicalItem.AttachmentMounts[newData[0]];
                        }
                    }
                    else
                    {
                        addedForLater = true;
                        if (toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                        {
                            list.Add(this);
                        }
                        else
                        {
                            toAttachByMountObjectID.Add(mountObjectID, new List<TrackedItem>() { this });
                        }
                    }

                    // Mount could be null if the mount index corresponds to a parent we have yet a receive a change to
                    if (mount != null && asATF.Attachment.curMount != mount)
                    {
                        ++data.ignoreParentChanged;
                        if (asATF.Attachment.curMount != null)
                        {
                            asATF.Attachment.Sensor.m_storedScaleMagnified = 1f;
                            asATF.Attachment.transform.localScale = new Vector3(1, 1, 1);
                            asATF.Attachment.DetachFromMount();
                        }

                        if (CanAttachToMount(asATF.Attachment, mount))
                        {
                            // Apply mount object scale
                            if (mount.GetRootMount().ParentToThis)
                            {
                                mount.GetRootMount().transform.localScale = mountObjectScale;
                            }
                            else
                            {
                                mount.MyObject.transform.localScale = mountObjectScale;
                            }

                            if (asATF.Attachment.CanScaleToMount && mount.CanThisRescale())
                            {
                                asATF.Attachment.ScaleToMount(mount);
                            }
                            asATF.Attachment.AttachToMount(mount, true);

                            // Check if there were any children attachments waiting for us
                            if (toAttachByMountObjectID.TryGetValue(data.trackedID, out List<TrackedItem> list))
                            {
                                for (int i = 0; i < list.Count; ++i)
                                {
                                    if (list[i] != null)
                                    {
                                        list[i].data.UpdateFromData(list[i].data);
                                    }
                                }

                                toAttachByMountObjectID.Remove(data.trackedID);
                            }
                        }
                        else if(!addedForLater) // Can't attach to mount yet, probably because parent is unattached attachment, but will need to eventually
                        {
                            if (toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                            {
                                list.Add(this);
                            }
                            else
                            {
                                toAttachByMountObjectID.Add(mountObjectID, new List<TrackedItem>() { this });
                            }
                        }
                        currentMountIndex = newData[0];
                        --data.ignoreParentChanged;
                    }
                }
            }
            modified |= preMountIndex != currentMountIndex;

            if (itemData.data == null)
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
                if (itemData.data[7] != newData[7])
                {
                    // Set bolt pos
                    asATF.Bolt.LastPos = asATF.Bolt.CurPos;
                    asATF.Bolt.CurPos = (AttachableTubeFedBolt.BoltPos)newData[7];
                }
                if (asATF.HasHandle && itemData.data[8] != newData[8])
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
                    asATF.m_isHammerCocked = newData[2] == 1;
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

            itemData.data = newData;

            return modified;
        }

        private void AttachableTubeFedChamberRound(FireArmRoundType roundType, FireArmRoundClass roundClass)
        {
            AttachableTubeFed asATF = null;
            if (dataObject is AttachableFirearm)
            {
                asATF = dataObject as AttachableTubeFed;
            }
            else // FVRFireArm and this refers to an integrated attachable firearm
            {
                asATF = (dataObject as FVRFireArm).GetIntegratedAttachableFirearm() as AttachableTubeFed;
            }
            if (((int)roundClass) == -1)
            {
                ++ChamberPatch.chamberSkip;
                asATF.Chamber.SetRound(null);
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
        }

        private FVRFireArmChamber AttachableTubeFedGetChamber()
        {
            AttachableTubeFed asATF = null;
            if (dataObject is AttachableFirearm)
            {
                asATF = dataObject as AttachableTubeFed;
            }
            else // FVRFireArm and this refers to an integrated attachable firearm
            {
                asATF = (dataObject as FVRFireArm).GetIntegratedAttachableFirearm() as AttachableTubeFed;
            }
            return asATF.Chamber;
        }

        private void ChamberAttachableTubeFedRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            AttachableTubeFed asATF = dataObject as AttachableTubeFed;

            ++ChamberPatch.chamberSkip;
            if (((int)roundClass) == -1)
            {
                asATF.Chamber.SetRound(null);
            }
            else
            {
                asATF.Chamber.SetRound(roundClass, asATF.Chamber.transform.position, asATF.Chamber.transform.rotation);
            }
            --ChamberPatch.chamberSkip;
        }

        private bool UpdateAttachableClosedBoltWeapon()
        {
            AttachableClosedBoltWeapon asACBW = dataObject as AttachableClosedBoltWeapon;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[24];
                modified = true;
            }

            byte preIndex = itemData.data[0];

            // Write attached mount index
            if (asACBW.Attachment.curMount == null)
            {
                itemData.data[0] = 255;
            }
            else
            {
                // Find the mount and set it
                bool found = false;
                for (int i = 0; i < asACBW.Attachment.curMount.MyObject.AttachmentMounts.Count; ++i)
                {
                    if (asACBW.Attachment.curMount.MyObject.AttachmentMounts[i] == asACBW.Attachment.curMount)
                    {
                        itemData.data[0] = (byte)i;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    itemData.data[0] = 255;
                }
            }
            modified |= preIndex != itemData.data[0];

            byte preval = itemData.data[1];

            // Write fire mode index
            itemData.data[1] = (byte)asACBW.FireSelectorModeIndex;

            modified |= preval != itemData.data[1];

            preval = itemData.data[2];

            // Write camBurst
            itemData.data[2] = (byte)asACBW.m_CamBurst;

            modified |= preval != itemData.data[2];

            preval = itemData.data[3];

            // Write hammer state
            itemData.data[3] = asACBW.IsHammerCocked ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[3];

            preval = itemData.data[4];
            byte preval0 = itemData.data[5];
            byte preval1 = itemData.data[6];
            byte preval2 = itemData.data[7];

            // Write chambered round class
            if (asACBW.Chamber.GetRound() == null || asACBW.Chamber.IsSpent || asACBW.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 6);
            }
            else
            {
                BitConverter.GetBytes((short)asACBW.Chamber.GetRound().RoundType).CopyTo(itemData.data, 4);
                BitConverter.GetBytes((short)asACBW.Chamber.GetRound().RoundClass).CopyTo(itemData.data, 6);
            }

            modified |= (preval != itemData.data[4] || preval0 != itemData.data[5] || preval1 != itemData.data[5] || preval2 != itemData.data[6]);

            preval = itemData.data[8];
            preval0 = itemData.data[9];
            preval1 = itemData.data[10];
            preval2 = itemData.data[11];

            // Write mountObjectID
            BitConverter.GetBytes(mountObjectID).CopyTo(itemData.data, 8);

            modified |= (preval != itemData.data[8] || preval0 != itemData.data[9] || preval1 != itemData.data[10] || preval2 != itemData.data[11]);

            // Write mount object scale
            if (transform.parent == null)
            {
                mountObjectScale = Vector3.one;
                preval = itemData.data[12];
                preval0 = itemData.data[13];
                preval1 = itemData.data[14];
                preval2 = itemData.data[15];
                BitConverter.GetBytes(1).CopyTo(itemData.data, 12);
                modified |= (preval != itemData.data[12] || preval0 != itemData.data[13] || preval1 != itemData.data[14] || preval2 != itemData.data[15]);

                preval = itemData.data[16];
                preval0 = itemData.data[17];
                preval1 = itemData.data[18];
                preval2 = itemData.data[19];
                BitConverter.GetBytes(1).CopyTo(itemData.data, 16);
                modified |= (preval != itemData.data[16] || preval0 != itemData.data[17] || preval1 != itemData.data[18] || preval2 != itemData.data[19]);

                preval = itemData.data[20];
                preval0 = itemData.data[21];
                preval1 = itemData.data[22];
                preval2 = itemData.data[23];
                BitConverter.GetBytes(1).CopyTo(itemData.data, 20);
                modified |= (preval != itemData.data[20] || preval0 != itemData.data[21] || preval1 != itemData.data[22] || preval2 != itemData.data[23]);
            }
            else
            {
                mountObjectScale = transform.parent.localScale;
                preval = itemData.data[12];
                preval0 = itemData.data[13];
                preval1 = itemData.data[14];
                preval2 = itemData.data[15];
                BitConverter.GetBytes(transform.parent.localScale.x).CopyTo(itemData.data, 12);
                modified |= (preval != itemData.data[12] || preval0 != itemData.data[13] || preval1 != itemData.data[14] || preval2 != itemData.data[15]);

                preval = itemData.data[16];
                preval0 = itemData.data[17];
                preval1 = itemData.data[18];
                preval2 = itemData.data[19];
                BitConverter.GetBytes(transform.parent.localScale.y).CopyTo(itemData.data, 16);
                modified |= (preval != itemData.data[16] || preval0 != itemData.data[17] || preval1 != itemData.data[18] || preval2 != itemData.data[19]);

                preval = itemData.data[20];
                preval0 = itemData.data[21];
                preval1 = itemData.data[22];
                preval2 = itemData.data[23];
                BitConverter.GetBytes(transform.parent.localScale.z).CopyTo(itemData.data, 20);
                modified |= (preval != itemData.data[20] || preval0 != itemData.data[21] || preval1 != itemData.data[22] || preval2 != itemData.data[23]);
            }

            return modified;
        }

        private bool UpdateGivenAttachableClosedBoltWeapon(byte[] newData)
        {
            bool modified = false;
            AttachableClosedBoltWeapon asACBW = dataObject as AttachableClosedBoltWeapon;

            byte preMountIndex = currentMountIndex;
            if (newData[0] == 255)
            {
                // Check if were waiting for mount object
                if (mountObjectID != -1 && toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                {
                    list.Remove(this);
                }

                // Should not be mounted, check if currently is
                if (asACBW.Attachment.curMount != null)
                {
                    asACBW.Attachment.Sensor.m_storedScaleMagnified = 1f;
                    asACBW.Attachment.transform.localScale = new Vector3(1, 1, 1);
                    ++data.ignoreParentChanged;
                    asACBW.Attachment.DetachFromMount();
                    --data.ignoreParentChanged;
                    currentMountIndex = 255;

                    // Detach from mount will recover rigidbody, set as kinematic if not controller
                    // TODO: Review: May not need to do this anymore due to RecoverRigidBodyPatch
                    if (data.controller != GameManager.ID)
                    {
                        Mod.SetKinematicRecursive(asACBW.Attachment.transform, true);
                    }
                }
            }
            else
            {
                mountObjectID = BitConverter.ToInt32(newData, 8);
                mountObjectScale = new Vector3(BitConverter.ToSingle(newData, 12), BitConverter.ToSingle(newData, 16), BitConverter.ToSingle(newData, 20));
                if (mountObjectID == -1)
                {
                    // Should not be mounted, check if currently is
                    if (asACBW.Attachment.curMount != null)
                    {
                        asACBW.Attachment.Sensor.m_storedScaleMagnified = 1f;
                        asACBW.Attachment.transform.localScale = new Vector3(1, 1, 1);
                        ++data.ignoreParentChanged;
                        asACBW.Attachment.DetachFromMount();
                        --data.ignoreParentChanged;
                        currentMountIndex = 255;

                        // Detach from mount will recover rigidbody, set as kinematic if not controller
                        // TODO: Review: May not need to do this anymore due to RecoverRigidBodyPatch
                        if (data.controller != GameManager.ID)
                        {
                            Mod.SetKinematicRecursive(asACBW.Attachment.transform, true);
                        }
                    }
                }
                else
                {
                    // Find mount instance we want to be mounted to
                    FVRFireArmAttachmentMount mount = null;
                    TrackedItemData parentTrackedItemData = null;
                    if (ThreadManager.host)
                    {
                        parentTrackedItemData = Server.objects[mountObjectID] as TrackedItemData;
                    }
                    else
                    {
                        parentTrackedItemData = Client.objects[mountObjectID] as TrackedItemData;
                    }

                    bool addedForLater = false;
                    if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null)
                    {
                        // We want to be mounted, we have a parent
                        if (parentTrackedItemData.physicalItem.physicalItem.AttachmentMounts.Count > newData[0])
                        {
                            mount = parentTrackedItemData.physicalItem.physicalItem.AttachmentMounts[newData[0]];
                        }
                    }
                    else
                    {
                        addedForLater = true;
                        if (toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                        {
                            list.Add(this);
                        }
                        else
                        {
                            toAttachByMountObjectID.Add(mountObjectID, new List<TrackedItem>() { this });
                        }
                    }

                    // Mount could be null if the mount index corresponds to a parent we have yet a receive a change to
                    if (mount != null && asACBW.Attachment.curMount != mount)
                    {
                        ++data.ignoreParentChanged;
                        if (asACBW.Attachment.curMount != null)
                        {
                            asACBW.Attachment.Sensor.m_storedScaleMagnified = 1f;
                            asACBW.Attachment.transform.localScale = new Vector3(1, 1, 1);
                            asACBW.Attachment.DetachFromMount();
                        }

                        if (CanAttachToMount(asACBW.Attachment, mount))
                        {
                            // Apply mount object scale
                            if (mount.GetRootMount().ParentToThis)
                            {
                                mount.GetRootMount().transform.localScale = mountObjectScale;
                            }
                            else
                            {
                                mount.MyObject.transform.localScale = mountObjectScale;
                            }

                            if (asACBW.Attachment.CanScaleToMount && mount.CanThisRescale())
                            {
                                asACBW.Attachment.ScaleToMount(mount);
                            }
                            asACBW.Attachment.AttachToMount(mount, true);

                            // Check if there were any children attachments waiting for us
                            if (toAttachByMountObjectID.TryGetValue(data.trackedID, out List<TrackedItem> list))
                            {
                                for (int i = 0; i < list.Count; ++i)
                                {
                                    if (list[i] != null)
                                    {
                                        list[i].data.UpdateFromData(list[i].data);
                                    }
                                }

                                toAttachByMountObjectID.Remove(data.trackedID);
                            }
                        }
                        else if(!addedForLater) // Can't attach to mount yet, probably because parent is unattached attachment, but will need to eventually
                        {
                            if (toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                            {
                                list.Add(this);
                            }
                            else
                            {
                                toAttachByMountObjectID.Add(mountObjectID, new List<TrackedItem>() { this });
                            }
                        }
                        currentMountIndex = newData[0];
                        --data.ignoreParentChanged;
                    }
                }
            }
            modified |= preMountIndex != currentMountIndex;

            if (itemData.data == null)
            {
                modified = true;

                // Set fire select mode
                asACBW.m_fireSelectorMode = newData[1];

                // Set camBurst
                asACBW.m_CamBurst = newData[2];
            }
            else
            {
                if (itemData.data[0] != newData[0])
                {
                    // Set fire select mode
                    asACBW.m_fireSelectorMode = newData[1];
                    modified = true;
                }
                if (itemData.data[1] != newData[1])
                {
                    // Set camBurst
                    asACBW.m_CamBurst = newData[2];
                    modified = true;
                }
            }

            // Set hammer state
            if (newData[3] == 0)
            {
                if (asACBW.IsHammerCocked)
                {
                    asACBW.m_isHammerCocked = BitConverter.ToBoolean(newData, 3);
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

            itemData.data = newData;

            return modified;
        }

        private void AttachableClosedBoltWeaponChamberRound(FireArmRoundType roundType, FireArmRoundClass roundClass)
        {
            AttachableClosedBoltWeapon asACBW = null;
            if (dataObject is AttachableFirearm)
            {
                asACBW = dataObject as AttachableClosedBoltWeapon;
            }
            else // FVRFireArm and this refers to an integrated attachable firearm
            {
                asACBW = (dataObject as FVRFireArm).GetIntegratedAttachableFirearm() as AttachableClosedBoltWeapon;
            }
            if (((int)roundClass) == -1)
            {
                ++ChamberPatch.chamberSkip;
                asACBW.Chamber.SetRound(null);
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
        }

        private FVRFireArmChamber AttachableClosedBoltWeaponGetChamber()
        {
            AttachableClosedBoltWeapon asACBW = null;
            if (dataObject is AttachableFirearm)
            {
                asACBW = dataObject as AttachableClosedBoltWeapon;
            }
            else // FVRFireArm and this refers to an integrated attachable firearm
            {
                asACBW = (dataObject as FVRFireArm).GetIntegratedAttachableFirearm() as AttachableClosedBoltWeapon;
            }
            return asACBW.Chamber;
        }

        private void ChamberAttachableClosedBoltWeaponRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            AttachableClosedBoltWeapon asACBW = dataObject as AttachableClosedBoltWeapon;

            ++ChamberPatch.chamberSkip;
            if (((int)roundClass) == -1)
            {
                asACBW.Chamber.SetRound(null);
            }
            else
            {
                asACBW.Chamber.SetRound(roundClass, asACBW.Chamber.transform.position, asACBW.Chamber.transform.rotation);
            }
            --ChamberPatch.chamberSkip;
        }

        private bool UpdateAttachableBreakActions()
        {
            AttachableBreakActions asABA = dataObject as AttachableBreakActions;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[22];
                modified = true;
            }

            byte preIndex = itemData.data[0];

            // Write attached mount index
            if (asABA.Attachment.curMount == null)
            {
                itemData.data[0] = 255;
            }
            else
            {
                // Find the mount and set it
                bool found = false;
                for (int i = 0; i < asABA.Attachment.curMount.MyObject.AttachmentMounts.Count; ++i)
                {
                    if (asABA.Attachment.curMount.MyObject.AttachmentMounts[i] == asABA.Attachment.curMount)
                    {
                        itemData.data[0] = (byte)i;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    itemData.data[0] = 255;
                }
            }
            modified |= preIndex != itemData.data[0];

            byte preval = itemData.data[1];

            // Write breachOpen
            itemData.data[1] = asABA.m_isBreachOpen ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[1];

            preval = itemData.data[2];
            byte preval0 = itemData.data[3];
            byte preval1 = itemData.data[4];
            byte preval2 = itemData.data[5];

            // Write chambered round class
            if (asABA.Chamber.GetRound() == null || asABA.Chamber.IsSpent || asABA.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 4);
            }
            else
            {
                BitConverter.GetBytes((short)asABA.Chamber.GetRound().RoundType).CopyTo(itemData.data, 2);
                BitConverter.GetBytes((short)asABA.Chamber.GetRound().RoundClass).CopyTo(itemData.data, 4);
            }

            modified |= (preval != itemData.data[2] || preval0 != itemData.data[3] || preval1 != itemData.data[4] || preval2 != itemData.data[5]);

            preval = itemData.data[6];
            preval0 = itemData.data[7];
            preval1 = itemData.data[8];
            preval2 = itemData.data[9];

            // Write mountObjectID
            BitConverter.GetBytes(mountObjectID).CopyTo(itemData.data, 6);

            modified |= (preval != itemData.data[6] || preval0 != itemData.data[7] || preval1 != itemData.data[8] || preval2 != itemData.data[9]);

            // Write mount object scale
            if (transform.parent == null)
            {
                mountObjectScale = Vector3.one;
                preval = itemData.data[10];
                preval0 = itemData.data[11];
                preval1 = itemData.data[12];
                preval2 = itemData.data[13];
                BitConverter.GetBytes(1).CopyTo(itemData.data, 10);
                modified |= (preval != itemData.data[10] || preval0 != itemData.data[11] || preval1 != itemData.data[12] || preval2 != itemData.data[13]);

                preval = itemData.data[14];
                preval0 = itemData.data[15];
                preval1 = itemData.data[16];
                preval2 = itemData.data[17];
                BitConverter.GetBytes(1).CopyTo(itemData.data, 14);
                modified |= (preval != itemData.data[14] || preval0 != itemData.data[15] || preval1 != itemData.data[16] || preval2 != itemData.data[17]);

                preval = itemData.data[18];
                preval0 = itemData.data[19];
                preval1 = itemData.data[20];
                preval2 = itemData.data[21];
                BitConverter.GetBytes(1).CopyTo(itemData.data, 18);
                modified |= (preval != itemData.data[18] || preval0 != itemData.data[19] || preval1 != itemData.data[20] || preval2 != itemData.data[21]);
            }
            else
            {
                mountObjectScale = transform.parent.localScale;
                preval = itemData.data[10];
                preval0 = itemData.data[11];
                preval1 = itemData.data[12];
                preval2 = itemData.data[13];
                BitConverter.GetBytes(transform.parent.localScale.x).CopyTo(itemData.data, 10);
                modified |= (preval != itemData.data[10] || preval0 != itemData.data[11] || preval1 != itemData.data[12] || preval2 != itemData.data[13]);

                preval = itemData.data[14];
                preval0 = itemData.data[15];
                preval1 = itemData.data[16];
                preval2 = itemData.data[17];
                BitConverter.GetBytes(transform.parent.localScale.y).CopyTo(itemData.data, 14);
                modified |= (preval != itemData.data[14] || preval0 != itemData.data[15] || preval1 != itemData.data[16] || preval2 != itemData.data[17]);

                preval = itemData.data[18];
                preval0 = itemData.data[19];
                preval1 = itemData.data[20];
                preval2 = itemData.data[21];
                BitConverter.GetBytes(transform.parent.localScale.z).CopyTo(itemData.data, 18);
                modified |= (preval != itemData.data[18] || preval0 != itemData.data[19] || preval1 != itemData.data[20] || preval2 != itemData.data[21]);
            }

            return modified;
        }

        private bool UpdateGivenAttachableBreakActions(byte[] newData)
        {
            bool modified = false;
            AttachableBreakActions asABA = dataObject as AttachableBreakActions;

            byte preMountIndex = currentMountIndex;
            if (newData[0] == 255)
            {
                // Check if were waiting for mount object
                if (mountObjectID != -1 && toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                {
                    list.Remove(this);
                }

                // Should not be mounted, check if currently is
                if (asABA.Attachment.curMount != null)
                {
                    asABA.Attachment.Sensor.m_storedScaleMagnified = 1f;
                    asABA.Attachment.transform.localScale = new Vector3(1, 1, 1);
                    ++data.ignoreParentChanged;
                    asABA.Attachment.DetachFromMount();
                    --data.ignoreParentChanged;
                    currentMountIndex = 255;

                    // Detach from mount will recover rigidbody, set as kinematic if not controller
                    // TODO: Review: May not need to do this anymore due to RecoverRigidBodyPatch
                    if (data.controller != GameManager.ID)
                    {
                        Mod.SetKinematicRecursive(asABA.Attachment.transform, true);
                    }
                }
            }
            else
            {
                mountObjectID = BitConverter.ToInt32(newData, 6);
                mountObjectScale = new Vector3(BitConverter.ToSingle(newData, 10), BitConverter.ToSingle(newData, 14), BitConverter.ToSingle(newData, 18));
                if (mountObjectID == -1)
                {
                    // Should not be mounted, check if currently is
                    if (asABA.Attachment.curMount != null)
                    {
                        asABA.Attachment.Sensor.m_storedScaleMagnified = 1f;
                        asABA.Attachment.transform.localScale = new Vector3(1, 1, 1);
                        ++data.ignoreParentChanged;
                        asABA.Attachment.DetachFromMount();
                        --data.ignoreParentChanged;
                        currentMountIndex = 255;

                        // Detach from mount will recover rigidbody, set as kinematic if not controller
                        // TODO: Review: May not need to do this anymore due to RecoverRigidBodyPatch
                        if (data.controller != GameManager.ID)
                        {
                            Mod.SetKinematicRecursive(asABA.Attachment.transform, true);
                        }
                    }
                }
                else
                {
                    // Find mount instance we want to be mounted to
                    FVRFireArmAttachmentMount mount = null;
                    TrackedItemData parentTrackedItemData = null;
                    if (ThreadManager.host)
                    {
                        parentTrackedItemData = Server.objects[mountObjectID] as TrackedItemData;
                    }
                    else
                    {
                        parentTrackedItemData = Client.objects[mountObjectID] as TrackedItemData;
                    }

                    bool addedForLater = false;
                    if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null)
                    {
                        // We want to be mounted, we have a parent
                        if (parentTrackedItemData.physicalItem.physicalItem.AttachmentMounts.Count > newData[0])
                        {
                            mount = parentTrackedItemData.physicalItem.physicalItem.AttachmentMounts[newData[0]];
                        }
                    }
                    else 
                    {
                        addedForLater = true;
                        if (toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                        {
                            list.Add(this);
                        }
                        else
                        {
                            toAttachByMountObjectID.Add(mountObjectID, new List<TrackedItem>() { this });
                        }
                    }

                    // Mount could be null if the mount index corresponds to a parent we have yet a receive a change to
                    if (mount != null && asABA.Attachment.curMount != mount)
                    {
                        ++data.ignoreParentChanged;
                        if (asABA.Attachment.curMount != null)
                        {
                            asABA.Attachment.Sensor.m_storedScaleMagnified = 1f;
                            asABA.Attachment.transform.localScale = new Vector3(1, 1, 1);
                            asABA.Attachment.DetachFromMount();
                        }

                        if (CanAttachToMount(asABA.Attachment, mount))
                        {
                            // Apply mount object scale
                            if (mount.GetRootMount().ParentToThis)
                            {
                                mount.GetRootMount().transform.localScale = mountObjectScale;
                            }
                            else
                            {
                                mount.MyObject.transform.localScale = mountObjectScale;
                            }

                            if (asABA.Attachment.CanScaleToMount && mount.CanThisRescale())
                            {
                                asABA.Attachment.ScaleToMount(mount);
                            }
                            asABA.Attachment.AttachToMount(mount, true);

                            // Check if there were any children attachments waiting for us
                            if (toAttachByMountObjectID.TryGetValue(data.trackedID, out List<TrackedItem> list))
                            {
                                for (int i = 0; i < list.Count; ++i)
                                {
                                    if (list[i] != null)
                                    {
                                        list[i].data.UpdateFromData(list[i].data);
                                    }
                                }

                                toAttachByMountObjectID.Remove(data.trackedID);
                            }
                        }
                        else if(!addedForLater) // Can't attach to mount yet, probably because parent is unattached attachment, but will need to eventually
                        {
                            if (toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                            {
                                list.Add(this);
                            }
                            else
                            {
                                toAttachByMountObjectID.Add(mountObjectID, new List<TrackedItem>() { this });
                            }
                        }
                        currentMountIndex = newData[0];
                        --data.ignoreParentChanged;
                    }
                }
            }
            modified |= preMountIndex != currentMountIndex;

            // Set breachOpen
            bool newVal = newData[1] == 1;
            if ((asABA.m_isBreachOpen && !newVal) || (!asABA.m_isBreachOpen && newVal))
            {
                asABA.ToggleBreach();
                modified = true;
            }
            asABA.m_isBreachOpen = newVal;

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

            itemData.data = newData;

            return modified;
        }

        private void AttachableBreakActionsChamberRound(FireArmRoundType roundType, FireArmRoundClass roundClass)
        {
            AttachableBreakActions asABA = null;
            if (dataObject is AttachableFirearm)
            {
                asABA = dataObject as AttachableBreakActions;
            }
            else // FVRFireArm and this refers to an integrated attachable firearm
            {
                asABA = (dataObject as FVRFireArm).GetIntegratedAttachableFirearm() as AttachableBreakActions;
            }
            if(((int)roundClass) == -1)
            {
                ++ChamberPatch.chamberSkip;
                asABA.Chamber.SetRound(null);
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
        }

        private FVRFireArmChamber AttachableBreakActionsGetChamber()
        {
            AttachableBreakActions asABA = null;
            if (dataObject is AttachableFirearm)
            {
                asABA = dataObject as AttachableBreakActions;
            }
            else // FVRFireArm and this refers to an integrated attachable firearm
            {
                asABA = (dataObject as FVRFireArm).GetIntegratedAttachableFirearm() as AttachableBreakActions;
            }
            return asABA.Chamber;
        }

        private void ChamberAttachableBreakActionsRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            AttachableBreakActions asABA = dataObject as AttachableBreakActions;

            ++ChamberPatch.chamberSkip;
            if (((int)roundClass) == -1)
            {
                asABA.Chamber.SetRound(null);
            }
            else
            {
                asABA.Chamber.SetRound(roundClass, asABA.Chamber.transform.position, asABA.Chamber.transform.rotation);
            }
            --ChamberPatch.chamberSkip;
        }

        private void UpdateAttachableFirearmParent()
        {
            FVRFireArmAttachment asAttachment = (dataObject as AttachableFirearm).Attachment;

            if (currentMountIndex != 255) // We want to be attached to a mount
            {
                if (mountObjectID != -1) // We have parent
                {
                    // We could be on wrong mount (or none physically) if we got a new mount through update but the parent hadn't been updated yet

                    // Get the mount we are supposed to be mounted to
                    FVRFireArmAttachmentMount mount = null;
                    TrackedItemData parentTrackedItemData = null;
                    if (ThreadManager.host)
                    {
                        parentTrackedItemData = Server.objects[mountObjectID] as TrackedItemData;
                    }
                    else
                    {
                        parentTrackedItemData = Client.objects[mountObjectID] as TrackedItemData;
                    }

                    if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null)
                    {
                        mount = parentTrackedItemData.physicalItem.physicalItem.AttachmentMounts[currentMountIndex];
                    }

                    // If not yet physically mounted to anything, can right away mount to the proper mount
                    if (asAttachment.curMount == null)
                    {
                        if (CanAttachToMount(asAttachment, mount))
                        {
                            // Apply mount object scale
                            if (mount.GetRootMount().ParentToThis)
                            {
                                mount.GetRootMount().transform.localScale = mountObjectScale;
                            }
                            else
                            {
                                mount.MyObject.transform.localScale = mountObjectScale;
                            }

                            if (asAttachment.CanScaleToMount && mount.CanThisRescale())
                            {
                                asAttachment.ScaleToMount(mount);
                            }
                            ++data.ignoreParentChanged;
                            asAttachment.AttachToMount(mount, true);
                            --data.ignoreParentChanged;

                            // Check if there were any children attachments waiting for us
                            if (toAttachByMountObjectID.TryGetValue(data.trackedID, out List<TrackedItem> list))
                            {
                                for (int i = 0; i < list.Count; ++i)
                                {
                                    if (list[i] != null)
                                    {
                                        list[i].data.UpdateFromData(list[i].data);
                                    }
                                }

                                toAttachByMountObjectID.Remove(data.trackedID);
                            }
                        }
                        else // Can't attach to mount yet, probably because parent is unattached attachment, but will need to eventually
                        {
                            if (toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                            {
                                list.Add(this);
                            }
                            else
                            {
                                toAttachByMountObjectID.Add(mountObjectID, new List<TrackedItem>() { this });
                            }
                        }
                    }
                    else if (asAttachment.curMount != mount) // Already mounted, but not on the right one, need to unmount, then mount of right one
                    {
                        ++data.ignoreParentChanged;
                        if (asAttachment.curMount != null)
                        {
                            asAttachment.Sensor.m_storedScaleMagnified = 1f;
                            asAttachment.transform.localScale = new Vector3(1, 1, 1);
                            asAttachment.DetachFromMount();
                        }

                        if (CanAttachToMount(asAttachment, mount))
                        {
                            // Apply mount object scale
                            if (mount.GetRootMount().ParentToThis)
                            {
                                mount.GetRootMount().transform.localScale = mountObjectScale;
                            }
                            else
                            {
                                mount.MyObject.transform.localScale = mountObjectScale;
                            }

                            if (asAttachment.CanScaleToMount && mount.CanThisRescale())
                            {
                                asAttachment.ScaleToMount(mount);
                            }
                            asAttachment.AttachToMount(mount, true);

                            // Check if there were any children attachments waiting for us
                            if (toAttachByMountObjectID.TryGetValue(data.trackedID, out List<TrackedItem> list))
                            {
                                for (int i = 0; i < list.Count; ++i)
                                {
                                    if (list[i] != null)
                                    {
                                        list[i].data.UpdateFromData(list[i].data);
                                    }
                                }

                                toAttachByMountObjectID.Remove(data.trackedID);
                            }
                        }
                        else // Can't attach to mount yet, probably because parent is unattached attachment, but will need to eventually
                        {
                            if (toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                            {
                                list.Add(this);
                            }
                            else
                            {
                                toAttachByMountObjectID.Add(mountObjectID, new List<TrackedItem>() { this });
                            }
                        }
                        --data.ignoreParentChanged;
                    }
                }
                // else, if this happens it is because we received a parent update to null and just haven't gotten the up to date mount index of -1 yet
                //       This will be handled on update
            }
            else // On update we will detach from any current mount if this is the case, no need to handle this here
            {
                // Check if were waiting for mount object
                if (mountObjectID != -1 && toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                {
                    list.Remove(this);
                }
            }
        }

        private bool UpdateLAPD2019Battery()
        {
            LAPD2019Battery asLAPD2019Battery = dataObject as LAPD2019Battery;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[4];
                modified = true;
            }

            byte preval = itemData.data[0];
            byte preval0 = itemData.data[1];
            byte preval1 = itemData.data[2];
            byte preval2 = itemData.data[3];

            // Write energy
            BitConverter.GetBytes(asLAPD2019Battery.GetEnergy()).CopyTo(itemData.data, 0);

            modified |= (preval != itemData.data[0] || preval0 != itemData.data[1] || preval1 != itemData.data[2] || preval2 != itemData.data[3]);

            return modified;
        }

        private bool UpdateGivenLAPD2019Battery(byte[] newData)
        {
            bool modified = false;
            LAPD2019Battery asLAPD2019Battery = dataObject as LAPD2019Battery;

            if (itemData.data == null)
            {
                modified = true;

                // Set energy
                asLAPD2019Battery.SetEnergy(BitConverter.ToSingle(newData, 0));
            }
            else
            {
                if (itemData.data[11] != newData[11] || itemData.data[12] != newData[12] || itemData.data[13] != newData[13] || itemData.data[14] != newData[14])
                {
                    // Set energy
                    asLAPD2019Battery.SetEnergy(BitConverter.ToSingle(newData, 0));
                    modified = true;
                }
            }

            itemData.data = newData;

            return modified;
        }

        private bool UpdateLAPD2019()
        {
            LAPD2019 asLAPD2019 = dataObject as LAPD2019;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[26];
                modified = true;
            }

            byte preval = itemData.data[0];

            // Write curChamber
            itemData.data[0] = (byte)asLAPD2019.CurChamber;

            modified |= preval != itemData.data[0];

            byte preval0;
            byte preval1;
            byte preval2;

            // Write chambered round classes
            for (int i = 0; i < 5; ++i)
            {
                int firstIndex = i * 4 + 1;
                preval = itemData.data[firstIndex];
                preval0 = itemData.data[firstIndex + 1];
                preval1 = itemData.data[firstIndex + 2];
                preval2 = itemData.data[firstIndex + 3];
                if (asLAPD2019.Chambers[i].GetRound() == null || asLAPD2019.Chambers[i].IsSpent || asLAPD2019.Chambers[i].GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(itemData.data, firstIndex + 2);
                }
                else
                {
                    BitConverter.GetBytes((short)asLAPD2019.Chambers[i].GetRound().RoundType).CopyTo(itemData.data, firstIndex);
                    BitConverter.GetBytes((short)asLAPD2019.Chambers[i].GetRound().RoundClass).CopyTo(itemData.data, firstIndex + 2);
                }

                modified |= (preval != itemData.data[firstIndex] || preval0 != itemData.data[firstIndex + 1] || preval1 != itemData.data[firstIndex + 2] || preval2 != itemData.data[firstIndex + 3]);
            }

            preval = itemData.data[21];
            preval0 = itemData.data[22];
            preval1 = itemData.data[23];
            preval2 = itemData.data[24];

            // Write capacitor charge
            BitConverter.GetBytes(asLAPD2019.m_capacitorCharge).CopyTo(itemData.data, 21);

            modified |= (preval != itemData.data[21] || preval0 != itemData.data[22] || preval1 != itemData.data[23] || preval2 != itemData.data[24]);

            preval = itemData.data[25];

            // Write capacitor charged
            itemData.data[25] = asLAPD2019.m_isCapacitorCharged ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[25];

            return modified;
        }

        private bool UpdateGivenLAPD2019(byte[] newData)
        {
            bool modified = false;
            LAPD2019 asLAPD2019 = dataObject as LAPD2019;

            if (itemData.data == null)
            {
                modified = true;

                // Set curChamber
                asLAPD2019.CurChamber = newData[0];

                // Set capacitor charge
                asLAPD2019.m_capacitorCharge = BitConverter.ToSingle(newData, 21);

                // Set capacitor charged
                asLAPD2019.m_isCapacitorCharged = newData[25] == 1;
            }
            else
            {
                if (itemData.data[0] != newData[0])
                {
                    // Set curChamber
                    asLAPD2019.CurChamber = newData[0];
                    modified = true;
                }
                if (itemData.data[21] != newData[21] || itemData.data[22] != newData[22] || itemData.data[23] != newData[23] || itemData.data[24] != newData[24])
                {
                    // Set capacitor charge
                    asLAPD2019.m_capacitorCharge = BitConverter.ToSingle(newData, 21);
                    modified = true;
                }
                if (itemData.data[25] != newData[25])
                {
                    // Set capacitor charged
                    asLAPD2019.m_isCapacitorCharged = newData[25] == 1;
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

            itemData.data = newData;

            return modified;
        }

        private int GetLAPD2019ChamberIndex(FVRFireArmChamber chamber)
        {
            LAPD2019 asLAPD2019 = dataObject as LAPD2019;
            FVRFireArmChamber[] chambers = asLAPD2019.Chambers;

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

        private bool UpdateSosigWeaponInterface()
        {
            SosigWeaponPlayerInterface asInterface = dataObject as SosigWeaponPlayerInterface;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[3];
                modified = true;
            }

            byte preval = itemData.data[0];
            byte preval0 = itemData.data[1];

            // Write shots left
            BitConverter.GetBytes((short)asInterface.W.m_shotsLeft).CopyTo(itemData.data, 0);

            modified |= (preval != itemData.data[0] || preval0 != itemData.data[1]);

            preval = itemData.data[2];

            // Write MechaState
            itemData.data[2] = (byte)asInterface.W.MechaState;

            modified |= preval != itemData.data[2];

            return modified;
        }

        private bool UpdateGivenSosigWeaponInterface(byte[] newData)
        {
            bool modified = false;
            SosigWeaponPlayerInterface asInterface = dataObject as SosigWeaponPlayerInterface;

            int debugStep = 0;
            try
            {
                if (itemData.data == null)
                {
                    debugStep = 1;
                    modified = true;

                    // Set shots left
                    asInterface.W.m_shotsLeft = BitConverter.ToInt16(newData, 0);

                    debugStep = 2;
                    // Set MechaState
                    asInterface.W.MechaState = (SosigWeapon.SosigWeaponMechaState)newData[2];
                }
                else
                {
                    debugStep = 3;
                    if (itemData.data[0] != newData[0] || itemData.data[1] != newData[1])
                    {
                        debugStep = 4;
                        // Set shots left
                        asInterface.W.m_shotsLeft = BitConverter.ToInt16(newData, 0);
                        modified = true;
                    }
                    if (itemData.data[2] != newData[2])
                    {
                        debugStep = 5;
                        // Set MechaState
                        asInterface.W.MechaState = (SosigWeapon.SosigWeaponMechaState)newData[2];
                        modified = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Mod.LogError("TrackedItem SosigWeaponPlayerInterface " + data.trackedID + " with local index " + data.localWaitingIndex + ", update given with newData size: " + newData.Length + " error at step: " + debugStep + ": " + ex.Message + ":\n" + ex.StackTrace);
            }

            itemData.data = newData;

            return modified;
        }

        private void SosigWeaponDestruction()
        {
            // Make sure this is not being interacted with by a Sosig
            SosigWeaponPlayerInterface asInterface = dataObject as SosigWeaponPlayerInterface;
            if (asInterface.W.SosigHoldingThis != null)
            {
                if (asInterface.W.HandHoldingThis != null)
                {
                    if (asInterface.W.HandHoldingThis.IsRightHand)
                    {
                        asInterface.W.SosigHoldingThis.Hand_Primary.DropHeldObject();
                    }
                    else
                    {
                        asInterface.W.SosigHoldingThis.Hand_Secondary.DropHeldObject();
                    }
                }
                else
                {
                    for (int i = 0; i < asInterface.W.SosigHoldingThis.Inventory.Slots.Count; ++i)
                    {
                        if (asInterface.W.SosigHoldingThis.Inventory.Slots[i] != null
                            && asInterface.W.SosigHoldingThis.Inventory.Slots[i].HeldObject == asInterface.W)
                        {
                            asInterface.W.SosigHoldingThis.Inventory.Slots[i].DetachHeldObject();
                            break;
                        }
                    }
                }
            }
        }

        private bool UpdateGrappleThrowable()
        {
            GrappleThrowable asGrappleThrowable = dataObject as GrappleThrowable;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[3];
                modified = true;
            }

            // Write rope free
            byte preval = itemData.data[0];

            itemData.data[0] = asGrappleThrowable.IsRopeFree ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[0];

            // Write has been thrown
            preval = itemData.data[1];

            itemData.data[1] = asGrappleThrowable.m_hasBeenThrown ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[1];

            // Write has landed
            preval = itemData.data[2];

            itemData.data[2] = asGrappleThrowable.m_hasLanded ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[2];

            return modified;
        }

        private bool UpdateGivenGrappleThrowable(byte[] newData)
        {
            bool modified = false;
            GrappleThrowable asGrappleThrowable = dataObject as GrappleThrowable;

            if (itemData.data == null)
            {
                // Set rope free
                if (newData[0] == 0)
                {
                    asGrappleThrowable.m_isRopeFree = false;
                    asGrappleThrowable.BundledRope.SetActive(true);
                    asGrappleThrowable.ClearRopeLengths();
                    asGrappleThrowable.FakeRopeLength.SetActive(false);
                    asGrappleThrowable.FakeKnot.SetActive(false);
                }
                else
                {
                    asGrappleThrowable.FreeRope();
                    asGrappleThrowable.FakeRopeLength.SetActive(true);
                }

                // Set has been thrown
                if (newData[1] == 0)
                {
                    asGrappleThrowable.m_hasBeenThrown = false;
                }
                else
                {
                    asGrappleThrowable.m_hasBeenThrown = true;
                    asGrappleThrowable.FakeRopeLength.SetActive(false);
                }

                // Set has landed
                asGrappleThrowable.m_hasLanded = newData[2] == 1;

                modified = true;
            }
            else
            {
                // Set rope free
                if (asGrappleThrowable.IsRopeFree && itemData.data[0] == 0)
                {
                    asGrappleThrowable.m_isRopeFree = false;
                    asGrappleThrowable.BundledRope.SetActive(true);
                    asGrappleThrowable.ClearRopeLengths();
                    asGrappleThrowable.FakeRopeLength.SetActive(false);
                    asGrappleThrowable.FakeKnot.SetActive(false);
                    modified = true;
                }
                else if (!asGrappleThrowable.IsRopeFree && itemData.data[0] == 1)
                {
                    asGrappleThrowable.FreeRope();
                    asGrappleThrowable.FakeRopeLength.SetActive(true);
                    modified = true;
                }

                // Set has been thrown
                if (newData[1] == 0 && asGrappleThrowable.m_hasBeenThrown)
                {
                    asGrappleThrowable.m_hasBeenThrown = false;
                    modified = true;
                }
                else if (newData[1] == 1 && !asGrappleThrowable.m_hasBeenThrown)
                {
                    asGrappleThrowable.m_hasBeenThrown = true;
                    asGrappleThrowable.FakeRopeLength.SetActive(false);
                    modified = true;
                }

                // Set has landed
                if ((newData[2] == 0 && asGrappleThrowable.m_hasLanded) ||
                    (newData[2] == 1 && !asGrappleThrowable.m_hasLanded))
                {
                    asGrappleThrowable.m_hasLanded = newData[2] == 1;
                    modified = true;
                }
            }

            itemData.data = newData;

            return modified;
        }

        private bool UpdateUberShatterable()
        {
            return false;
        }

        private bool UpdateGivenUberShatterable(byte[] newData)
        {
            return false;
        }

        private bool UpdateGasCuboid()
        {
            return false;
        }

        private bool UpdateGivenGasCuboid(byte[] newData)
        {
            return false;
        }

        private bool UpdateClosedBoltWeapon()
        {
            ClosedBoltWeapon asCBW = dataObject as ClosedBoltWeapon;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[asCBW.GetIntegratedAttachableFirearm() == null ? 7 : 11];
                modified = true;
            }

            byte preval = itemData.data[0];

            // Write fire mode index
            itemData.data[0] = (byte)asCBW.FireSelectorModeIndex;

            modified |= preval != itemData.data[0];

            preval = itemData.data[1];

            // Write camBurst
            itemData.data[1] = (byte)asCBW.m_CamBurst;

            modified |= preval != itemData.data[1];

            preval = itemData.data[2];

            // Write hammer state
            itemData.data[2] = asCBW.IsHammerCocked ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[2];

            preval = itemData.data[3];
            byte preval0 = itemData.data[4];
            byte preval1 = itemData.data[5];
            byte preval2 = itemData.data[6];

            // Write chambered round class
            if (asCBW.Chamber.GetRound() == null || asCBW.Chamber.IsSpent || asCBW.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 5);
            }
            else
            {
                BitConverter.GetBytes((short)asCBW.Chamber.GetRound().RoundType).CopyTo(itemData.data, 3);
                BitConverter.GetBytes((short)asCBW.Chamber.GetRound().RoundClass).CopyTo(itemData.data, 5);
            }

            modified |= (preval != itemData.data[3] || preval0 != itemData.data[4] || preval1 != itemData.data[5] || preval2 != itemData.data[6]);

            if (asCBW.GetIntegratedAttachableFirearm() != null)
            {
                preval = itemData.data[7];
                preval0 = itemData.data[8];
                preval1 = itemData.data[9];
                preval2 = itemData.data[10];

                // Write chambered round class
                FVRFireArmChamber integratedChamber = attachableFirearmGetChamberFunc();
                if (integratedChamber.GetRound() == null || integratedChamber.IsSpent || integratedChamber.GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 9);
                }
                else
                {
                    BitConverter.GetBytes((short)integratedChamber.GetRound().RoundType).CopyTo(itemData.data, 7);
                    BitConverter.GetBytes((short)integratedChamber.GetRound().RoundClass).CopyTo(itemData.data, 9);
                }

                modified |= (preval != itemData.data[7] || preval0 != itemData.data[8] || preval1 != itemData.data[9] || preval2 != itemData.data[10]);
            }

            return modified;
        }

        private bool UpdateGivenClosedBoltWeapon(byte[] newData)
        {
            bool modified = false;
            ClosedBoltWeapon asCBW = dataObject as ClosedBoltWeapon;

            if (itemData.data == null)
            {
                modified = true;

                // Set fire select mode
                asCBW.m_fireSelectorMode = newData[0];

                // Set camBurst
                asCBW.m_CamBurst = newData[1];
            }
            else
            {
                if (itemData.data[0] != newData[0])
                {
                    // Set fire select mode
                    asCBW.m_fireSelectorMode = newData[0];
                    modified = true;
                }
                if (itemData.data[1] != newData[1])
                {
                    // Set camBurst
                    asCBW.m_CamBurst = newData[1];
                    modified = true;
                }
            }

            // Set hammer state
            if (newData[2] == 0)
            {
                if (asCBW.IsHammerCocked)
                {
                    asCBW.m_isHammerCocked = BitConverter.ToBoolean(newData, 2);
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
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asCBW.Chamber.GetRound() != null)
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
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
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

            // Set integrated firearm chamber
            if (asCBW.GetIntegratedAttachableFirearm() != null)
            {
                chamberTypeIndex = BitConverter.ToInt16(newData, 7);
                chamberClassIndex = BitConverter.ToInt16(newData, 9);
                FVRFireArmChamber integratedChamber = attachableFirearmGetChamberFunc();
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (integratedChamber.GetRound() != null)
                    {
                        ++ChamberPatch.chamberSkip;
                        integratedChamber.SetRound(null, false);
                        --ChamberPatch.chamberSkip;
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (integratedChamber.GetRound() == null || integratedChamber.GetRound().RoundClass != roundClass)
                    {
                        if (integratedChamber.RoundType == roundType)
                        {
                            ++ChamberPatch.chamberSkip;
                            integratedChamber.SetRound(roundClass, integratedChamber.transform.position, integratedChamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                        }
                        else
                        {
                            FireArmRoundType prevRoundType = integratedChamber.RoundType;
                            integratedChamber.RoundType = roundType;
                            ++ChamberPatch.chamberSkip;
                            integratedChamber.SetRound(roundClass, integratedChamber.transform.position, integratedChamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                            integratedChamber.RoundType = prevRoundType;
                        }
                        modified = true;
                    }
                }
            }

            itemData.data = newData;

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
            if (chamberIndex == 1)
            {
                if (((int)roundClass) == -1)
                {
                    attachableFirearmGetChamberFunc().SetRound(null);
                }
                else
                {
                    attachableFirearmGetChamberFunc().SetRound(roundClass, asCBW.Chamber.transform.position, asCBW.Chamber.transform.rotation);
                }
            }
            else
            {
                if (((int)roundClass) == -1)
                {
                    asCBW.Chamber.SetRound(null);
                }
                else
                {
                    asCBW.Chamber.SetRound(roundClass, asCBW.Chamber.transform.position, asCBW.Chamber.transform.rotation);
                }
            }
            --ChamberPatch.chamberSkip;
        }

        private int GetClosedBoltWeaponChamberIndex(FVRFireArmChamber chamber)
        {
            ClosedBoltWeapon asCBW = (ClosedBoltWeapon)dataObject;

            if (asCBW.GetIntegratedAttachableFirearm() == null)
            {
                return 0;
            }
            else
            {
                return chamber == asCBW.Chamber ? 0 : 1;
            }
        }

        private bool FireCBW(int chamberIndex)
        {
            return (dataObject as ClosedBoltWeapon).Fire();
        }

        private bool UpdateHandgun()
        {
            Handgun asHandgun = dataObject as Handgun;
            bool modified = false;

            if (itemData.data == null)
            {
                itemData.data = new byte[asHandgun.GetIntegratedAttachableFirearm() == null ? 8 : 12];
                modified = true;
            }

            byte preval = itemData.data[0];

            // Write fire mode index
            itemData.data[0] = (byte)asHandgun.FireSelectorModeIndex;
            modified |= preval != itemData.data[0];
            preval = itemData.data[1];

            // Write camBurst
            itemData.data[1] = (byte)asHandgun.m_CamBurst;
            modified |= preval != itemData.data[1];
            preval = itemData.data[2];

            // Write hammer state
            itemData.data[2] = asHandgun.m_isHammerCocked ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[2];

            preval = itemData.data[3];
            byte preval0 = itemData.data[4];
            byte preval1 = itemData.data[5];
            byte preval2 = itemData.data[6];

            // Write chambered round class
            if (asHandgun.Chamber.GetRound() == null || asHandgun.Chamber.IsSpent || asHandgun.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 5);
            }
            else
            {
                BitConverter.GetBytes((short)asHandgun.Chamber.GetRound().RoundType).CopyTo(itemData.data, 3);
                BitConverter.GetBytes((short)asHandgun.Chamber.GetRound().RoundClass).CopyTo(itemData.data, 5);
            }

            modified |= (preval != itemData.data[3] || preval0 != itemData.data[4] || preval1 != itemData.data[5] || preval2 != itemData.data[6]);

            if (asHandgun.GetIntegratedAttachableFirearm() != null)
            {
                preval = itemData.data[7];
                preval0 = itemData.data[8];
                preval1 = itemData.data[9];
                preval2 = itemData.data[10];

                // Write chambered round class
                FVRFireArmChamber integratedChamber = attachableFirearmGetChamberFunc();
                if (integratedChamber.GetRound() == null || integratedChamber.IsSpent || integratedChamber.GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 9);
                }
                else
                {
                    BitConverter.GetBytes((short)integratedChamber.GetRound().RoundType).CopyTo(itemData.data, 7);
                    BitConverter.GetBytes((short)integratedChamber.GetRound().RoundClass).CopyTo(itemData.data, 9);
                }

                modified |= (preval != itemData.data[7] || preval0 != itemData.data[8] || preval1 != itemData.data[9] || preval2 != itemData.data[10]);

                if (integratedLaser != null)
                {
                    preval = itemData.data[11];
                    itemData.data[11] = integratedLaser.m_isOn ? (byte)1 : (byte)0;
                    modified |= preval != itemData.data[11];
                }
            }
            else
            {
                if (integratedLaser != null)
                {
                    preval = itemData.data[7];
                    itemData.data[7] = integratedLaser.m_isOn ? (byte)1 : (byte)0;
                    modified |= preval != itemData.data[7];
                }
            }

            return modified;
        }

        private bool UpdateGivenHandgun(byte[] newData)
        {
            bool modified = false;
            Handgun asHandgun = dataObject as Handgun;

            if (itemData.data == null)
            {
                modified = true;

                // Set fire select mode
                asHandgun.m_fireSelectorMode = newData[0];

                // Set camBurst
                asHandgun.m_CamBurst = newData[1];
            }
            else
            {
                if (itemData.data[0] != newData[0])
                {
                    // Set fire select mode
                    asHandgun.m_fireSelectorMode = newData[0];
                    modified = true;
                }
                if (itemData.data[1] != newData[1])
                {
                    // Set camBurst
                    asHandgun.m_CamBurst = newData[1];
                    modified = true;
                }
            }

            bool isHammerCocked = asHandgun.m_isHammerCocked;

            // Set hammer state
            if (newData[2] == 0)
            {
                if (isHammerCocked)
                {
                    asHandgun.m_isHammerCocked = BitConverter.ToBoolean(newData, 2);
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
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asHandgun.Chamber.GetRound() != null)
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
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
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

            // Set integrated firearm chamber
            if (asHandgun.GetIntegratedAttachableFirearm() != null)
            {
                chamberTypeIndex = BitConverter.ToInt16(newData, 7);
                chamberClassIndex = BitConverter.ToInt16(newData, 9);
                FVRFireArmChamber integratedChamber = attachableFirearmGetChamberFunc();
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (integratedChamber.GetRound() != null)
                    {
                        ++ChamberPatch.chamberSkip;
                        integratedChamber.SetRound(null, false);
                        --ChamberPatch.chamberSkip;
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (integratedChamber.GetRound() == null || integratedChamber.GetRound().RoundClass != roundClass)
                    {
                        if (integratedChamber.RoundType == roundType)
                        {
                            ++ChamberPatch.chamberSkip;
                            integratedChamber.SetRound(roundClass, integratedChamber.transform.position, integratedChamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                        }
                        else
                        {
                            FireArmRoundType prevRoundType = integratedChamber.RoundType;
                            integratedChamber.RoundType = roundType;
                            ++ChamberPatch.chamberSkip;
                            integratedChamber.SetRound(roundClass, integratedChamber.transform.position, integratedChamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                            integratedChamber.RoundType = prevRoundType;
                        }
                        modified = true;
                    }
                }

                if (integratedLaser != null)
                {
                    if (newData[11] == 1 && !integratedLaser.m_isOn)
                    {
                        integratedLaser.TurnOn();
                    }
                    else if (newData[11] == 0 && integratedLaser.m_isOn)
                    {
                        integratedLaser.TurnOff();
                    }
                }
            }
            else
            {
                if (integratedLaser != null)
                {
                    if (newData[7] == 1 && !integratedLaser.m_isOn)
                    {
                        integratedLaser.TurnOn();
                    }
                    else if (newData[7] == 0 && integratedLaser.m_isOn)
                    {
                        integratedLaser.TurnOff();
                    }
                }
            }

            itemData.data = newData;

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

        private int GetHandgunChamberIndex(FVRFireArmChamber chamber)
        {
            Handgun asHandgun = dataObject as Handgun;

            if (asHandgun.GetIntegratedAttachableFirearm() == null)
            {
                return 0;
            }
            else
            {
                return chamber == asHandgun.Chamber ? 0 : 1;
            }
        }

        private void ChamberHandgunRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            Handgun asHandgun = dataObject as Handgun;

            ++ChamberPatch.chamberSkip;
            if (chamberIndex == 1)
            {
                if (((int)roundClass) == -1)
                {
                    attachableFirearmGetChamberFunc().SetRound(null);
                }
                else
                {
                    attachableFirearmGetChamberFunc().SetRound(roundClass, asHandgun.Chamber.transform.position, asHandgun.Chamber.transform.rotation);
                }
            }
            else
            {
                if (((int)roundClass) == -1)
                {
                    asHandgun.Chamber.SetRound(null);
                }
                else
                {
                    asHandgun.Chamber.SetRound(roundClass, asHandgun.Chamber.transform.position, asHandgun.Chamber.transform.rotation);
                }
            }
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

            if (itemData.data == null)
            {
                itemData.data = new byte[asTFS.GetIntegratedAttachableFirearm() == null ? 8 : 12];
                modified = true;
            }

            byte preval = itemData.data[0];

            // Write fire mode index
            itemData.data[0] = asTFS.IsSafetyEngaged ? (byte)1 : (byte)0;

            modified |= preval != itemData.data[0];

            preval = itemData.data[1];

            // Write hammer state
            itemData.data[1] = BitConverter.GetBytes(asTFS.IsHammerCocked)[0];

            modified |= preval != itemData.data[1];

            preval = itemData.data[2];
            byte preval0 = itemData.data[3];
            byte preval1 = itemData.data[4];
            byte preval2 = itemData.data[5];

            // Write chambered round class
            if (asTFS.Chamber.GetRound() == null || asTFS.Chamber.IsSpent || asTFS.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 4);
            }
            else
            {
                BitConverter.GetBytes((short)asTFS.Chamber.GetRound().RoundType).CopyTo(itemData.data, 2);
                BitConverter.GetBytes((short)asTFS.Chamber.GetRound().RoundClass).CopyTo(itemData.data, 4);
            }

            modified |= (preval != itemData.data[2] || preval0 != itemData.data[3] || preval1 != itemData.data[4] || preval2 != itemData.data[5]);

            preval = itemData.data[6];

            // Write bolt handle pos
            itemData.data[6] = (byte)asTFS.Bolt.CurPos;

            modified |= preval != itemData.data[6];

            if (asTFS.HasHandle)
            {
                preval = itemData.data[7];

                // Write bolt handle pos
                itemData.data[7] = (byte)asTFS.Handle.CurPos;

                modified |= preval != itemData.data[7];
            }

            if (asTFS.GetIntegratedAttachableFirearm() != null)
            {
                preval = itemData.data[8];
                preval0 = itemData.data[9];
                preval1 = itemData.data[10];
                preval2 = itemData.data[11];

                // Write chambered round class
                FVRFireArmChamber integratedChamber = attachableFirearmGetChamberFunc();
                if (integratedChamber.GetRound() == null || integratedChamber.IsSpent || integratedChamber.GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 10);
                }
                else
                {
                    BitConverter.GetBytes((short)integratedChamber.GetRound().RoundType).CopyTo(itemData.data, 8);
                    BitConverter.GetBytes((short)integratedChamber.GetRound().RoundClass).CopyTo(itemData.data, 10);
                }

                modified |= (preval != itemData.data[8] || preval0 != itemData.data[9] || preval1 != itemData.data[10] || preval2 != itemData.data[11]);
            }

            return modified;
        }

        private bool UpdateGivenTubeFedShotgun(byte[] newData)
        {
            bool modified = false;
            TubeFedShotgun asTFS = dataObject as TubeFedShotgun;

            if (itemData.data == null)
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
                if (itemData.data[6] != newData[6])
                {
                    // Set bolt pos
                    asTFS.Bolt.LastPos = asTFS.Bolt.CurPos;
                    asTFS.Bolt.CurPos = (TubeFedShotgunBolt.BoltPos)newData[6];
                }
                if (asTFS.HasHandle && itemData.data[7] != newData[7])
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
                    asTFS.m_isHammerCocked = false;
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
            if (chamberClassIndex == -1) // We don't want round in chamber
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
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
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

            // Set integrated firearm chamber
            if (asTFS.GetIntegratedAttachableFirearm() != null)
            {
                chamberTypeIndex = BitConverter.ToInt16(newData, 8);
                chamberClassIndex = BitConverter.ToInt16(newData, 11);
                FVRFireArmChamber integratedChamber = attachableFirearmGetChamberFunc();
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (integratedChamber.GetRound() != null)
                    {
                        ++ChamberPatch.chamberSkip;
                        integratedChamber.SetRound(null, false);
                        --ChamberPatch.chamberSkip;
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (integratedChamber.GetRound() == null || integratedChamber.GetRound().RoundClass != roundClass)
                    {
                        if (integratedChamber.RoundType == roundType)
                        {
                            ++ChamberPatch.chamberSkip;
                            integratedChamber.SetRound(roundClass, integratedChamber.transform.position, integratedChamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                        }
                        else
                        {
                            FireArmRoundType prevRoundType = integratedChamber.RoundType;
                            integratedChamber.RoundType = roundType;
                            ++ChamberPatch.chamberSkip;
                            integratedChamber.SetRound(roundClass, integratedChamber.transform.position, integratedChamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                            integratedChamber.RoundType = prevRoundType;
                        }
                        modified = true;
                    }
                }
            }

            itemData.data = newData;

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

        private int GetTubeFedShotgunChamberIndex(FVRFireArmChamber chamber)
        {
            TubeFedShotgun asTFS = dataObject as TubeFedShotgun;

            if (asTFS.GetIntegratedAttachableFirearm() == null)
            {
                return 0;
            }
            else
            {
                return chamber == asTFS.Chamber ? 0 : 1;
            }
        }

        private void ChamberTubeFedShotgunRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            TubeFedShotgun asTFS = dataObject as TubeFedShotgun;

            ++ChamberPatch.chamberSkip;
            if (chamberIndex == 1)
            {
                if (((int)roundClass) == -1)
                {
                    attachableFirearmGetChamberFunc().SetRound(null);
                }
                else
                {
                    attachableFirearmGetChamberFunc().SetRound(roundClass, asTFS.Chamber.transform.position, asTFS.Chamber.transform.rotation);
                }
            }
            else
            {
                if (((int)roundClass) == -1)
                {
                    asTFS.Chamber.SetRound(null);
                }
                else
                {
                    asTFS.Chamber.SetRound(roundClass, asTFS.Chamber.transform.position, asTFS.Chamber.transform.rotation);
                }
            }
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

            if (itemData.data == null)
            {
                itemData.data = new byte[asBAR.GetIntegratedAttachableFirearm() == null ? 8 : 12];
                modified = true;
            }

            byte preval = itemData.data[0];

            // Write fire mode index
            itemData.data[0] = (byte)asBAR.m_fireSelectorMode;

            modified |= preval != itemData.data[0];

            preval = itemData.data[1];

            // Write hammer state
            itemData.data[1] = BitConverter.GetBytes(asBAR.IsHammerCocked)[0];

            modified |= preval != itemData.data[1];

            preval = itemData.data[2];
            byte preval0 = itemData.data[3];
            byte preval1 = itemData.data[4];
            byte preval2 = itemData.data[5];

            // Write chambered round class
            if (asBAR.Chamber.GetRound() == null || asBAR.Chamber.IsSpent || asBAR.Chamber.GetRound().IsSpent)
            {
                BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 4);
            }
            else
            {
                BitConverter.GetBytes((short)asBAR.Chamber.GetRound().RoundType).CopyTo(itemData.data, 2);
                BitConverter.GetBytes((short)asBAR.Chamber.GetRound().RoundClass).CopyTo(itemData.data, 4);
            }

            modified |= (preval != itemData.data[2] || preval0 != itemData.data[3] || preval1 != itemData.data[4] || preval2 != itemData.data[5]);

            preval = itemData.data[6];

            // Write bolt handle state
            itemData.data[6] = (byte)asBAR.CurBoltHandleState;

            modified |= preval != itemData.data[6];

            preval = itemData.data[7];

            // Write bolt handle rot
            itemData.data[7] = (byte)asBAR.BoltHandle.HandleRot;

            modified |= preval != itemData.data[7];

            if (asBAR.GetIntegratedAttachableFirearm() != null)
            {
                preval = itemData.data[8];
                preval0 = itemData.data[9];
                preval1 = itemData.data[10];
                preval2 = itemData.data[11];

                // Write chambered round class
                FVRFireArmChamber integratedChamber = attachableFirearmGetChamberFunc();
                if (integratedChamber.GetRound() == null || asBAR.Chamber.IsSpent || integratedChamber.GetRound().IsSpent)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(itemData.data, 10);
                }
                else
                {
                    BitConverter.GetBytes((short)integratedChamber.GetRound().RoundType).CopyTo(itemData.data, 8);
                    BitConverter.GetBytes((short)integratedChamber.GetRound().RoundClass).CopyTo(itemData.data, 10);
                }

                modified |= (preval != itemData.data[8] || preval0 != itemData.data[9] || preval1 != itemData.data[10] || preval2 != itemData.data[11]);
            }

            return modified;
        }

        private bool UpdateGivenBoltActionRifle(byte[] newData)
        {
            bool modified = false;
            BoltActionRifle asBAR = dataObject as BoltActionRifle;

            if (itemData.data == null)
            {
                modified = true;

                // Set fire select mode
                asBAR.m_fireSelectorMode = newData[0];

                // Set bolt handle state
                asBAR.LastBoltHandleState = asBAR.CurBoltHandleState;
                asBAR.CurBoltHandleState = (BoltActionRifle_Handle.BoltActionHandleState)newData[6];

                // Set bolt handle rot
                asBAR.BoltHandle.LastHandleRot = asBAR.BoltHandle.HandleRot;
                asBAR.BoltHandle.HandleRot = (BoltActionRifle_Handle.BoltActionHandleRot)newData[7];
            }
            else
            {
                if (itemData.data[0] != newData[0])
                {
                    // Set fire select mode
                    asBAR.m_fireSelectorMode = newData[0];
                    modified = true;
                }
                if (itemData.data[6] != newData[6])
                {
                    // Set bolt handle state
                    asBAR.LastBoltHandleState = asBAR.CurBoltHandleState;
                    asBAR.CurBoltHandleState = (BoltActionRifle_Handle.BoltActionHandleState)newData[6];
                }
                if (itemData.data[7] != newData[7])
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
                    asBAR.m_isHammerCocked = BitConverter.ToBoolean(newData, 1);
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
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asBAR.Chamber.GetRound() != null)
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
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
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

            // Set integrated firearm chamber
            if (asBAR.GetIntegratedAttachableFirearm() != null)
            {
                chamberTypeIndex = BitConverter.ToInt16(newData, 8);
                chamberClassIndex = BitConverter.ToInt16(newData, 11);
                FVRFireArmChamber integratedChamber = attachableFirearmGetChamberFunc();
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (integratedChamber.GetRound() != null)
                    {
                        ++ChamberPatch.chamberSkip;
                        integratedChamber.SetRound(null, false);
                        --ChamberPatch.chamberSkip;
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundType roundType = (FireArmRoundType)chamberTypeIndex;
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (integratedChamber.GetRound() == null || integratedChamber.GetRound().RoundClass != roundClass)
                    {
                        if (integratedChamber.RoundType == roundType)
                        {
                            ++ChamberPatch.chamberSkip;
                            integratedChamber.SetRound(roundClass, integratedChamber.transform.position, integratedChamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                        }
                        else
                        {
                            FireArmRoundType prevRoundType = integratedChamber.RoundType;
                            integratedChamber.RoundType = roundType;
                            ++ChamberPatch.chamberSkip;
                            integratedChamber.SetRound(roundClass, integratedChamber.transform.position, integratedChamber.transform.rotation);
                            --ChamberPatch.chamberSkip;
                            integratedChamber.RoundType = prevRoundType;
                        }
                        modified = true;
                    }
                }
            }

            itemData.data = newData;

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

        private int GetBoltActionRifleChamberIndex(FVRFireArmChamber chamber)
        {
            BoltActionRifle asBar = dataObject as BoltActionRifle;

            if (asBar.GetIntegratedAttachableFirearm() == null)
            {
                return 0;
            }
            else
            {
                return chamber == asBar.Chamber ? 0 : 1;
            }
        }

        private void ChamberBoltActionRifleRound(FireArmRoundClass roundClass, FireArmRoundType roundType, int chamberIndex)
        {
            BoltActionRifle asBar = dataObject as BoltActionRifle;

            ++ChamberPatch.chamberSkip;
            if (chamberIndex == 1)
            {
                if (((int)roundClass) == -1)
                {
                    attachableFirearmGetChamberFunc().SetRound(null);
                }
                else
                {
                    attachableFirearmGetChamberFunc().SetRound(roundClass, asBar.Chamber.transform.position, asBar.Chamber.transform.rotation);
                }
            }
            else
            {
                if (((int)roundClass) == -1)
                {
                    asBar.Chamber.SetRound(null);
                }
                else
                {
                    asBar.Chamber.SetRound(roundClass, asBar.Chamber.transform.position, asBar.Chamber.transform.rotation);
                }
            }
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

            if (itemData.data == null)
            {
                itemData.data = new byte[21];
                modified = true;
            }

            byte preIndex = itemData.data[0];

            // Write attached mount index
            if (asAttachment.curMount == null)
            {
                itemData.data[0] = 255;
            }
            else
            {
                // Find the mount and set it
                bool found = false;
                for (int i = 0; i < asAttachment.curMount.MyObject.AttachmentMounts.Count; ++i)
                {
                    if (asAttachment.curMount.MyObject.AttachmentMounts[i] == asAttachment.curMount)
                    {
                        itemData.data[0] = (byte)i;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    itemData.data[0] = 255;
                }
            }

            byte[] preVals = new byte[4];
            preVals[0] = itemData.data[1];
            preVals[1] = itemData.data[2];
            preVals[2] = itemData.data[3];
            preVals[3] = itemData.data[4];
            BitConverter.GetBytes(asAttachment.CatchRot).CopyTo(itemData.data, 1);
            modified |= (preVals[0] != itemData.data[1] || preVals[1] != itemData.data[2] || preVals[2] != itemData.data[3] || preVals[3] != itemData.data[4]);

            preVals[0] = itemData.data[5];
            preVals[1] = itemData.data[6];
            preVals[2] = itemData.data[7];
            preVals[3] = itemData.data[8];

            // Write mountObjectID
            BitConverter.GetBytes(mountObjectID).CopyTo(itemData.data, 5);

            modified |= (preVals[0] != itemData.data[5] || preVals[1] != itemData.data[6] || preVals[2] != itemData.data[7] || preVals[3] != itemData.data[8]);

            // Write mount object scale
            if (transform.parent == null)
            {
                mountObjectScale = Vector3.one;
                preVals[0] = itemData.data[9];
                preVals[1] = itemData.data[10];
                preVals[2] = itemData.data[11];
                preVals[3] = itemData.data[12];
                BitConverter.GetBytes(1).CopyTo(itemData.data, 9);
                modified |= (preVals[0] != itemData.data[9] || preVals[1] != itemData.data[10] || preVals[2] != itemData.data[11] || preVals[3] != itemData.data[12]);

                preVals[0] = itemData.data[13];
                preVals[1] = itemData.data[14];
                preVals[2] = itemData.data[15];
                preVals[3] = itemData.data[16];
                BitConverter.GetBytes(1).CopyTo(itemData.data, 13);
                modified |= (preVals[0] != itemData.data[13] || preVals[1] != itemData.data[14] || preVals[2] != itemData.data[15] || preVals[3] != itemData.data[16]);

                preVals[0] = itemData.data[17];
                preVals[1] = itemData.data[18];
                preVals[2] = itemData.data[19];
                preVals[3] = itemData.data[20];
                BitConverter.GetBytes(1).CopyTo(itemData.data, 17);
                modified |= (preVals[0] != itemData.data[17] || preVals[1] != itemData.data[18] || preVals[2] != itemData.data[19] || preVals[3] != itemData.data[20]);
            }
            else
            {
                mountObjectScale = transform.parent.localScale;
                preVals[0] = itemData.data[9];
                preVals[1] = itemData.data[10];
                preVals[2] = itemData.data[11];
                preVals[3] = itemData.data[12];
                BitConverter.GetBytes(transform.parent.localScale.x).CopyTo(itemData.data, 9);
                modified |= (preVals[0] != itemData.data[9] || preVals[1] != itemData.data[10] || preVals[2] != itemData.data[11] || preVals[3] != itemData.data[12]);

                preVals[0] = itemData.data[13];
                preVals[1] = itemData.data[14];
                preVals[2] = itemData.data[15];
                preVals[3] = itemData.data[16];
                BitConverter.GetBytes(transform.parent.localScale.y).CopyTo(itemData.data, 13);
                modified |= (preVals[0] != itemData.data[13] || preVals[1] != itemData.data[14] || preVals[2] != itemData.data[15] || preVals[3] != itemData.data[16]);

                preVals[0] = itemData.data[17];
                preVals[1] = itemData.data[18];
                preVals[2] = itemData.data[19];
                preVals[3] = itemData.data[20];
                BitConverter.GetBytes(transform.parent.localScale.z).CopyTo(itemData.data, 17);
                modified |= (preVals[0] != itemData.data[17] || preVals[1] != itemData.data[18] || preVals[2] != itemData.data[19] || preVals[3] != itemData.data[20]);
            }

            return modified || (preIndex != itemData.data[0]);
        }

        private bool UpdateGivenSuppressor(byte[] newData)
        {
            bool modified = false;
            Suppressor asAttachment = dataObject as Suppressor;

            if (itemData.data == null || itemData.data.Length != newData.Length)
            {
                itemData.data = new byte[5];
                itemData.data[0] = 255;
                currentMountIndex = 255;
                modified = true;
            }
            Mod.LogInfo("UpdateGivenSuppressor on " + name + " at " + data.trackedID + ", mount index: " + newData[0]);

            byte preMountIndex = currentMountIndex;
            if (newData[0] == 255)
            {
                Mod.LogInfo("\tNo mount index");
                // Check if were waiting for mount object
                if (mountObjectID != -1 && toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                {
                    Mod.LogInfo("\t\tWe were waiting for mount object, removing");
                    list.Remove(this);
                }

                // Should not be mounted, check if currently is
                if (asAttachment.curMount != null)
                {
                    Mod.LogInfo("\t\tWe have mount, detaching");
                    asAttachment.Sensor.m_storedScaleMagnified = 1f;
                    asAttachment.transform.localScale = new Vector3(1, 1, 1);
                    ++data.ignoreParentChanged;
                    asAttachment.DetachFromMount();
                    --data.ignoreParentChanged;
                    currentMountIndex = 255;

                    // Detach from mount will recover rigidbody, set as kinematic if not controller
                    if (data.controller != GameManager.ID)
                    {
                        Mod.SetKinematicRecursive(asAttachment.transform, true);
                    }
                }
            }
            else
            {
                mountObjectID = BitConverter.ToInt32(newData, 5);
                mountObjectScale = new Vector3(BitConverter.ToSingle(newData, 9), BitConverter.ToSingle(newData, 13), BitConverter.ToSingle(newData, 17));
                Mod.LogInfo("\tHave mount index, object ID: " + mountObjectID + ", scale: " + mountObjectScale.ToString());
                if (mountObjectID == -1)
                {
                    Mod.LogInfo("\t\tWe have mount index but no mount object yet");
                    // Detach from any mount we are still on
                    if (asAttachment.curMount != null)
                    {
                        asAttachment.Sensor.m_storedScaleMagnified = 1f;
                        asAttachment.transform.localScale = new Vector3(1, 1, 1);
                        ++data.ignoreParentChanged;
                        asAttachment.DetachFromMount();
                        --data.ignoreParentChanged;

                        // Detach from mount will recover rigidbody, set as kinematic if not controller
                        if (data.controller != GameManager.ID)
                        {
                            Mod.SetKinematicRecursive(asAttachment.transform, true);
                        }
                    }
                }
                else
                {
                    Mod.LogInfo("\t\tHave mount index and mount object ID");
                    // Find mount instance we want to be mounted to
                    FVRFireArmAttachmentMount mount = null;
                    TrackedItemData parentTrackedItemData = null;
                    if (ThreadManager.host)
                    {
                        parentTrackedItemData = Server.objects[mountObjectID] as TrackedItemData;
                    }
                    else
                    {
                        parentTrackedItemData = Client.objects[mountObjectID] as TrackedItemData;
                    }

                    bool addedForLater = false;
                    if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null)
                    {
                        Mod.LogInfo("\t\t\tGot mount object " + parentTrackedItemData.physicalItem.name);
                        // We want to be mounted, we have a parent
                        if (parentTrackedItemData.physicalItem.physicalItem.AttachmentMounts.Count > newData[0])
                        {
                            Mod.LogInfo("\t\t\t\tMount index fits");
                            mount = parentTrackedItemData.physicalItem.physicalItem.AttachmentMounts[newData[0]];
                        }
                    }
                    else
                    {
                        Mod.LogInfo("\t\t\tDont yet have mount object, adding to toAttachByMountObjectID");
                        addedForLater = true;
                        if (toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                        {
                            list.Add(this);
                        }
                        else
                        {
                            toAttachByMountObjectID.Add(mountObjectID, new List<TrackedItem>() { this });
                        }
                    }

                    // Mount could be null if the mount index corresponds to a parent we have yet a receive a change to
                    if (mount != null && mount != asAttachment.curMount)
                    {
                        Mod.LogInfo("\t\t\tGot mount and is different: " + mount.name);
                        ++data.ignoreParentChanged;
                        if (asAttachment.curMount != null)
                        {
                            Mod.LogInfo("\t\t\t\tDetaching from old mount first");
                            asAttachment.Sensor.m_storedScaleMagnified = 1f;
                            asAttachment.transform.localScale = new Vector3(1, 1, 1);
                            asAttachment.DetachFromMount();
                        }

                        if (CanAttachToMount(asAttachment, mount))
                        {
                            Mod.LogInfo("\t\t\tCan attach");
                            // Apply mount object scale
                            if (mount.GetRootMount().ParentToThis)
                            {
                                mount.GetRootMount().transform.localScale = mountObjectScale;
                            }
                            else
                            {
                                mount.MyObject.transform.localScale = mountObjectScale;
                            }

                            if (asAttachment.CanScaleToMount && mount.CanThisRescale())
                            {
                                Mod.LogInfo("\t\t\t\tRescaling first");
                                asAttachment.ScaleToMount(mount);
                            }
                            asAttachment.AttachToMount(mount, true);

                            // Check if there were any children attachments waiting for us
                            if (toAttachByMountObjectID.TryGetValue(data.trackedID, out List<TrackedItem> list))
                            {
                                Mod.LogInfo("\t\t\tChildren attachment waiting for us, updating them");
                                for (int i = 0; i < list.Count; ++i)
                                {
                                    if (list[i] != null)
                                    {
                                        Mod.LogInfo("\t\t\t\tUpdating " + list[i].name + " at " + list[i].data.trackedID);
                                        list[i].data.UpdateFromData(list[i].data);
                                    }
                                }

                                toAttachByMountObjectID.Remove(data.trackedID);
                            }
                        }
                        else if(!addedForLater) // Can't attach to mount yet, probably because parent is unattached attachment, but will need to eventually
                        {
                            Mod.LogInfo("\t\t\tcan't mount " + name + " on " + mount.name + " yet, adding to toAttachByMountObjectID");
                            if (toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                            {
                                list.Add(this);
                            }
                            else
                            {
                                toAttachByMountObjectID.Add(mountObjectID, new List<TrackedItem>() { this });
                            }
                        }
                        currentMountIndex = newData[0];
                        --data.ignoreParentChanged;
                    }
                }
            }

            float newRot = BitConverter.ToSingle(newData, 1);
            if (asAttachment.CatchRot != newRot)
            {
                asAttachment.CatchRot = newRot;
                asAttachment.transform.localEulerAngles = new Vector3(0f, 0f, newRot);
                modified = true;
            }

            itemData.data = newData;

            return modified || (preMountIndex != currentMountIndex);
        }

        private bool UpdateAttachment()
        {
            bool modified = false;
            FVRFireArmAttachment asAttachment = dataObject as FVRFireArmAttachment;

            if (itemData.data == null)
            {
                itemData.data = new byte[17 + attachmentInterfaceDataSize];
                modified = true;
            }

            byte preIndex = currentMountIndex;

            // Write attached mount index
            if (asAttachment.curMount == null)
            {
                itemData.data[0] = 255;
                currentMountIndex = 255;
                mountObjectID = -1;
            }
            else
            {
                // Find the mount and set it
                bool found = false;
                for (int i = 0; i < asAttachment.curMount.MyObject.AttachmentMounts.Count; ++i)
                {
                    if (asAttachment.curMount.MyObject.AttachmentMounts[i] == asAttachment.curMount)
                    {
                        itemData.data[0] = (byte)i;
                        currentMountIndex = itemData.data[0];
                        // Note: Instead of updating mountObjectID here, since it would be expensive, we set it on parent change and upon tracking
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    itemData.data[0] = 255;
                    currentMountIndex = 255;
                    mountObjectID = -1;
                }
            }

            byte[] preVals = new byte[4];
            preVals[0] = itemData.data[1];
            preVals[1] = itemData.data[2];
            preVals[2] = itemData.data[3];
            preVals[3] = itemData.data[4];

            // Write mountObjectID
            BitConverter.GetBytes(mountObjectID).CopyTo(itemData.data, 1);

            modified |= (preVals[0] != itemData.data[1] || preVals[1] != itemData.data[2] || preVals[2] != itemData.data[3] || preVals[3] != itemData.data[4]);

            // Write mount object scale
            if (transform.parent == null)
            {
                mountObjectScale = Vector3.one;
                preVals[0] = itemData.data[5];
                preVals[1] = itemData.data[6];
                preVals[2] = itemData.data[7];
                preVals[3] = itemData.data[8];
                BitConverter.GetBytes(1).CopyTo(itemData.data, 5);
                modified |= (preVals[0] != itemData.data[5] || preVals[1] != itemData.data[6] || preVals[2] != itemData.data[7] || preVals[3] != itemData.data[8]);

                preVals[0] = itemData.data[9];
                preVals[1] = itemData.data[10];
                preVals[2] = itemData.data[11];
                preVals[3] = itemData.data[12];
                BitConverter.GetBytes(1).CopyTo(itemData.data, 9);
                modified |= (preVals[0] != itemData.data[9] || preVals[1] != itemData.data[10] || preVals[2] != itemData.data[11] || preVals[3] != itemData.data[12]);

                preVals[0] = itemData.data[13];
                preVals[1] = itemData.data[14];
                preVals[2] = itemData.data[15];
                preVals[3] = itemData.data[16];
                BitConverter.GetBytes(1).CopyTo(itemData.data, 13);
                modified |= (preVals[0] != itemData.data[13] || preVals[1] != itemData.data[14] || preVals[2] != itemData.data[15] || preVals[3] != itemData.data[16]);
            }
            else
            {
                mountObjectScale = transform.parent.localScale;
                preVals[0] = itemData.data[5];
                preVals[1] = itemData.data[6];
                preVals[2] = itemData.data[7];
                preVals[3] = itemData.data[8];
                BitConverter.GetBytes(transform.parent.localScale.x).CopyTo(itemData.data, 5);
                modified |= (preVals[0] != itemData.data[5] || preVals[1] != itemData.data[6] || preVals[2] != itemData.data[7] || preVals[3] != itemData.data[8]);

                preVals[0] = itemData.data[9];
                preVals[1] = itemData.data[10];
                preVals[2] = itemData.data[11];
                preVals[3] = itemData.data[12];
                BitConverter.GetBytes(transform.parent.localScale.y).CopyTo(itemData.data, 9);
                modified |= (preVals[0] != itemData.data[9] || preVals[1] != itemData.data[10] || preVals[2] != itemData.data[11] || preVals[3] != itemData.data[12]);

                preVals[0] = itemData.data[13];
                preVals[1] = itemData.data[14];
                preVals[2] = itemData.data[15];
                preVals[3] = itemData.data[16];
                BitConverter.GetBytes(transform.parent.localScale.z).CopyTo(itemData.data, 13);
                modified |= (preVals[0] != itemData.data[13] || preVals[1] != itemData.data[14] || preVals[2] != itemData.data[15] || preVals[3] != itemData.data[16]);
            }

            // Do interface update
            if (attachmentInterfaceUpdateFunc != null)
            {
                attachmentInterfaceUpdateFunc(asAttachment, ref modified);
            }

            return modified || (preIndex != currentMountIndex);
        }

        private bool UpdateGivenAttachment(byte[] newData)
        {
            bool modified = false;
            FVRFireArmAttachment asAttachment = dataObject as FVRFireArmAttachment;

            if (itemData.data == null || itemData.data.Length != newData.Length)
            {
                itemData.data = new byte[1 + attachmentInterfaceDataSize];
                itemData.data[0] = 255;
                currentMountIndex = 255;
                modified = true;
            }

            byte preMountIndex = currentMountIndex;
            if (newData[0] == 255)
            {
                // Check if were waiting for mount object
                if (mountObjectID != -1 && toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                {
                    list.Remove(this);
                }

                // Should not be mounted, check if currently is
                if (asAttachment.curMount != null)
                {
                    asAttachment.Sensor.m_storedScaleMagnified = 1f;
                    asAttachment.transform.localScale = new Vector3(1, 1, 1);
                    ++data.ignoreParentChanged;
                    asAttachment.DetachFromMount();
                    --data.ignoreParentChanged;
                    currentMountIndex = 255;

                    // Detach from mount will recover rigidbody, set as kinematic if not controller
                    if (data.controller != GameManager.ID)
                    {
                        Mod.SetKinematicRecursive(asAttachment.transform, true);
                    }
                }
            }
            else // Have mount index
            {
                mountObjectID = BitConverter.ToInt32(newData, 1);
                mountObjectScale = new Vector3(BitConverter.ToSingle(newData, 5), BitConverter.ToSingle(newData, 9), BitConverter.ToSingle(newData, 13));
                if (mountObjectID == -1)
                {
                    // We have mount index but no mount object ID
                    // Detach from any mount we are still on
                    if (asAttachment.curMount != null)
                    {
                        asAttachment.Sensor.m_storedScaleMagnified = 1f;
                        asAttachment.transform.localScale = new Vector3(1, 1, 1);
                        asAttachment.DetachFromMount();

                        // Detach from mount will recover rigidbody, set as kinematic if not controller
                        if (data.controller != GameManager.ID)
                        {
                            Mod.SetKinematicRecursive(asAttachment.transform, true);
                        }
                    }
                }
                else // We have mount object ID and mount index, must attach
                {
                    // Find mount object by mount object ID
                    FVRFireArmAttachmentMount mount = null;
                    TrackedItemData parentTrackedItemData = null;
                    if (ThreadManager.host)
                    {
                        parentTrackedItemData = Server.objects[mountObjectID] as TrackedItemData;
                    }
                    else
                    {
                        parentTrackedItemData = Client.objects[mountObjectID] as TrackedItemData;
                    }

                    // Find mount instance we want to be mounted to
                    if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null)
                    {
                        // We want to be mounted, we have a parent
                        if (parentTrackedItemData.physicalItem.physicalItem.AttachmentMounts.Count > newData[0])
                        {
                            mount = parentTrackedItemData.physicalItem.physicalItem.AttachmentMounts[newData[0]];
                        }
                    }
                    else
                    {
                        if (toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                        {
                            list.Add(this);
                        }
                        else
                        {
                            toAttachByMountObjectID.Add(mountObjectID, new List<TrackedItem>() { this });
                        }
                    }

                    // Mount could be null if the mount index corresponds to a parent we have yet to receive a change to
                    if (mount != null && mount != asAttachment.curMount)
                    {
                        if (asAttachment.curMount != null)
                        {
                            asAttachment.Sensor.m_storedScaleMagnified = 1f;
                            asAttachment.transform.localScale = new Vector3(1, 1, 1);
                            asAttachment.DetachFromMount();
                        }

                        if (CanAttachToMount(asAttachment, mount))
                        {
                            // Apply mount object scale
                            if (mount.GetRootMount().ParentToThis)
                            {
                                mount.GetRootMount().transform.localScale = mountObjectScale;
                            }
                            else
                            {
                                mount.MyObject.transform.localScale = mountObjectScale;
                            }

                            if (asAttachment.CanScaleToMount && mount.CanThisRescale())
                            {
                                asAttachment.ScaleToMount(mount);
                            }
                            asAttachment.AttachToMount(mount, true);

                            // Check if there were any children attachments waiting for us
                            if (toAttachByMountObjectID.TryGetValue(data.trackedID, out List<TrackedItem> list))
                            {
                                for (int i = 0; i < list.Count; ++i)
                                {
                                    if (list[i] != null)
                                    {
                                        list[i].data.UpdateFromData(list[i].data);
                                    }
                                }

                                toAttachByMountObjectID.Remove(data.trackedID);
                            }
                        }
                        else // Can't attach to mount yet, probably because parent is unattached attachment, but will need to eventually
                        {
                            if (toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                            {
                                list.Add(this);
                            }
                            else
                            {
                                toAttachByMountObjectID.Add(mountObjectID, new List<TrackedItem>() { this });
                            }
                        }
                        currentMountIndex = newData[0];
                    }
                }
            }

            // Do interface update
            if (attachmentInterfaceUpdateGivenFunc != null)
            {
                attachmentInterfaceUpdateGivenFunc(asAttachment, newData, ref modified);
            }

            itemData.data = newData;

            return modified || (preMountIndex != currentMountIndex);
        }

        // This should be called when the parent of an attachment changes
        // The point is that the attachment could have gotten up to date data telling it that it should
        // be attached to a specific mount on its parent
        // But when it got this update, it was not yet parented to the correct parent
        // This would mean that it was attached on the wrong mount or not mounted at all
        // This data will not change until something changes on controller side
        // Data update does not specify parent, the parent must be set by sending a specific order to update it (TODO: Review: could we just send the parent ID in every update instead of only full?)
        // So when the parent changes, we must call this to attach it to proper mount on new parent
        private void UpdateAttachmentParent()
        {
            FVRFireArmAttachment asAttachment = dataObject as FVRFireArmAttachment;

            if (itemData.data[0] != 255) // We want to be attached to a mount
            {
                if (mountObjectID != -1) // We have parent
                {
                    // We could be on wrong mount (or none physically) if we got a new mount through update but the parent hadn't been updated yet

                    // Get the mount we are supposed to be mounted to
                    FVRFireArmAttachmentMount mount = null;
                    TrackedItemData parentTrackedItemData = null;
                    if (ThreadManager.host)
                    {
                        parentTrackedItemData = Server.objects[mountObjectID] as TrackedItemData;
                    }
                    else
                    {
                        parentTrackedItemData = Client.objects[mountObjectID] as TrackedItemData;
                    }

                    Mod.LogInfo("Trying to mount item at " + data.trackedID + " " + name + " to parent mount: " + itemData.data[0]);
                    if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null && parentTrackedItemData.physicalItem.physicalItem.AttachmentMounts.Count > itemData.data[0])
                    {
                        mount = parentTrackedItemData.physicalItem.physicalItem.AttachmentMounts[itemData.data[0]];
                        Mod.LogInfo("\tGot enough mounts, null?: " + (mount == null));
                    }
                    else // Can't attach to mount yet, probably because parent is unattached attachment, but will need to eventually
                    {
                        if (toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                        {
                            list.Add(this);
                        }
                        else
                        {
                            toAttachByMountObjectID.Add(mountObjectID, new List<TrackedItem>() { this });
                        }
                    }

                    // If not yet physically mounted to anything, can right away mount to the proper mount
                    if (asAttachment.curMount == null)
                    {
                        if (mount != null && CanAttachToMount(asAttachment, mount))
                        {
                            // Apply mount object scale
                            if (mount.GetRootMount().ParentToThis)
                            {
                                mount.GetRootMount().transform.localScale = mountObjectScale;
                            }
                            else
                            {
                                mount.MyObject.transform.localScale = mountObjectScale;
                            }

                            if (asAttachment.CanScaleToMount && mount.CanThisRescale())
                            {
                                asAttachment.ScaleToMount(mount);
                            }
                            ++data.ignoreParentChanged;
                            asAttachment.AttachToMount(mount, true);
                            --data.ignoreParentChanged;

                            // Check if there were any children attachments waiting for us
                            if (toAttachByMountObjectID.TryGetValue(data.trackedID, out List<TrackedItem> list))
                            {
                                for (int i = 0; i < list.Count; ++i)
                                {
                                    if (list[i] != null)
                                    {
                                        list[i].data.UpdateFromData(list[i].data);
                                    }
                                }

                                toAttachByMountObjectID.Remove(data.trackedID);
                            }
                        }
                        else // Can't attach to mount yet, probably because parent is unattached attachment, but will need to eventually
                        {
                            if (toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                            {
                                list.Add(this);
                            }
                            else
                            {
                                toAttachByMountObjectID.Add(mountObjectID, new List<TrackedItem>() { this });
                            }
                        }
                    }
                    else if (asAttachment.curMount != mount) // Already mounted, but not on the right one, need to unmount, then mount of right one
                    {
                        ++data.ignoreParentChanged;
                        if (asAttachment.curMount != null)
                        {
                            asAttachment.Sensor.m_storedScaleMagnified = 1f;
                            asAttachment.transform.localScale = new Vector3(1, 1, 1);
                            asAttachment.DetachFromMount();
                        }

                        if (mount != null && CanAttachToMount(asAttachment, mount))
                        {
                            // Apply mount object scale
                            if (mount.GetRootMount().ParentToThis)
                            {
                                mount.GetRootMount().transform.localScale = mountObjectScale;
                            }
                            else
                            {
                                mount.MyObject.transform.localScale = mountObjectScale;
                            }

                            if (asAttachment.CanScaleToMount && mount.CanThisRescale())
                            {
                                asAttachment.ScaleToMount(mount);
                            }
                            asAttachment.AttachToMount(mount, true);

                            // Check if there were any children attachments waiting for us
                            if (toAttachByMountObjectID.TryGetValue(data.trackedID, out List<TrackedItem> list))
                            {
                                for (int i = 0; i < list.Count; ++i)
                                {
                                    if (list[i] != null)
                                    {
                                        list[i].data.UpdateFromData(list[i].data);
                                    }
                                }

                                toAttachByMountObjectID.Remove(data.trackedID);
                            }
                        }
                        else // Can't attach to mount yet, probably because parent is unattached attachment, but will need to eventually
                        {
                            if (toAttachByMountObjectID.TryGetValue(mountObjectID, out List<TrackedItem> list))
                            {
                                list.Add(this);
                            }
                            else
                            {
                                toAttachByMountObjectID.Add(mountObjectID, new List<TrackedItem>() { this });
                            }
                        }
                        --data.ignoreParentChanged;
                    }
                }
                // else, if this happens it is because we received a parent update to null and just haven't gotten the up to date mount index of -1 yet
                //       This will be handled on update
            }
            // else, on update we will detach from any current mount if this is the case, no need to handle this here
        }

        private void AttachmentParentChanged()
        {
            if (data.controller == GameManager.ID)
            {
                FVRFireArmAttachment asAttachment = dataObject is AttachableFirearm ? (dataObject as AttachableFirearm).Attachment : dataObject as FVRFireArmAttachment;

                if (asAttachment.curMount == null)
                {
                    mountObjectID = -1;
                }
                else
                {
                    if (GameManager.trackedItemByItem.TryGetValue(asAttachment.curMount.MyObject, out TrackedItem myTrackedItem))
                    {
                        mountObjectID = myTrackedItem.data.trackedID;
                    }
                    else
                    {
                        mountObjectID = -1;
                    }
                }
            }
        }

        private void UpdateAttachableBipod(FVRFireArmAttachment att, ref bool modified)
        {
            AttachableBipodInterface asInterface = att.AttachmentInterface as AttachableBipodInterface;

            // Write expanded
            byte preval = itemData.data[5];
            itemData.data[5] = asInterface.Bipod.m_isBipodExpanded ? (byte)1 : (byte)0;
            modified |= preval != itemData.data[5];
        }

        private void UpdateGivenAttachableBipod(FVRFireArmAttachment att, byte[] newData, ref bool modified)
        {
            AttachableBipodInterface asInterface = att.AttachmentInterface as AttachableBipodInterface;

            // Set expanded
            if ((newData[5] == 1 && !asInterface.Bipod.m_isBipodExpanded) || (newData[5] == 0 && asInterface.Bipod.m_isBipodExpanded))
            {
                asInterface.Bipod.Toggle();
                modified = true;
            }
        }

        private void UpdateFlagPoseSwitcher(FVRFireArmAttachment att, ref bool modified)
        {
            FlagPoseSwitcher asInterface = att.AttachmentInterface as FlagPoseSwitcher;

            // Write index
            byte preval0 = itemData.data[5];
            byte preval1 = itemData.data[6];
            BitConverter.GetBytes((short)asInterface.m_index).CopyTo(itemData.data, 5);
            modified |= (preval0 != itemData.data[5] || preval1 != itemData.data[6]);
        }

        private void UpdateGivenFlagPoseSwitcher(FVRFireArmAttachment att, byte[] newData, ref bool modified)
        {
            FlagPoseSwitcher asInterface = att.AttachmentInterface as FlagPoseSwitcher;

            // Set index
            int newIndex = BitConverter.ToInt16(newData, 5);
            if (newIndex != asInterface.m_index)
            {
                asInterface.m_index = newIndex;
                asInterface.Flag.localPosition = asInterface.Poses[newIndex].localPosition;
                asInterface.Flag.localRotation = asInterface.Poses[newIndex].localRotation;
                modified = true;
            }
        }

        private void UpdateFlipSight(FVRFireArmAttachment att, ref bool modified)
        {
            FlipSight asInterface = att.AttachmentInterface as FlipSight;

            // Write up
            byte preval0 = itemData.data[5];
            itemData.data[5] = asInterface.IsUp ? (byte)1 : (byte)0;
            modified |= preval0 != itemData.data[5];
        }

        private void UpdateGivenFlipSight(FVRFireArmAttachment att, byte[] newData, ref bool modified)
        {
            FlipSight asInterface = att.AttachmentInterface as FlipSight;

            // Set up
            if ((newData[5] == 1 && !asInterface.IsUp) || (newData[5] == 0 && asInterface.IsUp))
            {
                asInterface.Flip();
                modified = true;
            }
        }

        private void UpdateFlipSightY(FVRFireArmAttachment att, ref bool modified)
        {
            FlipSightY asInterface = att.AttachmentInterface as FlipSightY;

            // Write up
            byte preval0 = itemData.data[5];
            itemData.data[5] = asInterface.IsUp ? (byte)1 : (byte)0;
            modified |= preval0 != itemData.data[5];
        }

        private void UpdateGivenFlipSightY(FVRFireArmAttachment att, byte[] newData, ref bool modified)
        {
            FlipSightY asInterface = att.AttachmentInterface as FlipSightY;

            // Set up
            if ((newData[5] == 1 && !asInterface.IsUp) || (newData[5] == 0 && asInterface.IsUp))
            {
                asInterface.Flip();
                modified = true;
            }
        }

        private void UpdateLAM(FVRFireArmAttachment att, ref bool modified)
        {
            LAM asInterface = att.AttachmentInterface as LAM;

            // Write state
            byte preval0 = itemData.data[5];
            itemData.data[5] = (byte)asInterface.LState;
            modified |= preval0 != itemData.data[5];
        }

        private void UpdateGivenLAM(FVRFireArmAttachment att, byte[] newData, ref bool modified)
        {
            LAM asInterface = att.AttachmentInterface as LAM;

            // Set state
            if ((LAM.LAMState)newData[5] != asInterface.LState)
            {
                asInterface.LState = (LAM.LAMState)newData[5];

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
                        asInterface.FlashlightLight.Light.intensity = 2;
                    }
                    else
                    {
                        asInterface.FlashlightLight.Light.intensity = 0.5f;
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
            byte preval0 = itemData.data[5];
            itemData.data[5] = asInterface.BeamHitPoint.activeSelf ? (byte)1 : (byte)0;
            modified |= preval0 != itemData.data[5];
        }

        private void UpdateGivenLaserPointer(FVRFireArmAttachment att, byte[] newData, ref bool modified)
        {
            LaserPointer asInterface = att.AttachmentInterface as LaserPointer;

            // Set up
            if ((newData[5] == 1 && !asInterface.BeamHitPoint.activeSelf) || (newData[5] == 0 && asInterface.BeamHitPoint.activeSelf))
            {
                asInterface.ToggleOn();
                modified = true;
            }
        }

        private void UpdateTacticalFlashlight(FVRFireArmAttachment att, ref bool modified)
        {
            TacticalFlashlight asInterface = att.AttachmentInterface as TacticalFlashlight;

            // Write on
            byte preval0 = itemData.data[5];
            itemData.data[5] = asInterface.LightParts.activeSelf ? (byte)1 : (byte)0;
            modified |= preval0 != itemData.data[5];
        }

        private void UpdateGivenTacticalFlashlight(FVRFireArmAttachment att, byte[] newData, ref bool modified)
        {
            TacticalFlashlight asInterface = att.AttachmentInterface as TacticalFlashlight;

            // Set up
            if ((newData[5] == 1 && !asInterface.LightParts.activeSelf) || (newData[5] == 0 && asInterface.LightParts.activeSelf))
            {
                asInterface.ToggleOn();
                modified = true;
            }
        }

        private bool UpdateMagazine()
        {
            bool modified = false;
            FVRFireArmMagazine asMag = dataObject as FVRFireArmMagazine;

            int necessarySize = asMag.m_capacity * 2 + 10;

            if (itemData.data == null || itemData.data.Length < necessarySize)
            {
                itemData.data = new byte[necessarySize];
                modified = true;
            }

            byte preval0 = itemData.data[0];
            byte preval1 = itemData.data[1];

            // Write count of loaded rounds
            BitConverter.GetBytes((short)asMag.m_numRounds).CopyTo(itemData.data, 0);

            modified |= (preval0 != itemData.data[0] || preval1 != itemData.data[1]);

            // Write loaded round classes
            for (int i = 0; i < asMag.m_numRounds; ++i)
            {
                preval0 = itemData.data[i * 2 + 2];
                preval1 = itemData.data[i * 2 + 3];

                BitConverter.GetBytes((short)asMag.LoadedRounds[i].LR_Class).CopyTo(itemData.data, i * 2 + 2);

                modified |= (preval0 != itemData.data[i * 2 + 2] || preval1 != itemData.data[i * 2 + 3]);
            }

            // Write loaded into firearm
            preval0 = itemData.data[necessarySize - 8];
            itemData.data[necessarySize - 8] = asMag.FireArm != null ? (byte)1 : (byte)0;
            modified |= preval0 != itemData.data[necessarySize - 8];

            // Write secondary slot index, TODO: Having to look through each secondary slot for equality every update is obviously not optimal
            // We might want to look into patching (Attachable)Firearm's LoadMagIntoSecondary and eject from secondary to keep track of this instead
            preval0 = itemData.data[necessarySize - 7];
            if (asMag.FireArm == null)
            {
                itemData.data[necessarySize - 7] = (byte)255;
            }
            else
            {
                bool found = false;
                for (int i = 0; i < asMag.FireArm.SecondaryMagazineSlots.Length; ++i)
                {
                    if (asMag.FireArm.SecondaryMagazineSlots[i].Magazine == asMag)
                    {
                        found = true;
                        itemData.data[necessarySize - 7] = (byte)i;
                        break;
                    }
                }
                if (!found)
                {
                    itemData.data[necessarySize - 7] = (byte)255;
                }
            }
            modified |= preval0 != itemData.data[necessarySize - 7];

            // Write loaded into AttachableFirearm
            preval0 = itemData.data[necessarySize - 6];
            itemData.data[necessarySize - 6] = asMag.AttachableFireArm != null ? (byte)1 : (byte)0;
            modified |= preval0 != itemData.data[necessarySize - 6];

            // Write secondary slot index, TODO: Having to look through each secondary slot for equality every update is obviously not optimal
            // We might want to look into patching (Attachable)Firearm's LoadMagIntoSecondary and eject from secondary to keep track of this instead
            preval0 = itemData.data[necessarySize - 5];
            if (asMag.AttachableFireArm == null)
            {
                itemData.data[necessarySize - 5] = (byte)255;
            }
            else
            {
                bool found = false;
                for (int i = 0; i < asMag.AttachableFireArm.SecondaryMagazineSlots.Length; ++i)
                {
                    if (asMag.AttachableFireArm.SecondaryMagazineSlots[i].Magazine == asMag)
                    {
                        itemData.data[necessarySize - 5] = (byte)i;
                        break;
                    }
                }
                if (!found)
                {
                    itemData.data[necessarySize - 5] = (byte)255;
                }
            }
            modified |= preval0 != itemData.data[necessarySize - 5];

            // Write fuel amount left
            preval0 = itemData.data[necessarySize - 4];
            preval1 = itemData.data[necessarySize - 3];
            byte preval2 = itemData.data[necessarySize - 2];
            byte preval3 = itemData.data[necessarySize - 1];
            BitConverter.GetBytes(asMag.FuelAmountLeft).CopyTo(itemData.data, necessarySize - 4);
            modified |= (preval0 != itemData.data[necessarySize - 4] || preval1 != itemData.data[necessarySize - 3] || preval2 != itemData.data[necessarySize - 2] || preval3 != itemData.data[necessarySize - 1]);

            return modified;
        }

        private bool UpdateGivenMagazine(byte[] newData)
        {
            bool modified = false;
            FVRFireArmMagazine asMag = dataObject as FVRFireArmMagazine;

            if (itemData.data == null || itemData.data.Length != newData.Length)
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
                if (asMag.LoadedRounds.Length > i && asMag.LoadedRounds[i] != null && newClass == asMag.LoadedRounds[i].LR_Class)
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
                    TrackedItemData parentTrackedItemData = null;
                    if (ThreadManager.host)
                    {
                        parentTrackedItemData = Server.objects[data.parent] as TrackedItemData;
                    }
                    else
                    {
                        parentTrackedItemData = Client.objects[data.parent] as TrackedItemData;
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
                                    for (int i = 0; i < asMag.FireArm.SecondaryMagazineSlots.Length; ++i)
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
                        else if (asMag.AttachableFireArm != null)
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
                    TrackedItemData parentTrackedItemData = null;
                    if (ThreadManager.host)
                    {
                        parentTrackedItemData = Server.objects[data.parent] as TrackedItemData;
                    }
                    else
                    {
                        parentTrackedItemData = Client.objects[data.parent] as TrackedItemData;
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
                else if (asMag.AttachableFireArm != null)
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

            itemData.data = newData;

            return modified;
        }

        private bool UpdateMinigunBox()
        {
            bool modified = false;
            MinigunBox asMinigunBox = dataObject as MinigunBox;

            if (itemData.data == null)
            {
                itemData.data = new byte[8];
                modified = true;
            }

            // Write bullet count
            byte preval0 = itemData.data[0];
            byte preval1 = itemData.data[1];
            BitConverter.GetBytes((short)asMinigunBox.NumBulletsLeft).CopyTo(itemData.data, 0);
            modified |= (preval0 != itemData.data[0] || preval1 != itemData.data[1]);

            // Write loaded into firearm
            preval0 = itemData.data[2];
            itemData.data[2] = asMinigunBox.FireArm != null ? (byte)1 : (byte)0;
            modified |= preval0 != itemData.data[2];

            // Write secondary slot index, TODO: Having to look through each secondary slot for equality every update is obviously not optimal
            // We might want to look into patching (Attachable)Firearm's LoadMagIntoSecondary and eject from secondary to keep track of this instead
            preval0 = itemData.data[3];
            if (asMinigunBox.FireArm == null)
            {
                itemData.data[3] = (byte)255;
            }
            else
            {
                bool found = false;
                for (int i = 0; i < asMinigunBox.FireArm.SecondaryMagazineSlots.Length; ++i)
                {
                    if (asMinigunBox.FireArm.SecondaryMagazineSlots[i].Magazine == asMinigunBox)
                    {
                        found = true;
                        itemData.data[3] = (byte)i;
                        break;
                    }
                }
                if (!found)
                {
                    itemData.data[3] = (byte)255;
                }
            }
            modified |= preval0 != itemData.data[3];

            // Write loaded into AttachableFirearm
            preval0 = itemData.data[4];
            itemData.data[4] = asMinigunBox.AttachableFireArm != null ? (byte)1 : (byte)0;
            modified |= preval0 != itemData.data[4];

            // Write secondary slot index, TODO: Having to look through each secondary slot for equality every update is obviously not optimal
            // We might want to look into patching (Attachable)Firearm's LoadMagIntoSecondary and eject from secondary to keep track of this instead
            preval0 = itemData.data[5];
            if (asMinigunBox.AttachableFireArm == null)
            {
                itemData.data[5] = (byte)255;
            }
            else
            {
                bool found = false;
                for (int i = 0; i < asMinigunBox.AttachableFireArm.SecondaryMagazineSlots.Length; ++i)
                {
                    if (asMinigunBox.AttachableFireArm.SecondaryMagazineSlots[i].Magazine == asMinigunBox)
                    {
                        itemData.data[5] = (byte)i;
                        break;
                    }
                }
                if (!found)
                {
                    itemData.data[5] = (byte)255;
                }
            }
            modified |= preval0 != itemData.data[5];

            return modified;
        }

        private bool UpdateGivenMinigunBox(byte[] newData)
        {
            bool modified = false;
            MinigunBox asMinigunBox = dataObject as MinigunBox;

            if (itemData.data == null || itemData.data.Length != newData.Length)
            {
                modified = true;
            }

            // Set bullet count
            int preRoundCount = asMinigunBox.NumBulletsLeft;
            asMinigunBox.NumBulletsLeft = BitConverter.ToInt16(newData, 0);
            modified |= preRoundCount != asMinigunBox.NumBulletsLeft;

            // Load into firearm if necessary
            if (newData[2] == 1)
            {
                if (data.parent != -1)
                {
                    TrackedItemData parentTrackedItemData = null;
                    if (ThreadManager.host)
                    {
                        parentTrackedItemData = Server.objects[data.parent] as TrackedItemData;
                    }
                    else
                    {
                        parentTrackedItemData = Client.objects[data.parent] as TrackedItemData;
                    }

                    if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null && parentTrackedItemData.physicalItem.dataObject is FVRFireArm)
                    {
                        // We want to be loaded in a firearm, we have a parent, it is a firearm
                        if (asMinigunBox.FireArm != null)
                        {
                            if (asMinigunBox.FireArm != parentTrackedItemData.physicalItem.dataObject)
                            {
                                // Unload from current, load into new firearm
                                if (asMinigunBox.FireArm.Magazine == asMinigunBox)
                                {
                                    asMinigunBox.FireArm.EjectMag(true);
                                }
                                else
                                {
                                    for (int i = 0; i < asMinigunBox.FireArm.SecondaryMagazineSlots.Length; ++i)
                                    {
                                        if (asMinigunBox.FireArm.SecondaryMagazineSlots[i].Magazine == asMinigunBox)
                                        {
                                            asMinigunBox.FireArm.EjectSecondaryMagFromSlot(i, true);
                                            break;
                                        }
                                    }
                                }
                                if (newData[3] == 255)
                                {
                                    ++MagazinePatch.loadSkip;
                                    asMinigunBox.Load(parentTrackedItemData.physicalItem.dataObject as FVRFireArm);
                                    --MagazinePatch.loadSkip;
                                }
                                else
                                {
                                    ++MagazinePatch.loadSkip;
                                    asMinigunBox.LoadIntoSecondary(parentTrackedItemData.physicalItem.dataObject as FVRFireArm, newData[3]);
                                    --MagazinePatch.loadSkip;
                                }
                                modified = true;
                            }
                        }
                        else if (asMinigunBox.AttachableFireArm != null)
                        {
                            // Unload from current, load into new firearm
                            if (asMinigunBox.AttachableFireArm.Magazine == asMinigunBox)
                            {
                                asMinigunBox.AttachableFireArm.EjectMag(true);
                            }
                            else
                            {
                                for (int i = 0; i < asMinigunBox.AttachableFireArm.SecondaryMagazineSlots.Length; ++i)
                                {
                                    if (asMinigunBox.AttachableFireArm.SecondaryMagazineSlots[i].Magazine == asMinigunBox)
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
                                asMinigunBox.Load(parentTrackedItemData.physicalItem.dataObject as FVRFireArm);
                                --MagazinePatch.loadSkip;
                            }
                            else
                            {
                                ++MagazinePatch.loadSkip;
                                asMinigunBox.LoadIntoSecondary(parentTrackedItemData.physicalItem.dataObject as FVRFireArm, newData[3]);
                                --MagazinePatch.loadSkip;
                            }
                            modified = true;
                        }
                        else
                        {
                            // Load into firearm
                            if (newData[3] == 255)
                            {
                                ++MagazinePatch.loadSkip;
                                asMinigunBox.Load(parentTrackedItemData.physicalItem.dataObject as FVRFireArm);
                                --MagazinePatch.loadSkip;
                            }
                            else
                            {
                                ++MagazinePatch.loadSkip;
                                asMinigunBox.LoadIntoSecondary(parentTrackedItemData.physicalItem.dataObject as FVRFireArm, newData[3]);
                                --MagazinePatch.loadSkip;
                            }
                            modified = true;
                        }
                    }
                }
            }
            else if (newData[4] == 1)
            {
                if (data.parent != -1)
                {
                    TrackedItemData parentTrackedItemData = null;
                    if (ThreadManager.host)
                    {
                        parentTrackedItemData = Server.objects[data.parent] as TrackedItemData;
                    }
                    else
                    {
                        parentTrackedItemData = Client.objects[data.parent] as TrackedItemData;
                    }

                    if (parentTrackedItemData != null && parentTrackedItemData.physicalItem != null && parentTrackedItemData.physicalItem.dataObject is AttachableFirearmPhysicalObject)
                    {
                        // We want to be loaded in a AttachableFireArm, we have a parent, it is a AttachableFireArm
                        if (asMinigunBox.AttachableFireArm != null)
                        {
                            if (asMinigunBox.AttachableFireArm != (parentTrackedItemData.physicalItem.dataObject as AttachableFirearmPhysicalObject).FA)
                            {
                                // Unload from current, load into new AttachableFireArm
                                if (asMinigunBox.AttachableFireArm.Magazine == asMinigunBox)
                                {
                                    asMinigunBox.AttachableFireArm.EjectMag(true);
                                }
                                else
                                {
                                    for (int i = 0; i < asMinigunBox.AttachableFireArm.SecondaryMagazineSlots.Length; ++i)
                                    {
                                        if (asMinigunBox.AttachableFireArm.SecondaryMagazineSlots[i].Magazine == asMinigunBox)
                                        {
                                            // TODO: Future: When H3 adds support for secondary slots on attachable firearm uncomment the following:
                                            //asMag.AttachableFireArm.EjectSecondaryMagFromSlot(i, true);
                                            break;
                                        }
                                    }
                                }
                                if (newData[5] == 255)
                                {
                                    ++MagazinePatch.loadSkip;
                                    asMinigunBox.Load((parentTrackedItemData.physicalItem.dataObject as AttachableFirearmPhysicalObject).FA);
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
                        else if (asMinigunBox.FireArm != null)
                        {
                            // Unload from current firearm, load into new AttachableFireArm
                            if (asMinigunBox.FireArm.Magazine == asMinigunBox)
                            {
                                asMinigunBox.FireArm.EjectMag(true);
                            }
                            else
                            {
                                for (int i = 0; i < asMinigunBox.FireArm.SecondaryMagazineSlots.Length; ++i)
                                {
                                    if (asMinigunBox.FireArm.SecondaryMagazineSlots[i].Magazine == asMinigunBox)
                                    {
                                        asMinigunBox.FireArm.EjectSecondaryMagFromSlot(i, true);
                                        break;
                                    }
                                }
                            }
                            if (newData[5] == 255)
                            {
                                ++MagazinePatch.loadSkip;
                                asMinigunBox.Load((parentTrackedItemData.physicalItem.dataObject as AttachableFirearmPhysicalObject).FA);
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
                            if (newData[5] == 255)
                            {
                                ++MagazinePatch.loadSkip;
                                asMinigunBox.Load((parentTrackedItemData.physicalItem.dataObject as AttachableFirearmPhysicalObject).FA);
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
                if (asMinigunBox.FireArm != null)
                {
                    // Don't want to be loaded, but we are loaded, unload
                    if (asMinigunBox.FireArm.Magazine == asMinigunBox)
                    {
                        asMinigunBox.FireArm.EjectMag(true);
                    }
                    else
                    {
                        for (int i = 0; i < asMinigunBox.FireArm.SecondaryMagazineSlots.Length; ++i)
                        {
                            if (asMinigunBox.FireArm.SecondaryMagazineSlots[i].Magazine == asMinigunBox)
                            {
                                asMinigunBox.FireArm.EjectSecondaryMagFromSlot(i, true);
                                break;
                            }
                        }
                    }
                    modified = true;
                }
                else if (asMinigunBox.AttachableFireArm != null)
                {
                    if (asMinigunBox.AttachableFireArm.Magazine == asMinigunBox)
                    {
                        asMinigunBox.AttachableFireArm.EjectMag(true);
                    }
                    else
                    {
                        for (int i = 0; i < asMinigunBox.AttachableFireArm.SecondaryMagazineSlots.Length; ++i)
                        {
                            if (asMinigunBox.AttachableFireArm.SecondaryMagazineSlots[i].Magazine == asMinigunBox)
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

            itemData.data = newData;

            return modified;
        }

        private bool UpdateClip()
        {
            bool modified = false;
            FVRFireArmClip asClip = dataObject as FVRFireArmClip;

            int necessarySize = asClip.m_capacity * 2 + 3;

            if (itemData.data == null || itemData.data.Length < necessarySize)
            {
                itemData.data = new byte[necessarySize];
                modified = true;
            }

            byte preval0 = itemData.data[0];
            byte preval1 = itemData.data[1];

            // Write count of loaded rounds
            BitConverter.GetBytes((short)asClip.m_numRounds).CopyTo(itemData.data, 0);

            modified |= (preval0 != itemData.data[0] || preval1 != itemData.data[1]);

            // Write loaded round classes
            for (int i = 0; i < asClip.m_numRounds; ++i)
            {
                preval0 = itemData.data[i * 2 + 2];
                preval1 = itemData.data[i * 2 + 3];

                BitConverter.GetBytes((short)asClip.LoadedRounds[i].LR_Class).CopyTo(itemData.data, i * 2 + 2);

                modified |= (preval0 != itemData.data[i * 2 + 2] || preval1 != itemData.data[i * 2 + 3]);
            }

            // Write loaded into firearm
            preval0 = itemData.data[necessarySize - 1];
            itemData.data[necessarySize - 1] = asClip.FireArm != null ? (byte)1 : (byte)0;
            modified |= preval0 != itemData.data[necessarySize - 1];

            return modified;
        }

        private bool UpdateGivenClip(byte[] newData)
        {
            bool modified = false;
            FVRFireArmClip asClip = dataObject as FVRFireArmClip;

            if (itemData.data == null || itemData.data.Length != newData.Length)
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
                    TrackedItemData parentTrackedItemData = null;
                    if (ThreadManager.host)
                    {
                        parentTrackedItemData = Server.objects[data.parent] as TrackedItemData;
                    }
                    else
                    {
                        parentTrackedItemData = Client.objects[data.parent] as TrackedItemData;
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

            itemData.data = newData;

            return modified;
        }

        private bool UpdateSpeedloader()
        {
            bool modified = false;
            Speedloader asSpeedloader = dataObject as Speedloader;

            int necessarySize = asSpeedloader.Chambers.Count * 2;

            if (itemData.data == null || itemData.data.Length < necessarySize)
            {
                itemData.data = new byte[necessarySize];
                modified = true;
            }

            byte preval0;
            byte preval1;

            // Write loaded round classes (-1 for none)
            for (int i = 0; i < asSpeedloader.Chambers.Count; ++i)
            {
                preval0 = itemData.data[i * 2];
                preval1 = itemData.data[i * 2 + 1];

                if (asSpeedloader.Chambers[i].IsLoaded && !asSpeedloader.Chambers[i].IsSpent)
                {
                    BitConverter.GetBytes((short)asSpeedloader.Chambers[i].LoadedClass).CopyTo(itemData.data, i * 2);
                }
                else
                {
                    BitConverter.GetBytes((short)-1).CopyTo(itemData.data, i * 2);
                }

                modified |= (preval0 != itemData.data[i * 2] || preval1 != itemData.data[i * 2 + 1]);
            }

            return modified;
        }

        private bool UpdateGivenSpeedloader(byte[] newData)
        {
            bool modified = false;
            Speedloader asSpeedloader = dataObject as Speedloader;

            if (itemData.data == null || itemData.data.Length != newData.Length)
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
                else if (classIndex == -1 && asSpeedloader.Chambers[i].IsLoaded)
                {
                    asSpeedloader.Chambers[i].Unload();
                }
            }

            itemData.data = newData;

            return modified;
        }
        #endregion

        public static bool CanAttachToMount(FVRFireArmAttachment attachment, FVRFireArmAttachmentMount mount)
        {
            if (mount.Parent == null)
            {
                return false;
            }

            // Check if MyObject attachment is actually attached, because we can't make assumption that if Parent != null then curMount != null
            if(mount.MyObject is FVRFireArmAttachment && (mount.MyObject as FVRFireArmAttachment).curMount == null)
            {
                return false;
            }

            if (mount.AttachmentsList.Count >= mount.m_maxAttachments)
            {
                return false;
            }
            if (attachment.AttachmentInterface != null && attachment.AttachmentInterface is AttachableBipodInterface && mount.GetRootMount().MyObject.Bipod != null)
            {
                return false;
            }
            if (attachment is Suppressor)
            {
                if (mount.GetRootMount().MyObject is SingleActionRevolver && !(mount.GetRootMount().MyObject as SingleActionRevolver).AllowsSuppressor)
                {
                    return false;
                }
                if (mount.GetRootMount().MyObject is Revolver && !(mount.GetRootMount().MyObject as Revolver).AllowsSuppressor)
                {
                    return false;
                }
            }
            if (attachment is AttachableMeleeWeapon && mount.GetRootMount().MyObject is FVRFireArm)
            {
                FVRFireArm fvrfireArm = mount.GetRootMount().MyObject as FVRFireArm;
                if (fvrfireArm.CurrentAttachableMeleeWeapon != null)
                {
                    return false;
                }
            }
            return true;
        }

        public virtual void FixedUpdate()
        {
            if (interpolated && physicalItem != null && data.controller != GameManager.ID && itemData.position != null && itemData.rotation != null)
            {
                if (Vector3.Distance(data.parent == -1 ? physicalItem.transform.position : physicalItem.transform.localPosition, itemData.position) > 0.001f)
                {
                    positionSet = false;
                    if (itemData.previousPos != null && itemData.velocity.magnitude < 1f)
                    {
                        if (data.parent == -1)
                        {
                            physicalItem.transform.position = Vector3.Lerp(physicalItem.transform.position, itemData.position + itemData.velocity, interpolationSpeed * Time.deltaTime);
                        }
                        else
                        {
                            physicalItem.transform.localPosition = Vector3.Lerp(physicalItem.transform.localPosition, itemData.position + itemData.velocity, interpolationSpeed * Time.deltaTime);
                        }
                    }
                    else
                    {
                        if (data.parent == -1)
                        {
                            physicalItem.transform.position = itemData.position;
                        }
                        else
                        {
                            physicalItem.transform.localPosition = itemData.position;
                        }
                    }
                }
                else if (!positionSet)
                {
                    if (data.parent == -1)
                    {
                        physicalItem.transform.position = itemData.position;
                    }
                    else
                    {
                        physicalItem.transform.localPosition = itemData.position;
                    }
                    positionSet = true;
                }
                if (Quaternion.Angle(data.parent == -1 ? physicalItem.transform.rotation : physicalItem.transform.localRotation, itemData.rotation) > 0.1f)
                {
                    rotationSet = false;
                    if (data.parent == -1)
                    {
                        physicalItem.transform.rotation = Quaternion.Lerp(physicalItem.transform.rotation, itemData.rotation, interpolationSpeed * Time.deltaTime);
                    }
                    else
                    {
                        physicalItem.transform.localRotation = Quaternion.Lerp(physicalItem.transform.localRotation, itemData.rotation, interpolationSpeed * Time.deltaTime);
                    }
                }
                else if (!rotationSet)
                {
                    if (data.parent == -1)
                    {
                        physicalItem.transform.rotation = itemData.rotation;
                    }
                    else
                    {
                        physicalItem.transform.localRotation = itemData.rotation;
                    }
                    rotationSet = true;
                }
            }
        }

        protected override void OnDestroy()
        {
            GameManager.OnSceneLeft -= OnSceneLeft;
            GameManager.OnSceneJoined -= OnSceneJoined;
            GameManager.OnInstanceJoined -= OnInstanceJoined;
            GameManager.OnPlayerBodyInit -= OnPlayerBodyInit;

            // A skip of the entire destruction process may be used if H3MP has become irrelevant, like in the case of disconnection
            if (skipFullDestroy)
            {
                return;
            }

            GameManager.trackedItemByItem.Remove(physicalItem);
            if (physicalItem is SosigWeaponPlayerInterface)
            {
                GameManager.trackedItemBySosigWeapon.Remove((physicalItem as SosigWeaponPlayerInterface).W);
            }
            GameManager.trackedObjectByInteractive.Remove(physicalItem);
            if (removeTrackedDamageables != null)
            {
                removeTrackedDamageables();
            }

            // Call onDestruction in case the item subtype has something to do when it gets destroyed (See sosigWeaponPlayerInterface subtype)
            if (onDestruction != null)
            {
                onDestruction();
            }

            // Ensure uncontrolled, which has to be done no matter what OnDestroy because we will not have the physicalObject anymore
            EnsureUncontrolled();

            base.OnDestroy();
        }

        public override void EnsureUncontrolled()
        {
            if (physicalItem.m_hand != null)
            {
                physicalItem.ForceBreakInteraction();
            }
            if (physicalItem.QuickbeltSlot != null)
            {
                physicalItem.ClearQuickbeltState();
            }
        }

        public override void BeginInteraction(FVRViveHand hand)
        {
            if (data.controller != GameManager.ID)
            {
                // Take control

                // Send to all clients
                data.TakeControlRecursive();

                // Update locally
                Mod.SetKinematicRecursive(physical.transform, false);
            }
        }

        public override void OnTransformParentChanged()
        {
            if(parentChanged != null)
            {
                parentChanged();
            }

            base.OnTransformParentChanged();
        }

        private void OnPlayerBodyInit(FVRPlayerBody playerBody)
        {
            // Item may have wanted to unsecure but we arrived in the scene before CurrentPlayerBody was set
            // so we must now do the check to unsecure
            if (securedCode != -1 && !GameManager.sceneLoading)
            {
                Unsecure();
            }
        }

        protected virtual void OnSceneLeft(string scene, string destination)
        {
            TrackedObjectData.ObjectBringType bring = TrackedObjectData.ObjectBringType.No;
            data.ShouldBring(true, ref bring);

            // We might want to bring an item with us across scenes
            if (data.IsControlled(out int interactionID))
            {
                if ((int)bring > 0)
                {
                    // Secure parent object
                    if(data.parent == -1)
                    {
                        EnsureUncontrolled();
                        transform.parent = null;
                        DontDestroyOnLoad(gameObject);
                        securedCode = interactionID;
                    }

                    data.SetScene(destination, true);
                }
                // else, dont want to bring, destruction handled by scene change so no need to do it here
            }

            // NOTE: Don't want to do the following, because when we change scene, we don't want to bring along objects that
            //       are just loose in the world. This is unlike an instance change, where we will want to, because despite the switch, scene remains the same
            //       so loose objects are going to be at the same pos/rot as they were before

            //else if(bring == TrackedObjectData.ObjectBringType.Yes) // Not actively controlled, but want to secure object to make our own copy in the new scene
            //{
            //    ++GameManager.giveControlOfDestroyed;
            //    if(data.parent == -1)
            //    {
            //        transform.parent = null;
            //        GameManager.retrack.Add(gameObject);
            //        DontDestroyOnLoad(gameObject);
            //    }
            //    GameManager.DestroyTrackedScripts(data);
            //    --GameManager.giveControlOfDestroyed;
            //}
        }

        protected virtual void OnSceneJoined(string scene, string source)
        {
            // Since items can be secured with a certain interaction that require the CurrentPlayerBody to be set, we need to wait for it
            if(securedCode != -1 && GM.CurrentPlayerBody != null)
            {
                Unsecure();
            }
        }

        private void Unsecure()
        {
            SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());

            if (securedCode == 1) // Left hand
            {
                GM.CurrentPlayerBody.LeftHand.GetComponent<FVRViveHand>().ForceSetInteractable(physicalItem);
            }
            else if (securedCode == 2) // Right hand
            {
                GM.CurrentPlayerBody.RightHand.GetComponent<FVRViveHand>().ForceSetInteractable(physicalItem);
            }
            else if (securedCode >= 3 && securedCode <= 258) // Internal QBS, index = securedCode - 3
            {
                if (GM.CurrentPlayerBody.QBSlots_Internal.Count > securedCode - 3)
                {
                    physicalItem.SetQuickBeltSlot(GM.CurrentPlayerBody.QBSlots_Internal[securedCode - 3]);
                }
            }
            else if (securedCode >= 259 && securedCode <= 514) // Added QBS, index = securedCode - 259
            {
                if (GM.CurrentPlayerBody.QBSlots_Added.Count > securedCode - 259)
                {
                    physicalItem.SetQuickBeltSlot(GM.CurrentPlayerBody.QBSlots_Added[securedCode - 259]);
                }
            }

            securedCode = -1;
        }

        protected virtual void OnInstanceJoined(int instance, int source)
        {
            // An instance switch could happen during loading, at which point we want to change instance of wtv objects
            // we decided to bring along with us during scene change
            // Note: The new instance processing is done in Joined() because if we put it in Left() and then data.SetInstance(instance),
            //       the object would get destroyed because the new instance is not our current instance
            if (GameManager.sceneLoading)
            {
                if(securedCode != -1)
                {
                    data.SetInstance(instance, true);
                }
            }
            else // Not in scene loading process, we can process the object ourselves
            {
                TrackedObjectData.ObjectBringType bring = TrackedObjectData.ObjectBringType.No;
                data.ShouldBring(false, ref bring);

                ++GameManager.giveControlOfDestroyed;

                if (bring == TrackedObjectData.ObjectBringType.Yes)
                {
                    // Want to bring everything with us
                    // What we are interacting with, we will bring with us completely, destroying it on remote sides
                    // Whet we do not interact with, we will make a copy of in the new instance
                    if(data.IsControlled(out int interactionID))
                    {
                        data.SetInstance(instance, true);
                    }
                    else // Not interacting with
                    {
                        // Bring item, but also want to leave it there for people in the instance we just left
                        // So destroy just the script, giving control to anyone in the old instance,
                        // then retrack to make a new copy of it in the new instance
                        bool hadNoParent = data.parent == -1;

                        // Destroy all physical tracked scripts in the hierarchy recursively
                        GameManager.DestroyTrackedScripts(data);

                        // Only sync the top parent of items. The children will also get retracked as children
                        if (hadNoParent)
                        {
                            GameManager.SyncTrackedObjects(transform, true, null);
                        }
                    }
                }
                else if(bring == TrackedObjectData.ObjectBringType.OnlyInteracted && data.IsControlled(out int interactionID))
                {
                    data.SetInstance(instance, true);
                }
                else // Don't want to bring, destroy
                {
                    DestroyImmediate(gameObject);
                }

                --GameManager.giveControlOfDestroyed;
            }
        }

        public override bool HandleShatter(UberShatterable shatterable, Vector3 point, Vector3 dir, float intensity, bool received, int clientID, byte[] data)
        {
            if(handleShatterSpecific != null)
            {
                return handleShatterSpecific(shatterable, point, dir, intensity, received, clientID, data);
            }

            return true;
        }

        private bool HandleShatterableShatter(UberShatterable shatterable, Vector3 point, Vector3 dir, float intensity, bool received, int clientID, byte[] data)
        {
            List<byte> newAdditionalData = new List<byte>();
            newAdditionalData.Add(1);
            newAdditionalData.Add(1);
            newAdditionalData.AddRange(BitConverter.GetBytes(point.x));
            newAdditionalData.AddRange(BitConverter.GetBytes(point.y));
            newAdditionalData.AddRange(BitConverter.GetBytes(point.z));
            newAdditionalData.AddRange(BitConverter.GetBytes(dir.x));
            newAdditionalData.AddRange(BitConverter.GetBytes(dir.y));
            newAdditionalData.AddRange(BitConverter.GetBytes(dir.z));
            newAdditionalData.AddRange(BitConverter.GetBytes(intensity));
            itemData.additionalData = newAdditionalData.ToArray();

            if (received)
            {
                ++UberShatterableShatterPatch.skip;
                ((UberShatterable)dataObject).Shatter(point, dir, intensity);
                --UberShatterableShatterPatch.skip;

                if (ThreadManager.host)
                {
                    ServerSend.UberShatterableShatter(this.data.trackedID, point, dir, intensity, null, clientID);
                }
            }
            else
            {
                if (ThreadManager.host)
                {
                    ServerSend.UberShatterableShatter(this.data.trackedID, point, dir, intensity, null);
                }
                else if (itemData.trackedID != -1)
                {
                    ClientSend.UberShatterableShatter(this.data.trackedID, point, dir, intensity, null);
                }
            }

            return true;
        }
    }
}
