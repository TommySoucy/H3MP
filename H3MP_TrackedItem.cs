using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static UnityEngine.ParticleSystem;

namespace H3MP
{
    public class H3MP_TrackedItem : MonoBehaviour
    {
        public H3MP_TrackedItemData data;

        // Update
        public delegate bool UpdateData(); // The updateFunc and updateGivenFunc should return a bool indicating whether data has been modified
        public delegate bool UpdateDataWithGiven(byte[] newData);
        public delegate bool FireFirearm();
        public UpdateData updateFunc; // Update the item's data based on its physical state since we are the controller
        public UpdateDataWithGiven updateGivenFunc; // Update the item's data and state based on data provided by another client
        public FireFirearm fireFunc; // Fires the corresponding firearm type
        public UnityEngine.Object dataObject;

        public bool sendDestroy = true; // To prevent feeback loops

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
            data.data[1] = (byte)typeof(ClosedBoltWeapon).GetField("m_CamBurst").GetValue(asCBW);

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
                typeof(ClosedBoltWeapon).GetField("m_fireSelectorMode").SetValue(asCBW, (int)newData[0]);

                // Set camBurst
                typeof(ClosedBoltWeapon).GetField("m_CamBurst").SetValue(asCBW, (int)newData[1]);
            }
            else 
            {
                if (data.data[0] != newData[0])
                {
                    // Set fire select mode
                    typeof(ClosedBoltWeapon).GetField("m_fireSelectorMode").SetValue(asCBW, (int)newData[0]);
                    modified = true;
                }
                if (data.data[1] != newData[1])
                {
                    // Set camBurst
                    typeof(ClosedBoltWeapon).GetField("m_CamBurst").SetValue(asCBW, (int)newData[1]);
                    modified = true;
                }
            }

            // Set hammer state
            if (newData[2] == 0)
            {
                if (asCBW.IsHammerCocked)
                {
                    typeof(ClosedBoltWeapon).GetField("m_isHammerCocked").SetValue(asCBW, BitConverter.ToBoolean(newData, 2));
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
            data.data[1] = (byte)typeof(ClosedBoltWeapon).GetField("m_CamBurst").GetValue(asHandgun);

            modified |= preval != data.data[1];

            preval = data.data[2];

            // Write hammer state
            data.data[2] = BitConverter.GetBytes((bool)typeof(Handgun).GetField("m_isHammerCocked").GetValue(asHandgun))[0];

            modified |= preval != data.data[2];

            preval = data.data[3];
            byte preval0 = data.data[4];

            // Write chambered round class
            if(asHandgun.Chamber.GetRound() == null)
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
                typeof(Handgun).GetField("m_fireSelectorMode").SetValue(asHandgun, (int)newData[0]);

                // Set camBurst
                typeof(Handgun).GetField("m_CamBurst").SetValue(asHandgun, (int)newData[1]);
            }
            else 
            {
                if (data.data[0] != newData[0])
                {
                    // Set fire select mode
                    typeof(Handgun).GetField("m_fireSelectorMode").SetValue(asHandgun, (int)newData[0]);
                    modified = true;
                }
                if (data.data[1] != newData[1])
                {
                    // Set camBurst
                    typeof(Handgun).GetField("m_CamBurst").SetValue(asHandgun, (int)newData[1]);
                    modified = true;
                }
            }

            FieldInfo hammerCockedField = typeof(Handgun).GetField("m_isHammerCocked");
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
            data.data[0] = (byte)typeof(TubeFedShotgun).GetField("m_fireSelectorMode").GetValue(asTFS);

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
                typeof(TubeFedShotgun).GetField("m_fireSelectorMode").SetValue(asTFS, (int)newData[0]);

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
                    typeof(TubeFedShotgun).GetField("m_fireSelectorMode").SetValue(asTFS, (int)newData[0]);
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
                    typeof(TubeFedShotgun).GetField("m_isHammerCocked").SetValue(asTFS, BitConverter.ToBoolean(newData, 1));
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
            data.data[0] = (byte)typeof(BoltActionRifle).GetField("m_fireSelectorMode").GetValue(asBAR);

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
                typeof(BoltActionRifle).GetField("m_fireSelectorMode").SetValue(asBAR, (int)newData[0]);

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
                    typeof(BoltActionRifle).GetField("m_fireSelectorMode").SetValue(asBAR, (int)newData[0]);
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
                    typeof(BoltActionRifle).GetField("m_isHammerCocked").SetValue(asBAR, BitConverter.ToBoolean(newData, 1));
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

                    if (parentTrackedItemData != null && parentTrackedItemData.physicalObject != null && parentTrackedItemData.physicalObject.dataObject is FVRFireArm)
                    {
                        // We want to be loaded in a firearm, we have a parent, it is a firearm
                        if (asMag.FireArm != null)
                        {
                            if (asMag.FireArm != parentTrackedItemData.physicalObject.dataObject)
                            {
                                // Unload from current, load into new firearm
                                asMag.FireArm.EjectMag();
                                asMag.Load(parentTrackedItemData.physicalObject.dataObject as FVRFireArm);
                                modified = true;
                            }
                        }
                        else
                        {
                            // Load into firearm
                            asMag.Load(parentTrackedItemData.physicalObject.dataObject as FVRFireArm);
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

                    if (parentTrackedItemData != null && parentTrackedItemData.physicalObject != null && parentTrackedItemData.physicalObject.dataObject is FVRFireArm)
                    {
                        // We want to be loaded in a firearm, we have a parent, it is a firearm
                        if (asClip.FireArm != null)
                        {
                            if (asClip.FireArm != parentTrackedItemData.physicalObject.dataObject)
                            {
                                // Unload from current, load into new firearm
                                asClip.FireArm.EjectClip();
                                asClip.Load(parentTrackedItemData.physicalObject.dataObject as FVRFireArm);
                                modified = true;
                            }
                        }
                        else
                        {
                            // Load into firearm
                            asClip.Load(parentTrackedItemData.physicalObject.dataObject as FVRFireArm);
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

        private void OnDestroy()
        {
            if (H3MP_ThreadManager.host)
            {
                if (H3MP_GameManager.giveControlOfDestroyed)
                {
                    // We just want to give control of our items to another client (usually because leaving scene with other clients left inside)
                    if (data.controller == 0)
                    {
                        int firstPlayerInScene = 0;
                        foreach (KeyValuePair<int, H3MP_PlayerManager> player in H3MP_GameManager.players)
                        {
                            firstPlayerInScene = player.Key;
                            break;
                        }

                        H3MP_ServerSend.GiveControl(data.trackedID, firstPlayerInScene);
                    }
                }
                else
                {
                    if (sendDestroy)
                    {
                        H3MP_ServerSend.DestroyItem(data.trackedID);
                    }

                    H3MP_Server.items[data.trackedID] = null;
                    H3MP_Server.availableItemIndices.Add(data.trackedID);
                }
                if(data.controller == 0)
                {
                    H3MP_GameManager.items[data.localtrackedID] = H3MP_GameManager.items[H3MP_GameManager.items.Count - 1];
                    H3MP_GameManager.items[data.localtrackedID].localtrackedID = data.localtrackedID;
                    H3MP_GameManager.items.RemoveAt(H3MP_GameManager.items.Count - 1);
                }
            }
            else
            {
                if (H3MP_GameManager.giveControlOfDestroyed)
                {
                    if (data.controller == H3MP_Client.singleton.ID)
                    {
                        int firstPlayerInScene = 0;
                        foreach (KeyValuePair<int, H3MP_PlayerManager> player in H3MP_GameManager.players)
                        {
                            firstPlayerInScene = player.Key;
                            break;
                        }

                        H3MP_ClientSend.GiveControl(data.trackedID, firstPlayerInScene);
                    }
                }
                else
                {
                    if (sendDestroy)
                    {
                        H3MP_ClientSend.DestroyItem(data.trackedID);
                    }

                    H3MP_Client.items[data.trackedID] = null;
                }
                if (data.controller == H3MP_Client.singleton.ID)
                {
                    H3MP_GameManager.items[data.localtrackedID] = H3MP_GameManager.items[H3MP_GameManager.items.Count - 1];
                    H3MP_GameManager.items[data.localtrackedID].localtrackedID = data.localtrackedID;
                    H3MP_GameManager.items.RemoveAt(H3MP_GameManager.items.Count - 1);
                }
            }
        }

        private void OnTransformParentChanged()
        {
            Debug.Log("Tracked item: "+gameObject.name+" parent changed");
            if (data.ignoreParentChanged)
            {
                data.ignoreParentChanged = false;
                return;
            }

            if(data.controller == (H3MP_ThreadManager.host ? 0 : H3MP_Client.singleton.ID))
            {
                Debug.Log("\tWe are in control");
                Transform currentParent = transform.parent;
                H3MP_TrackedItem parentTrackedItem = null;
                while (currentParent != null)
                {
                    parentTrackedItem = currentParent.GetComponent<H3MP_TrackedItem>();
                    if(parentTrackedItem != null)
                    {
                        break;
                    }
                    currentParent = currentParent.parent;
                }
                if(parentTrackedItem != null)
                {
                    Debug.Log("\t\tItem has tracked item parent");
                    if (parentTrackedItem.data.trackedID != data.parent)
                    {
                        Debug.Log("\t\tIt is a new one, sending to other clients");
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

                        Debug.Log("\t\tUpdating locally");
                        // Update local
                        data.SetParent(parentTrackedItem.data, false);
                    }
                }
                else if(data.parent != -1)
                {
                    Debug.Log("\t\tItem does not have tracked item parent");
                    // We were detached from current parent
                    // Update other clients
                    if (H3MP_ThreadManager.host)
                    {
                        H3MP_ServerSend.ItemParent(data.trackedID, parentTrackedItem.data.trackedID);
                    }
                    else
                    {
                        H3MP_ClientSend.ItemParent(data.trackedID, parentTrackedItem.data.trackedID);
                    }

                    // Update locally
                    data.SetParent(null, false);
                }
            }
        }
    }
}
