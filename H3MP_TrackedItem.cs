using FistVR;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace H3MP
{
    public class H3MP_TrackedItem : MonoBehaviour
    {
        public H3MP_TrackedItemData data;

        // Unknown tracked ID queues
        public static Dictionary<int, KeyValuePair<int, bool>> unknownTrackedIDs = new Dictionary<int, KeyValuePair<int, bool>>();
        public static Dictionary<int, List<int>> unknownParentTrackedIDs = new Dictionary<int, List<int>>();
        public static Dictionary<int, int> unknownControlTrackedIDs = new Dictionary<int, int>();
        public static List<int> unknownDestroyTrackedIDs = new List<int>();

        // Update
        public delegate bool UpdateData(); // The updateFunc and updateGivenFunc should return a bool indicating whether data has been modified
        public delegate bool UpdateDataWithGiven(byte[] newData);
        public delegate bool FireFirearm();
        public delegate void UpdateParent();
        public UpdateData updateFunc; // Update the item's data based on its physical state since we are the controller
        public UpdateDataWithGiven updateGivenFunc; // Update the item's data and state based on data provided by another client
        public FireFirearm fireFunc; // Fires the corresponding firearm type
        public UpdateParent updateParentFunc; // Update the item's state depending on current parent
        public byte currentMountIndex = 255; // Used by attachment, TODO: This limits number of mounts to 255, if necessary could make index into a short
        public FVRPhysicalObject dataObject;
        public FVRPhysicalObject physicalObject;

        public bool sendDestroy = true; // To prevent feeback loops
        public static int skipDestroy;

        private void Awake()
        {
            InitItemType();
        }

        // MOD: This will check which type this item is so we can keep track of its data more efficiently
        //      A mod with a custom item type which has custom data should postfix this to check if this item is of custom type
        //      to keep a ref to the object itself and set delegate update functions
        private void InitItemType()
        {
            FVRPhysicalObject physObj = GetComponent<FVRPhysicalObject>();

            // For each relevant type for which we may want to store additional data, we set a specific update function and the object ref
            if(physObj is FVRFireArmMagazine)
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
            }
            else if (physObj is BoltActionRifle)
            {
                BoltActionRifle asBAR = (BoltActionRifle)physObj;
                updateFunc = UpdateBoltActionRifle;
                updateGivenFunc = UpdateGivenBoltActionRifle;
                dataObject = asBAR;
                fireFunc = asBAR.Fire;
            }
            else if (physObj is Handgun)
            {
                Handgun asHandgun = (Handgun)physObj;
                updateFunc = UpdateHandgun;
                updateGivenFunc = UpdateGivenHandgun;
                dataObject = asHandgun;
                fireFunc = asHandgun.Fire;
            }
            else if (physObj is TubeFedShotgun)
            {
                TubeFedShotgun asTFS = (TubeFedShotgun)physObj;
                updateFunc = UpdateTubeFedShotgun;
                updateGivenFunc = UpdateGivenTubeFedShotgun;
                dataObject = asTFS;
                fireFunc = asTFS.Fire;
            }
            else if (physObj is FVRFireArmAttachment)
            {
                FVRFireArmAttachment asAttachment = (FVRFireArmAttachment)physObj;
                updateFunc = UpdateAttachment;
                updateGivenFunc = UpdateGivenAttachment;
                updateParentFunc = UpdateAttachmentParent;
                dataObject = asAttachment;
            }
            /* TODO: All other type of firearms below
            else if (physObj is Revolver)
            {
                updateFunc = UpdateRevolver;
                updateGivenFunc = UpdateGivenRevolver;
                dataObject = physObj as Revolver;
            }
            else if (physObj is BAP)
            {
                updateFunc = UpdateBAP;
                updateGivenFunc = UpdateGivenBAP;
                dataObject = physObj as BAP;
            }
            else if (physObj is BreakActionWeapon)
            {
                updateFunc = UpdateBreakActionWeapon;
                updateGivenFunc = UpdateGivenBreakActionWeapon;
                dataObject = physObj as BreakActionWeapon;
            }
            else if (physObj is LeverActionFirearm)
            {
                updateFunc = UpdateLeverActionFirearm;
                updateGivenFunc = UpdateGivenLeverActionFirearm;
                dataObject = physObj as LeverActionFirearm;
            }
            else if (physObj is RevolvingShotgun)
            {
                updateFunc = UpdateRevolvingShotgun;
                updateGivenFunc = UpdateGivenRevolvingShotgun;
                dataObject = physObj as RevolvingShotgun;
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
                updateFunc = UpdateFlaregun;
                updateGivenFunc = UpdateGivenFlaregun;
                dataObject = physObj as Flaregun;
            }
            else if (physObj is FlintlockWeapon)
            {
                updateFunc = UpdateFlintlockWeapon;
                updateGivenFunc = UpdateGivenFlintlockWeapon;
                dataObject = physObj as FlintlockWeapon;
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
            else if (physObj is LAPD2019)
            {
                updateFunc = UpdateLAPD2019;
                updateGivenFunc = UpdateGivenLAPD2019;
                dataObject = physObj as LAPD2019;
            }
            else if (physObj is OpenBoltReceiver)
            {
                updateFunc = UpdateOpenBoltReceiver;
                updateGivenFunc = UpdateGivenOpenBoltReceiver;
                dataObject = physObj as OpenBoltReceiver;
            }
            else if (physObj is M72)
            {
                updateFunc = UpdateM72;
                updateGivenFunc = UpdateGivenM72;
                dataObject = physObj as M72;
            }
            else if (physObj is Minigun)
            {
                updateFunc = UpdateMinigun;
                updateGivenFunc = UpdateGivenMinigun;
                dataObject = physObj as Minigun;
            }
            else if (physObj is PotatoGun)
            {
                updateFunc = UpdatePotatoGun;
                updateGivenFunc = UpdateGivenPotatoGun;
                dataObject = physObj as PotatoGun;
            }
            else if (physObj is RemoteMissileLauncher)
            {
                updateFunc = UpdateRemoteMissileLauncher;
                updateGivenFunc = UpdateGivenRemoteMissileLauncher;
                dataObject = physObj as RemoteMissileLauncher;
            }
            else if (physObj is RGM40)
            {
                updateFunc = UpdateRGM40;
                updateGivenFunc = UpdateGivenRGM40;
                dataObject = physObj as RGM40;
            }
            else if (physObj is RollingBlock)
            {
                updateFunc = UpdateRollingBlock;
                updateGivenFunc = UpdateGivenRollingBlock;
                dataObject = physObj as RollingBlock;
            }
            else if (physObj is RPG7)
            {
                updateFunc = UpdateRPG7;
                updateGivenFunc = UpdateGivenRPG7;
                dataObject = physObj as RPG7;
            }
            else if (physObj is SimpleLauncher)
            {
                updateFunc = UpdateSimpleLauncher;
                updateGivenFunc = UpdateGivenSimpleLauncher;
                dataObject = physObj as SimpleLauncher;
            }
            else if (physObj is SimpleLauncher2)
            {
                updateFunc = UpdateSimpleLauncher2;
                updateGivenFunc = UpdateGivenSimpleLauncher2;
                dataObject = physObj as SimpleLauncher2;
            }
            else if (physObj is SingleActionRevolver)
            {
                updateFunc = UpdateSingleActionRevolver;
                updateGivenFunc = UpdateGivenSingleActionRevolver;
                dataObject = physObj as SingleActionRevolver;
            }
            else if (physObj is StingerLauncher)
            {
                updateFunc = UpdateStingerLauncher;
                updateGivenFunc = UpdateGivenStingerLauncher;
                dataObject = physObj as StingerLauncher;
            }
            else if (physObj is MF2_RL)
            {
                updateFunc = UpdateMF2_RL;
                updateGivenFunc = UpdateGivenMF2_RL;
                dataObject = physObj as MF2_RL;
            }
            */
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
            data.data[2] = BitConverter.GetBytes(asCBW.IsHammerCocked)[0];

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

                    // Detach from mount will recover rigidbody, store and destroy again if not controller
                    if (data.controller != H3MP_GameManager.ID)
                    {
                        asAttachment.StoreAndDestroyRigidbody();
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
                    if (parentTrackedItemData.physicalItem.dataObject.AttachmentMounts.Count > mountIndex)
                    {
                        mount = parentTrackedItemData.physicalItem.dataObject.AttachmentMounts[mountIndex];
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
                        mount = parentTrackedItemData.physicalItem.dataObject.AttachmentMounts[currentMountIndex];
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

            int necessarySize = asMag.m_numRounds * 2 + 3;

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
            BitConverter.GetBytes(asMag.FireArm != null).CopyTo(data.data, necessarySize - 1);

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
                        if (asMag.FireArm != null)
                        {
                            if (asMag.FireArm != parentTrackedItemData.physicalItem.dataObject)
                            {
                                // Unload from current, load into new firearm
                                asMag.FireArm.EjectMag();
                                asMag.Load(parentTrackedItemData.physicalItem.dataObject as FVRFireArm);
                                modified = true;
                            }
                        }
                        else
                        {
                            // Load into firearm
                            asMag.Load(parentTrackedItemData.physicalItem.dataObject as FVRFireArm);
                            modified = true;
                        }
                    }
                }
            }
            else if(asMag.FireArm != null)
            {
                // Don't want to be loaded, but we are loaded, unload
                asMag.FireArm.EjectMag();
                modified = true;
            }

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

        private void OnDestroy()
        {
            //tracked list so that when we get the tracked ID we can send the destruction to server and only then can we remove it from the list
            H3MP_GameManager.trackedItemByItem.Remove(physicalObject);

            if (H3MP_ThreadManager.host)
            {
                if (H3MP_GameManager.giveControlOfDestroyed)
                {
                    // We just want to give control of our items to another client (usually because leaving scene with other clients left inside)
                    if (data.controller == 0)
                    {
                        int otherPlayer = Mod.GetBestPotentialObjectHost(data.controller);

                        if (otherPlayer != -1)
                        {
                            H3MP_ServerSend.GiveControl(data.trackedID, otherPlayer);

                            // Also change controller locally
                            data.controller = otherPlayer;
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

                    H3MP_Server.items[data.trackedID] = null;
                    H3MP_Server.availableItemIndices.Add(data.trackedID);
                }
                if(data.localTrackedID != -1)
                {
                    Debug.Log("Removing game manager item at : "+data.localTrackedID);
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

                        if (otherPlayer != -1)
                        {
                            if(data.trackedID == -1)
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
                                data.controller = otherPlayer;
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

                            H3MP_Client.items[data.trackedID] = null;
                        }
                    }
                    else if (!sendDestroy)
                    {
                        sendDestroy = true;
                    }

                    if (data.trackedID != -1)
                    {
                        H3MP_Client.items[data.trackedID] = null;
                    }
                }
                if (removeFromLocal && data.localTrackedID != -1)
                {
                    data.RemoveFromLocal();
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
