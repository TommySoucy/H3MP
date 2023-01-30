using FistVR;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static FistVR.sblpCell;

namespace H3MP
{
    public class H3MP_TrackedItem : MonoBehaviour
    {
        public static float interpolationSpeed = 12f;

        public H3MP_TrackedItemData data;
        public bool awoken;
        public bool sendOnAwake;

        // Unknown tracked ID queues
        public static Dictionary<int, KeyValuePair<int, bool>> unknownTrackedIDs = new Dictionary<int, KeyValuePair<int, bool>>();
        public static Dictionary<int, List<int>> unknownParentTrackedIDs = new Dictionary<int, List<int>>();
        public static Dictionary<int, int> unknownControlTrackedIDs = new Dictionary<int, int>();
        public static List<int> unknownDestroyTrackedIDs = new List<int>();

        // Update
        public delegate bool UpdateData(); // The updateFunc and updateGivenFunc should return a bool indicating whether data has been modified
        public delegate bool UpdateDataWithGiven(byte[] newData);
        public delegate bool FireFirearm();
        public delegate void FirearmUpdateOverrideSetter(FireArmRoundClass roundClass);
        public delegate bool FireSosigGun(float recoilMult);
        public delegate void FireAttachableFirearm(bool firedFromInterface);
        public delegate void FireAttachableFirearmChamberRound(FireArmRoundClass roundClass);
        public delegate FVRFireArmChamber FireAttachableFirearmGetChamber();
        public delegate void UpdateParent();
        public UpdateData updateFunc; // Update the item's data based on its physical state since we are the controller
        public UpdateDataWithGiven updateGivenFunc; // Update the item's data and state based on data provided by another client
        public FireFirearm fireFunc; // Fires the corresponding firearm type
        public FirearmUpdateOverrideSetter setFirearmUpdateOverride; // Set fire update override data
        public FireAttachableFirearm attachableFirearmFunc; // Fires the corresponding attachable firearm type
        public FireAttachableFirearmChamberRound attachableFirearmChamberRoundFunc; // Loads the chamber of the attachable firearm with round of class
        public FireAttachableFirearmGetChamber attachableFirearmGetChamberFunc; // Returns the chamber of the corresponding attachable firearm
        public FireSosigGun sosigWeaponfireFunc; // Fires the corresponding sosig weapon
        public UpdateParent updateParentFunc; // Update the item's state depending on current parent
        public byte currentMountIndex = 255; // Used by attachment, TODO: This limits number of mounts to 255, if necessary could make index into a short
        public UnityEngine.Object dataObject;
        public FVRPhysicalObject physicalObject;

        // StingerLauncher specific
        public StingerMissile stingerMissile;

        // TrackedItemReferences array
        // Used by certain item subtypes who need to get access to their TrackedItem very often (On Update for example)
        // This is used to bypass having to find the item in a datastructure too often
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
        public static int skipDestroy;
        public bool skipFullDestroy;

        private void Awake()
        {
            InitItemType();

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
                    H3MP_ClientSend.TrackedItem(data);
                }
            }
        }

        // MOD: This will check which type this item is so we can keep track of its data more efficiently
        //      A mod with a custom item type which has custom data should postfix this to check if this item is of custom type
        //      to keep a ref to the object itself and set delegate update functions
        private void InitItemType()
        {
            Mod.LogInfo("inititemtype called on " + gameObject.name);
            FVRPhysicalObject physObj = GetComponent<FVRPhysicalObject>();

            // For each relevant type for which we may want to store additional data, we set a specific update function and the object ref
            // NOTE: We want to handle a subtype before its parent type (ex.: AttachableFirearmPhysicalObject before FVRFireArmAttachment) 
            // TODO: Maybe instead of having a big if statement like this, put all of them in a dictionnary for faster lookup
            if (physObj is FVRFireArmMagazine)
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
                fireFunc = asCBW.Fire;
                setFirearmUpdateOverride = SetCBWUpdateOverride;
            }
            else if (physObj is OpenBoltReceiver)
            {
                OpenBoltReceiver asOBR = (OpenBoltReceiver)physObj;
                updateFunc = UpdateOpenBoltReceiver;
                updateGivenFunc = UpdateGivenOpenBoltReceiver;
                dataObject = asOBR;
                fireFunc = asOBR.Fire;
                setFirearmUpdateOverride = SetOBRUpdateOverride;
            }
            else if (physObj is BoltActionRifle)
            {
                BoltActionRifle asBAR = (BoltActionRifle)physObj;
                updateFunc = UpdateBoltActionRifle;
                updateGivenFunc = UpdateGivenBoltActionRifle;
                dataObject = asBAR;
                fireFunc = asBAR.Fire;
                setFirearmUpdateOverride = SetBARUpdateOverride;
            }
            else if (physObj is Handgun)
            {
                Handgun asHandgun = (Handgun)physObj;
                updateFunc = UpdateHandgun;
                updateGivenFunc = UpdateGivenHandgun;
                dataObject = asHandgun;
                fireFunc = asHandgun.Fire;
                setFirearmUpdateOverride = SetHandgunUpdateOverride;
            }
            else if (physObj is TubeFedShotgun)
            {
                TubeFedShotgun asTFS = (TubeFedShotgun)physObj;
                updateFunc = UpdateTubeFedShotgun;
                updateGivenFunc = UpdateGivenTubeFedShotgun;
                dataObject = asTFS;
                fireFunc = asTFS.Fire;
                setFirearmUpdateOverride = SetTFSUpdateOverride;
            }
            else if (physObj is Revolver)
            {
                Revolver asRevolver = (Revolver)physObj;
                updateFunc = UpdateRevolver;
                updateGivenFunc = UpdateGivenRevolver;
                dataObject = asRevolver;
            }
            else if (physObj is SingleActionRevolver)
            {
                SingleActionRevolver asSAR = (SingleActionRevolver)physObj;
                updateFunc = UpdateSingleActionRevolver;
                updateGivenFunc = UpdateGivenSingleActionRevolver;
                dataObject = asSAR;
            }
            else if (physObj is RevolvingShotgun)
            {
                RevolvingShotgun asRS = (RevolvingShotgun)physObj;
                updateFunc = UpdateRevolvingShotgun;
                updateGivenFunc = UpdateGivenRevolvingShotgun;
                dataObject = asRS;
            }
            else if (physObj is BAP)
            {
                BAP asBAP = (BAP)physObj;
                updateFunc = UpdateBAP;
                updateGivenFunc = UpdateGivenBAP;
                dataObject = asBAP;
                fireFunc = asBAP.Fire;
                setFirearmUpdateOverride = SetBAPUpdateOverride;
            }
            else if (physObj is BreakActionWeapon)
            {
                updateFunc = UpdateBreakActionWeapon;
                updateGivenFunc = UpdateGivenBreakActionWeapon;
                dataObject = physObj as BreakActionWeapon;
            }
            else if (physObj is LeverActionFirearm)
            {
                LeverActionFirearm LAF = (LeverActionFirearm)physObj;
                updateFunc = UpdateLeverActionFirearm;
                updateGivenFunc = UpdateGivenLeverActionFirearm;
                dataObject = LAF;
            }
            else if(physicalObject is PinnedGrenade)
            {
                PinnedGrenade asPG = (PinnedGrenade)physObj;
                updateFunc = UpdatePinnedGrenade;
                updateGivenFunc = UpdateGivenPinnedGrenade;
                if (asPG.SpawnOnSplode == null)
                {
                    asPG.SpawnOnSplode = new List<GameObject>();
                }
                GameObject trackedItemRef = new GameObject("PGTrackedItemRef");
                if (availableTrackedItemRefIndices.Count == 0)
                {
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
                trackedItemReferences[availableTrackedItemRefIndices.Count - 1] = this;
                trackedItemRef.hideFlags = HideFlags.HideAndDontSave + availableTrackedItemRefIndices[availableTrackedItemRefIndices.Count - 1];
                availableTrackedItemRefIndices.RemoveAt(availableTrackedItemRefIndices.Count - 1);
                asPG.SpawnOnSplode.Add(trackedItemRef);
            }
            else if(physicalObject is FVRGrenade)
            {
                FVRGrenade asGrenade = (FVRGrenade)physObj;
                updateFunc = UpdateGrenade;
                updateGivenFunc = UpdateGivenGrenade;
                Dictionary<int, float> timings = Mod.FVRGrenade_FuseTimings.GetValue(asGrenade) as Dictionary<int, float>;
                if (timings == null)
                {
                    timings = new Dictionary<int, float>();
                    Mod.FVRGrenade_FuseTimings.SetValue(asGrenade, timings);
                }
                if (availableTrackedItemRefIndices.Count == 0)
                {
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
                timings.Add(-1, availableTrackedItemRefIndices[availableTrackedItemRefIndices.Count - 1]);
                trackedItemReferences[availableTrackedItemRefIndices.Count - 1] = this;
                availableTrackedItemRefIndices.RemoveAt(availableTrackedItemRefIndices.Count - 1);
            }
            else if (physObj is Derringer)
            {
                updateFunc = UpdateDerringer;
                updateGivenFunc = UpdateGivenDerringer;
                dataObject = physObj as Derringer;
            }
            else if (physObj is FlameThrower)
            {
                updateFunc = UpdateFlameThrower;
                updateGivenFunc = UpdateGivenFlameThrower;
                dataObject = physObj as FlameThrower;
            }
            else if (physObj is Flaregun)
            {
                Flaregun asFG = physObj as Flaregun;
                updateFunc = UpdateFlaregun;
                updateGivenFunc = UpdateGivenFlaregun;
                dataObject = asFG;
                fireFunc = FireFlaregun;
                setFirearmUpdateOverride = SetFlaregunUpdateOverride;
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
            }
            else if (physObj is HCB)
            {
                updateFunc = UpdateHCB;
                updateGivenFunc = UpdateGivenHCB;
                dataObject = physObj as HCB;
            }
            else if (physObj is M72)
            {
                M72 asM72 = physObj as M72;
                updateFunc = UpdateM72;
                updateGivenFunc = UpdateGivenM72;
                dataObject = asM72;
                fireFunc = FireM72;
                setFirearmUpdateOverride = SetM72UpdateOverride;
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
            }
            else if (physObj is RemoteMissileLauncher)
            {
                RemoteMissileLauncher asRML = physObj as RemoteMissileLauncher;
                updateFunc = UpdateRemoteMissileLauncher;
                updateGivenFunc = UpdateGivenRemoteMissileLauncher;
                dataObject = asRML;
                fireFunc = FireRemoteMissileLauncher;
                setFirearmUpdateOverride = SetRemoteMissileLauncherUpdateOverride;
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
            }
            else if (physObj is RollingBlock)
            {
                RollingBlock asRB = physObj as RollingBlock;
                updateFunc = UpdateRollingBlock;
                updateGivenFunc = UpdateGivenRollingBlock;
                dataObject = asRB;
                fireFunc = FireRollingBlock;
                setFirearmUpdateOverride = SetRollingBlockUpdateOverride;
            }
            else if (physObj is RPG7)
            {
                RPG7 asRPG7 = physObj as RPG7;
                updateFunc = UpdateRPG7;
                updateGivenFunc = UpdateGivenRPG7;
                dataObject = asRPG7;
                fireFunc = FireRPG7;
                setFirearmUpdateOverride = SetRPG7UpdateOverride;
            }
            else if (physObj is SimpleLauncher)
            {
                SimpleLauncher asSimpleLauncher = physObj as SimpleLauncher;
                updateFunc = UpdateSimpleLauncher;
                updateGivenFunc = UpdateGivenSimpleLauncher;
                dataObject = asSimpleLauncher;
                fireFunc = FireSimpleLauncher;
                setFirearmUpdateOverride = SetSimpleLauncherUpdateOverride;
            }
            else if (physObj is SimpleLauncher2)
            {
                SimpleLauncher2 asSimpleLauncher = physObj as SimpleLauncher2;
                updateFunc = UpdateSimpleLauncher2;
                updateGivenFunc = UpdateGivenSimpleLauncher2;
                dataObject = asSimpleLauncher;
                fireFunc = FireSimpleLauncher2;
                setFirearmUpdateOverride = SetSimpleLauncher2UpdateOverride;
            }
            else if (physObj is MF2_RL)
            {
                MF2_RL asMF2_RL = physObj as MF2_RL;
                updateFunc = UpdateMF2_RL;
                updateGivenFunc = UpdateGivenMF2_RL;
                dataObject = asMF2_RL;
                fireFunc = FireMF2_RL;
                setFirearmUpdateOverride = SetMF2_RLUpdateOverride;
            }
            else if (physObj is LAPD2019)
            {
                updateFunc = UpdateLAPD2019;
                updateGivenFunc = UpdateGivenLAPD2019;
                dataObject = physObj as LAPD2019;
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
                    attachableFirearmFunc = (asAttachableFirearmPhysicalObject.FA as AttachableBreakActions).Fire;
                    attachableFirearmChamberRoundFunc = AttachableBreakActionsChamberRound;
                    attachableFirearmGetChamberFunc = AttachableBreakActionsGetChamber;
                }
                else if(asAttachableFirearmPhysicalObject.FA is AttachableClosedBoltWeapon)
                {
                    updateFunc = UpdateAttachableClosedBoltWeapon;
                    updateGivenFunc = UpdateGivenAttachableClosedBoltWeapon;
                    attachableFirearmFunc = (asAttachableFirearmPhysicalObject.FA as AttachableClosedBoltWeapon).Fire;
                    attachableFirearmChamberRoundFunc = AttachableClosedBoltWeaponChamberRound;
                    attachableFirearmGetChamberFunc = AttachableClosedBoltWeaponGetChamber;
                }
                else if(asAttachableFirearmPhysicalObject.FA is AttachableTubeFed)
                {
                    updateFunc = UpdateAttachableTubeFed;
                    updateGivenFunc = UpdateGivenAttachableTubeFed;
                    attachableFirearmFunc = (asAttachableFirearmPhysicalObject.FA as AttachableTubeFed).Fire;
                    attachableFirearmChamberRoundFunc = AttachableTubeFedChamberRound;
                    attachableFirearmGetChamberFunc = AttachableTubeFedGetChamber;
                }
                else if(asAttachableFirearmPhysicalObject.FA is GP25)
                {
                    updateFunc = UpdateGP25;
                    updateGivenFunc = UpdateGivenGP25;
                    attachableFirearmFunc = (asAttachableFirearmPhysicalObject.FA as GP25).Fire;
                    attachableFirearmChamberRoundFunc = GP25ChamberRound;
                    attachableFirearmGetChamberFunc = GP25GetChamber;
                }
                else if(asAttachableFirearmPhysicalObject.FA is M203)
                {
                    updateFunc = UpdateM203;
                    updateGivenFunc = UpdateGivenM203;
                    attachableFirearmFunc = (asAttachableFirearmPhysicalObject.FA as M203).Fire;
                    attachableFirearmChamberRoundFunc = M203ChamberRound;
                    attachableFirearmGetChamberFunc = M203GetChamber;
                }
                updateParentFunc = UpdateAttachableFirearmParent;
                dataObject = asAttachableFirearmPhysicalObject.FA;
            }
            else if (physObj is FVRFireArmAttachment)
            {
                FVRFireArmAttachment asAttachment = (FVRFireArmAttachment)physObj;
                updateFunc = UpdateAttachment;
                updateGivenFunc = UpdateGivenAttachment;
                updateParentFunc = UpdateAttachmentParent;
                dataObject = asAttachment;
            }
            else if (physObj is SosigWeaponPlayerInterface)
            {
                SosigWeaponPlayerInterface asInterface = (SosigWeaponPlayerInterface)physObj;
                updateFunc = UpdateSosigWeaponInterface;
                updateGivenFunc = UpdateGivenSosigWeaponInterface;
                dataObject = asInterface;
                sosigWeaponfireFunc = asInterface.W.FireGun;
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
            else
            {
                if (data.data[0] != newData[0])
                {
                    // Set lever released
                    if (newData[0] == 1 && !(bool)Mod.FVRGrenade_m_isLeverReleased.GetValue(asGrenade))
                    {
                        asGrenade.ReleaseLever();
                    }
                    modified = true;
                }
                if (data.data[1] != newData[1])
                {
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
                    modified = true;
                }
                if (data.data[1] != newData[1])
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
                    modified = true;
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

            if (data.data == null)
            {
                data.data = new byte[rings == null ? 0 : rings.Count + 1];
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
                if (data.data[0] != newData[0])
                {
                    // Set lever released
                    if (newData[0] == 1 && !asPG.IsLeverReleased())
                    {
                        asPG.ReleaseLever();
                    }
                    modified = true;
                }
            }

            if (rings != null)
            {
                for (int i = 0; i < rings.Count; ++i)
                {
                    if (newData[i+1] == 1 && !rings[i].HasPinDetached())
                    {
                        rings[i].PopOutRoutine();
                        modified = true;
                    }
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
                data.data = new byte[2];
                modified = true;
            }

            byte preval = data.data[0];
            byte preval0 = data.data[1];

            // Write chambered round class
            if (asMF2_RL.Chamber.GetRound() == null)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 0);
            }
            else
            {
                BitConverter.GetBytes((short)asMF2_RL.Chamber.GetRound().RoundClass).CopyTo(data.data, 0);
            }

            modified |= (preval != data.data[0] || preval0 != data.data[1]);

            return modified;
        }

        private bool UpdateGivenMF2_RL(byte[] newData)
        {
            bool modified = false;
            MF2_RL asMF2_RL = (MF2_RL)dataObject;

            // Set chamber
            short chamberClassIndex = BitConverter.ToInt16(newData, 0);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asMF2_RL.Chamber.GetRound() != null)
                {
                    asMF2_RL.Chamber.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asMF2_RL.Chamber.GetRound() == null || asMF2_RL.Chamber.GetRound().RoundClass != roundClass)
                {
                    asMF2_RL.Chamber.SetRound(roundClass, asMF2_RL.Chamber.transform.position, asMF2_RL.Chamber.transform.rotation);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool FireMF2_RL()
        {
            MF2_RL asMF2_RL = (MF2_RL)dataObject;
            asMF2_RL.Fire();
            return true;
        }

        private void SetMF2_RLUpdateOverride(FireArmRoundClass roundClass)
        {
            MF2_RL asMF2_RL = (MF2_RL)dataObject;
            asMF2_RL.Chamber.SetRound(roundClass, asMF2_RL.Chamber.transform.position, asMF2_RL.Chamber.transform.rotation);
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

            int necessarySize = asRevolver.Cylinder.NumChambers * 2 + 2;

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
            for (int i = 0; i < asRevolver.Cylinder.Chambers.Length; ++i)
            {
                int firstIndex = i * 2 + 2;
                preval0 = data.data[firstIndex];
                preval1 = data.data[firstIndex + 1];

                if (asRevolver.Cylinder.Chambers[i].GetRound() == null)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(data.data, firstIndex);
                }
                else
                {
                    BitConverter.GetBytes((short)asRevolver.Cylinder.Chambers[i].GetRound().RoundClass).CopyTo(data.data, firstIndex);
                }

                modified |= (preval0 != data.data[firstIndex] || preval1 != data.data[firstIndex + 1]);
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
                int firstIndex = i * 2 + 2;
                short chamberClassIndex = BitConverter.ToInt16(newData, firstIndex);
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (asRevolver.Cylinder.Chambers[i].GetRound() != null)
                    {
                        asRevolver.Cylinder.Chambers[i].SetRound(null, false);
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (asRevolver.Cylinder.Chambers[i].GetRound() == null || asRevolver.Cylinder.Chambers[i].GetRound().RoundClass != roundClass)
                    {
                        asRevolver.Cylinder.Chambers[i].SetRound(roundClass, asRevolver.Cylinder.Chambers[i].transform.position, asRevolver.Cylinder.Chambers[i].transform.rotation);
                        modified = true;
                    }
                }
            }

            data.data = newData;

            return modified;
        }

        private bool UpdateSimpleLauncher2()
        {
            SimpleLauncher2 asSimpleLauncher = (SimpleLauncher2)dataObject;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[3];
                modified = true;
            }

            byte preval = data.data[0];

            // Write mode
            data.data[0] = (byte)(int)asSimpleLauncher.Mode;

            modified |= preval != data.data[0];

            preval = data.data[1];
            byte preval0 = data.data[2];

            // Write chambered round class
            if (asSimpleLauncher.Chamber.GetRound() == null)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 1);
            }
            else
            {
                BitConverter.GetBytes((short)asSimpleLauncher.Chamber.GetRound().RoundClass).CopyTo(data.data, 1);
            }

            modified |= (preval != data.data[1] || preval0 != data.data[2]);

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
            short chamberClassIndex = BitConverter.ToInt16(newData, 1);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asSimpleLauncher.Chamber.GetRound() != null)
                {
                    asSimpleLauncher.Chamber.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asSimpleLauncher.Chamber.GetRound() == null || asSimpleLauncher.Chamber.GetRound().RoundClass != roundClass)
                {
                    asSimpleLauncher.Chamber.SetRound(roundClass, asSimpleLauncher.Chamber.transform.position, asSimpleLauncher.Chamber.transform.rotation);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool FireSimpleLauncher2()
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

        private void SetSimpleLauncher2UpdateOverride(FireArmRoundClass roundClass)
        {
            SimpleLauncher2 asSimpleLauncher = (SimpleLauncher2)dataObject;
            asSimpleLauncher.Chamber.SetRound(roundClass, asSimpleLauncher.Chamber.transform.position, asSimpleLauncher.Chamber.transform.rotation);
        }
        
        private bool UpdateSimpleLauncher()
        {
            SimpleLauncher asSimpleLauncher = (SimpleLauncher)dataObject;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[2];
                modified = true;
            }

            byte preval = data.data[0];
            byte preval0 = data.data[1];

            // Write chambered round class
            if (asSimpleLauncher.Chamber.GetRound() == null)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 0);
            }
            else
            {
                BitConverter.GetBytes((short)asSimpleLauncher.Chamber.GetRound().RoundClass).CopyTo(data.data, 0);
            }

            modified |= (preval != data.data[0] || preval0 != data.data[1]);

            return modified;
        }

        private bool UpdateGivenSimpleLauncher(byte[] newData)
        {
            bool modified = false;
            SimpleLauncher asSimpleLauncher = (SimpleLauncher)dataObject;

            // Set chamber
            short chamberClassIndex = BitConverter.ToInt16(newData, 0);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asSimpleLauncher.Chamber.GetRound() != null)
                {
                    asSimpleLauncher.Chamber.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asSimpleLauncher.Chamber.GetRound() == null || asSimpleLauncher.Chamber.GetRound().RoundClass != roundClass)
                {
                    asSimpleLauncher.Chamber.SetRound(roundClass, asSimpleLauncher.Chamber.transform.position, asSimpleLauncher.Chamber.transform.rotation);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool FireSimpleLauncher()
        {
            SimpleLauncher asSimpleLauncher = (SimpleLauncher)dataObject;
            asSimpleLauncher.Fire();
            return true;
        }

        private void SetSimpleLauncherUpdateOverride(FireArmRoundClass roundClass)
        {
            SimpleLauncher asSimpleLauncher = (SimpleLauncher)dataObject;
            asSimpleLauncher.Chamber.SetRound(roundClass, asSimpleLauncher.Chamber.transform.position, asSimpleLauncher.Chamber.transform.rotation);
        }

        private bool UpdateRPG7()
        {
            RPG7 asRPG7 = (RPG7)dataObject;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[3];
                modified = true;
            }

            byte preval = data.data[0];

            // Write hammer state
            data.data[0] = (bool)Mod.RPG7_m_isHammerCocked.GetValue(asRPG7) ? (byte)1 : (byte)0;

            modified |= preval != data.data[0];

            preval = data.data[1];
            byte preval0 = data.data[2];

            // Write chambered round class
            if (asRPG7.Chamber.GetRound() == null)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 1);
            }
            else
            {
                BitConverter.GetBytes((short)asRPG7.Chamber.GetRound().RoundClass).CopyTo(data.data, 1);
            }

            modified |= (preval != data.data[1] || preval0 != data.data[2]);

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
            short chamberClassIndex = BitConverter.ToInt16(newData, 1);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asRPG7.Chamber.GetRound() != null)
                {
                    asRPG7.Chamber.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asRPG7.Chamber.GetRound() == null || asRPG7.Chamber.GetRound().RoundClass != roundClass)
                {
                    asRPG7.Chamber.SetRound(roundClass, asRPG7.Chamber.transform.position, asRPG7.Chamber.transform.rotation);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool FireRPG7()
        {
            RPG7 asRPG7 = (RPG7)dataObject;
            asRPG7.Fire();
            return true;
        }

        private void SetRPG7UpdateOverride(FireArmRoundClass roundClass)
        {
            RPG7 asRPG7 = (RPG7)dataObject;
            Mod.RPG7_m_isHammerCocked.SetValue(asRPG7, true);
            asRPG7.Chamber.SetRound(roundClass, asRPG7.Chamber.transform.position, asRPG7.Chamber.transform.rotation);
        }

        private bool UpdateRollingBlock()
        {
            RollingBlock asRB = (RollingBlock)dataObject;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[3];
                modified = true;
            }

            byte preval = data.data[0];

            // Write block state
            data.data[0] = (byte)(int)Mod.RollingBlock_m_state.GetValue(asRB);

            modified |= preval != data.data[0];

            preval = data.data[1];
            byte preval0 = data.data[2];

            // Write chambered round class
            if (asRB.Chamber.GetRound() == null)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 1);
            }
            else
            {
                BitConverter.GetBytes((short)asRB.Chamber.GetRound().RoundClass).CopyTo(data.data, 1);
            }

            modified |= (preval != data.data[1] || preval0 != data.data[2]);

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
            short chamberClassIndex = BitConverter.ToInt16(newData, 1);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asRB.Chamber.GetRound() != null)
                {
                    asRB.Chamber.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asRB.Chamber.GetRound() == null || asRB.Chamber.GetRound().RoundClass != roundClass)
                {
                    asRB.Chamber.SetRound(roundClass, asRB.Chamber.transform.position, asRB.Chamber.transform.rotation);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool FireRollingBlock()
        {
            RollingBlock asRB = (RollingBlock)dataObject;
            Mod.RollingBlock_Fire.Invoke(asRB, null);
            return true;
        }

        private void SetRollingBlockUpdateOverride(FireArmRoundClass roundClass)
        {
            RollingBlock asRB = (RollingBlock)dataObject;

            asRB.Chamber.SetRound(roundClass, asRB.Chamber.transform.position, asRB.Chamber.transform.rotation);
        }

        private bool UpdateRGM40()
        {
            RGM40 asRGM40 = dataObject as RGM40;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[2];
                modified = true;
            }

            byte preval = data.data[0];
            byte preval0 = data.data[1];

            // Write chambered round class
            if (asRGM40.Chamber.GetRound() == null)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 0);
            }
            else
            {
                BitConverter.GetBytes((short)asRGM40.Chamber.GetRound().RoundClass).CopyTo(data.data, 0);
            }

            modified |= (preval != data.data[0] || preval0 != data.data[1]);

            return modified;
        }

        private bool UpdateGivenRGM40(byte[] newData)
        {
            bool modified = false;
            RGM40 asRGM40 = dataObject as RGM40;

            // Set chamber
            short chamberClassIndex = BitConverter.ToInt16(newData, 0);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asRGM40.Chamber.GetRound() != null)
                {
                    asRGM40.Chamber.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asRGM40.Chamber.GetRound() == null || asRGM40.Chamber.GetRound().RoundClass != roundClass)
                {
                    asRGM40.Chamber.SetRound(roundClass, asRGM40.Chamber.transform.position, asRGM40.Chamber.transform.rotation);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool FireRGM40()
        {
            RGM40 asRGM40 = dataObject as RGM40;

            asRGM40.Fire();
            return true;
        }

        private void SetRGM40UpdateOverride(FireArmRoundClass roundClass)
        {
            RGM40 asRGM40 = dataObject as RGM40;

            asRGM40.Chamber.SetRound(roundClass, asRGM40.Chamber.transform.position, asRGM40.Chamber.transform.rotation);
        }

        private bool UpdateRemoteMissileLauncher()
        {
            RemoteMissileLauncher asRML = dataObject as RemoteMissileLauncher;
            bool modified = false;

            object missile = Mod.RemoteMissileLauncher_m_missile.GetValue(asRML);

            if (data.data == null)
            {
                data.data = new byte[missile == null ? 3 : 35];
                modified = true;
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
                RemoteMissile actualMissile = missile as RemoteMissile;
                modified = true;

                // Write missile pos
                BitConverter.GetBytes(actualMissile.transform.position.x).CopyTo(data.data, 3);
                BitConverter.GetBytes(actualMissile.transform.position.y).CopyTo(data.data, 7);
                BitConverter.GetBytes(actualMissile.transform.position.z).CopyTo(data.data, 11);

                // Write missile rot
                BitConverter.GetBytes(actualMissile.transform.rotation.eulerAngles.x).CopyTo(data.data, 15);
                BitConverter.GetBytes(actualMissile.transform.rotation.eulerAngles.y).CopyTo(data.data, 19);
                BitConverter.GetBytes(actualMissile.transform.rotation.eulerAngles.z).CopyTo(data.data, 23);

                // Write target speed
                BitConverter.GetBytes((float)Mod.RemoteMissile_speed.GetValue(actualMissile)).CopyTo(data.data, 27);

                // Write speed
                BitConverter.GetBytes((float)Mod.RemoteMissile_tarSpeed.GetValue(actualMissile)).CopyTo(data.data, 31);
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
                    asRML.Chamber.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                if (asRML.Chamber.GetRound() == null)
                {
                    asRML.Chamber.SetRound(FireArmRoundClass.FragExplosive, asRML.Chamber.transform.position, asRML.Chamber.transform.rotation);
                    modified = true;
                }
            }

            object missile = Mod.RemoteMissileLauncher_m_missile.GetValue(asRML);
            if (missile != null)
            {
                if (newData[2] == 1)
                {
                    RemoteMissile actualMissile = missile as RemoteMissile;
                    actualMissile.transform.position = new Vector3(BitConverter.ToSingle(newData, 3), BitConverter.ToSingle(newData, 7), BitConverter.ToSingle(newData, 11));
                    actualMissile.transform.rotation = Quaternion.Euler(BitConverter.ToSingle(newData, 15), BitConverter.ToSingle(newData, 19), BitConverter.ToSingle(newData, 23));

                    Mod.RemoteMissile_speed.SetValue(actualMissile, BitConverter.ToSingle(newData, 27));
                    Mod.RemoteMissile_tarSpeed.SetValue(actualMissile, BitConverter.ToSingle(newData, 31));
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

        private bool FireRemoteMissileLauncher()
        {
            RemoteMissileLauncher asRML = dataObject as RemoteMissileLauncher;
            Mod.RemoteMissileLauncher_FireShot.Invoke(asRML, null);
            return true;
        }

        private void SetRemoteMissileLauncherUpdateOverride(FireArmRoundClass roundClass)
        {
            RemoteMissileLauncher asRML = dataObject as RemoteMissileLauncher;

            asRML.Chamber.SetRound(roundClass, asRML.Chamber.transform.position, asRML.Chamber.transform.rotation);
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
                    asPG.Chamber.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                if (asPG.Chamber.GetRound() == null)
                {
                    asPG.Chamber.SetRound(FireArmRoundClass.FMJ, asPG.Chamber.transform.position, asPG.Chamber.transform.rotation);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool FirePotatoGun()
        {
            PotatoGun asPG = dataObject as PotatoGun;
            asPG.Fire();
            return true;
        }

        private void SetPotatoGunUpdateOverride(FireArmRoundClass roundClass)
        {
            PotatoGun asPG = dataObject as PotatoGun;

            asPG.Chamber.SetRound(roundClass, asPG.Chamber.transform.position, asPG.Chamber.transform.rotation);
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
                if (data.data[0] != newData[0])
                {
                    // Set safety
                    bool currentSafety = (bool)Mod.M72_m_isSafetyEngaged.GetValue(asM72);
                    if ((currentSafety && newData[0] == 0) || (!currentSafety && newData[0] == 1))
                    {
                        asM72.ToggleSafety();
                    }
                    modified = true;
                }
                if (data.data[1] != newData[1])
                {
                    // Set cap
                    if ((asM72.CanTubeBeGrabbed() && newData[1] == 0) || (!asM72.CanTubeBeGrabbed() && newData[1] == 1))
                    {
                        asM72.ToggleCap();
                    }
                    modified = true;
                }
                if (data.data[2] != newData[2])
                {
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
            }

            if (newData[3] == 0) // We don't want round in chamber
            {
                if (asM72.Chamber.GetRound() != null)
                {
                    asM72.Chamber.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                if (asM72.Chamber.GetRound() == null)
                {
                    asM72.Chamber.SetRound(FireArmRoundClass.FragExplosive, asM72.Chamber.transform.position, asM72.Chamber.transform.rotation);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool FireM72()
        {
            M72 asM72 = dataObject as M72;
            asM72.Fire();
            return true;
        }

        private void SetM72UpdateOverride(FireArmRoundClass roundClass)
        {
            M72 asM72 = dataObject as M72;

            asM72.Chamber.SetRound(roundClass, asM72.Chamber.transform.position, asM72.Chamber.transform.rotation);
        }

        private bool UpdateOpenBoltReceiver()
        {
            OpenBoltReceiver asOBR = dataObject as OpenBoltReceiver;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[5];
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

            // Write chambered round class
            if (asOBR.Chamber.GetRound() == null)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 2);
            }
            else
            {
                BitConverter.GetBytes((short)asOBR.Chamber.GetRound().RoundClass).CopyTo(data.data, 2);
            }

            modified |= (preval != data.data[2] || preval0 != data.data[3]);

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
            short chamberClassIndex = BitConverter.ToInt16(newData, 2);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asOBR.Chamber.GetRound() != null)
                {
                    asOBR.Chamber.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asOBR.Chamber.GetRound() == null || asOBR.Chamber.GetRound().RoundClass != roundClass)
                {
                    asOBR.Chamber.SetRound(roundClass, asOBR.Chamber.transform.position, asOBR.Chamber.transform.rotation);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private void SetOBRUpdateOverride(FireArmRoundClass roundClass)
        {
            OpenBoltReceiver asOBR = dataObject as OpenBoltReceiver;

            asOBR.Chamber.SetRound(roundClass, asOBR.Chamber.transform.position, asOBR.Chamber.transform.rotation);
        }

        private bool UpdateHCB()
        {
            HCB asHCB = dataObject as HCB;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[4];
                modified = true;
            }

            byte preval0 = data.data[0];

            // Write m_sledState
            data.data[0] = (byte)Mod.HCB_m_sledState.GetValue(asHCB);

            modified |= preval0 != data.data[0];

            preval0 = data.data[1];

            // Write chamber accessible
            data.data[1] = asHCB.Chamber.IsAccessible ? (byte)1 : (byte)0;

            modified |= preval0 != data.data[1];

            // Write chambered rounds
            preval0 = data.data[2];
            byte preval1 = data.data[3];

            if (asHCB.Chamber.GetRound() == null)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 2);
            }
            else
            {
                BitConverter.GetBytes((short)asHCB.Chamber.GetRound().RoundClass).CopyTo(data.data, 2);
            }

            modified |= (preval0 != data.data[2] || preval1 != data.data[3]);

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
            short chamberClassIndex = BitConverter.ToInt16(newData, 2);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asHCB.Chamber.GetRound() != null)
                {
                    asHCB.Chamber.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asHCB.Chamber.GetRound() == null || asHCB.Chamber.GetRound().RoundClass != roundClass)
                {
                    asHCB.Chamber.SetRound(roundClass, asHCB.Chamber.transform.position, asHCB.Chamber.transform.rotation);
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

            int necessarySize = asGG.Chambers.Length * 2 + 2;

            if (data.data == null)
            {
                data.data = new byte[necessarySize];
                modified = true;
            }

            byte preval0 = data.data[0];

            // Write cur chamber
            data.data[0] = (byte)Mod.GrappleGun_m_curChamber.GetValue(asGG);

            modified |= preval0 != data.data[0];

            preval0 = data.data[1];

            // Write mag loaded
            data.data[1] = asGG.IsMagLoaded ? (byte)1 : (byte)0;

            modified |= preval0 != data.data[1];

            // Write chambered rounds
            byte preval1;
            for (int i = 0; i < asGG.Chambers.Length; ++i)
            {
                int firstIndex = i * 2 + 2;
                preval0 = data.data[firstIndex];
                preval1 = data.data[firstIndex + 1];

                if (asGG.Chambers[i].GetRound() == null)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(data.data, firstIndex);
                }
                else
                {
                    BitConverter.GetBytes((short)asGG.Chambers[i].GetRound().RoundClass).CopyTo(data.data, firstIndex);
                }

                modified |= (preval0 != data.data[firstIndex] || preval1 != data.data[firstIndex + 1]);
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
                if (data.data[1] != newData[1])
                {
                    // Set cyl loaded
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
                    modified = true;
                }
            }

            // Set chambers
            for (int i = 0; i < asGG.Chambers.Length; ++i)
            {
                int firstIndex = i * 2 + 2;
                short chamberClassIndex = BitConverter.ToInt16(newData, firstIndex);
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (asGG.Chambers[i].GetRound() != null)
                    {
                        asGG.Chambers[i].SetRound(null, false);
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (asGG.Chambers[i].GetRound() == null || asGG.Chambers[i].GetRound().RoundClass != roundClass)
                    {
                        asGG.Chambers[i].SetRound(roundClass, asGG.Chambers[i].transform.position, asGG.Chambers[i].transform.rotation);
                        modified = true;
                    }
                }
            }

            data.data = newData;

            return modified;
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
                data.data = new byte[3];
                modified = true;
            }

            byte preval = data.data[0];

            // Write hammer state
            data.data[0] = (bool)Mod.Flaregun_m_isHammerCocked.GetValue(asFG) ? (byte)1: (byte)0;

            modified |= preval != data.data[0];

            preval = data.data[1];
            byte preval0 = data.data[2];

            // Write chambered round class
            if (asFG.Chamber.GetRound() == null)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 1);
            }
            else
            {
                BitConverter.GetBytes((short)asFG.Chamber.GetRound().RoundClass).CopyTo(data.data, 1);
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
            short chamberClassIndex = BitConverter.ToInt16(newData, 1);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asFG.Chamber.GetRound() != null)
                {
                    asFG.Chamber.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asFG.Chamber.GetRound() == null || asFG.Chamber.GetRound().RoundClass != roundClass)
                {
                    asFG.Chamber.SetRound(roundClass, asFG.Chamber.transform.position, asFG.Chamber.transform.rotation);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private bool FireFlaregun()
        {
            Mod.Flaregun_Fire.Invoke((dataObject as Flaregun), null);
            return true;
        }

        private void SetFlaregunUpdateOverride(FireArmRoundClass roundClass)
        {
            Flaregun asFG = dataObject as Flaregun;
            asFG.Chamber.SetRound(roundClass, asFG.Chamber.transform.position, asFG.Chamber.transform.rotation);
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

        private bool UpdateLeverActionFirearm()
        {
            LeverActionFirearm asLAF = dataObject as LeverActionFirearm;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[6];
                modified = true;
            }

            // Write chamber round
            byte preval0 = data.data[0];
            byte preval1 = data.data[1];

            if (asLAF.Chamber.GetRound() == null)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 0);
            }
            else
            {
                BitConverter.GetBytes((short)asLAF.Chamber.GetRound().RoundClass).CopyTo(data.data, 0);
            }

            modified |= (preval0 != data.data[0] || preval1 != data.data[1]);

            // Write hammer state
            preval0 = data.data[2];

            data.data[2] = asLAF.IsHammerCocked ? (byte)1 : (byte)0;

            modified |= preval0 != data.data[2];

            if (asLAF.UsesSecondChamber)
            {
                // Write chamber2 round
                preval0 = data.data[3];
                preval1 = data.data[4];

                if (asLAF.Chamber2.GetRound() == null)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(data.data, 3);
                }
                else
                {
                    BitConverter.GetBytes((short)asLAF.Chamber2.GetRound().RoundClass).CopyTo(data.data, 3);
                }

                modified |= (preval0 != data.data[3] || preval1 != data.data[4]);

                // Write hammer2 state
                preval0 = data.data[5];

                data.data[5] = ((bool)Mod.LeverActionFirearm_m_isHammerCocked2.GetValue(asLAF)) ? (byte)1 : (byte)0;

                modified |= preval0 != data.data[5];
            }

            return modified;
        }

        private bool UpdateGivenLeverActionFirearm(byte[] newData)
        {
            bool modified = false;
            LeverActionFirearm asLAF = dataObject as LeverActionFirearm;

            // Set chamber round
            short chamberClassIndex = BitConverter.ToInt16(newData, 0);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asLAF.Chamber.GetRound() != null)
                {
                    asLAF.Chamber.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asLAF.Chamber.GetRound() == null || asLAF.Chamber.GetRound().RoundClass != roundClass)
                {
                    asLAF.Chamber.SetRound(roundClass, asLAF.Chamber.transform.position, asLAF.Chamber.transform.rotation);
                    modified = true;
                }
            }

            // Set hammer state
            Mod.LeverActionFirearm_m_isHammerCocked.SetValue(asLAF, newData[0] == 1);

            // Set chamber2 round
            chamberClassIndex = BitConverter.ToInt16(newData, 3);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asLAF.Chamber2.GetRound() != null)
                {
                    asLAF.Chamber2.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asLAF.Chamber2.GetRound() == null || asLAF.Chamber2.GetRound().RoundClass != roundClass)
                {
                    asLAF.Chamber2.SetRound(roundClass, asLAF.Chamber2.transform.position, asLAF.Chamber2.transform.rotation);
                    modified = true;
                }
            }

            // Set hammer2 state
            Mod.LeverActionFirearm_m_isHammerCocked2.SetValue(asLAF, newData[5] == 1);

            data.data = newData;

            return modified;
        }

        private bool UpdateDerringer()
        {
            Derringer asDerringer = dataObject as Derringer;
            bool modified = false;

            int necessarySize = asDerringer.Barrels.Count * 2 + 1;

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
            for (int i = 0; i < asDerringer.Barrels.Count; ++i)
            {
                // Write chambered round
                int firstIndex = i * 2 + 1;
                preval0 = data.data[firstIndex];
                preval1 = data.data[firstIndex + 1];

                if (asDerringer.Barrels[i].Chamber.GetRound() == null)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(data.data, firstIndex);
                }
                else
                {
                    BitConverter.GetBytes((short)asDerringer.Barrels[i].Chamber.GetRound().RoundClass).CopyTo(data.data, firstIndex);
                }

                modified |= (preval0 != data.data[firstIndex] || preval1 != data.data[firstIndex + 1]);
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
                if (data.data[0] != newData[0])
                {
                    // Set hammer state
                    if (newData[0] == 1 && !asDerringer.IsExternalHammerCocked())
                    {
                        Mod.Derringer_CockHammer.Invoke(asDerringer, null);
                    }
                    else if (newData[0] == 0 && asDerringer.IsExternalHammerCocked())
                    {
                        Mod.Derringer_m_isExternalHammerCocked.SetValue(asDerringer, false);
                    }
                    modified = true;
                }
            }

            // Set barrels
            for (int i = 0; i < asDerringer.Barrels.Count; ++i)
            {
                int firstIndex = i * 2 + 1;
                short chamberClassIndex = BitConverter.ToInt16(newData, firstIndex);
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (asDerringer.Barrels[i].Chamber.GetRound() != null)
                    {
                        asDerringer.Barrels[i].Chamber.SetRound(null, false);
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (asDerringer.Barrels[i].Chamber.GetRound() == null || asDerringer.Barrels[i].Chamber.GetRound().RoundClass != roundClass)
                    {
                        asDerringer.Barrels[i].Chamber.SetRound(roundClass, asDerringer.Barrels[i].Chamber.transform.position, asDerringer.Barrels[i].Chamber.transform.rotation);
                        modified = true;
                    }
                }
            }

            data.data = newData;

            return modified;
        }

        private bool UpdateBreakActionWeapon()
        {
            BreakActionWeapon asBreakActionWeapon = dataObject as BreakActionWeapon;
            bool modified = false;

            int necessarySize = asBreakActionWeapon.Barrels.Length * 3;

            if (data.data == null)
            {
                data.data = new byte[necessarySize];
                modified = true;
            }

            // Write chambered rounds
            byte preval0;
            byte preval1;
            for (int i = 0; i < asBreakActionWeapon.Barrels.Length; ++i)
            {
                // Write chambered round
                int firstIndex = i * 3;
                preval0 = data.data[firstIndex];
                preval1 = data.data[firstIndex + 1];

                if (asBreakActionWeapon.Barrels[i].Chamber.GetRound() == null)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(data.data, firstIndex);
                }
                else
                {
                    BitConverter.GetBytes((short)asBreakActionWeapon.Barrels[i].Chamber.GetRound().RoundClass).CopyTo(data.data, firstIndex);
                }

                modified |= (preval0 != data.data[firstIndex] || preval1 != data.data[firstIndex + 1]);

                // Write hammer state
                preval0 = data.data[firstIndex + 2];

                data.data[firstIndex + 2] = asBreakActionWeapon.Barrels[i].m_isHammerCocked ? (byte)1 : (byte)0;

                modified |= preval0 != data.data[firstIndex + 2];
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
                int firstIndex = i * 3;
                short chamberClassIndex = BitConverter.ToInt16(newData, firstIndex);
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (asBreakActionWeapon.Barrels[i].Chamber.GetRound() != null)
                    {
                        asBreakActionWeapon.Barrels[i].Chamber.SetRound(null, false);
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (asBreakActionWeapon.Barrels[i].Chamber.GetRound() == null || asBreakActionWeapon.Barrels[i].Chamber.GetRound().RoundClass != roundClass)
                    {
                        asBreakActionWeapon.Barrels[i].Chamber.SetRound(roundClass, asBreakActionWeapon.Barrels[i].Chamber.transform.position, asBreakActionWeapon.Barrels[i].Chamber.transform.rotation);
                        modified = true;
                    }
                }

                asBreakActionWeapon.Barrels[i].m_isHammerCocked = newData[firstIndex + 2] == 1;
            }

            data.data = newData;

            return modified;
        }

        private bool UpdateBAP()
        {
            BAP asBAP = dataObject as BAP;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[4];
                modified = true;
            }

            byte preval = data.data[0];

            // Write fire mode index
            data.data[0] = (byte)Mod.BAP_m_fireSelectorMode.GetValue(asBAP);

            modified |= preval != data.data[0];

            preval = data.data[1];

            // Write hammer state
            data.data[1] = BitConverter.GetBytes((bool)Mod.BAP_m_isHammerCocked.GetValue(asBAP))[0];

            modified |= preval != data.data[1];

            preval = data.data[2];
            byte preval0 = data.data[3];

            // Write chambered round class
            if (asBAP.Chamber.GetRound() == null)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 2);
            }
            else
            {
                BitConverter.GetBytes((short)asBAP.Chamber.GetRound().RoundClass).CopyTo(data.data, 2);
            }

            modified |= (preval != data.data[2] || preval0 != data.data[3]);

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
            short chamberClassIndex = BitConverter.ToInt16(newData, 2);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asBAP.Chamber.GetRound() != null)
                {
                    asBAP.Chamber.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asBAP.Chamber.GetRound() == null || asBAP.Chamber.GetRound().RoundClass != roundClass)
                {
                    asBAP.Chamber.SetRound(roundClass, asBAP.Chamber.transform.position, asBAP.Chamber.transform.rotation);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private void SetBAPUpdateOverride(FireArmRoundClass roundClass)
        {
            BAP asBAP = dataObject as BAP;

            asBAP.Chamber.SetRound(roundClass, asBAP.Chamber.transform.position, asBAP.Chamber.transform.rotation);
        }

        private bool UpdateRevolvingShotgun()
        {
            RevolvingShotgun asRS = dataObject as RevolvingShotgun;
            bool modified = false;

            int necessarySize = asRS.Chambers.Length * 2 + 2;

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
            for (int i = 0; i < asRS.Chambers.Length; ++i)
            {
                int firstIndex = i * 2 + 2;
                preval0 = data.data[firstIndex];
                preval1 = data.data[firstIndex + 1];

                if (asRS.Chambers[i].GetRound() == null)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(data.data, firstIndex);
                }
                else
                {
                    BitConverter.GetBytes((short)asRS.Chambers[i].GetRound().RoundClass).CopyTo(data.data, firstIndex);
                }

                modified |= (preval0 != data.data[firstIndex] || preval1 != data.data[firstIndex + 1]);
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
                if (data.data[1] != newData[1])
                {
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
                    modified = true;
                }
            }

            // Set chambers
            for (int i = 0; i < asRS.Chambers.Length; ++i)
            {
                int firstIndex = i * 2 + 2;
                short chamberClassIndex = BitConverter.ToInt16(newData, firstIndex);
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (asRS.Chambers[i].GetRound() != null)
                    {
                        asRS.Chambers[i].SetRound(null, false);
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (asRS.Chambers[i].GetRound() == null || asRS.Chambers[i].GetRound().RoundClass != roundClass)
                    {
                        asRS.Chambers[i].SetRound(roundClass, asRS.Chambers[i].transform.position, asRS.Chambers[i].transform.rotation);
                        modified = true;
                    }
                }
            }

            data.data = newData;

            return modified;
        }

        private bool UpdateRevolver()
        {
            Revolver asRevolver = dataObject as Revolver;
            bool modified = false;

            int necessarySize = asRevolver.Cylinder.numChambers * 2 + 1;

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
            for (int i = 0; i < asRevolver.Chambers.Length; ++i)
            {
                int firstIndex = i * 2 + 1;
                preval0 = data.data[firstIndex];
                preval1 = data.data[firstIndex + 1];

                if (asRevolver.Chambers[i].GetRound() == null)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(data.data, firstIndex);
                }
                else
                {
                    BitConverter.GetBytes((short)asRevolver.Chambers[i].GetRound().RoundClass).CopyTo(data.data, firstIndex);
                }

                modified |= (preval0 != data.data[firstIndex] || preval1 != data.data[firstIndex + 1]);
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
                int firstIndex = i * 2 + 1;
                short chamberClassIndex = BitConverter.ToInt16(newData, firstIndex);
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (asRevolver.Chambers[i].GetRound() != null)
                    {
                        asRevolver.Chambers[i].SetRound(null, false);
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (asRevolver.Chambers[i].GetRound() == null || asRevolver.Chambers[i].GetRound().RoundClass != roundClass)
                    {
                        asRevolver.Chambers[i].SetRound(roundClass, asRevolver.Chambers[i].transform.position, asRevolver.Chambers[i].transform.rotation);
                        modified = true;
                    }
                }
            }

            data.data = newData;

            return modified;
        }

        private bool UpdateM203()
        {
            M203 asM203 = dataObject as M203;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[2];
                modified = true;
            }

            byte preval = data.data[0];
            byte preval0 = data.data[1];

            // Write chambered round class
            if (asM203.Chamber.GetRound() == null)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 0);
            }
            else
            {
                BitConverter.GetBytes((short)asM203.Chamber.GetRound().RoundClass).CopyTo(data.data, 0);
            }

            modified |= (preval != data.data[0] || preval0 != data.data[1]);

            return modified;
        }

        private bool UpdateGivenM203(byte[] newData)
        {
            bool modified = data.data == null;
            M203 asM203 = dataObject as M203;

            // Set chamber
            short chamberClassIndex = BitConverter.ToInt16(newData, 0);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asM203.Chamber.GetRound() != null)
                {
                    asM203.Chamber.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asM203.Chamber.GetRound() == null || asM203.Chamber.GetRound().RoundClass != roundClass)
                {
                    asM203.Chamber.SetRound(roundClass, asM203.Chamber.transform.position, asM203.Chamber.transform.rotation);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private void M203ChamberRound(FireArmRoundClass roundClass)
        {
            M203 asM203 = dataObject as M203;
            asM203.Chamber.SetRound(roundClass, asM203.Chamber.transform.position, asM203.Chamber.transform.rotation);
        }

        private FVRFireArmChamber M203GetChamber()
        {
            M203 asM203 = dataObject as M203;
            return asM203.Chamber;
        }

        private bool UpdateGP25()
        {
            GP25 asGP25 = dataObject as GP25;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[3];
                modified = true;
            }

            byte preval = data.data[0];

            // Write safety
            data.data[0] = (byte)(asGP25.m_safetyEngaged ? 1:0);

            modified |= preval != data.data[0];

            preval = data.data[1];
            byte preval0 = data.data[2];

            // Write chambered round class
            if (asGP25.Chamber.GetRound() == null)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 1);
            }
            else
            {
                BitConverter.GetBytes((short)asGP25.Chamber.GetRound().RoundClass).CopyTo(data.data, 1);
            }

            modified |= (preval != data.data[1] || preval0 != data.data[2]);

            return modified;
        }

        private bool UpdateGivenGP25(byte[] newData)
        {
            bool modified = false;
            GP25 asGP25 = dataObject as GP25;

            if (data.data == null)
            {
                modified = true;

                // Set safety
                asGP25.m_safetyEngaged = newData[0] == 1;
            }
            else
            {
                if (data.data[0] != newData[0])
                {
                    // Set safety
                    asGP25.m_safetyEngaged = newData[0] == 1;
                    modified = true;
                }
            }

            // Set chamber
            short chamberClassIndex = BitConverter.ToInt16(newData, 1);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asGP25.Chamber.GetRound() != null)
                {
                    asGP25.Chamber.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asGP25.Chamber.GetRound() == null || asGP25.Chamber.GetRound().RoundClass != roundClass)
                {
                    asGP25.Chamber.SetRound(roundClass, asGP25.Chamber.transform.position, asGP25.Chamber.transform.rotation);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private void GP25ChamberRound(FireArmRoundClass roundClass)
        {
            GP25 asGP25 = dataObject as GP25;
            asGP25.Chamber.SetRound(roundClass, asGP25.Chamber.transform.position, asGP25.Chamber.transform.rotation);
        }

        private FVRFireArmChamber GP25GetChamber()
        {
            GP25 asGP25 = dataObject as GP25;
            return asGP25.Chamber;
        }

        private bool UpdateAttachableTubeFed()
        {
            AttachableTubeFed asATF = dataObject as AttachableTubeFed;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[6];
                modified = true;
            }

            byte preval = data.data[0];

            // Write fire mode index
            data.data[0] = (byte)(int)typeof(TubeFedShotgun).GetField("m_fireSelectorMode", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(asATF);

            modified |= preval != data.data[0];

            preval = data.data[1];

            // Write hammer state
            data.data[1] = BitConverter.GetBytes(asATF.IsHammerCocked)[0];

            modified |= preval != data.data[1];

            preval = data.data[2];
            byte preval0 = data.data[3];

            // Write chambered round class
            if (asATF.Chamber.GetRound() == null)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 2);
            }
            else
            {
                BitConverter.GetBytes((short)asATF.Chamber.GetRound().RoundClass).CopyTo(data.data, 2);
            }

            modified |= (preval != data.data[3] || preval0 != data.data[4]);

            preval = data.data[4];

            // Write bolt handle pos
            data.data[4] = (byte)asATF.Bolt.CurPos;

            modified |= preval != data.data[4];

            if (asATF.HasHandle)
            {
                preval = data.data[5];

                // Write bolt handle pos
                data.data[5] = (byte)asATF.Handle.CurPos;

                modified |= preval != data.data[5];
            }

            return modified;
        }

        private bool UpdateGivenAttachableTubeFed(byte[] newData)
        {
            bool modified = false;
            AttachableTubeFed asATF = dataObject as AttachableTubeFed;

            if (data.data == null)
            {
                modified = true;

                // Set fire select mode
                typeof(TubeFedShotgun).GetField("m_fireSelectorMode", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asATF, (int)newData[0]);

                // Set bolt pos
                asATF.Bolt.LastPos = asATF.Bolt.CurPos;
                asATF.Bolt.CurPos = (AttachableTubeFedBolt.BoltPos)newData[4];

                if (asATF.HasHandle)
                {
                    // Set handle pos
                    asATF.Handle.LastPos = asATF.Handle.CurPos;
                    asATF.Handle.CurPos = (AttachableTubeFedFore.BoltPos)newData[5];
                }
            }
            else
            {
                if (data.data[0] != newData[0])
                {
                    // Set fire select mode
                    typeof(TubeFedShotgun).GetField("m_fireSelectorMode", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asATF, (int)newData[0]);
                    modified = true;
                }
                if (data.data[4] != newData[4])
                {
                    // Set bolt pos
                    asATF.Bolt.LastPos = asATF.Bolt.CurPos;
                    asATF.Bolt.CurPos = (AttachableTubeFedBolt.BoltPos)newData[4];
                }
                if (asATF.HasHandle && data.data[5] != newData[5])
                {
                    // Set handle pos
                    asATF.Handle.LastPos = asATF.Handle.CurPos;
                    asATF.Handle.CurPos = (AttachableTubeFedFore.BoltPos)newData[5];
                }
            }

            // Set hammer state
            if (newData[1] == 0)
            {
                if (asATF.IsHammerCocked)
                {
                    typeof(TubeFedShotgun).GetField("m_isHammerCocked", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asATF, BitConverter.ToBoolean(newData, 1));
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
            short chamberClassIndex = BitConverter.ToInt16(newData, 2);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asATF.Chamber.GetRound() != null)
                {
                    asATF.Chamber.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asATF.Chamber.GetRound() == null || asATF.Chamber.GetRound().RoundClass != roundClass)
                {
                    asATF.Chamber.SetRound(roundClass, asATF.Chamber.transform.position, asATF.Chamber.transform.rotation);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private void AttachableTubeFedChamberRound(FireArmRoundClass roundClass)
        {
            AttachableTubeFed asATF = dataObject as AttachableTubeFed;
            asATF.Chamber.SetRound(roundClass, asATF.Chamber.transform.position, asATF.Chamber.transform.rotation);
        }

        private FVRFireArmChamber AttachableTubeFedGetChamber()
        {
            AttachableTubeFed asATF = dataObject as AttachableTubeFed;
            return asATF.Chamber;
        }

        private bool UpdateAttachableClosedBoltWeapon()
        {
            AttachableClosedBoltWeapon asACBW = dataObject as AttachableClosedBoltWeapon;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[5];
                modified = true;
            }

            byte preval = data.data[0];

            // Write fire mode index
            data.data[0] = (byte)asACBW.FireSelectorModeIndex;

            modified |= preval != data.data[0];

            preval = data.data[1];

            // Write camBurst
            data.data[1] = (byte)(int)typeof(AttachableClosedBoltWeapon).GetField("m_CamBurst", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(asACBW);

            modified |= preval != data.data[1];

            preval = data.data[2];

            // Write hammer state
            data.data[2] = BitConverter.GetBytes(asACBW.IsHammerCocked)[0];

            modified |= preval != data.data[2];

            preval = data.data[3];
            byte preval0 = data.data[4];

            // Write chambered round class
            if (asACBW.Chamber.GetRound() == null)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 3);
            }
            else
            {
                BitConverter.GetBytes((short)asACBW.Chamber.GetRound().RoundClass).CopyTo(data.data, 3);
            }

            modified |= (preval != data.data[3] || preval0 != data.data[4]);

            return modified;
        }

        private bool UpdateGivenAttachableClosedBoltWeapon(byte[] newData)
        {
            bool modified = false;
            AttachableClosedBoltWeapon asACBW = dataObject as AttachableClosedBoltWeapon;

            if (data.data == null)
            {
                modified = true;

                // Set fire select mode
                typeof(ClosedBoltWeapon).GetField("m_fireSelectorMode", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asACBW, (int)newData[0]);

                // Set camBurst
                typeof(ClosedBoltWeapon).GetField("m_CamBurst", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asACBW, (int)newData[1]);
            }
            else
            {
                if (data.data[0] != newData[0])
                {
                    // Set fire select mode
                    typeof(ClosedBoltWeapon).GetField("m_fireSelectorMode", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asACBW, (int)newData[0]);
                    modified = true;
                }
                if (data.data[1] != newData[1])
                {
                    // Set camBurst
                    typeof(ClosedBoltWeapon).GetField("m_CamBurst", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asACBW, (int)newData[1]);
                    modified = true;
                }
            }

            // Set hammer state
            if (newData[2] == 0)
            {
                if (asACBW.IsHammerCocked)
                {
                    typeof(ClosedBoltWeapon).GetField("m_isHammerCocked", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asACBW, BitConverter.ToBoolean(newData, 2));
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
            short chamberClassIndex = BitConverter.ToInt16(newData, 3);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asACBW.Chamber.GetRound() != null)
                {
                    asACBW.Chamber.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asACBW.Chamber.GetRound() == null || asACBW.Chamber.GetRound().RoundClass != roundClass)
                {
                    asACBW.Chamber.SetRound(roundClass, asACBW.Chamber.transform.position, asACBW.Chamber.transform.rotation);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private void AttachableClosedBoltWeaponChamberRound(FireArmRoundClass roundClass)
        {
            AttachableClosedBoltWeapon asACBW = dataObject as AttachableClosedBoltWeapon;
            asACBW.Chamber.SetRound(roundClass, asACBW.Chamber.transform.position, asACBW.Chamber.transform.rotation);
        }

        private FVRFireArmChamber AttachableClosedBoltWeaponGetChamber()
        {
            AttachableClosedBoltWeapon asACBW = dataObject as AttachableClosedBoltWeapon;
            return asACBW.Chamber;
        }

        private bool UpdateAttachableBreakActions()
        {
            AttachableBreakActions asABA = dataObject as AttachableBreakActions;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[1];
                modified = true;
            }

            byte preval = data.data[0];

            // Write breachOpen
            data.data[0] = ((bool)Mod.AttachableBreakActions_m_isBreachOpen.GetValue(asABA)) ? (byte)1 : (byte)0;

            modified |= preval != data.data[0];

            preval = data.data[1];
            byte preval0 = data.data[2];

            // Write chambered round class
            if (asABA.Chamber.GetRound() == null)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 1);
            }
            else
            {
                BitConverter.GetBytes((short)asABA.Chamber.GetRound().RoundClass).CopyTo(data.data, 1);
            }

            modified |= (preval != data.data[1] || preval0 != data.data[2]);

            return modified;
        }

        private bool UpdateGivenAttachableBreakActions(byte[] newData)
        {
            bool modified = false;
            AttachableBreakActions asABA = dataObject as AttachableBreakActions;

            if (data.data == null)
            {
                modified = true;

                // Set breachOpen
                bool current = ((bool)Mod.AttachableBreakActions_m_isBreachOpen.GetValue(asABA));
                bool newVal = newData[0] == 1;
                if ((current && !newVal) || (!current && newVal))
                {
                    asABA.ToggleBreach();
                }
                Mod.AttachableBreakActions_m_isBreachOpen.SetValue(asABA, newVal);
            }
            else
            {
                if (data.data[0] != newData[0])
                {
                    // Set breachOpen
                    bool current = ((bool)Mod.AttachableBreakActions_m_isBreachOpen.GetValue(asABA));
                    bool newVal = newData[0] == 1;
                    if ((current && !newVal)||(!current && newVal))
                    {
                        asABA.ToggleBreach();
                    }
                    Mod.AttachableBreakActions_m_isBreachOpen.SetValue(asABA, newVal);
                    modified = true;
                }
            }

            // Set chamber
            short chamberClassIndex = BitConverter.ToInt16(newData, 1);
            if (chamberClassIndex == -1) // We don't want round in chamber
            {
                if (asABA.Chamber.GetRound() != null)
                {
                    asABA.Chamber.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                if (asABA.Chamber.GetRound() == null || asABA.Chamber.GetRound().RoundClass != roundClass)
                {
                    asABA.Chamber.SetRound(roundClass, asABA.Chamber.transform.position, asABA.Chamber.transform.rotation);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private void AttachableBreakActionsChamberRound(FireArmRoundClass roundClass)
        {
            AttachableBreakActions asABA = dataObject as AttachableBreakActions;
            asABA.Chamber.SetRound(roundClass, asABA.Chamber.transform.position, asABA.Chamber.transform.rotation);
        }

        private FVRFireArmChamber AttachableBreakActionsGetChamber()
        {
            AttachableBreakActions asABA = dataObject as AttachableBreakActions;
            return asABA.Chamber;
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

                    if (parentTrackedItemData != null && parentTrackedItemData.physicalItem)
                    {
                        mount = parentTrackedItemData.physicalItem.physicalObject.AttachmentMounts[currentMountIndex];
                    }

                    // If not yet physically mounted to anything, can right away mount to the proper mount
                    if (asAttachment.curMount == null)
                    {
                        ++data.ignoreParentChanged;
                        asAttachment.AttachToMount(mount, true);
                        --data.ignoreParentChanged;
                    }
                    else if (asAttachment.curMount != mount) // Already mounted, but not on the right one, need to unmount, then mount of right one
                    {
                        ++data.ignoreParentChanged;
                        if (asAttachment.curMount != null)
                        {
                            asAttachment.DetachFromMount();
                        }

                        asAttachment.AttachToMount(mount, true);
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
                data.data = new byte[15];
                modified = true;
            }

            byte preval = data.data[0];

            // Write curChamber
            data.data[0] = (byte)asLAPD2019.CurChamber;

            modified |= preval != data.data[0];

            byte preval0;

            // Write chambered round classes
            for(int i=0; i < 5; ++i)
            {
                int firstIndex = i * 2 + 1;
                preval = data.data[firstIndex];
                preval0 = data.data[firstIndex + 1];
                if (asLAPD2019.Chambers[i].GetRound() == null)
                {
                    BitConverter.GetBytes((short)-1).CopyTo(data.data, firstIndex);
                }
                else
                {
                    BitConverter.GetBytes((short)asLAPD2019.Chambers[i].GetRound().RoundClass).CopyTo(data.data, firstIndex);
                }

                modified |= (preval != data.data[firstIndex] || preval0 != data.data[firstIndex + 1]);
            }

            preval = data.data[11];
            preval0 = data.data[12];
            byte preval1 = data.data[13];
            byte preval2 = data.data[14];

            // Write capacitor charge
            BitConverter.GetBytes((float)Mod.LAPD2019_m_capacitorCharge.GetValue(asLAPD2019)).CopyTo(data.data, 11);

            modified |= (preval != data.data[11] || preval0 != data.data[12] || preval1 != data.data[13] || preval2 != data.data[14]);

            preval = data.data[15];

            // Write capacitor charged
            data.data[15] = (bool)Mod.LAPD2019_m_isCapacitorCharged.GetValue(asLAPD2019) ? (byte)1 : (byte)0;

            modified |= preval != data.data[15];

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
                Mod.LAPD2019_m_capacitorCharge.SetValue(asLAPD2019, BitConverter.ToSingle(newData, 11));

                // Set capacitor charged
                Mod.LAPD2019_m_capacitorCharge.SetValue(asLAPD2019, newData[15] == 1);
            }
            else
            {
                if (data.data[0] != newData[0])
                {
                    // Set curChamber
                    asLAPD2019.CurChamber = newData[0];
                    modified = true;
                }
                if (data.data[11] != newData[11] || data.data[12] != newData[12] || data.data[13] != newData[13] || data.data[14] != newData[14])
                {
                    // Set capacitor charge
                    Mod.LAPD2019_m_capacitorCharge.SetValue(asLAPD2019, BitConverter.ToSingle(newData, 11));
                    modified = true;
                }
                if (data.data[15] != newData[15])
                {
                    // Set capacitor charged
                    Mod.LAPD2019_m_capacitorCharge.SetValue(asLAPD2019, newData[15] == 1);
                    modified = true;
                }
            }

            // Set chambers
            for (int i = 0; i < 5; ++i)
            {
                short chamberClassIndex = BitConverter.ToInt16(newData, i * 2 + 1);
                if (chamberClassIndex == -1) // We don't want round in chamber
                {
                    if (asLAPD2019.Chambers[i].GetRound() != null)
                    {
                        asLAPD2019.Chambers[i].SetRound(null, false);
                        modified = true;
                    }
                }
                else // We want a round in the chamber
                {
                    FireArmRoundClass roundClass = (FireArmRoundClass)chamberClassIndex;
                    if (asLAPD2019.Chambers[i].GetRound() == null || asLAPD2019.Chambers[i].GetRound().RoundClass != roundClass)
                    {
                        asLAPD2019.Chambers[i].SetRound(roundClass, asLAPD2019.Chambers[i].transform.position, asLAPD2019.Chambers[i].transform.rotation);
                        modified = true;
                    }
                }
            }

            data.data = newData;

            return modified;
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
            BitConverter.GetBytes((short)(int)typeof(SosigWeapon).GetField("m_shotsLeft", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(asInterface.W)).CopyTo(data.data, 0);

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
                typeof(SosigWeapon).GetField("m_shotsLeft", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asInterface.W, BitConverter.ToInt16(newData, 0));

                // Set MechaState
                asInterface.W.MechaState = (SosigWeapon.SosigWeaponMechaState)newData[2];
            }
            else 
            {
                if (data.data[0] != newData[0] || data.data[1] != newData[1])
                {
                    // Set shots left
                    typeof(SosigWeapon).GetField("m_shotsLeft", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asInterface.W, BitConverter.ToInt16(newData, 0));
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
                data.data = new byte[5];
                modified = true;
            }

            byte preval = data.data[0];

            // Write fire mode index
            data.data[0] = (byte)asCBW.FireSelectorModeIndex;

            modified |= preval != data.data[0];

            preval = data.data[1];

            // Write camBurst
            data.data[1] = (byte)(int)typeof(ClosedBoltWeapon).GetField("m_CamBurst", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(asCBW);

            modified |= preval != data.data[1];

            preval = data.data[2];

            // Write hammer state
            data.data[2] = asCBW.IsHammerCocked ? (byte)1 : (byte)0;

            modified |= preval != data.data[2];

            preval = data.data[3];
            byte preval0 = data.data[4];

            // Write chambered round class
            if(asCBW.Chamber.GetRound() == null)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 3);
            }
            else
            {
                BitConverter.GetBytes((short)asCBW.Chamber.GetRound().RoundClass).CopyTo(data.data, 3);
            }

            modified |= (preval != data.data[3] || preval0 != data.data[4]);

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
                typeof(ClosedBoltWeapon).GetField("m_fireSelectorMode", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asCBW, (int)newData[0]);

                // Set camBurst
                typeof(ClosedBoltWeapon).GetField("m_CamBurst", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asCBW, (int)newData[1]);
            }
            else 
            {
                if (data.data[0] != newData[0])
                {
                    // Set fire select mode
                    typeof(ClosedBoltWeapon).GetField("m_fireSelectorMode", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asCBW, (int)newData[0]);
                    modified = true;
                }
                if (data.data[1] != newData[1])
                {
                    // Set camBurst
                    typeof(ClosedBoltWeapon).GetField("m_CamBurst", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asCBW, (int)newData[1]);
                    modified = true;
                }
            }

            // Set hammer state
            if (newData[2] == 0)
            {
                if (asCBW.IsHammerCocked)
                {
                    typeof(ClosedBoltWeapon).GetField("m_isHammerCocked", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asCBW, BitConverter.ToBoolean(newData, 2));
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
            short chamberClassIndex = BitConverter.ToInt16(newData, 3);
            if(chamberClassIndex == -1) // We don't want round in chamber
            {
                if(asCBW.Chamber.GetRound() != null)
                {
                    asCBW.Chamber.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundClass roundClass = (FireArmRoundClass) chamberClassIndex;
                if (asCBW.Chamber.GetRound() == null || asCBW.Chamber.GetRound().RoundClass != roundClass)
                {
                    asCBW.Chamber.SetRound(roundClass, asCBW.Chamber.transform.position, asCBW.Chamber.transform.rotation);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private void SetCBWUpdateOverride(FireArmRoundClass roundClass)
        {
            ClosedBoltWeapon asCBW = (ClosedBoltWeapon)dataObject;

            asCBW.Chamber.SetRound(roundClass, asCBW.Chamber.transform.position, asCBW.Chamber.transform.rotation);
        }

        private bool UpdateHandgun()
        {
            Handgun asHandgun = dataObject as Handgun;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[5];
                modified = true;
            }

            byte preval = data.data[0];

            // Write fire mode index
            data.data[0] = (byte)asHandgun.FireSelectorModeIndex;

            modified |= preval != data.data[0];

            preval = data.data[1];

            // Write camBurst
            data.data[1] = (byte)(int)typeof(Handgun).GetField("m_CamBurst", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(asHandgun);

            modified |= preval != data.data[1];

            preval = data.data[2];

            // Write hammer state
            data.data[2] = BitConverter.GetBytes((bool)typeof(Handgun).GetField("m_isHammerCocked", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(asHandgun))[0];

            modified |= preval != data.data[2];

            preval = data.data[3];
            byte preval0 = data.data[4];

            // Write chambered round class
            if (asHandgun.Chamber.GetRound() == null)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 3);
            }
            else
            {
                BitConverter.GetBytes((short)asHandgun.Chamber.GetRound().RoundClass).CopyTo(data.data, 3);
            }

            modified |= (preval != data.data[3] || preval0 != data.data[4]);

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
                typeof(Handgun).GetField("m_fireSelectorMode", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asHandgun, (int)newData[0]);

                // Set camBurst
                typeof(Handgun).GetField("m_CamBurst", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asHandgun, (int)newData[1]);
            }
            else 
            {
                if (data.data[0] != newData[0])
                {
                    // Set fire select mode
                    typeof(Handgun).GetField("m_fireSelectorMode", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asHandgun, (int)newData[0]);
                    modified = true;
                }
                if (data.data[1] != newData[1])
                {
                    // Set camBurst
                    typeof(Handgun).GetField("m_CamBurst", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asHandgun, (int)newData[1]);
                    modified = true;
                }
            }

            FieldInfo hammerCockedField = typeof(Handgun).GetField("m_isHammerCocked", BindingFlags.NonPublic | BindingFlags.Instance);
            bool isHammerCocked = (bool)hammerCockedField.GetValue(asHandgun);

            // Set hammer state
            if (newData[2] == 0)
            {
                if (isHammerCocked)
                {
                    hammerCockedField.SetValue(asHandgun, BitConverter.ToBoolean(newData, 2));
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
            short chamberClassIndex = BitConverter.ToInt16(newData, 3);
            if(chamberClassIndex == -1) // We don't want round in chamber
            {
                if(asHandgun.Chamber.GetRound() != null)
                {
                    asHandgun.Chamber.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundClass roundClass = (FireArmRoundClass) chamberClassIndex;
                if (asHandgun.Chamber.GetRound() == null || asHandgun.Chamber.GetRound().RoundClass != roundClass)
                {
                    asHandgun.Chamber.SetRound(roundClass, asHandgun.Chamber.transform.position, asHandgun.Chamber.transform.rotation);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private void SetHandgunUpdateOverride(FireArmRoundClass roundClass)
        {
            Handgun asHandgun = dataObject as Handgun;

            asHandgun.Chamber.SetRound(roundClass, asHandgun.Chamber.transform.position, asHandgun.Chamber.transform.rotation);
        }

        private bool UpdateTubeFedShotgun()
        {
            TubeFedShotgun asTFS = dataObject as TubeFedShotgun;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[6];
                modified = true;
            }

            byte preval = data.data[0];

            // Write fire mode index
            data.data[0] = (byte)(int)typeof(TubeFedShotgun).GetField("m_fireSelectorMode", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(asTFS);

            modified |= preval != data.data[0];

            preval = data.data[1];

            // Write hammer state
            data.data[1] = BitConverter.GetBytes(asTFS.IsHammerCocked)[0];

            modified |= preval != data.data[1];

            preval = data.data[2];
            byte preval0 = data.data[3];

            // Write chambered round class
            if(asTFS.Chamber.GetRound() == null)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 2);
            }
            else
            {
                BitConverter.GetBytes((short)asTFS.Chamber.GetRound().RoundClass).CopyTo(data.data, 2);
            }

            modified |= (preval != data.data[3] || preval0 != data.data[4]);

            preval = data.data[4];

            // Write bolt handle pos
            data.data[4] = (byte)asTFS.Bolt.CurPos;

            modified |= preval != data.data[4];

            if (asTFS.HasHandle)
            {
                preval = data.data[5];

                // Write bolt handle pos
                data.data[5] = (byte)asTFS.Handle.CurPos;

                modified |= preval != data.data[5];
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

                // Set fire select mode
                typeof(TubeFedShotgun).GetField("m_fireSelectorMode", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asTFS, (int)newData[0]);

                // Set bolt pos
                asTFS.Bolt.LastPos = asTFS.Bolt.CurPos;
                asTFS.Bolt.CurPos = (TubeFedShotgunBolt.BoltPos)newData[4];

                if (asTFS.HasHandle)
                {
                    // Set handle pos
                    asTFS.Handle.LastPos = asTFS.Handle.CurPos;
                    asTFS.Handle.CurPos = (TubeFedShotgunHandle.BoltPos)newData[5];
                }
            }
            else 
            {
                if (data.data[0] != newData[0])
                {
                    // Set fire select mode
                    typeof(TubeFedShotgun).GetField("m_fireSelectorMode", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asTFS, (int)newData[0]);
                    modified = true;
                }
                if (data.data[4] != newData[4])
                {
                    // Set bolt pos
                    asTFS.Bolt.LastPos = asTFS.Bolt.CurPos;
                    asTFS.Bolt.CurPos = (TubeFedShotgunBolt.BoltPos)newData[4];
                }
                if (asTFS.HasHandle && data.data[5] != newData[5])
                {
                    // Set handle pos
                    asTFS.Handle.LastPos = asTFS.Handle.CurPos;
                    asTFS.Handle.CurPos = (TubeFedShotgunHandle.BoltPos)newData[5];
                }
            }

            // Set hammer state
            if (newData[1] == 0)
            {
                if (asTFS.IsHammerCocked)
                {
                    typeof(TubeFedShotgun).GetField("m_isHammerCocked", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asTFS, BitConverter.ToBoolean(newData, 1));
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
            short chamberClassIndex = BitConverter.ToInt16(newData, 2);
            if(chamberClassIndex == -1) // We don't want round in chamber
            {
                if(asTFS.Chamber.GetRound() != null)
                {
                    asTFS.Chamber.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundClass roundClass = (FireArmRoundClass) chamberClassIndex;
                if (asTFS.Chamber.GetRound() == null || asTFS.Chamber.GetRound().RoundClass != roundClass)
                {
                    asTFS.Chamber.SetRound(roundClass, asTFS.Chamber.transform.position, asTFS.Chamber.transform.rotation);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private void SetTFSUpdateOverride(FireArmRoundClass roundClass)
        {
            TubeFedShotgun asTFS = dataObject as TubeFedShotgun;

            asTFS.Chamber.SetRound(roundClass, asTFS.Chamber.transform.position, asTFS.Chamber.transform.rotation);
        }

        private bool UpdateBoltActionRifle()
        {
            BoltActionRifle asBAR = dataObject as BoltActionRifle;
            bool modified = false;

            if (data.data == null)
            {
                data.data = new byte[6];
                modified = true;
            }

            byte preval = data.data[0];

            // Write fire mode index
            data.data[0] = (byte)(int)typeof(BoltActionRifle).GetField("m_fireSelectorMode", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(asBAR);

            modified |= preval != data.data[0];

            preval = data.data[1];

            // Write hammer state
            data.data[1] = BitConverter.GetBytes(asBAR.IsHammerCocked)[0];

            modified |= preval != data.data[1];

            preval = data.data[2];
            byte preval0 = data.data[3];

            // Write chambered round class
            if(asBAR.Chamber.GetRound() == null)
            {
                BitConverter.GetBytes((short)-1).CopyTo(data.data, 2);
            }
            else
            {
                BitConverter.GetBytes((short)asBAR.Chamber.GetRound().RoundClass).CopyTo(data.data, 2);
            }

            modified |= (preval != data.data[3] || preval0 != data.data[4]);

            preval = data.data[4];

            // Write bolt handle state
            data.data[4] = (byte)asBAR.CurBoltHandleState;

            modified |= preval != data.data[4];

            preval = data.data[5];

            // Write bolt handle rot
            data.data[5] = (byte)asBAR.BoltHandle.HandleRot;

            modified |= preval != data.data[5];

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
                typeof(BoltActionRifle).GetField("m_fireSelectorMode", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asBAR, (int)newData[0]);

                // Set bolt handle state
                asBAR.LastBoltHandleState = asBAR.CurBoltHandleState;
                asBAR.CurBoltHandleState = (BoltActionRifle_Handle.BoltActionHandleState)newData[4];

                // Set bolt handle rot
                asBAR.BoltHandle.LastHandleRot = asBAR.BoltHandle.HandleRot;
                asBAR.BoltHandle.HandleRot = (BoltActionRifle_Handle.BoltActionHandleRot)newData[5];
            }
            else 
            {
                if (data.data[0] != newData[0])
                {
                    // Set fire select mode
                    typeof(BoltActionRifle).GetField("m_fireSelectorMode", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asBAR, (int)newData[0]);
                    modified = true;
                }
                if (data.data[4] != newData[4])
                {
                    // Set bolt handle state
                    asBAR.LastBoltHandleState = asBAR.CurBoltHandleState;
                    asBAR.CurBoltHandleState = (BoltActionRifle_Handle.BoltActionHandleState)newData[4];
                }
                if (data.data[5] != newData[5])
                {
                    // Set bolt handle rot
                    asBAR.BoltHandle.LastHandleRot = asBAR.BoltHandle.HandleRot;
                    asBAR.BoltHandle.HandleRot = (BoltActionRifle_Handle.BoltActionHandleRot)newData[5];
                }
            }

            // Set hammer state
            if (newData[1] == 0)
            {
                if (asBAR.IsHammerCocked)
                {
                    typeof(BoltActionRifle).GetField("m_isHammerCocked", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(asBAR, BitConverter.ToBoolean(newData, 1));
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
            short chamberClassIndex = BitConverter.ToInt16(newData, 2);
            if(chamberClassIndex == -1) // We don't want round in chamber
            {
                if(asBAR.Chamber.GetRound() != null)
                {
                    asBAR.Chamber.SetRound(null, false);
                    modified = true;
                }
            }
            else // We want a round in the chamber
            {
                FireArmRoundClass roundClass = (FireArmRoundClass) chamberClassIndex;
                if (asBAR.Chamber.GetRound() == null || asBAR.Chamber.GetRound().RoundClass != roundClass)
                {
                    asBAR.Chamber.SetRound(roundClass, asBAR.Chamber.transform.position, asBAR.Chamber.transform.rotation);
                    modified = true;
                }
            }

            data.data = newData;

            return modified;
        }

        private void SetBARUpdateOverride(FireArmRoundClass roundClass)
        {
            BoltActionRifle asBar = dataObject as BoltActionRifle;

            asBar.Chamber.SetRound(roundClass, asBar.Chamber.transform.position, asBar.Chamber.transform.rotation);
        }

        private bool UpdateAttachment()
        {
            bool modified = false;
            FVRFireArmAttachment asAttachment = dataObject as FVRFireArmAttachment;

            if (data.data == null)
            {
                data.data = new byte[1];
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
                for(int i=0; i < asAttachment.curMount.Parent.AttachmentMounts.Count; ++i)
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

            return modified || (preIndex != data.data[0]);
        }

        private bool UpdateGivenAttachment(byte[] newData)
        {
            bool modified = false;
            FVRFireArmAttachment asAttachment = dataObject as FVRFireArmAttachment;

            if (data.data == null || data.data.Length != newData.Length)
            {
                data.data = new byte[1];
                data.data[0] = 255;
                currentMountIndex = 255;
                modified = true;
            }

            // If mount doesn't actually change, just return now
            byte mountIndex = newData[0];
            if(currentMountIndex == mountIndex)
            {
                return modified;
            }
            data.data[0] = mountIndex;

            byte preMountIndex = currentMountIndex;
            if (mountIndex == 255)
            {
                // Should not be mounted, check if currently is
                if(asAttachment.curMount != null)
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

                if (parentTrackedItemData != null && parentTrackedItemData.physicalItem)
                {
                    // We want to be mounted, we have a parent
                    if (parentTrackedItemData.physicalItem.physicalObject.AttachmentMounts.Count > mountIndex)
                    {
                        mount = parentTrackedItemData.physicalItem.physicalObject.AttachmentMounts[mountIndex];
                    }
                }

                // Mount could be null if the mount index corresponds to a parent we have yet a receive a change to
                if (mount != null)
                {
                    ++data.ignoreParentChanged;
                    if (asAttachment.curMount != null)
                    {
                        asAttachment.DetachFromMount();
                    }

                    asAttachment.AttachToMount(mount, true);
                    currentMountIndex = mountIndex;
                    --data.ignoreParentChanged;
                }
            }

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

                    if (parentTrackedItemData != null && parentTrackedItemData.physicalItem)
                    {
                        mount = parentTrackedItemData.physicalItem.physicalObject.AttachmentMounts[currentMountIndex];
                    }

                    // If not yet physically mounted to anything, can right away mount to the proper mount
                    if (asAttachment.curMount == null)
                    {
                        ++data.ignoreParentChanged;
                        asAttachment.AttachToMount(mount, true);
                        --data.ignoreParentChanged;
                    }
                    else if(asAttachment.curMount != mount) // Already mounted, but not on the right one, need to unmount, then mount of right one
                    {
                        ++data.ignoreParentChanged;
                        if (asAttachment.curMount != null)
                        {
                            asAttachment.DetachFromMount();
                        }

                        asAttachment.AttachToMount(mount, true);
                        --data.ignoreParentChanged;
                    }
                }
                // else, if this happens it is because we received a parent update to null and just haven't gotten the up to date mount index of -1 yet
                //       This will be handled on update
            }
            // else, on update we will detach from any current mount if this is the case, no need to handle this here
        }

        private bool UpdateMagazine()
        {
            bool modified = false;
            FVRFireArmMagazine asMag = dataObject as FVRFireArmMagazine;

            int necessarySize = asMag.m_numRounds * 2 + 10;

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
            data.data[necessarySize - 8] = asMag.FireArm != null ? (byte)1 : (byte)0;

            // Write secondary slot index, TODO: Having to look through each secondary slot for equality every update is obviously not optimal
            // We might want to look into patching (Attachable)Firearm's LoadMagIntoSecondary and eject from secondary to keep track of this instead
            if(asMag.FireArm == null)
            {
                data.data[necessarySize - 7] = (byte)255;
            }
            else
            {
                for (int i = 0; i < asMag.FireArm.SecondaryMagazineSlots.Length; ++i)
                {
                    if (asMag.FireArm.SecondaryMagazineSlots[i].Magazine == asMag)
                    {
                        data.data[necessarySize - 7] = (byte)i;
                        break;
                    }
                }
            }

            // Write loaded into AttachableFirearm
            data.data[necessarySize - 6] = asMag.AttachableFireArm != null ? (byte)1 : (byte)0;

            // Write secondary slot index, TODO: Having to look through each secondary slot for equality every update is obviously not optimal
            // We might want to look into patching (Attachable)Firearm's LoadMagIntoSecondary and eject from secondary to keep track of this instead
            if (asMag.AttachableFireArm == null)
            {
                data.data[necessarySize - 5] = (byte)255;
            }
            else
            {
                for (int i = 0; i < asMag.AttachableFireArm.SecondaryMagazineSlots.Length; ++i)
                {
                    if (asMag.AttachableFireArm.SecondaryMagazineSlots[i].Magazine == asMag)
                    {
                        data.data[necessarySize - 5] = (byte)i;
                        break;
                    }
                }
            }

            // Write fuel amount left
            BitConverter.GetBytes(asMag.FuelAmountLeft).CopyTo(data.data, necessarySize - 4);

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
                    asMag.AddRound(newClass, false, false);
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
                                    asMag.Load(parentTrackedItemData.physicalItem.dataObject as FVRFireArm);
                                }
                                else
                                {
                                    asMag.LoadIntoSecondary(parentTrackedItemData.physicalItem.dataObject as FVRFireArm, newData[newData.Length - 7]);
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
                                        //TODO: When H3 adds support for secondary slots on attachable firearm uncomment the following:
                                        //asMag.AttachableFireArm.EjectSecondaryMagFromSlot(i, true);
                                        break;
                                    }
                                }
                            }
                            if (newData[newData.Length - 7] == 255)
                            {
                                asMag.Load(parentTrackedItemData.physicalItem.dataObject as FVRFireArm);
                            }
                            else
                            {
                                asMag.LoadIntoSecondary(parentTrackedItemData.physicalItem.dataObject as FVRFireArm, newData[newData.Length - 7]);
                            }
                            modified = true;
                        }
                        else
                        {
                            // Load into firearm
                            if (newData[newData.Length - 7] == 255)
                            {
                                asMag.Load(parentTrackedItemData.physicalItem.dataObject as FVRFireArm);
                            }
                            else
                            {
                                asMag.LoadIntoSecondary(parentTrackedItemData.physicalItem.dataObject as FVRFireArm, newData[newData.Length - 7]);
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
                                            //TODO: When H3 adds support for secondary slots on attachable firearm uncomment the following:
                                            //asMag.AttachableFireArm.EjectSecondaryMagFromSlot(i, true);
                                            break;
                                        }
                                    }
                                }
                                if (newData[newData.Length - 5] == 255)
                                {
                                    asMag.Load((parentTrackedItemData.physicalItem.dataObject as AttachableFirearmPhysicalObject).FA);
                                }
                                else
                                {
                                    //TODO: When H3 adds support for secondary slots on attachable firearm uncomment the following:
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
                                asMag.Load((parentTrackedItemData.physicalItem.dataObject as AttachableFirearmPhysicalObject).FA);
                            }
                            else
                            {
                                //TODO: When H3 adds support for secondary slots on attachable firearm uncomment the following:
                                //asMag.LoadIntoSecondary((parentTrackedItemData.physicalItem.dataObject as AttachableFirearmPhysicalObject).FA, newData[newData.Length - 1]);
                            }
                            modified = true;
                        }
                        else
                        {
                            // Load into AttachableFireArm
                            if (newData[newData.Length - 5] == 255)
                            {
                                asMag.Load((parentTrackedItemData.physicalItem.dataObject as AttachableFirearmPhysicalObject).FA);
                            }
                            else
                            {
                                //TODO: When H3 adds support for secondary slots on attachable firearm uncomment the following:
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
                                //TODO: When H3 adds support for secondary slots on attachable firearm uncomment the following:
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

            int necessarySize = asClip.m_numRounds * 2 + 3;

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
            BitConverter.GetBytes(asClip.FireArm != null).CopyTo(data.data, necessarySize - 1);

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
                    asClip.AddRound(newClass, false, false);
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
                                asClip.Load(parentTrackedItemData.physicalItem.dataObject as FVRFireArm);
                                modified = true;
                            }
                        }
                        else
                        {
                            // Load into firearm
                            asClip.Load(parentTrackedItemData.physicalItem.dataObject as FVRFireArm);
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

            int necessarySize = asSpeedloader.Chambers.Count * 2 + 2;

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

                if (asSpeedloader.Chambers[i].IsLoaded)
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
                    asSpeedloader.Chambers[i].Load(newClass, false);
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
            if (physicalObject != null && data.controller != H3MP_GameManager.ID && data.position != null && data.rotation != null)
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
            if (skipFullDestroy)
            {
                return;
            }

            //tracked list so that when we get the tracked ID we can send the destruction to server and only then can we remove it from the list
            H3MP_GameManager.trackedItemByItem.Remove(physicalObject);
            if (physicalObject is SosigWeaponPlayerInterface)
            {
                H3MP_GameManager.trackedItemBySosigWeapon.Remove((physicalObject as SosigWeaponPlayerInterface).W);
            }

            if (H3MP_ThreadManager.host)
            {
                if (H3MP_GameManager.giveControlOfDestroyed)
                {
                    // We just want to give control of our items to another client (usually because leaving scene with other clients left inside)
                    if (data.controller == 0)
                    {
                        int otherPlayer = Mod.GetBestPotentialObjectHost(data.controller);

                        if (otherPlayer == -1)
                        {
                            // No one to give control of item to, destroy it
                            if (sendDestroy && skipDestroy == 0)
                            {
                                H3MP_ServerSend.DestroyItem(data.trackedID);
                            }
                            else if (!sendDestroy)
                            {
                                sendDestroy = true;
                            }

                            if (data.removeFromListOnDestroy && H3MP_Server.items[data.trackedID] != null)
                            {
                                H3MP_Server.items[data.trackedID] = null;
                                H3MP_Server.availableItemIndices.Add(data.trackedID);
                                H3MP_GameManager.itemsByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
                            }
                        }
                        else
                        {
                            H3MP_ServerSend.GiveControl(data.trackedID, otherPlayer);

                            // Also change controller locally
                            data.SetController(otherPlayer);
                        }
                    }
                }
                else
                {
                    if (sendDestroy && skipDestroy == 0)
                    {
                        H3MP_ServerSend.DestroyItem(data.trackedID);
                    }
                    else if (!sendDestroy)
                    {
                        sendDestroy = true;
                    }

                    if (data.removeFromListOnDestroy && H3MP_Server.items[data.trackedID] != null)
                    {
                        H3MP_Server.items[data.trackedID] = null;
                        H3MP_Server.availableItemIndices.Add(data.trackedID);
                        H3MP_GameManager.itemsByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
                    }
                }
                if (data.localTrackedID != -1)
                {
                    H3MP_GameManager.items[data.localTrackedID] = H3MP_GameManager.items[H3MP_GameManager.items.Count - 1];
                    H3MP_GameManager.items[data.localTrackedID].localTrackedID = data.localTrackedID;
                    H3MP_GameManager.items.RemoveAt(H3MP_GameManager.items.Count - 1);
                    data.localTrackedID = -1;
                }
            }
            else
            {
                bool removeFromLocal = true;
                if (H3MP_GameManager.giveControlOfDestroyed)
                {
                    if (data.controller == H3MP_Client.singleton.ID)
                    {
                        int otherPlayer = Mod.GetBestPotentialObjectHost(data.controller);

                        if (otherPlayer == -1)
                        {
                            if (sendDestroy && skipDestroy == 0)
                            {
                                if (data.trackedID == -1)
                                {
                                    if (!unknownDestroyTrackedIDs.Contains(data.localTrackedID))
                                    {
                                        unknownDestroyTrackedIDs.Add(data.localTrackedID);
                                    }

                                    // We want to keep it in local until we give destruction order
                                    removeFromLocal = false;
                                }
                                else
                                {
                                    H3MP_ClientSend.DestroyItem(data.trackedID);

                                    H3MP_Client.items[data.trackedID] = null;
                                    H3MP_GameManager.itemsByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
                                }
                            }
                            else if (!sendDestroy)
                            {
                                sendDestroy = true;
                            }

                            if (data.removeFromListOnDestroy && data.trackedID != -1)
                            {
                                H3MP_Client.items[data.trackedID] = null;
                                H3MP_GameManager.itemsByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
                            }
                        }
                        else
                        {
                            if (data.trackedID == -1)
                            {
                                if (unknownControlTrackedIDs.ContainsKey(data.localTrackedID))
                                {
                                    unknownControlTrackedIDs[data.localTrackedID] = otherPlayer;
                                }
                                else
                                {
                                    unknownControlTrackedIDs.Add(data.localTrackedID, otherPlayer);
                                }

                                // We want to keep it in local until we give control
                                removeFromLocal = false;
                            }
                            else
                            {
                                H3MP_ClientSend.GiveControl(data.trackedID, otherPlayer);

                                // Also change controller locally
                                data.SetController(otherPlayer);
                            }
                        }
                    }
                }
                else
                {
                    if (sendDestroy && skipDestroy == 0)
                    {
                        if (data.trackedID == -1)
                        {
                            if (!unknownDestroyTrackedIDs.Contains(data.localTrackedID))
                            {
                                unknownDestroyTrackedIDs.Add(data.localTrackedID);
                            }

                            // We want to keep it in local until we give destruction order
                            removeFromLocal = false;
                        }
                        else
                        {
                            H3MP_ClientSend.DestroyItem(data.trackedID);

                            if (data.removeFromListOnDestroy)
                            {
                                H3MP_Client.items[data.trackedID] = null;
                                H3MP_GameManager.itemsByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
                            }
                        }
                    }
                    else if (!sendDestroy)
                    {
                        sendDestroy = true;
                    }

                    if (data.removeFromListOnDestroy && data.trackedID != -1)
                    {
                        H3MP_Client.items[data.trackedID] = null;
                        H3MP_GameManager.itemsByInstanceByScene[data.scene][data.instance].Remove(data.trackedID);
                    }
                }
                if (removeFromLocal && data.localTrackedID != -1)
                {
                    data.RemoveFromLocal();
                }
            }

            data.removeFromListOnDestroy = true;
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
                    //      If ours is not yet known, put our local tracked ID in a wait dict with value as parent's LOCAL tracked ID if it is under our control
                    //      and the actual tracked ID if not, when we receive the tracked ID we set the parent
                    //          Note that if the parent is under our control, we need to store the local tracked ID because we might not have its tracked ID yet either
                    //          If it is not under our control then we have guarantee that is has a tracked ID
                    //      If the parent's tracked ID is not yet known, put it in a wait dict where key is the local tracked ID of the parent,
                    //      and the value is a list of all children that must be attached to this parent once we know the parent's tracked ID
                    //          Note that if we do not know the parent's tracked ID, it is because it is under our control
                    bool haveParentID = parentTrackedItem.data.trackedID != -1;
                    if (data.trackedID == -1)
                    {
                        KeyValuePair<int, bool> parentIDPair = new KeyValuePair<int, bool>(haveParentID ? parentTrackedItem.data.trackedID : parentTrackedItem.data.localTrackedID, haveParentID);
                        if (unknownTrackedIDs.ContainsKey(data.localTrackedID))
                        {
                            unknownTrackedIDs[data.localTrackedID] = parentIDPair;
                        }
                        else
                        {
                            unknownTrackedIDs.Add(data.localTrackedID, parentIDPair);
                        }
                    }
                    else
                    {
                        if(haveParentID)
                        {
                            if (parentTrackedItem.data.trackedID != data.parent)
                            {
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
                            if (unknownParentTrackedIDs.ContainsKey(parentTrackedItem.data.localTrackedID))
                            {
                                unknownParentTrackedIDs[parentTrackedItem.data.localTrackedID].Add(data.trackedID);
                            }
                            else
                            {
                                unknownParentTrackedIDs.Add(parentTrackedItem.data.localTrackedID, new List<int>() { data.trackedID });
                            }
                        }
                    }
                }
                else if (data.parent != -1)
                {
                    if (data.trackedID == -1)
                    {
                        KeyValuePair<int, bool> parentIDPair = new KeyValuePair<int, bool>(-1, false);
                        if (unknownTrackedIDs.ContainsKey(data.localTrackedID))
                        {
                            unknownTrackedIDs[data.localTrackedID] = parentIDPair;
                        }
                        else
                        {
                            unknownTrackedIDs.Add(data.localTrackedID, parentIDPair);
                        }
                    }
                    else
                    {
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
